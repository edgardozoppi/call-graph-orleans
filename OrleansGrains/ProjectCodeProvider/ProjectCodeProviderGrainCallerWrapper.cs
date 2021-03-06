﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Orleans;
using Orleans.Runtime;
using System.Threading;
using OrleansInterfaces;
using CodeGraphModel;
using Common;
using OrleansClient.Statistics;

namespace OrleansClient.Analysis
{
	[Serializable]
	internal class ProjectCodeProviderGrainCallerWrapper : IProjectCodeProviderGrain
	{
		private IProjectCodeProviderGrain providerGrain;

		internal ProjectCodeProviderGrainCallerWrapper(IProjectCodeProviderGrain providerGrain)
		{
			this.providerGrain = providerGrain;
		}

		private void SetRequestContext()
		{
			RequestContext.Set(StatsHelper.CALLER_ADDR_CONTEXT, StatsHelper.CreateMyIPAddrContext());
		}

		public Task<MethodDescriptor> GetOverridenMethodAsync(MethodDescriptor methodDescriptor)
		{
			this.SetRequestContext();
			return providerGrain.GetOverridenMethodAsync(methodDescriptor);
		}

		public Task<PropagationEffects> AddMethodAsync(MethodDescriptor methodToAdd)
		{
			this.SetRequestContext();
			return providerGrain.AddMethodAsync(methodToAdd);
		}

		public Task<IEntity> CreateMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			this.SetRequestContext();
			return providerGrain.CreateMethodEntityAsync(methodDescriptor);
		}

		public Task<MethodDescriptor> FindMethodImplementationAsync(MethodDescriptor methodDescriptor, TypeDescriptor typeDescriptor)
		{
			this.SetRequestContext();
			return providerGrain.FindMethodImplementationAsync(methodDescriptor, typeDescriptor);
		}

		public Task ForceDeactivationAsync()
		{
			this.SetRequestContext();
			return providerGrain.ForceDeactivationAsync();
		}

		public Task<IEnumerable<TypeDescriptor>> GetCompatibleInstantiatedTypesAsync(TypeDescriptor type)
		{
			this.SetRequestContext();
			return providerGrain.GetCompatibleInstantiatedTypesAsync(type);
		}

		public Task<SymbolReference> GetDeclarationInfoAsync(MethodDescriptor methodDescriptor)
		{
			this.SetRequestContext();
			return providerGrain.GetDeclarationInfoAsync(methodDescriptor);
		}

		public Task<IEnumerable<FileResponse>> GetDocumentEntitiesAsync(string documentPath)
		{
			this.SetRequestContext();
			return providerGrain.GetDocumentEntitiesAsync(documentPath);
		}

		public Task<IEnumerable<FileResponse>> GetDocumentsAsync()
		{
			this.SetRequestContext();
			return providerGrain.GetDocumentsAsync();
		}

		public Task<SymbolReference> GetInvocationInfoAsync(CallContext callContext)
		{
			this.SetRequestContext();
			return providerGrain.GetInvocationInfoAsync(callContext);
		}

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			this.SetRequestContext();
			return providerGrain.GetMethodEntityAsync(methodDescriptor);
		}

		public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			this.SetRequestContext();
			return providerGrain.GetModificationsAsync(modifiedDocuments);
		}

		public Task<IEnumerable<MethodDescriptor>> GetRootsAsync(AnalysisRootKind rootKind = AnalysisRootKind.Default)
		{
			this.SetRequestContext();
			return providerGrain.GetRootsAsync(rootKind);
		}

		public Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			this.SetRequestContext();
			return providerGrain.GetReachableMethodsAsync();
		}

        public Task<int> GetReachableMethodsCountAsync()
        {
            this.SetRequestContext();
            return providerGrain.GetReachableMethodsCountAsync();
        }

        public Task<bool> IsSubtypeAsync(TypeDescriptor typeDescriptor1, TypeDescriptor typeDescriptor2)
		{
			this.SetRequestContext();
			return providerGrain.IsSubtypeAsync(typeDescriptor1, typeDescriptor2);
		}

		public Task ReloadAsync()
		{
			this.SetRequestContext();
			return providerGrain.ReloadAsync();
		}

		public Task<PropagationEffects> RemoveMethodAsync(MethodDescriptor methodToUpdate)
		{
			this.SetRequestContext();
			return providerGrain.RemoveMethodAsync(methodToUpdate);
		}

		public Task ReplaceDocumentAsync(string documentPath, string newDocumentPath = null)
		{
			this.SetRequestContext();
			return providerGrain.ReplaceDocumentAsync(documentPath, newDocumentPath);
		}

		public Task ReplaceDocumentSourceAsync(string source, string documentPath)
		{
			this.SetRequestContext();
			return providerGrain.ReplaceDocumentSourceAsync(source, documentPath);
		}

		public Task SetProjectPathAsync(string fullPath)
		{
			this.SetRequestContext();
			return providerGrain.SetProjectPathAsync(fullPath);
		}

		public Task SetProjectSourceAsync(string source)
		{
			this.SetRequestContext();
			return providerGrain.SetProjectSourceAsync(source);
		}

		public Task SetProjectFromTestAsync(string testName)
		{
			this.SetRequestContext();
			return providerGrain.SetProjectFromTestAsync(testName);
		}

		public Task RelocateAsync(string projectPath)
		{
			this.SetRequestContext();
			return providerGrain.RelocateAsync(projectPath);
		}

		public Task<MethodDescriptor> GetRandomMethodAsync()
		{
			//this.SetRequestContext();
			return providerGrain.GetRandomMethodAsync();
		}

		public Task<bool> IsReachableAsync(MethodDescriptor methodDescriptor)
		{
			//this.SetRequestContext();
			return providerGrain.IsReachableAsync(methodDescriptor);
		}

		public Task AddObserverAsync(IEntityGrainObserverNotifications observer)
		{
			return providerGrain.AddObserverAsync(observer);
		}

		public Task RemoveObserverAsync(IEntityGrainObserverNotifications observer)
		{
			return providerGrain.RemoveObserverAsync(observer);
		}

		public IProjectCodeProviderGrain AsReference()
		{
			return providerGrain;
		}
	}
}
