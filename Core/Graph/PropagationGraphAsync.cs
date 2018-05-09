// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using OrleansClient.Communication;
using OrleansClient.Roslyn;
using System.Diagnostics;
using Common;

namespace OrleansClient
{
	/// <summary>
	/// Propagation graph: is the main data structure of the algorithm
	/// The basic idea is that concrete types flow from the graph nodes which represent proggram expressions (variables, fields, invocaitons)
	/// The invocations are dummy nodes. Their role is to trigger the propagation of data to the callees. When a type reach an invocation 
	/// it is marked to be processesd later
	/// </summary>
	/// <typeparam name="N"></typeparam>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="M"></typeparam>
	internal partial class PropagationGraph
	{
		public int UpdateCount { get; private set; }
		public int WorklistSize { get; private set; }

		internal async Task<bool> DiffPropAsync(IEnumerable<TypeDescriptor> src, string edge, PropGraphNodeDescriptor n, PropagationKind propKind)
		{
			Logger.LogS("PropagationGraph", "DiffPropAsync", "Diff({0},{1})", src, n);

			if (propKind == PropagationKind.REMOVE_TYPES || propKind == PropagationKind.REMOVE_ASSIGNMENT)
			{
				return DiffDelProp(n, edge, src);
			}
			else
			{
				return await DiffPropAsync(src, edge, n);
			}
		}

		internal async Task<bool> DiffPropAsync(IEnumerable<TypeDescriptor> src, string edge, PropGraphNodeDescriptor n)
		{
			if (IsCallNode(n) || IsDelegateCallNode(n))
			{
				var msg = string.Format("DiffPropAsync invoked with a call node '{0}'", n);
				throw new Exception(msg);
			}

			var newTypes = GetAddedTypes(n);
			if (newTypes.IsSupersetOf(src))
				return false;

			var oldTypes = GetTypes(n);
			var compatibleTypes = new HashSet<TypeDescriptor>();

			foreach (var t in src)
			{
				if (!oldTypes.Contains(t))
				{
					var isAsig = await IsAssignableAsync(t, n);

					if (isAsig)
					{
						compatibleTypes.Add(t);
					}
				}
			}

			AddTypes(n, edge, compatibleTypes);
			newTypes = GetTypes(n);
			newTypes = new HashSet<TypeDescriptor>(newTypes);

			if (newTypes.Count > oldTypes.Count)
			{
				newTypes.ExceptWith(oldTypes);
				this.UpdateCount += newTypes.Count;
				this.AddToWorkList(n);
				this.AddTypes(n, newTypes);
				return true;
			}

			return false;
		}

		internal async Task<bool> IsAssignableAsync(TypeDescriptor t1, PropGraphNodeDescriptor analysisNode)
		{
			// Contract.Assert(this.codeProvider!=null);
			if (codeProvider == null)
			{
				return true;
			}

			var res = true;
			// Ugly
			var type1 = t1;
			var type2 = analysisNode.Type;
			var isSubType = await this.codeProvider.IsSubtypeAsync(type1, type2);

			if (!isSubType)
			{
				if (!type2.IsDelegate)
				{
					if (!IsCallNode(analysisNode) && !IsDelegateCallNode(analysisNode))
					{
						return false;
					}
				}
			}

			return res;
		}

		internal async Task<PropagationEffects> PropagateAsync(IProjectCodeProvider codeProvider)
		{
			this.UpdateCount = 0;
			this.WorklistSize = workList.Count;

			//Logger.Log("Add Working Set size {0}", this.workList.Count);
			this.codeProvider = codeProvider;

			var calls = new HashSet<CallInfo>();
			var retModified = false;

			while (workList.Count > 0)
			{
				var analysisNode = workList.First();
				this.RemoveFromWorkList(analysisNode);

				if (IsCallNode(analysisNode) || IsDelegateCallNode(analysisNode))
				{
					//this.UpdateCount++;
					calls.Add(GetInvocationInfo(analysisNode));
					continue;
				}
				if (IsRetNode(analysisNode))
				{
					retModified = true;
				}

				var v = GetVertex(analysisNode);
				var types = GetAddedTypes(analysisNode);

				foreach (var v1 in graph.GetTargets(v.Id))
				{
					var n1 = GetAnalysisNode(v1);

					if (IsCallNode(n1) || IsDelegateCallNode(n1))
					{
						calls.Add(GetInvocationInfo(n1));
					}
					else
					{
						await DiffPropAsync(types, analysisNode.ToString(), n1);
					}

					//var e = graph.GetEdge(v.Id, v1.Id);
					//e.Value.Types = types;

					DiffPropDelegates(GetDelegates(analysisNode), n1);
				}
			}

			this.HasBeenPropagated = true;
			return new PropagationEffects(calls, retModified, PropagationKind.ADD_TYPES, this.UpdateCount, this.WorklistSize);
		}

		internal void ResetUpdateCount()
		{
			this.UpdateCount = 0;
		}

		internal Task<PropagationEffects> PropagateDeletionAsync(IProjectCodeProvider codeProvider)
		{
			//Logger.Log("Delete Working Set size {0}", this.deletionWorkList.Count);
			this.codeProvider = codeProvider;

			var calls = new HashSet<CallInfo>();
			var retModified = false;

			while (deletionWorkList.Count > 0)
			{
				var analysisNode = deletionWorkList.First();
				this.RemoveFromDeletionWorkList(analysisNode);

				if (IsCallNode(analysisNode) || IsDelegateCallNode(analysisNode))
				{
					calls.Add(GetInvocationInfo(analysisNode));
					continue;
				}
				if (IsRetNode(analysisNode))
				{
					retModified = true;
				}

				var v = GetVertex(analysisNode);
				var types = GetDeletedTypes(analysisNode);

				foreach (var v1 in graph.GetTargets(v.Id))
				{
					var n1 = GetAnalysisNode(v1);

					if (IsCallNode(n1) || IsDelegateCallNode(n1))
					{
						calls.Add(GetInvocationInfo(n1));
					}
					else
					{
						DiffDelProp(n1, analysisNode.ToString(), types);
					}

					//if (DiffDelProp(n1, analysisNode.ToString(), types))
					//{
					//	var e = graph.GetEdge(v.Id, v1.Id);
					//	e.Value.Types.ExceptWith(types);
					//}

					// DiffPropDelegates(GetDelegates(analysisNode), n1);
				}
			}

			return Task.FromResult(new PropagationEffects(calls, retModified, PropagationKind.REMOVE_TYPES));
		}

		// DIEGO 
		// TODO: ADD Here the instantiated types
		internal async Task<ISet<TypeDescriptor>> GetPotentialTypesAsync(PropGraphNodeDescriptor n, MethodCallInfo callInfo, IProjectCodeProvider codeProvider)
		{
			var result = new HashSet<TypeDescriptor>();
			var types = this.GetTypes(n);

			if (types.Count == 0)
			{
				var instantiatedTypes = await codeProvider.GetCompatibleInstantiatedTypesAsync(callInfo.Receiver.Type);
				types.UnionWith(instantiatedTypes);

				if (types.Count == 0)
				{
					types.Add(callInfo.Receiver.Type);
				}
			}

			result.UnionWith(types);

			//foreach (var typeDescriptor in types)
			//{
			//    // TO-DO fix by adding a where T: AnalysisType
			//    if (typeDescriptor.IsConcreteType)
			//    {
			//        result.Add(typeDescriptor);
			//    }
			//    else
			//    {
			//        Contract.Assert(callInfo.InstantiatedTypes != null);
			//        foreach (var candidateType in callInfo.InstantiatedTypes)
			//        {
			//            var isSubtype = await codeProvider.IsSubtypeAsync(candidateType, typeDescriptor);
			//            if (isSubtype)
			//            {
			//                result.Add(candidateType);
			//            }
			//        }
			//    }
			//}

			return result;
		}

		/// <summary>
		///  C
		/// </summary>
		/// <param name="invoInfo"></param>
		/// <param name="codeProvider"></param>
		/// <returns></returns>
		internal Task<ISet<MethodDescriptor>> ComputeCalleesForNodeAsync(CallInfo invoInfo, IProjectCodeProvider codeProvider)
		{
			//TODO: Ugly... but we needed this refactor for moving stuff to the common project 
			if (invoInfo is MethodCallInfo)
			{
				return ComputeCalleesForCallNodeAsync((MethodCallInfo)invoInfo, codeProvider);
			}
			Contract.Assert(invoInfo is DelegateCallInfo);
			return ComputeCalleesForDelegateNodeAsync((DelegateCallInfo)invoInfo, codeProvider);
		}

		internal async Task<ISet<MethodDescriptor>> ComputeCalleesForCallNodeAsync(MethodCallInfo callInfo, IProjectCodeProvider codeProvider)
		{
			Contract.Assert(codeProvider != null);
			var calleesForNode = new HashSet<MethodDescriptor>();

			//if (methodCallInfo.Method.IsStatic || !methodCallInfo.Method.IsVirtual)
			if (callInfo.Method.IsStatic)
			{
				// Static method call
				calleesForNode.Add(callInfo.Method);
			}
			else if (!callInfo.Method.IsVirtual)
			{
				// Non-virtual instance method call
				calleesForNode.Add(callInfo.Method);
			}
			else if (callInfo.Receiver != null)
			{
				// Virtual instance method call

				// I replaced the invocation for a local call to mark that functionality is missing
				//var callees = GetPotentialTypes(this.Receiver, propGraph)
				//    .Select(t => this.Callee.FindMethodImplementation(t));

				var potentialTypes = await this.GetPotentialTypesAsync(callInfo.Receiver, callInfo, codeProvider);

				foreach (var type in potentialTypes)
				{
					Contract.Assert(type != null);
					Contract.Assert(callInfo != null);

					var realCallee = await codeProvider.FindMethodImplementationAsync(callInfo.Method, type);
					Contract.Assert(realCallee != null);

					calleesForNode.Add(realCallee);
				}
			}
			else
			{
				calleesForNode.Add(callInfo.Method);
			}
			return calleesForNode;
		}

		internal async Task<ISet<MethodDescriptor>> ComputeCalleesForDelegateNodeAsync(DelegateCallInfo callInfo, IProjectCodeProvider codeProvider)
		{
			return await GetDelegateCalleesAsync(callInfo.Delegate, codeProvider);
		}

		private async Task<ISet<MethodDescriptor>> GetDelegateCalleesAsync(VariableNode delegateNode, IProjectCodeProvider codeProvider)
		{
			var callees = new HashSet<MethodDescriptor>();
			var typeDescriptors = this.GetTypes(delegateNode);
			foreach (var delegateInstance in this.GetDelegates(delegateNode))
			{
				if (typeDescriptors.Count() > 0)
				{
					foreach (var typeDescriptor in typeDescriptors)
					{
						// TO-DO!!!
						// Ugly: I'll fix it
						//var aMethod = delegateInstance.FindMethodImplementation(type);
						var aMethod = await codeProvider.FindMethodImplementationAsync(delegateInstance, typeDescriptor);
						callees.Add(aMethod);
					}
				}
				else
				{
					// if Count is 0, it is a delegate that do not came form an instance variable
					callees.Add(delegateInstance);
				}
			}

			return callees;
		}

		public async Task<MethodCalleesInfo> FixUnknownCalleesAsync(IProjectCodeProvider codeProvider)
		{
			var resolvedCallees = new HashSet<MethodDescriptor>();
			//var unknownCallees = new HashSet<PropGraphNodeDescriptor>();
			var hasUnknownCallees = false;

			foreach (var callNode in this.CallNodes)
			{
				var invInfo = this.GetInvocationInfo(callNode);

				if (invInfo is MethodCallInfo)
				{
					var callInfo = invInfo as MethodCallInfo;

					if (callInfo.Receiver != null)
					{
						var potentialTypes = this.GetTypes(callInfo.Receiver);

						if (potentialTypes.Count == 0)
						{
							potentialTypes = await this.GetPotentialTypesAsync(callInfo.Receiver, callInfo, codeProvider);

							this.Add(callInfo.Receiver, "declared_type", potentialTypes);
							this.AddToWorkList(callInfo.Receiver);
							//unknownCallees.Add(callInfo.Receiver);
							hasUnknownCallees = true;
						}
						else
						{
							var callees = await ComputeCalleesForCallNodeAsync(callInfo, codeProvider);
							resolvedCallees.UnionWith(callees);
						}
					}
					else
					{
						resolvedCallees.Add(callInfo.Method);
					}
				}
				//else if (invInfo is DelegateCallInfo)
				//{
				//	var callInfo = invInfo as DelegateCallInfo;
				//}
			}

			var result = new MethodCalleesInfo(resolvedCallees, hasUnknownCallees);
			return result;
		}
	}
}