using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using OrleansClient.Communication;
using OrleansClient.Roslyn;
using System.Diagnostics.Contracts;
using CodeGraphModel;
using Common;

namespace OrleansClient.Analysis
{
	/// <summary>
	/// This Classs plays the role of the MethodEntityProcessor but it is used by the OndemandAnalysisAsync
	/// and OnDemandOrleans
	/// This should replace the MethodEntityProcessor when we get rid of the MethodEntityProccesor
	/// </summary>
	internal class MethodEntityWithPropagator : IMethodEntityWithPropagator
	{
		private List<int> UpdateHistory = new List<int>();
		private MethodEntity methodEntity;
		private IProjectCodeProvider codeProvider;
		private CallContext newCallContext;

		//private Queue<PropagationEffects> propagationEffectsToSend;
		//private Orleans.Runtime.Logger logger = GrainClient.Logger;

		///// <summary>
		///// This build a MethodEntityPropagator with a solution
		///// The solution provides its CodeProvicer
		///// </summary>
		///// <param name="methodDescriptor"></param>
		///// <param name="solutionManager"></param>
		//public MethodEntityWithPropagator(MethodDescriptor methodDescriptor, IProjectCodeProvider codeProvider)
		//{
		//	var methodDescriptorToSearch = methodDescriptor.BaseDescriptor;
		//
		//	this.codeProvider = codeProvider;
		//	this.propagationEffectsToSend = new Queue<PropagationEffects>();
		//	this.methodEntity = (MethodEntity)this.codeProvider.CreateMethodEntityAsync(methodDescriptorToSearch).Result;
		//
		//	//var providerEntity = ProjectCodeProvider.FindCodeProviderAndEntity(methodDescriptorToSearch, solutionManager.Solution).Result;
		//	//this.methodEntity = providerEntity.Item2;
		//	//this.codeProvider = providerEntity.Item1;
		//
		//	if (methodDescriptor.IsAnonymousDescriptor)
		//	{
		//		this.methodEntity = this.methodEntity.GetAnonymousMethodEntity((AnonymousMethodDescriptor)methodDescriptor);
		//	}            
		//}

		/// <summary>
		/// Creates the Propagator using directly an entity and a provider
		/// This can be used by the MethodEntityGrain
		/// </summary>
		/// <param name="methodEntity"></param>
		/// <param name="provider"></param>
		public MethodEntityWithPropagator(MethodEntity methodEntity, IProjectCodeProvider provider)
		{
			this.codeProvider = provider;
			this.methodEntity = methodEntity;
			//this.propagationEffectsToSend = new Queue<PropagationEffects>();
		}

		public Task<PropagationEffects> PropagateAsync(PropagationKind propKind, IEnumerable<PropGraphNodeDescriptor> reWorkSet)
		{
			Contract.Requires(reWorkSet != null);

			switch (propKind)
			{
				case PropagationKind.ADD_TYPES:
					methodEntity.PropGraph.AddToWorkList(reWorkSet);
					break;

				case PropagationKind.REMOVE_TYPES:
					methodEntity.PropGraph.AddToDeletionWorkList(reWorkSet);
					break;

				default:
					throw new Exception("Unsupported propagation kind");
			}

			return PropagateAsync(propKind);
		}

		public Task<PropagationEffects> PropagateAsync(PropagationKind propKind)
		{
			this.methodEntity.PropGraph.ResetUpdateCount();
			return InternalPropagateAsync(propKind);
		}

		private async Task<PropagationEffects> InternalPropagateAsync(PropagationKind propKind)
		{
			//Logger.LogS("MethodEntityProp", "PropagateAsync", "Propagation for {0} ", this.methodEntity.MethodDescriptor);
			//Logger.Log("Propagating {0} to {1}", this.methodEntity.MethodDescriptor, propKind);

			// var codeProvider = await ProjectGrainWrapper.CreateProjectGrainWrapperAsync(this.methodEntity.MethodDescriptor);
			PropagationEffects propagationEffects = null;

			switch (propKind)
			{
				case PropagationKind.ADD_TYPES:
					propagationEffects = await this.methodEntity.PropGraph.PropagateAsync(codeProvider);
					break;

				case PropagationKind.REMOVE_TYPES:
					propagationEffects = await this.methodEntity.PropGraph.PropagateDeletionAsync(codeProvider);
					break;

				default:
					throw new Exception("Unsupported propagation kind");
			}

			await this.PopulatePropagationEffectsInfo(propagationEffects, propKind);

			this.methodEntity.PropGraph.RemoveDeletedTypes();

			Logger.LogS("MethodEntityGrain", "PropagateAsync", "End Propagation for {0} ", this.methodEntity.MethodDescriptor);
			//this.methodEntity.Save(@"C:\Temp\"+this.methodEntity.MethodDescriptor.MethodName + @".dot");

			//if (propagationEffects.CalleesInfo.Count > 100)
			//{
			//	int index = 0;
			//	var count = propagationEffects.CalleesInfo.Count;
			//	var callessInfo = propagationEffects.CalleesInfo.ToList();
			//	propagationEffects.CalleesInfo = new HashSet<CallInfo>(callessInfo.GetRange(index, count > 100 ? 100 : count));
			//	propagationEffects.MoreEffectsToFetch = true;
			//
			//	while (count > 100)
			//	{
			//		count -= 100;
			//		index += 100;
			//
			//		var propEffect = new PropagationEffects(new HashSet<CallInfo>(callessInfo.GetRange(index, count > 100 ? 100 : count)), false);
			//
			//		if (count > 100)
			//		{
			//			propEffect.MoreEffectsToFetch = true;
			//		}
			//
			//		this.propagationEffectsToSend.Enqueue(propEffect);                   
			//	}
			//}
			//this.UpdateHistory.Add(this.methodEntity.PropGraph.UpdateCount);
			return propagationEffects;
		}

		private async Task PopulatePropagationEffectsInfo(PropagationEffects propagationEffects, PropagationKind propKind)
		{
			await this.PopulateCalleesInfo(propagationEffects.CalleesInfo, propKind);

			if (this.methodEntity.ReturnVariable != null || propKind == PropagationKind.REMOVE_TYPES)
			{
				if (!propagationEffects.ResultChanged && this.newCallContext != null)
				{
					propagationEffects.ResultChanged = true;
				}

				this.PopulateCallersInfo(propagationEffects.CallersInfo, propKind);
			}
		}

		private async Task PopulateCalleesInfo(IEnumerable<CallInfo> calleesInfo, PropagationKind propKind)
		{
			foreach (var calleeInfo in calleesInfo)
			{
				//  Add instanciated types! 
				// Diego: Ben. This may not work well in parallel... 
				// We need a different way to update this info
				//calleeInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;

				// TODO: This is because of the refactor
				if (calleeInfo is MethodCallInfo)
				{
					var methodCallInfo = calleeInfo as MethodCallInfo;
					//methodCallInfo.ReceiverPossibleTypes = GetTypes(methodCallInfo.Receiver);
					methodCallInfo.PossibleCallees = await this.GetPossibleCalleesForMethodCallAsync(methodCallInfo.Receiver, methodCallInfo.Method, codeProvider);
				}
				else if (calleeInfo is DelegateCallInfo)
				{
					var delegateCalleeInfo = calleeInfo as DelegateCallInfo;
					//delegateCalleeInfo.ReceiverPossibleTypes = GetTypes(delegateCalleeInfo.Delegate);
					delegateCalleeInfo.PossibleCallees = await this.GetPossibleCalleesForDelegateCallAsync(delegateCalleeInfo.Delegate, codeProvider);
				}

				calleeInfo.ArgumentsPossibleTypes.Clear();

				for (int i = 0; i < calleeInfo.Arguments.Count; i++)
				{
					var arg = calleeInfo.Arguments[i];
					var types = GetTypes(arg, propKind);
					var potentialTypes = new HashSet<TypeDescriptor>(types);
					calleeInfo.ArgumentsPossibleTypes.Add(potentialTypes);
				}
			}
		}

		private void PopulateCallersInfo(ISet<ReturnInfo> callersInfo, PropagationKind propKind)
		{
			if (this.newCallContext != null)
			{
				PopulateCallersInfo(callersInfo, this.newCallContext, propKind);
				this.newCallContext = null;
			}
			else
			{
				foreach (var callerContext in this.methodEntity.Callers)
				{
					PopulateCallersInfo(callersInfo, callerContext, propKind);
				}
			}
		}

		private void PopulateCallersInfo(ISet<ReturnInfo> callersInfo, CallContext callerContext, PropagationKind propKind)
		{
			var returnInfo = new ReturnInfo(this.methodEntity.MethodDescriptor, callerContext);
			var types = GetTypes(this.methodEntity.ReturnVariable, propKind);

			returnInfo.ResultPossibleTypes.UnionWith(types);
			//returnInfo.InstantiatedTypes = this.methodEntity.InstantiatedTypes;

			callersInfo.Add(returnInfo);
		}

		public async Task<PropagationEffects> PropagateAsync(CallMessageInfo callMessageInfo)
		{
			this.methodEntity.PropGraph.ResetUpdateCount();

			Logger.LogS("MethodEntityGrain", "PropagateAsync-call", "Propagation for {0} ", callMessageInfo.Callee);

			if (!this.methodEntity.CanBeAnalized)
			{
				var calleesInfo = new HashSet<CallInfo>();
				return new PropagationEffects(calleesInfo, false);
			}

			var edge = string.Format("{0} {1}", callMessageInfo.Caller, callMessageInfo.CallNode);

			if (this.methodEntity.ThisRef != null && callMessageInfo.ReceiverType != null)
			{
				var receiverPossibleTypes = new TypeDescriptor[] { callMessageInfo.ReceiverType };
				await this.methodEntity.PropGraph.DiffPropAsync(receiverPossibleTypes, edge, this.methodEntity.ThisRef, callMessageInfo.PropagationKind);
			}

			for (var i = 0; i < this.methodEntity.ParameterNodes.Count; i++)
			{
				var parameterNode = this.methodEntity.ParameterNodes[i];

				if (parameterNode != null)
				{
					//TODO: Hack. Remove later!
					var argumentPossibleTypes = new HashSet<TypeDescriptor>();

					if (i < callMessageInfo.ArgumentsPossibleTypes.Count)
					{
						argumentPossibleTypes.UnionWith(callMessageInfo.ArgumentsPossibleTypes[i]);
					}
					else
					{
						argumentPossibleTypes.Add(parameterNode.Type);
					}

					await this.methodEntity.PropGraph.DiffPropAsync(argumentPossibleTypes, edge, parameterNode, callMessageInfo.PropagationKind);
				}
			}

			var context = new CallContext(callMessageInfo.Caller, callMessageInfo.LHS, callMessageInfo.CallNode);
			var isNewContext = this.methodEntity.AddToCallers(context);

			if (isNewContext)
			{
				this.newCallContext = context;
			}

			var effects = await InternalPropagateAsync(callMessageInfo.PropagationKind);
			Logger.LogS("MethodEntityGrain", "PropagateAsync-call", "End Propagation for {0} ", callMessageInfo.Callee);
			return effects;
		}

		public async Task<PropagationEffects> PropagateAsync(ReturnMessageInfo returnMessageInfo)
		{
			this.methodEntity.PropGraph.ResetUpdateCount();

			Logger.LogS("MethodEntityGrain", "PropagateAsync-return", "Propagation for {0} ", returnMessageInfo.Caller);
			//PropGraph.Add(lhs, retValues);

			if (returnMessageInfo.LHS != null)
			{
				var edge = string.Format("{0}", returnMessageInfo.Callee);
				var possibleTypes = returnMessageInfo.ResultPossibleTypes.ToList();
				await this.methodEntity.PropGraph.DiffPropAsync(possibleTypes, edge, returnMessageInfo.LHS, returnMessageInfo.PropagationKind);
			}

			/// We need to recompute possible calless 
			var effects = await InternalPropagateAsync(returnMessageInfo.PropagationKind);
			Logger.LogS("MethodEntityGrain", "PropagateAsync-return", "End Propagation for {0} ", returnMessageInfo.Caller);

			//if (returnMessageInfo.PropagationKind == PropagationKind.REMOVE_TYPES)
			//{
			//    var invoInfo = from callNode in this.methodEntity.PropGraph.CallNodes
			//                   select this.methodEntity.PropGraph.GetInvocationInfo(callNode);

			//    await this.PopulateCalleesInfo(invoInfo);
			//}

			return effects;
		}

		public async Task<ISet<MethodDescriptor>> GetCalleesAsync(int invocationPosition)
		{
			var invocationNode = this.GetCallSiteByOrdinal(invocationPosition);
			ISet<MethodDescriptor> result;
			var calleesForNode = new HashSet<MethodDescriptor>();
			var invExp = methodEntity.PropGraph.GetInvocationInfo(invocationNode);
			Contract.Assert(invExp != null);
			Contract.Assert(codeProvider != null);
			var calleeResult = await methodEntity.PropGraph.ComputeCalleesForNodeAsync(invExp, codeProvider);
			calleesForNode.UnionWith(calleeResult);
			result = calleesForNode;
			return result;
			// return await CallGraphQueryInterface.GetCalleesAsync(methodEntity, invocationNode, this.codeProvider);
		}

		internal AnalysisCallNode GetCallSiteByOrdinal(int invocationPosition)
		{
			foreach (var callNode in this.methodEntity.PropGraph.CallNodes)
			{
				if (callNode.InMethodPosition == invocationPosition)
				{
					return callNode;
				}
			}

			throw new ArgumentException();
			//return null;
		}

		public Task<int> GetInvocationCountAsync()
		{
			return Task.FromResult(methodEntity.PropGraph.CallNodes.Count());
		}

		private async Task<ISet<ResolvedCallee>> GetPossibleCalleesForMethodCallAsync(PropGraphNodeDescriptor receiver, MethodDescriptor method, IProjectCodeProvider codeProvider)
		{
			var possibleCallees = new HashSet<ResolvedCallee>();

			// TODO: This is not good: one reason is that loads like b = this.f are not working
			// in a method m after call r.m() because only the value of r is passed and not all its structure (fields)

			//if (methodCallInfo.IsConstructor || methodCallInfo.Method.IsStatic)
			//if (methodCallInfo.Method.IsStatic)
			//if (methodCallInfo.Method.IsStatic || !methodCallInfo.Method.IsVirtual)
			if (method.IsStatic)
			{
				// Static method call
				var resolvedCallee = new ResolvedCallee(method);

				possibleCallees.Add(resolvedCallee);
			}
			else if (!method.IsVirtual)
			{
				// Non-virtual method call
				var receiverPossibleTypes = this.GetAllTypes(receiver);

				if (receiverPossibleTypes.Count > 0)
				{
					foreach (var receiverType in receiverPossibleTypes)
					{
						var resolvedCallee = new ResolvedCallee(receiverType, method);

						possibleCallees.Add(resolvedCallee);
					}
				}
			}
			else
			{
				// Instance method call

				//// I need to compute all the callees
				//// In case of a deletion we can discard the deleted callee

				//// If callInfo.ReceiverPossibleTypes == {} it means that some info in missing => we should be conservative and use the instantiated types (RTA) 
				//// TODO: I make this False for testing what happens if we remove this

				//if (conservativeWithTypes && methodCallInfo.ReceiverPossibleTypes.Count == 0)
				//{
				//	// TO-DO: Should I fix the node in the receiver to show that is not loaded. Ideally I should use the declared type. 
				//	// Here I will use the already instantiated types

				//	foreach (var candidateTypeDescriptor in methodCallInfo.InstantiatedTypes)
				//	{
				//		var isSubtype = await codeProvider.IsSubtypeAsync(candidateTypeDescriptor, methodCallInfo.Receiver.Type);

				//		if (isSubtype)
				//		{
				//			methodCallInfo.ReceiverPossibleTypes.Add(candidateTypeDescriptor);
				//		}
				//	}
				//}

				var receiverPossibleTypes = this.GetAllTypes(receiver);

				if (receiverPossibleTypes.Count > 0)
				{
					foreach (var receiverType in receiverPossibleTypes)
					{
						// Given a method m and T find the most accurate implementation wrt to T
						// it can be T.m or the first super class implementing m
						var methodDescriptor = await codeProvider.FindMethodImplementationAsync(method, receiverType);
						var resolvedCallee = new ResolvedCallee(receiverType, methodDescriptor);

						possibleCallees.Add(resolvedCallee);
					}
				}
				//else
				//{
				//    // We don't have any possibleType for the receiver,
				//    // so we just use the receiver's declared type to
				//    // identify the calle method implementation
				//    possibleCallees.Add(methodCallInfo.Method);
				//}
			}

			return possibleCallees;
		}

		private async Task<ISet<ResolvedCallee>> GetPossibleCalleesForDelegateCallAsync(DelegateVariableNode @delegate, IProjectCodeProvider codeProvider)
		{
			var possibleCallees = new HashSet<ResolvedCallee>();
			var possibleDelegateMethods = this.GetPossibleMethodsForDelegate(@delegate);

			foreach (var method in possibleDelegateMethods)
			{
				if (method.IsStatic)
				{
					// Static method call
					var resolvedCallee = new ResolvedCallee(method);

					possibleCallees.Add(resolvedCallee);
				}
				else
				{
					// Instance method call
					var receiverPossibleTypes = this.GetAllTypes(@delegate);

					if (receiverPossibleTypes.Count > 0)
					{
						foreach (var receiverType in receiverPossibleTypes)
						{
							//var aMethod = delegateInstance.FindMethodImplementation(t);
							// Diego: Should I use : codeProvider.FindImplementation(delegateInstance, t);
							var callee = await codeProvider.FindMethodImplementationAsync(method, receiverType);
							var resolvedCallee = new ResolvedCallee(receiverType, callee);

							possibleCallees.Add(resolvedCallee);
						}
					}
					else
					{
						// We don't have any possibleType for the receiver,
						// so we just use the receiver's declared type to
						// identify the callee method implementation

						// if Count is 0, it is a delegate that do not came form an instance variable
						var receiverType = @delegate.Type;
						var resolvedCallee = new ResolvedCallee(receiverType, method);

						possibleCallees.Add(resolvedCallee);
					}
				}
			}

			return possibleCallees;
		}

		private ISet<TypeDescriptor> GetAllTypes(PropGraphNodeDescriptor node)
		{
			var result = new HashSet<TypeDescriptor>();
			var types = GetTypes(node);
			var deletedTypes = GetDeletedTypes(node);

			result.UnionWith(types);
			result.UnionWith(deletedTypes);

			return result;
		}

		private ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor node, PropagationKind prop)
		{
			switch (prop)
			{
				case PropagationKind.ADD_TYPES:
					return GetTypes(node);
				case PropagationKind.REMOVE_TYPES:
					return GetDeletedTypes(node);
				default:
					var msg = string.Format("Unsupported propagation kind: {0}", prop);
					throw new Exception(msg);
			}
		}

		private ISet<TypeDescriptor> GetTypes(PropGraphNodeDescriptor node)
		{
			if (node != null)
			{
				return this.methodEntity.PropGraph.GetTypes(node);
			}
			else
			{
				return new HashSet<TypeDescriptor>();
			}
		}

		private ISet<TypeDescriptor> GetDeletedTypes(PropGraphNodeDescriptor node)
		{
			if (node != null)
			{
				return this.methodEntity.PropGraph.GetDeletedTypes(node);
			}
			else
			{
				return new HashSet<TypeDescriptor>();
			}
		}

		internal ISet<MethodDescriptor> GetPossibleMethodsForDelegate(DelegateVariableNode node)
		{
			return this.methodEntity.PropGraph.GetDelegates(node);
		}

		public Task<IEntity> GetMethodEntityAsync()
		{
			// Contract.Assert(this.methodEntity != null);
			return Task.FromResult<IEntity>(this.methodEntity);
		}

		public async Task<ISet<MethodDescriptor>> GetCalleesAsync()
		{
			var result = new HashSet<MethodDescriptor>();

			foreach (var callNode in methodEntity.PropGraph.CallNodes)
			{
				var callees = await GetCalleesAsync(callNode);
				result.UnionWith(callees);
			}

			return result;
			// return CallGraphQueryInterface.GetCalleesAsync(this.methodEntity, codeProvider);
		}

		public async Task<IDictionary<AnalysisCallNode, ISet<MethodDescriptor>>> GetCalleesInfoAsync()
		{
			var calleesPerEntity = new Dictionary<AnalysisCallNode, ISet<MethodDescriptor>>();

			foreach (var calleeNode in this.methodEntity.PropGraph.CallNodes)
			{
				calleesPerEntity[calleeNode] = await GetCalleesAsync(calleeNode);
			}

			return calleesPerEntity;
			// return CallGraphQueryInterface.GetCalleesInfo(this.methodEntity, this.codeProvider);
		}

		private async Task<ISet<MethodDescriptor>> GetCalleesAsync(AnalysisCallNode node)
		{
			var result = new HashSet<MethodDescriptor>();
			var invExp = methodEntity.PropGraph.GetInvocationInfo(node);

			var calleeResult = await methodEntity.PropGraph.ComputeCalleesForNodeAsync(invExp, codeProvider);

			result.UnionWith(calleeResult);
			return result;
		}

		public Task<bool> IsInitializedAsync()
		{
			return Task.FromResult(this.methodEntity != null);
		}

		public Task<IEnumerable<CallContext>> GetCallersAsync()
		{
			return Task.FromResult(this.methodEntity.Callers.AsEnumerable());
		}

		public Task<IEnumerable<SymbolReference>> GetCallersDeclarationInfoAsync()
		{
			// TODO: BUG! The declaration info should be of the caller.
			//var references = from caller in this.methodEntity.Callers
			//				 select CodeGraphHelper.GetMethodReferenceInfo(caller.CallNode, this.methodEntity.DeclarationInfo);

			//var result = references.ToList().AsEnumerable();
			//return Task.FromResult(result);
			throw new NotImplementedException();
		}

		public Task<IEnumerable<TypeDescriptor>> GetInstantiatedTypesAsync()
		{
			return Task.FromResult(this.methodEntity.InstantiatedTypes.AsEnumerable());
		}

		public Task<SymbolReference> GetDeclarationInfoAsync()
		{
			return Task.FromResult(this.methodEntity.ReferenceInfo);
		}

		public Task<IEnumerable<Annotation>> GetAnnotationsAsync()
		{
			var result = new List<CodeGraphModel.Annotation>();
			//result.Add(this.methodEntity.DeclarationInfo);

			foreach (var callNode in this.methodEntity.PropGraph.CallNodes)
			{
				var invocationInfo = Roslyn.CodeGraphHelper.GetMethodInvocationInfo(this.methodEntity.MethodDescriptor, callNode);
				result.Add(invocationInfo);
			}

			foreach (var anonymousEntity in this.methodEntity.GetAnonymousMethodEntities())
			{
				foreach (var callNode in anonymousEntity.PropGraph.CallNodes)
				{
					var invocationInfo = Roslyn.CodeGraphHelper.GetMethodInvocationInfo(anonymousEntity.MethodDescriptor, callNode);
					invocationInfo.range = CodeGraphHelper.GetAbsoluteRange(invocationInfo.range, anonymousEntity.DeclarationInfo.range);
					result.Add(invocationInfo);
				}
			}

			return Task.FromResult(result.AsEnumerable());
		}

		public Task<PropagationEffects> RemoveMethodAsync()
		{
			var calleesInfo = from callNode in this.methodEntity.PropGraph.CallNodes
							  let calleeInfo = this.methodEntity.PropGraph.GetInvocationInfo(callNode)
							  select calleeInfo.Clone(calleeInfo.PossibleCallees);
			//select calleeInfo;

			var propagationEffects = new PropagationEffects(calleesInfo, true, PropagationKind.REMOVE_TYPES);

			//// The use of ADD_TYPES here is not an error!
			//await this.PopulatePropagationEffectsInfo(propagationEffects, PropagationKind.ADD_TYPES);
			//return propagationEffects;

			// The use of ADD_TYPES here is not an error!
			this.PopulateCallersInfo(propagationEffects.CallersInfo, PropagationKind.ADD_TYPES);
			return Task.FromResult(propagationEffects);
		}

		//public async Task<PropagationEffects> UpdateMethodAsync(ISet<ReturnInfo> callersToUpdate)
		//{
		//	var propagagationEffecs = new PropagationEffects(callersToUpdate);
		//	await this.PopulatePropagationEffectsInfo(propagagationEffecs, PropagationKind.ADD_TYPES);
		//	return propagagationEffecs;
		//}

		public Task UnregisterCallerAsync(CallContext callContext)
		{
			this.methodEntity.RemoveFromCallers(callContext);
			return Task.CompletedTask;
		}

		public Task UseDeclaredTypesForParameters()
		{
			foreach (var parameterNode in this.methodEntity.ParameterNodes)
			{
				this.methodEntity.PropGraph.Add(parameterNode, "declared_type", parameterNode.Type);
			}

			if (this.methodEntity.ThisRef != null)
			{
				this.methodEntity.PropGraph.Add(this.methodEntity.ThisRef, "declared_type", this.methodEntity.ThisRef.Type);
			}

			return Task.CompletedTask;
		}

		public Task<MethodCalleesInfo> FixUnknownCalleesAsync()
		{
			var resolvedCallees = new HashSet<MethodDescriptor>();
			var unknownCallees = new HashSet<MethodDescriptor>();

			var result = methodEntity.PropGraph.FixUnknownCalleesAsync(codeProvider);
			return result;
		}

		//public Task UnregisterCalleeAsync(CallContext callContext)
		//{
		//	var invoInfo = this.methodEntity.PropGraph.GetInvocationInfo(callContext.CallNode);
		//	var receiverTypes = this.GetTypes(invoInfo.Receiver);
		//	//this.methodEntity.PropGraph.CallNodes.Remove(callContext.CallNode);
		//	return TaskDone.Done;
		//}

		//public Task<PropagationEffects> GetMoreEffects()
		//{
		//	return Task.FromResult(this.propagationEffectsToSend.Dequeue());
		//}
	}
}
