// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace Common
{
	[Serializable]
	public abstract class CallInfo
	{
		public bool IsConstructor { get; set; }
		public MethodDescriptor Caller { get; set; }
		public IList<PropGraphNodeDescriptor> Arguments { get; set; }
		public PropGraphNodeDescriptor Receiver { get; set; }
		public VariableNode LHS { get; set; }
		public AnalysisCallNode CallNode { get; set; }

		// Stores added/deleted types for each argument.
		public IList<ISet<TypeDescriptor>> ArgumentsModifiedTypes { get; set; }
		// Stores added/deleted callees.
		public ISet<ResolvedCallee> ModifiedCallees { get; set; }
		// Stores all types for each argument.
		public IList<ISet<TypeDescriptor>> ArgumentsAllTypes { get; set; }
		// Stores all callees.
		public ISet<ResolvedCallee> AllCallees { get; set; }

		/*
		 * Possible cases:
		 * 
		 * - addition/deletion of some argument types:
		 *   propagate to all callees the added/deleted argument types.
		 *   
		 * - addition/deletion of some callees:
		 *   propagate to the added/deleted callees all argument types.
		 *   
		 * - addition/deletion of some argument types + addition/deletion of some callees:
		 *   propagate to all callees the added/deleted argument types +
		 *   propagate to the added/deleted callees all argument types.
		 */
		
		public CallInfo(MethodDescriptor caller, AnalysisCallNode callNode,
			PropGraphNodeDescriptor receiver, IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
		{
			this.Caller = caller;
			this.Arguments = arguments;
			this.LHS = lhs;
			this.Receiver = receiver;
			this.CallNode = callNode;
			this.ArgumentsModifiedTypes = new List<ISet<TypeDescriptor>>();
			this.ModifiedCallees = new HashSet<ResolvedCallee>();
			this.ArgumentsAllTypes = new List<ISet<TypeDescriptor>>();
			this.AllCallees = new HashSet<ResolvedCallee>();
		}

		public CallInfo(CallInfo other, IEnumerable<ResolvedCallee> modifiedCallees, IEnumerable<ResolvedCallee> allCallees)
		{
			this.IsConstructor = other.IsConstructor;
			this.Caller = other.Caller;
			this.LHS = other.LHS;
			this.Receiver = other.Receiver;
			this.CallNode = other.CallNode;
			this.Arguments = new List<PropGraphNodeDescriptor>(other.Arguments);
			this.ModifiedCallees = new HashSet<ResolvedCallee>();
			this.AllCallees = new HashSet<ResolvedCallee>();
			this.ArgumentsModifiedTypes = new List<ISet<TypeDescriptor>>();
			this.ArgumentsAllTypes = new List<ISet<TypeDescriptor>>();

			if (modifiedCallees != null)
			{
				this.ModifiedCallees.UnionWith(modifiedCallees);
			}

			if (allCallees != null)
			{
				this.AllCallees.UnionWith(allCallees);
			}

			if (other.ArgumentsAllTypes != null)
			{
				var argumentsAllTypes = this.ArgumentsAllTypes as List<ISet<TypeDescriptor>>;
				argumentsAllTypes.AddRange(other.ArgumentsAllTypes);
			}

			if (other.ArgumentsModifiedTypes != null)
			{
				var argumentsModifiedTypes = this.ArgumentsModifiedTypes as List<ISet<TypeDescriptor>>;
				argumentsModifiedTypes.AddRange(other.ArgumentsModifiedTypes);
			}
		}

		public abstract CallInfo Clone(IEnumerable<ResolvedCallee> modifiedCallees, IEnumerable<ResolvedCallee> allCallees);

		public bool IsStatic
		{
			get { return this.Receiver == null; }
		}
	}

	[Serializable]
	public class MethodCallInfo : CallInfo
	{
		public MethodDescriptor Method { get; private set; }

		public MethodCallInfo(MethodDescriptor caller, AnalysisCallNode callNode, MethodDescriptor method,
			PropGraphNodeDescriptor receiver, IList<PropGraphNodeDescriptor> arguments,
			VariableNode lhs, bool isConstructor)
			: base(caller, callNode, receiver, arguments, lhs)
		{
			this.IsConstructor = isConstructor;
			this.Method = method;
		}

		public MethodCallInfo(MethodDescriptor caller, AnalysisCallNode callNode,
			MethodDescriptor method, IList<PropGraphNodeDescriptor> arguments,
			VariableNode lhs, bool isConstructor)
			: this(caller, callNode, method, null, arguments, lhs, isConstructor)
		{
		}

		public MethodCallInfo(MethodCallInfo other, IEnumerable<ResolvedCallee> modifiedCallees, IEnumerable<ResolvedCallee> allCallees)
			: base(other, modifiedCallees, allCallees)
		{
			this.Method = other.Method;
		}

		public override CallInfo Clone(IEnumerable<ResolvedCallee> modifiedCallees, IEnumerable<ResolvedCallee> allCallees)
		{
			var result = new MethodCallInfo(this, modifiedCallees, allCallees);
			return result;
		}
	}

	[Serializable]
	public class DelegateCallInfo : CallInfo
	{
		public DelegateVariableNode Delegate { get; private set; }

		public DelegateCallInfo(MethodDescriptor caller, AnalysisCallNode callNode,
			DelegateVariableNode calleeDelegate, PropGraphNodeDescriptor receiver,
			IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
			: base(caller, callNode, receiver, arguments, lhs)
		{
			this.Delegate = calleeDelegate;
		}

		public DelegateCallInfo(MethodDescriptor caller, AnalysisCallNode callNode,
			DelegateVariableNode @delegate, IList<PropGraphNodeDescriptor> arguments, VariableNode lhs)
			: this(caller, callNode, @delegate, null, arguments, lhs)
		{
		}

		public DelegateCallInfo(DelegateCallInfo other, IEnumerable<ResolvedCallee> modifiedCallees, IEnumerable<ResolvedCallee> allCallees)
			: base(other, modifiedCallees, allCallees)
		{
			this.Delegate = other.Delegate;
		}

		public override CallInfo Clone(IEnumerable<ResolvedCallee> modifiedCallees, IEnumerable<ResolvedCallee> allCallees)
		{
			var result = new DelegateCallInfo(this, modifiedCallees, allCallees);
			return result;
		}
	}

	[Serializable]
	public class ReturnInfo
	{
		public ISet<TypeDescriptor> ResultPossibleTypes { get; set; }
		public CallContext CallerContext { get; private set; }
		public MethodDescriptor Callee { get; set; }

		public ReturnInfo(MethodDescriptor callee, CallContext callerContext)
		{
			this.ResultPossibleTypes = new HashSet<TypeDescriptor>();
			this.Callee = callee;
			this.CallerContext = callerContext;
		}
	}
}
