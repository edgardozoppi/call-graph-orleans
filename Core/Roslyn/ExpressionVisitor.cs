﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OrleansClient.Analysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;

namespace OrleansClient.Roslyn
{
	/// <summary>
	/// This interface represent the way we model an expression from the AST in term of the our analysis structure
	/// The analysis use to kinds of information:
	///    - Types: Information about concrete types 
	///    - Nodes: an element in the Propagation Graph (where concrete types flow). It represent a local variable, parameter or field
	///             There are special nodes that represent method invocations (more about  this later)
	/// </summary>
	internal abstract class AnalysisExpression
	{
		protected TypeDescriptor typeDescriptor;
		protected PropGraphNodeDescriptor aNode;

		internal SyntaxNodeOrToken Expression { get; private set; }
		internal ITypeSymbol Type { get; set; }

		public AnalysisExpression(SyntaxNodeOrToken expression, ITypeSymbol type, SyntaxNode declarationNode)
		{
			this.Type = type;
			this.Expression = expression;
			this.DeclarationNode = declarationNode;
		}

		public virtual PropGraphNodeDescriptor GetAnalysisNode()
		{
			if (aNode == null)
			{
				this.aNode = CreateAnalysisNode();
			}

			return aNode;
		}

		protected virtual PropGraphNodeDescriptor CreateAnalysisNode()
		{
			var type = this.GetType();

			Console.WriteLine(type.Name);

			throw new NotImplementedException();
		}

		public virtual TypeDescriptor GetAnalysisType()
		{
			if (typeDescriptor == null)
			{
				//aType = new ConcreteType(Type);
				typeDescriptor = Utils.CreateTypeDescriptor(this.Type);
			}

			return typeDescriptor;
		}

		public virtual void ProcessAssignment(VariableNode lhsAnalysisNode, MethodSyntaxVisitor methodVisitor)
		{
			methodVisitor.RegisterAssignment(lhsAnalysisNode, this);
		}

		public virtual AnalysisExpression ProcessArgument(ArgumentSyntax argNode, ExpressionVisitor visitor)
		{
			return this;
		}

		public SyntaxNode DeclarationNode { get; set; }
	}

	/// <summary>
	/// Represents a Constant. 
	/// </summary>
	internal class Constant : AnalysisExpression
	{
		internal Constant(SyntaxNodeOrToken expression, ITypeSymbol type, SyntaxNode declarationNode)
			: base(expression, type, declarationNode)
		{ }

		public override PropGraphNodeDescriptor GetAnalysisNode()
		{
			return null;
		}
	}

	/// <summary>
	/// Represents an allocation (i.e:, new statament)
	/// </summary>
	internal class Allocation : AnalysisExpression
	{
		/// <summary>
		/// A node in the PropGraph that represent the result returned by the allocation
		/// </summary>
		public VariableNode ReturnedVariableNode { get; private set; }

		internal Allocation(SyntaxNodeOrToken expression, ITypeSymbol type, VariableNode returnedVariableNode, SyntaxNode declarationNode)
			: base(expression, type, declarationNode)
		{
			this.ReturnedVariableNode = returnedVariableNode;
		}

		public override void ProcessAssignment(VariableNode lhsAnalysisNode, MethodSyntaxVisitor methodVisitor)
		{
			var allocType = this.GetAnalysisType();
			methodVisitor.RegisterNewExpressionAssignment(lhsAnalysisNode, allocType);
			//base.ProcessAssignment(lhsAnalysisNode, methodVisitor);
		}

		public override PropGraphNodeDescriptor GetAnalysisNode()
		{
			return this.ReturnedVariableNode;
		}
	}

	/// <summary>
	/// Represents an identifier like a variable, parameter
	/// </summary>
	internal class Identifier : AnalysisExpression
	{
		internal ISymbol Symbol { get; set; }

		internal Identifier(SyntaxNodeOrToken expression, ITypeSymbol type, ISymbol symbol, SyntaxNode declarationNode)
			: base(expression, type, declarationNode)
		{
			this.Symbol = symbol;
		}

		protected override PropGraphNodeDescriptor CreateAnalysisNode()
		{
			return new VariableNode(this.Symbol.Name, Utils.CreateTypeDescriptor(this.Type));
		}
	}

	internal class Parameter : Identifier
	{
		private int position;

		internal Parameter(int position, SyntaxNodeOrToken expression, ITypeSymbol type, ISymbol symbol, SyntaxNode declarationNode)
			: base(expression, type, symbol, declarationNode)
		{
			this.position = position;
		}

		protected override PropGraphNodeDescriptor CreateAnalysisNode()
		{
			return new ParameterNode(this.Expression.ToString(), position, Utils.CreateTypeDescriptor(this.Type));
		}
	}

	/// <summary>
	/// Represents a Field  or a Property
	/// </summary>
	internal class Field : Identifier
	{
		private string className;
		private string field;

		internal Field(SyntaxNodeOrToken expression, ITypeSymbol type, ISymbol symbol, SyntaxNode declarationNode)
			: base(expression, type, symbol, declarationNode)
		{
			this.className = symbol.ContainingType.Name;
			this.field = symbol.Name;
		}

		protected override PropGraphNodeDescriptor CreateAnalysisNode()
		{
			return new FieldNode(this.className, this.field, Utils.CreateTypeDescriptor(this.Type));
		}
	}

	internal class Method : Identifier
	{
		public bool IsDelegate { get; protected set; }
		public MethodDescriptor MethodDescriptor { get; protected set; }

		internal Method(SyntaxNodeOrToken expression, ITypeSymbol type, IMethodSymbol symbol, SyntaxNode declarationNode)
			: base(expression, symbol.ReturnType, symbol, declarationNode)
		{
			this.IsDelegate = false;
			this.MethodDescriptor = Utils.CreateMethodDescriptor(symbol);
		}

		protected Method(SyntaxNodeOrToken expression, ITypeSymbol type, IMethodSymbol symbol, MethodDescriptor methodDescriptor, SyntaxNode declarationNode)
			: base(expression, symbol.ReturnType, symbol, declarationNode)
		{
			this.IsDelegate = false;
			this.MethodDescriptor = methodDescriptor;
		}

		public IMethodSymbol RoslynMethod
		{
			get { return this.Symbol as IMethodSymbol; }
		}
	}

	internal class Lambda : Method
	{
		public Lambda(SyntaxNode expression, ITypeSymbol type, IMethodSymbol symbol, AnonymousMethodDescriptor methodDescriptor, SyntaxNode declarationNode)
			: base(expression, type, symbol, methodDescriptor, declarationNode)
		{
			this.IsDelegate = true;
		}

		public override void ProcessAssignment(VariableNode lhsAnalysisNode, MethodSyntaxVisitor methodVisitor)
		{
			//methodVisitor.RegisterDelegate(lhsAnalysisNode, methodSymbol);
			methodVisitor.RegisterDelegate(lhsAnalysisNode, this.MethodDescriptor);
			methodVisitor.RegisterAssignment(lhsAnalysisNode, this);
		}

		public AnonymousMethodDescriptor AnonymousMethodDescriptor
		{
			get { return this.MethodDescriptor as AnonymousMethodDescriptor; }
		}
	}

	internal class Property : Identifier
	{
		public IMethodSymbol RoslynMethod { get; private set; }
		public bool IsDelegate { get; private set; }

		internal Property(SyntaxNodeOrToken expression, ITypeSymbol typeSymbol, ISymbol symbol, bool isSetter, SyntaxNode declarationNode)
			: base(expression, typeSymbol, symbol, declarationNode)
		{
			this.IsDelegate = false;
			var propertySymbol = symbol as IPropertySymbol;
			this.RoslynMethod = isSetter ? propertySymbol.SetMethod : propertySymbol.GetMethod;

			if (this.RoslynMethod == null && propertySymbol.IsOverride)
			{
				propertySymbol = propertySymbol.OverriddenProperty;
				this.RoslynMethod = isSetter ? propertySymbol.SetMethod : propertySymbol.GetMethod;
			}
		}

		public override AnalysisExpression ProcessArgument(ArgumentSyntax argNode, ExpressionVisitor visitor)
		{
			return visitor.AnalyzePropertyGetter(argNode.Expression, visitor.ThisRef, null, this);
		}
	}

	/// <summary>
	/// Represent a Field, Property, or method access
	/// </summary>
	internal class MemberAccess : AnalysisExpression
	{
		internal Identifier NameExpresion { get; private set; }
		internal AnalysisExpression ReferenceExpresion { get; private set; }
		internal ISymbol Field { get; private set; }
		internal SyntaxNodeOrToken FieldSyntax { get; private set; }

		internal MemberAccess(MemberAccessExpressionSyntax ex, AnalysisExpression r, ExpressionSyntax rSyntax,
			Identifier field, SyntaxNodeOrToken fSyntax, ITypeSymbol type, SyntaxNode declarationNode)
			: base(ex.Name, type, declarationNode)
		{
			this.Field = field.Symbol;
			this.FieldSyntax = fSyntax;
			this.NameExpresion = field;
			this.ReferenceExpresion = r;
		}

		protected override PropGraphNodeDescriptor CreateAnalysisNode()
		{
			if (this.Field.Kind == SymbolKind.Property)
			{
				var property = this.NameExpresion as Property;
				var order = Utils.GetStatementNumber(this.Expression);
				var analysisCallNode = new AnalysisCallNode(this.Field.ContainingType.Name + "." + this.Field.Name,
					Utils.CreateTypeDescriptor(this.Type),
					Utils.CreateLocationDescriptor(order, this.Expression, this.DeclarationNode),
					Utils.CreateAnalysisCallNodeAdditionalInfo(property.RoslynMethod));
				return new PropertyVariableNode(this.Field.ContainingType.Name + "." + this.Field.Name,
					Utils.CreateTypeDescriptor(this.Type), analysisCallNode);
			}
			else
			{
				return new FieldNode(
					this.Field.ContainingType.Name,
					this.Field.Name,
					Utils.CreateTypeDescriptor(this.Type));
			}
		}

		public override void ProcessAssignment(VariableNode lhsAnalysisNode, MethodSyntaxVisitor methodVisitor)
		{
			// Check for delegate invocation (ie. x = s.Delegate())
			if (this.NameExpresion is Method)
			{
				var methodDescriptor = (this.NameExpresion as Method).MethodDescriptor;
				methodVisitor.RegisterAssignment(lhsAnalysisNode, this.ReferenceExpresion);
				methodVisitor.RegisterDelegate(lhsAnalysisNode, methodDescriptor);
			}
			else
			{
				methodVisitor.RegisterAssignment(lhsAnalysisNode, this);
			}
		}
		public override AnalysisExpression ProcessArgument(ArgumentSyntax argNode, ExpressionVisitor visitor)
		{
			if (this.NameExpresion is Property)
			{
				return visitor.AnalyzePropertyGetter(argNode.Expression, null, this.ReferenceExpresion, this.NameExpresion as Property);
			}
			// To-do Support delegates like s.Delegate here!
			if (this.NameExpresion is Method)
			{ }
			return base.ProcessArgument(argNode, visitor);
		}
	}

	/// <summary>
	/// DIEGO: I would like to remove this
	/// Represent a non supported expression
	/// </summary>
	internal class UnsupportedExpression : AnalysisExpression
	{
		//SyntaxNodeOrToken expression;
		internal UnsupportedExpression(SyntaxNodeOrToken ex, ITypeSymbol type, ISymbol s)
			: base(ex, type, null)
		{
			var descriptor = Utils.CreateTypeDescriptor(type);
			this.aNode = new UnsupportedNode(descriptor);
			this.typeDescriptor = descriptor;
		}
	}

	/// <summary>
	/// Represent a method invocation
	/// </summary>
	internal class Call : AnalysisExpression
	{
		/// <summary>
		///  It doesn't contain information but is used to connect the arguments to the call
		///  Essentially is a type reach this node, we need to propagate info to the callee
		/// </summary>
		public AnalysisCallNode CallNode { get; private set; }

		/// <summary>
		/// A node in the PropGraph that represent the result returned in the invocation
		/// </summary>
		public VariableNode ReturnedVariableNode { get; private set; }

		public IMethodSymbol RoslynMethod { get; private set; }

		//public AnalysisMethod Method { get; private set; }
		public MethodDescriptor Method { get; private set; }

		internal Call(ExpressionSyntax node, ITypeSymbol type, IMethodSymbol method,
					AnalysisCallNode callNode, VariableNode returnedVariableNode, SyntaxNode declarationNode)
			: base(node, type, declarationNode)
		{
			this.RoslynMethod = method;
			this.Method = Utils.CreateMethodDescriptor(method);
			this.CallNode = callNode;
			this.ReturnedVariableNode = returnedVariableNode;
		}

		public override PropGraphNodeDescriptor GetAnalysisNode()
		{
			return this.CallNode;
		}
		public override void ProcessAssignment(VariableNode lhsAnalysisNode, MethodSyntaxVisitor methodVisitor)
		{
			//Contract.Requires(lhsAnalysisNode is VariableNode);
			methodVisitor.RegisterCallLHS(lhsAnalysisNode, this);
		}
		public override AnalysisExpression ProcessArgument(ArgumentSyntax argNode, ExpressionVisitor visitor)
		{
			return visitor.Visit(argNode.Expression);
			// return visitor.AnalyzeInvocation(argNode.Expression as InvocationExpressionSyntax); 
		}
	}
	internal class DelegateCall : Call
	{
		internal DelegateCall(ExpressionSyntax node, ITypeSymbol type,
							 IMethodSymbol method, AnalysisCallNode analysisCallNode, VariableNode returnedVariableNode, SyntaxNode declarationNode) :
			base(node, type, method, analysisCallNode, returnedVariableNode, declarationNode)
		{
			//this.cn = cn;
		}
		public override PropGraphNodeDescriptor GetAnalysisNode()
		{
			return this.CallNode;
		}
	}

	internal class ExpressionVisitor : CSharpSyntaxVisitor<AnalysisExpression>
	{
		private SemanticModel model;
		private StatementProcessor statementProcessor;
		private MethodSyntaxVisitor roslynMethodVisitor;
		// This mapping is used for the temporary variables used in nested calls 
		private IDictionary<ExpressionSyntax, PropGraphNodeDescriptor> tempLH = new Dictionary<ExpressionSyntax, PropGraphNodeDescriptor>();
		private bool leftHandSide;

		internal PropGraphNodeDescriptor ThisRef
		{
			get { return roslynMethodVisitor.ThisRef; }
		}

		internal ExpressionVisitor(SemanticModel model,
			StatementProcessor stProcessor,
			MethodSyntaxVisitor roslynMethodVisitor)
		{
			Contract.Requires(model != null);

			this.model = model;
			this.statementProcessor = stProcessor;
			this.roslynMethodVisitor = roslynMethodVisitor;
		}

		internal LocationDescriptor CreateLocationDescriptor(int invocationPosition, SyntaxNodeOrToken syntaxNode)
		{
			return Utils.CreateLocationDescriptor(invocationPosition, syntaxNode, this.roslynMethodVisitor.DeclarationNode);
		}

		public override AnalysisExpression DefaultVisit(SyntaxNode node)
		{
			//return null;
			return base.DefaultVisit(node);
		}
		public override AnalysisExpression VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
		{
			return Visit(node.Expression);
			//return base.VisitParenthesizedExpression(node);
		}

		public override AnalysisExpression VisitLiteralExpression(LiteralExpressionSyntax node)
		{
			var symbol = this.model.GetSymbolInfo(node).Symbol;
			var type = this.model.GetTypeInfo(node).Type;
			if (type != null && Utils.IsTypeForAnalysis(type))
			{
				return new Constant(node, type, this.roslynMethodVisitor.DeclarationNode);
			}
			else
			{
				return null;
			}
		}

		public override AnalysisExpression VisitInitializerExpression(InitializerExpressionSyntax node)
		{
			foreach (var expression in node.Expressions)
			{
				Visit(expression);
			}

			return null;
		}

		public override AnalysisExpression VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
		{
			return Visit(node.Operand);
		}

		public override AnalysisExpression VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
		{
			return base.Visit(node.Operand);
		}

		public override AnalysisExpression VisitFromClause(FromClauseSyntax node)
		{
			return Visit(node.Expression);
		}

		public override AnalysisExpression VisitWhereClause(WhereClauseSyntax node)
		{
			return Visit(node.Condition);
		}

		public override AnalysisExpression VisitSelectClause(SelectClauseSyntax node)
		{
			return Visit(node.Expression);
		}

		public override AnalysisExpression VisitConditionalExpression(ConditionalExpressionSyntax node)
		{
			return Visit(node.Condition);
		}

		public override AnalysisExpression VisitAssignmentExpression(AssignmentExpressionSyntax node)
		{
			this.leftHandSide = true;
			var lhs = Visit(node.Left);
			this.leftHandSide = false;
			var rhs = Visit(node.Right);

			if (lhs != null)
			{
				this.statementProcessor.RegisterLocalVariable(lhs.GetAnalysisNode());
				//this.roslynMethodVisitor.RegisterVariable(lhs.GetSynTaxExpression(),
				//                                          lhs.GetAnalysisType().RoslynType,
				//                                          lhs.GetRoslynSymbol());

				if (lhs is MemberAccess)
				{
					var memberAccess = lhs as MemberAccess;
					if (memberAccess.NameExpresion is Property)
					{
						var property = memberAccess.NameExpresion as Property;
						if (property.RoslynMethod != null &&
							property.RoslynMethod.MethodKind == MethodKind.PropertySet)
						{
							// TODO : Process Property Setter
							AnalyzePropertySetter(node, null, memberAccess.ReferenceExpresion, property, rhs);
						}
					}
				}

				if (rhs != null)
				{
					var variableNode = lhs.GetAnalysisNode() as VariableNode;

					if (lhs is Call)
					{
						var call = lhs as Call;
						variableNode = call.ReturnedVariableNode;
                    }

					if (variableNode == null)
					{
					}

					//Contract.Assert(variableNode != null);

					rhs.ProcessAssignment(variableNode, this.roslynMethodVisitor);

					//if (rhs is Allocation)
					//{
					//    // Get the type of the allocation 
					//    var allocType = rhs.GetAnalysisType();
					//    statementProcessor.RegisterNewExpressionAssignment(lhs.GetAnalysisNode(), allocType);
					//}
					//else
					//{
					//    rhs.ProcessAssignment(lhs.GetAnalysisNode(), this.roslynMethodVisitor);
					//    statementProcessor.RegisterAssignment(lhs.GetAnalysisNode(), rhs.GetAnalysisNode());
					//}
				}
			}

			if (rhs == null)
			{
				rhs = CreateUnsupportedExpression(node.Right);
			}

			return rhs;
		}

		public override AnalysisExpression VisitIdentifierName(IdentifierNameSyntax node)
		{
			var symbol = this.model.GetSymbolInfo(node).Symbol;
			var type = GetTypeSymbol(node);

			//if (symbol == null && type.TypeKind == TypeKind.Dynamic)
			if (symbol == null)
			{
				return null;
			}

			switch (symbol.Kind)
			{
				case SymbolKind.Field:
					{
						return new Field(node, type, symbol, this.roslynMethodVisitor.DeclarationNode);
					}

				case SymbolKind.Method:
					{
						// Do you need to add a thisRef? 
						// In the current version no because a node is C.f
						return new Method(node, type, (IMethodSymbol)symbol, this.roslynMethodVisitor.DeclarationNode);
					}

				case SymbolKind.Property:
					{
						// For the special case of propery setter, e.g. this.a.b.c = value, a and b are getters but c is setter

						//var isSetter = this.leftHandSide &&
						//	((node.Parent is AssignmentExpressionSyntax) ||
						//	 (node.Parent != null && node.Parent.Parent is AssignmentExpressionSyntax));

						var isSetter = this.leftHandSide &&
							((node.Parent is AssignmentExpressionSyntax) ||
							 (node.Parent is MemberAccessExpressionSyntax &&
							 (node.Parent as MemberAccessExpressionSyntax).Name == node &&
							  node.Parent.Parent is AssignmentExpressionSyntax));

						return new Property(node, type, symbol, isSetter, this.roslynMethodVisitor.DeclarationNode);
					}

				case SymbolKind.Local:
					{
						return new Identifier(node, type, symbol, this.roslynMethodVisitor.DeclarationNode);
					}

				case SymbolKind.Parameter:
					{
						// I need to return the already created parameter node
						var i = 0;

						foreach (var parameterNode in this.statementProcessor.ParameterNodes)
						{
							Contract.Assert(parameterNode != null);
							if (parameterNode.Name.Equals(symbol.Name))
							{
								return new Parameter(i, node, type, symbol, this.roslynMethodVisitor.DeclarationNode);
							}
							i++;
						}

						if (this.statementProcessor.Method.IsAnonymousDescriptor)
						{
							goto case SymbolKind.Local;
						}

						Contract.Assert(false, "Can't find parameter by name " + symbol.Name);
						return null;
					}

				case SymbolKind.NamedType:
					{
						// Todo create a NamedType type?
						return new Identifier(node, type, symbol, this.roslynMethodVisitor.DeclarationNode);
					}

				//case SymbolKind.Namespace:
				//	break;

				default:
					{
						return new Identifier(node, type, symbol, this.roslynMethodVisitor.DeclarationNode);
					}
			}

			throw new ArgumentException();
		}

		#region allocation
		/// <summary>
		/// Process a new statement. This consist in two opetrations:
		/// 1) allocation 
		/// 2) constructor invocation. 
		/// We register the invocationa and return the concrete type from the allocation
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public override AnalysisExpression VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
		{
			this.roslynMethodVisitor.InvocationPosition++;

			var allocAnalysisExpression = ProcessConstructor(node, node.ArgumentList);
			return allocAnalysisExpression;
		}

		private Allocation ProcessConstructor(ExpressionSyntax node, ArgumentListSyntax argumentListSyntax)
		{
			var type = this.model.GetTypeInfo(node);
			var symbol = this.model.GetSymbolInfo(node).Symbol;
			// Create an allocation expression
			var tempLHS = CreateAndRegisterTemporaryLHVar(node, type.Type);
			var allocAnalysisExpression = new Allocation(node, type.Type, tempLHS, this.roslynMethodVisitor.DeclarationNode);

			this.statementProcessor.RegisterNewExpressionAssignment(tempLHS, allocAnalysisExpression.GetAnalysisType());
			// Process the constructor as a call
			if (symbol != null)
			{
				IMethodSymbol roslynMethod = (IMethodSymbol)symbol;
				// Process parameters
				var callNode = new AnalysisCallNode(roslynMethod.Name,
					Utils.CreateTypeDescriptor(type.Type), /*roslynMethod.ReturnType*/
					this.CreateLocationDescriptor(this.roslynMethodVisitor.InvocationPosition, node),
					Utils.CreateAnalysisCallNodeAdditionalInfo(roslynMethod)); //node.GetLocation()

				var methodDescriptor = Utils.CreateMethodDescriptor(roslynMethod);

				if (roslynMethod.MethodKind == MethodKind.Conversion)
				{
					Contract.Assert(roslynMethod.IsStatic);
					//Contract.Assert(roslynMethod.MethodKind == MethodKind.Conversion);
					var args = ProcessArguments(argumentListSyntax, roslynMethod.Parameters);
					statementProcessor.RegisterStaticCall(methodDescriptor, args, tempLHS, callNode);
				}
				else if (roslynMethod.MethodKind == MethodKind.Constructor)
				{
					Contract.Assert(!roslynMethod.IsStatic);
					//Contract.Assert(roslynMethod.MethodKind == MethodKind.Constructor);
					var args = ProcessArguments(argumentListSyntax, roslynMethod.Parameters);
					statementProcessor.RegisterConstructorCall(methodDescriptor, args, tempLHS, callNode);
				}
			}
			return allocAnalysisExpression;
		}

		private IList<PropGraphNodeDescriptor> ProcessArguments(ArgumentListSyntax argumentListSyntax,
			ImmutableArray<IParameterSymbol> parameters)
		{
			var paramList = parameters.ToList();
			//var args = new List<PropGraphNodeDescriptor>(parameters.Length);
			var args = new PropGraphNodeDescriptor[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				if (argumentListSyntax != null && argumentListSyntax.Arguments != null
					&& argumentListSyntax.Arguments.Count > i)
				{
					PropGraphNodeDescriptor anode = null;
					var argSyntax = argumentListSyntax.Arguments[i];
					var a_prime = this.Visit(argSyntax);
					if (a_prime != null)
					{
						if (a_prime is Call) // || a_prime is Property)
						{
							var callExp = a_prime as Call;
							anode = callExp.ReturnedVariableNode;
						}
						else
						{
							anode = a_prime.GetAnalysisNode();
						}
					}
					int pos = i;
					if (argSyntax.NameColon != null)
					{
						var parName = argSyntax.NameColon.Name.ToString();
						pos = paramList.FindIndex(p => p.Name.Equals(parName));
					}
					args[pos] = anode;
				}
				// else args[i]=null;
			}
			Contract.Assert(args.Length == parameters.Length);
			return new List<PropGraphNodeDescriptor>(args);
		}

		/// <summary>
		/// TO DO
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public override AnalysisExpression VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
		{
			var lambdaSymbol = (IMethodSymbol)model.GetSymbolInfo(node).Symbol;
			var lambdaMethodDescriptor = Utils.CreateMethodDescriptor(lambdaSymbol);
			var baseMethodDescriptor = this.statementProcessor.Method;
			var methodDescriptor = new AnonymousMethodDescriptor(baseMethodDescriptor, lambdaMethodDescriptor);
			var lambdaMethodParser = new LambdaMethodParser(model, node.Body, lambdaSymbol, methodDescriptor, this.roslynMethodVisitor.ContainingTypeNode, this.roslynMethodVisitor.DeclarationNode);
			var methodEntity = lambdaMethodParser.ParseMethod();

			statementProcessor.RegisterAnonymousMethod(methodDescriptor, methodEntity);
			return new Lambda(node, lambdaSymbol.ReturnType, lambdaSymbol, methodDescriptor, this.roslynMethodVisitor.DeclarationNode);
		}

		public override AnalysisExpression VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
		{
			var lambdaSymbol = (IMethodSymbol)model.GetSymbolInfo(node).Symbol;
			var lambdaMethodDescriptor = Utils.CreateMethodDescriptor(lambdaSymbol);
			var baseMethodDescriptor = this.statementProcessor.Method;
			var methodDescriptor = new AnonymousMethodDescriptor(baseMethodDescriptor, lambdaMethodDescriptor);
			var lambdaMethodParser = new LambdaMethodParser(model, node.Body, lambdaSymbol, methodDescriptor, this.roslynMethodVisitor.ContainingTypeNode, this.roslynMethodVisitor.DeclarationNode);
			var methodEntity = lambdaMethodParser.ParseMethod();

			statementProcessor.RegisterAnonymousMethod(methodDescriptor, methodEntity);
			return new Lambda(node, lambdaSymbol.ReturnType, lambdaSymbol, methodDescriptor, this.roslynMethodVisitor.DeclarationNode);
		}

		public override AnalysisExpression VisitExpressionStatement(ExpressionStatementSyntax node)
		{
			return Visit(node.Expression);
		}

		public override AnalysisExpression VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
		{
			return base.VisitAnonymousObjectCreationExpression(node);
		}
		/// <summary>
		/// TO DO
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public override AnalysisExpression VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
		{
			return ProcessConstructor(node, null);
		}
		#endregion

		/// <summary>
		/// TO DO
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public override AnalysisExpression VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
		{
			return base.VisitAnonymousMethodExpression(node);
		}

		public override AnalysisExpression VisitArgument(ArgumentSyntax node)
		{
			var analysisExpression = Visit(node.Expression);

			//if (analysisExpression != null)
			//{
			//	analysisExpression.ProcessArgument(node, this);
			//}

			//if(analysisExpression is Property)
			//{
			//    Property property = analysisExpression as Property;
			//    analysisExpression = AnalyzeProperty(node.Expression, this.ThisRef, null, property);
			//}
			//else if(analysisExpression is Call)
			//{
			//    analysisExpression = AnalyzeInvocation(node.Expression as InvocationExpressionSyntax);
			//}

			return analysisExpression;
		}

		public override AnalysisExpression VisitBinaryExpression(BinaryExpressionSyntax node)
		{
			var lhs = Visit(node.Left);
			var rhs = Visit(node.Right);

			return CreateUnsupportedExpression(node);
		}

		/// <summary>
		/// Process an invocation.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public override AnalysisExpression VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			//// TODO: Remove these lines
			//if (node.ToString() == "Parse(\"/rules\")")
			//{
			//}

			//this.roslynMethodVisitor.InvocationPosition++;

			// CHECK
			var callExpression = node.Expression;

			// in Roslyn rc1 they use Kind() instead of CSharpKind()...
			//if (callExpression.Kind() == (SyntaxKind.SimpleMemberAccessExpression) ||
			//callExpression.Kind() == (SyntaxKind.IdentifierName))

			if (callExpression.Kind() == SyntaxKind.SimpleMemberAccessExpression ||
				callExpression.Kind() == SyntaxKind.IdentifierName)
			{
				return AnalyzeInvocation(node);
			}
			return CreateUnsupportedExpression(node);
		}

		internal AnalysisExpression AnalyzeInvocation(InvocationExpressionSyntax node)
		{
			VariableNode lh = null;
			Call call = null;
			// Analyze if it is actually a chain of invocations
			// in Roslyn rc1 they use Kind() instead of CSharpKind...
			if (node.Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
			{
				var simpleAccessMemberExpression = node.Expression as MemberAccessExpressionSyntax;
				// Analyze the expresion that can be an invocation or an access path
				var analysisExpression = Visit(simpleAccessMemberExpression.Expression);
				if (analysisExpression is Call)
				{
					// In the case of a nested call we use an artificial var as the left value
					// of an invocation (TempRV = call...)
					// That TempRV can be used as a parameter (e.g. the receiver) in the next call
					call = analysisExpression as Call;
					lh = call.ReturnedVariableNode;
				}
				// This is a Property and then a call (e.g, Prop.Method())
				else if (analysisExpression is Property)
				{
					call = AnalyzePropertyGetter(simpleAccessMemberExpression, this.ThisRef, null,
												analysisExpression as Property);
					if (call != null)
					{
						lh = call.ReturnedVariableNode;
					}
				}
				else
				{
					// Diego: TO-DO
					// This is for Identifier of MemberAccess 
					// These cases are handled later by TrytToGetReceiver 
					// It will be nice to handle here to avoid revisiting the node
					// And we already have here almost all the information
				}
			}
			else
			{
				// Idem previous TO-DO:  handled later by TrytToGetReceiver
				// This case is for static calls or virtual call with implicit "this"
			}
			return RegisterCall(node, lh);
		}

		/// <summary>
		/// Register an invocation of the form: a.call(....)
		/// The variable tempReceiver is used when the call includes a nested call
		/// e.g : call1(...).call2(...), tempReceiver represents the result of call1() 
		/// </summary>
		/// <param name="node"></param>
		/// <param name="args"></param>
		/// <param name="tempReceiver"></param>
		/// <returns></returns>
		private AnalysisExpression RegisterCall(InvocationExpressionSyntax node, VariableNode tempReceiver)
		{
			VariableNode lh = null;
			AnalysisExpression result = null;

			var callSymbolinfo = this.model.GetSymbolInfo(node);
			var methodSymbol = callSymbolinfo.Symbol;
			// This shouldn't happend but if it fails to get the method, at least tries to get a candidate
			if (methodSymbol == null && callSymbolinfo.CandidateSymbols.Length > 0)
			{
				methodSymbol = callSymbolinfo.CandidateSymbols[0];
			}

			if (methodSymbol != null)
			{
				IMethodSymbol methodInvokedSymbol = methodSymbol as IMethodSymbol;
				var methodDescriptor = Utils.CreateMethodDescriptor(methodInvokedSymbol);
				lh = CreateAndRegisterTemporaryLHVar(node, methodInvokedSymbol);

				var args = ProcessArguments(node.ArgumentList, methodInvokedSymbol.Parameters);

				this.roslynMethodVisitor.InvocationPosition++;

				// Delegate? 
				if (methodInvokedSymbol.MethodKind == MethodKind.DelegateInvoke)
				{
					var delegateNode = node.Expression;
					var memberAccess = delegateNode as MemberAccessExpressionSyntax;

					if (memberAccess != null && memberAccess.Name.Identifier.ValueText == "Invoke")
					{
						delegateNode = memberAccess.Expression;
					}

					// Get the delegate variable (SHOULD use a cache!)
					var delegateVarNode = roslynMethodVisitor.RegisterVariable(delegateNode);

					var callNode = new AnalysisCallNode(methodInvokedSymbol.Name,
						Utils.CreateTypeDescriptor(methodInvokedSymbol.ReturnType),
						this.CreateLocationDescriptor(this.roslynMethodVisitor.InvocationPosition, delegateNode),
						Utils.CreateAnalysisCallNodeAdditionalInfo(methodInvokedSymbol)); //node.GetLocation()

					statementProcessor.RegisterStaticDelegateCall(methodDescriptor, args, lh, (DelegateVariableNode)delegateVarNode, callNode);
					result = new DelegateCall(node, methodInvokedSymbol.ReturnType, methodInvokedSymbol, callNode, lh, this.roslynMethodVisitor.DeclarationNode);
				}
				// Normal invocation    
				else
				{
					var callNode = new AnalysisCallNode(methodInvokedSymbol.Name,
						Utils.CreateTypeDescriptor(methodInvokedSymbol.ReturnType),
						this.CreateLocationDescriptor(this.roslynMethodVisitor.InvocationPosition, node.Expression),
						Utils.CreateAnalysisCallNodeAdditionalInfo(methodInvokedSymbol)); //node.GetLocation()

					VariableNode receiverArg = null;
					if (!methodInvokedSymbol.IsStatic)
					{
						// Diego: Check types!
						var receiver = TryToGetReceiver(node, methodInvokedSymbol);

						if (receiver != null && receiver is VariableNode)
						{
							// Contract.Assert(receiver is VariableNode);

							// Try to get receiver, when this is not given by the previous nested call
							receiverArg = tempReceiver == null ? (VariableNode)receiver : tempReceiver;
							if (receiverArg != null)
							{
								if (receiverArg != receiver)
								{
									statementProcessor.RegisterAssignment(receiverArg, (VariableNode)receiver);
								}

								if (methodInvokedSymbol.IsExtensionMethod)
								{
									// We adapt the method as it is an extension
									if (methodInvokedSymbol.IsExtensionMethod)
									{
										methodInvokedSymbol = methodInvokedSymbol.ReducedFrom;
										methodDescriptor = Utils.CreateMethodDescriptor(methodInvokedSymbol);
									}

									args.Insert(0, receiverArg);
									statementProcessor.RegisterStaticCall(methodDescriptor, args, lh, callNode);
									result = new Call(node, methodInvokedSymbol.ReturnType, methodInvokedSymbol, callNode, lh,
														this.roslynMethodVisitor.DeclarationNode);
								}
								else
								{
									// Register the invocation in the PropGraph
									statementProcessor.RegisterVirtualCall(methodDescriptor, receiverArg, args, lh, callNode);
								}
							}
							else // To-DO FIX: This is not correct because is not static (in can be a lamba o query or reduced expression)
							{
								statementProcessor.RegisterStaticCall(methodDescriptor, args, lh, callNode);
							}

							result = new Call(node, methodInvokedSymbol.ReturnType, methodInvokedSymbol, callNode, lh, this.roslynMethodVisitor.DeclarationNode);
						}
					}
					else
					{
						statementProcessor.RegisterStaticCall(methodDescriptor, args, lh, callNode);
						result = new Call(node, methodInvokedSymbol.ReturnType, methodInvokedSymbol, callNode, lh, this.roslynMethodVisitor.DeclarationNode);
					}
				}
			}
			else
			{

			}
			return result;
		}

		private VariableNode CreateAndRegisterTemporaryLHVar(ExpressionSyntax node, IMethodSymbol roslynMethod)
		{
			VariableNode tempRV = null;
			//var tmpExp = SyntaxFactory.ParseName("T_" + tempLH.Count);
			if (!roslynMethod.ReturnsVoid && Utils.IsTypeForAnalysis(roslynMethod.ReturnType))
			{
				// Watch out this: Before it was roslynMethod.ReceiverType
				tempRV = this.CreateAndRegisterTemporaryLHVar(node, roslynMethod.ReturnType);
			}

			return tempRV;
		}

		private VariableNode CreateAndRegisterTemporaryLHVar(ExpressionSyntax node, ITypeSymbol roslynType)
		{
			// Creates the node that will hold the value returned by the call, new, etc
			var tempRV = new VariableNode("T_" + tempLH.Count, Utils.CreateTypeDescriptor(roslynType));

			tempLH[node] = tempRV;
			statementProcessor.RegisterLocalVariable(tempRV);

			return tempRV;
		}

		public override AnalysisExpression VisitThisExpression(ThisExpressionSyntax node)
		{
			if (this.ThisRef != null)
			{
				var symbol = this.model.GetSymbolInfo(node).Symbol;
				var type = GetTypeSymbol(node);
				// If we find I real "this" we replace with the fake one
				// I did this to keep the same node 
				return new Identifier(node, type, symbol, this.roslynMethodVisitor.DeclarationNode);
			}
			else
			{
				return null;
			}
		}

		private PropGraphNodeDescriptor TryToGetReceiver(ExpressionSyntax node, IMethodSymbol ms)
		{
			PropGraphNodeDescriptor receiverArg = null;
			//if(node is ObjectCreationExpressionSyntax)
			//{
			//    var objExp = node as ObjectCreationExpressionSyntax;
			//    receiverArg = thisRef; 
			//}
			if (node is InvocationExpressionSyntax)
			{
				var callExp = node as InvocationExpressionSyntax;
				// in Roslyn rc1 they use Kind() instead of CSharpKind...
				switch (callExp.Expression.Kind())
				{
					case SyntaxKind.SimpleMemberAccessExpression:
						var sma = callExp.Expression as MemberAccessExpressionSyntax;
						if (!tempLH.TryGetValue(sma.Expression, out receiverArg))
						{
							var anaExp = Visit(sma.Expression);
							if (anaExp != null)
							{
								receiverArg = (PropGraphNodeDescriptor)anaExp.GetAnalysisNode();
							}
							else
							{
								return null;
							}
						}
						break;
					/// Identifier means a method without "this?'
					case SyntaxKind.IdentifierName:
						receiverArg = this.ThisRef;
						break;
					default:
						// CHECK!!!!
						var anaExp2 = Visit(callExp.Expression);
						receiverArg = anaExp2.GetAnalysisNode();
						break;
				}
			}
			return receiverArg;
		}

		public override AnalysisExpression VisitCastExpression(CastExpressionSyntax node)
		{
			// TODO: should you not be filtering the types that come out of this to only let the ones that match the cast??
			return Visit(node.Expression);
		}

		public override AnalysisExpression VisitConstructorInitializer(ConstructorInitializerSyntax node)
		{
			return base.VisitConstructorInitializer(node);
		}

		public override AnalysisExpression VisitElementAccessExpression(ElementAccessExpressionSyntax node)
		{
			// This is for args[i]. I'm just taking args!
			return Visit(node.Expression);
		}

		public override AnalysisExpression VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
		{
			AnalysisExpression res = null;
			PropGraphNodeDescriptor receiverArg = null;
			// A MemberExpression looks like reference.Name, reference can be a path and Name is an identifier of a Field or Delegate
			var nameExpresssion = Visit(node.Name);

			if (nameExpresssion != null)
			{
				var symbol = this.model.GetSymbolInfo(node.Name).Symbol;

				//if (nameExpresssion == null)
				//{
				//    // If is a namespace, that means that the reference expression is the prexif of the namespace
				//    if (symbol.Kind == SymbolKind.Namespace)
				//    {
				//        return CreateUnsupportedExpression(node);
				//    }
				//}

				// If it is a type, the reference is a namespace
				if (symbol.Kind == SymbolKind.NamedType)
				{
					return CreateUnsupportedExpression(node);
				}
				var referenceExpression = Visit(node.Expression);

				if (referenceExpression != null)
				{
					if (nameExpresssion is Property)
					{
						// The property getter is analyzed here, but the setters are analyzed in the assigments (and converted to call set(value))
						var property = (Property)nameExpresssion;

						if (property.RoslynMethod != null &&
							property.RoslynMethod.MethodKind == MethodKind.PropertyGet)
						{
							res = AnalyzePropertyGetter(node, receiverArg, referenceExpression, property);
						}
					}
					//else if (nameExpresssion is Method)
					//{
					//    // Is it a delegate because is not an invocation
					//    var methodExpression = nameExpresssion as Method;
					//    var delegateMethod = methodExpression.RoslynMethod;
					//    //this.StatementProcessor.RegisterDelegateAssignment(lhsAnalysisNode, new AMethod(delegateMethod));
					//}
					return new MemberAccess(node, referenceExpression, node.Expression,
											(Identifier)nameExpresssion, node.Name,
											nameExpresssion.Type, this.roslynMethodVisitor.DeclarationNode);
				}
			}

			return CreateUnsupportedExpression(node);
		}

		/// <summary>
		/// Obtain and register the method call determined by the property
		/// </summary>
		/// <param name="node"></param>
		/// <param name="receiverArg"></param>
		/// <param name="referenceAnalysisExpression"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		internal Call AnalyzePropertyGetter(ExpressionSyntax node,
					 PropGraphNodeDescriptor receiverArg,
					 AnalysisExpression referenceAnalysisExpression,
					 Property property)
		{
			this.roslynMethodVisitor.InvocationPosition++;
			var roslynMethod = property.RoslynMethod;
			// we treat this as an invocation
			var callNode = new AnalysisCallNode(roslynMethod.Name,
				Utils.CreateTypeDescriptor(roslynMethod.ReturnType),
				this.CreateLocationDescriptor(this.roslynMethodVisitor.InvocationPosition, node),
				Utils.CreateAnalysisCallNodeAdditionalInfo(roslynMethod));

			var methodDescriptor = Utils.CreateMethodDescriptor(roslynMethod);

			//if (receiverArg == null)
			if (receiverArg == null && !methodDescriptor.IsStatic)
			{
				if (referenceAnalysisExpression != null)
				{
					receiverArg = referenceAnalysisExpression.GetAnalysisNode();
				}
			}

			var lhs = CreateAndRegisterTemporaryLHVar(node, roslynMethod);
			var args = new List<PropGraphNodeDescriptor>();
			statementProcessor.RegisterPropertyCall(methodDescriptor, receiverArg, args, lhs, callNode);

			return new Call(node, roslynMethod.ReturnType, roslynMethod, callNode, lhs, this.roslynMethodVisitor.DeclarationNode);
		}

		internal Call AnalyzePropertySetter(ExpressionSyntax node,
					PropGraphNodeDescriptor receiverArg,
					 AnalysisExpression referenceAnalysisExpression,
					 Property property, AnalysisExpression rhs)
		{
			this.roslynMethodVisitor.InvocationPosition++;
			var roslynMethod = property.RoslynMethod;
			// we treat this as an invocation
			var callNode = new AnalysisCallNode(roslynMethod.Name,
				Utils.CreateTypeDescriptor(roslynMethod.ReturnType),
				this.CreateLocationDescriptor(this.roslynMethodVisitor.InvocationPosition, property.Expression),
				Utils.CreateAnalysisCallNodeAdditionalInfo(roslynMethod));

			var methodDescriptor = Utils.CreateMethodDescriptor(roslynMethod);

			//if (receiverArg == null)
			if (receiverArg == null && !methodDescriptor.IsStatic)
			{
				if (referenceAnalysisExpression != null)
				{
					receiverArg = referenceAnalysisExpression.GetAnalysisNode();
				}
			}

			var lhs = CreateAndRegisterTemporaryLHVar(node, roslynMethod);
			var args = new List<PropGraphNodeDescriptor>();
			var arg = rhs == null ? null : rhs.GetAnalysisNode();

			args.Add(arg);
			statementProcessor.RegisterPropertyCall(methodDescriptor, receiverArg, args, lhs, callNode);

			return new Call(node, roslynMethod.ReturnType, roslynMethod, callNode, lhs, this.roslynMethodVisitor.DeclarationNode);
		}

		public override AnalysisExpression VisitBaseExpression(BaseExpressionSyntax node)
		{
			// I treat this like a this reference...
			var symbol = this.model.GetSymbolInfo(node).Symbol;
			//return new Identifier(node, this.ThisRef.DeclaredType, symbol);
			return new Identifier(node, symbol.ContainingType, symbol, this.roslynMethodVisitor.DeclarationNode);
		}

		public override AnalysisExpression VisitAwaitExpression(AwaitExpressionSyntax node)
		{
			var exp = Visit(node.Expression);

			if (exp != null && exp.Type is INamedTypeSymbol)
			{
				var type = (INamedTypeSymbol)exp.Type;
				if (type.IsGenericType)
				{
					exp.Type = type.TypeArguments.Single();
				}
				else
				{
					// This should never happen
					exp.Type = this.model.Compilation.GetSpecialType(SpecialType.System_Void);
				}
			}

			return exp;
		}

		private ITypeSymbol GetTypeSymbol(SyntaxNode node)
		{
			var typeInfo = this.model.GetTypeInfo(node);
			var type = typeInfo.Type;
			return type;
		}

		private ITypeSymbol GetTypeSymbol(ExpressionSyntax node)
		{
			var typeInfo = this.model.GetTypeInfo(node);
			var type = typeInfo.Type;
			return type;
		}
		/// <summary>
		/// Creates a node for expressions we can not deal
		/// Diego: Maybe it'd be better to return null for the VTA analysis
		/// For the Declared Type propagation we can return the declared type
		/// </summary>
		/// <param name="exp"></param>
		/// <returns></returns>
		private AnalysisExpression CreateUnsupportedExpression(ExpressionSyntax exp)
		{
			var type = GetTypeSymbol(exp);
			var symbol = this.model.GetSymbolInfo(exp).Symbol;
			if (Utils.IsTypeForAnalysis(type))
				return new UnsupportedExpression(exp, type, symbol);
			return null;
		}
	}
}
