// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System;
using Common;
using System.Linq;

namespace OrleansClient
{
	/// <summary>
	/// This class represents the data that a node in the PropGraph can carry
	/// Types: concrete Types (we store the possible types per incoming edge)
	/// AddedTypes: is the set of concrete types we have added. Used to propagate an addition of a type or an edge in the graph
	/// DeletedTypes: is the set of concrete types we have deleted. Used to propagate a removal of a type or an edge in the graph
	/// Node: is tne node value (the graph from Nuri uses numbers for vertex)
	/// CallNode: some node may include information about an invocation (the call nodes)
	/// </summary>
	[Serializable]
	internal class GraphNodeAnnotationData
	{
		// Stores the possible types per incoming edge (edge/caller context/callee).
		private IDictionary<string, ISet<TypeDescriptor>> _types;

		internal ISet<TypeDescriptor> AddedTypes { get; private set; }
		internal ISet<TypeDescriptor> DeletedTypes { get; private set; }
		internal ISet<MethodDescriptor> Delegates { get; set; }
		internal PropGraphNodeDescriptor Node { get; set; }
		internal CallNode CallNode { get; set; }
		internal bool HasRetValue { get; set; }

		internal GraphNodeAnnotationData(PropGraphNodeDescriptor node)
		{
			_types = new Dictionary<string, ISet<TypeDescriptor>>();

			this.Node = node;
			this.HasRetValue = false;
			this.AddedTypes = new HashSet<TypeDescriptor>();
			this.DeletedTypes = new HashSet<TypeDescriptor>();
			this.Delegates = new HashSet<MethodDescriptor>();			
		}

		internal IEnumerable<string> Edges
		{
			get { return _types.Keys; }
		}

		internal ISet<TypeDescriptor> Types
		{
			get
			{
				var result = new HashSet<TypeDescriptor>();

				foreach (var set in _types.Values)
				{
					result.UnionWith(set);
				}

				return result;
			}
		}

		internal IEnumerable<TypeDescriptor> GetTypes(string edge)
		{
			var result = Enumerable.Empty<TypeDescriptor>();
			ISet<TypeDescriptor> edgeTypes;
			var exists = _types.TryGetValue(edge, out edgeTypes);

			if (exists)
			{
				result = edgeTypes;
			}

			return result;
		}

		internal void AddType(string edge, TypeDescriptor type)
		{
			ISet<TypeDescriptor> edgeTypes;
			var exists = _types.TryGetValue(edge, out edgeTypes);

			if (!exists)
			{
				edgeTypes = new HashSet<TypeDescriptor>();
				_types.Add(edge, edgeTypes);
			}

			edgeTypes.Add(type);
		}

		internal void AddTypes(string edge, IEnumerable<TypeDescriptor> types)
		{
			ISet<TypeDescriptor> edgeTypes;
			var exists = _types.TryGetValue(edge, out edgeTypes);

			if (!exists)
			{
				edgeTypes = new HashSet<TypeDescriptor>();
				_types.Add(edge, edgeTypes);
			}

			edgeTypes.UnionWith(types);
		}

		internal void RemoveType(string edge, TypeDescriptor type)
		{
			ISet<TypeDescriptor> edgeTypes;
			var exists = _types.TryGetValue(edge, out edgeTypes);

			if (exists)
			{
				edgeTypes.Remove(type);
			}
		}

		internal void RemoveTypes(string edge, IEnumerable<TypeDescriptor> types)
		{
			var removedTypes = new HashSet<TypeDescriptor>();
			ISet<TypeDescriptor> edgeTypes;
			var exists = _types.TryGetValue(edge, out edgeTypes);

			if (exists)
			{
				edgeTypes.ExceptWith(types);
			}
		}
	}

	[Serializable]
	internal abstract class CallNode
	{
		public bool IsConstructor { get; set; }
		public MethodDescriptor Caller { get; set; }
		public IList<PropGraphNodeDescriptor> Arguments { get; set; }
		public PropGraphNodeDescriptor Receiver { get; set; }
		public VariableNode LHS { get; set; }
		public AnalysisCallNode Node { get; set; }

		// Stores all possible callees.
		public ISet<ResolvedCallee> PossibleCallees { get; set; }

		public CallNode(MethodDescriptor caller, AnalysisCallNode callNode,
			PropGraphNodeDescriptor receiver, IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
		{
			this.Caller = caller;
			this.Arguments = arguments;
			this.LHS = lhs;
			this.Receiver = receiver;
			this.Node = callNode;
			this.PossibleCallees = new HashSet<ResolvedCallee>();
		}

		public bool IsStatic
		{
			get { return this.Receiver == null; }
		}

		public abstract CallInfo ToCallInfo();
	}

	[Serializable]
	internal class MethodCallNode : CallNode
	{
		public MethodDescriptor Method { get; private set; }

		public MethodCallNode(MethodDescriptor caller, AnalysisCallNode callNode, MethodDescriptor method,
			PropGraphNodeDescriptor receiver, IList<PropGraphNodeDescriptor> arguments,
			VariableNode lhs, bool isConstructor)
			: base(caller, callNode, receiver, arguments, lhs)
		{
			this.IsConstructor = isConstructor;
			this.Method = method;
		}

		public MethodCallNode(MethodDescriptor caller, AnalysisCallNode callNode,
			MethodDescriptor method, IList<PropGraphNodeDescriptor> arguments,
			VariableNode lhs, bool isConstructor)
			: this(caller, callNode, method, null, arguments, lhs, isConstructor)
		{
		}

		public override CallInfo ToCallInfo()
		{
			var result = new MethodCallInfo(this.Caller, this.Node, this.Method, this.Receiver, this.Arguments, this.LHS, this.IsConstructor);
			return result;
		}
	}

	[Serializable]
	internal class DelegateCallNode : CallNode
	{
		public DelegateVariableNode Delegate { get; private set; }

		public DelegateCallNode(MethodDescriptor caller, AnalysisCallNode callNode,
			DelegateVariableNode calleeDelegate, PropGraphNodeDescriptor receiver,
			IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
			: base(caller, callNode, receiver, arguments, lhs)
		{
			this.Delegate = calleeDelegate;
		}

		public DelegateCallNode(MethodDescriptor caller, AnalysisCallNode callNode,
			DelegateVariableNode @delegate, IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
			: this(caller, callNode, @delegate, null, arguments, lhs)
		{
		}

		public override CallInfo ToCallInfo()
		{
			var result = new DelegateCallInfo(this.Caller, this.Node, this.Delegate, this.Receiver, this.Arguments, this.LHS);
			return result;
		}
	}

	/// <summary>
	/// This class represents the data that an edge in the PropGraph can carry
	/// </summary>
	[Serializable]
	internal class GraphEdgeAnnotationData
	{
		internal ISet<TypeDescriptor> Types { get; set; }

		internal GraphEdgeAnnotationData()
		{
			this.Types = new HashSet<TypeDescriptor>();
		}
	}
}
