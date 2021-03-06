﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using OrleansInterfaces;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Orleans.Concurrency;
using Common;
using OrleansClient.Statistics;

namespace OrleansClient.Analysis
{
	// TODO: Add instantiated types
	//public interface ISolutionState : IGrainState
    public class SolutionState
    {
		public string SolutionPath { get; set; }
		public string Source { get; set; }
		public string TestName { get; set; }
	}

    //[StorageProvider(ProviderName = "FileStore")]
    //[StorageProvider(ProviderName = "MemoryStore")]
	[StorageProvider(ProviderName = "AzureTableStore")]
	[Reentrant]
	public class SolutionGrain : Grain<SolutionState>, ISolutionGrain, IEntityGrainObserverNotifications
	//public class SolutionGrain : Grain, ISolutionGrain, IEntityGrainObserverNotifications
	{
        [NonSerialized]
        //private ISolutionManager solutionManager;
        private OrleansSolutionManager solutionManager;
		[NonSerialized]
		private int projectsReadyCount;
        [NonSerialized]
        private int messagesCounter = 0;
		//private SolutionState State;

		//private Task WriteStateAsync()
		//{
		//	return TaskDone.Done;
		//}

		//private Task ClearStateAsync()
		//{
		//	return TaskDone.Done;
		//}

		public override async Task OnActivateAsync()
        {
			//this.State = new SolutionState();

			await StatsHelper.RegisterActivation("SolutionGrain", this.GrainFactory);

			Logger.OrleansLogger = this.GetLogger();
            Logger.LogInfo(this.GetLogger(), "SolutionGrain", "OnActivate","Enter");
			
			this.projectsReadyCount = 0;

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
				try
				{
					if (!string.IsNullOrEmpty(this.State.SolutionPath))
					{
						this.solutionManager = await OrleansSolutionManager.CreateFromSolutionAsync(this, this.GrainFactory, this.State.SolutionPath);
					}
					else if (!string.IsNullOrEmpty(this.State.Source))
					{
						this.solutionManager = await OrleansSolutionManager.CreateFromSourceAsync(this, this.GrainFactory, this.State.Source);
					}
					else if (!string.IsNullOrEmpty(this.State.TestName))
					{
						this.solutionManager = await OrleansSolutionManager.CreateFromTestAsync(this, this.GrainFactory, this.State.TestName);
					}

					//if (this.solutionManager != null)
					//{
					//	await this.WaitForAllProjects();
					//}
				}
				catch (Exception ex)
				{
					var inner = ex;
					while (inner is AggregateException) inner = inner.InnerException;

					Logger.LogError(this.GetLogger(), "SolutionGrain", "OnActivate", "Error:\n{0}\nInner:\n{1}", ex, inner);
					throw ex;
				}
			//});

			Logger.LogInfo(this.GetLogger(), "SolutionGrain", "OnActivate", "Exit");
		}

		public override Task OnDeactivateAsync()
		{
			return StatsHelper.RegisterDeactivation("SolutionGrain", this.GrainFactory); 
		}

		#region IEntityGrainObserver

		public async Task StartObservingAsync(IObservableEntityGrain target)
		{
			//Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "StartObserving", "Enter");

			await target.AddObserverAsync(this);

			//Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "StartObserving", "Exit");
		}

		public async Task StopObservingAsync(IObservableEntityGrain target)
		{
			//Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "StopObserving", "Enter");

			await target.RemoveObserverAsync(this);

			//Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "StopObserving", "Exit");
		}

		public void OnStatusChanged(IObservableEntityGrain sender, EntityGrainStatus newState)
		{
			if (newState == EntityGrainStatus.Ready)
			{
				this.projectsReadyCount++;
			}
		}

		private async Task WaitForAllProjects()
		{
			while (this.projectsReadyCount < this.solutionManager.ProjectsCount)
			{
				await Task.Delay(100);
			}
		}

		#endregion

		public async Task SetSolutionPathAsync(string solutionPath)
        {
			await StatsHelper.RegisterMsg("SolutionGrain::SetSolutionPath", this.GrainFactory);

			Logger.LogInfo(this.GetLogger(), "SolutionGrain", "SetSolutionPath", "Enter");

            this.State.SolutionPath = solutionPath;
			this.State.Source = null;
            this.State.TestName = null;

            await this.WriteStateAsync();
			this.projectsReadyCount = 0;

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
				try
				{
					this.solutionManager = await OrleansSolutionManager.CreateFromSolutionAsync(this, this.GrainFactory, this.State.SolutionPath);

					//await this.WaitForAllProjects();
				}
				catch (Exception ex)
				{
					var inner = ex;
					while (inner is AggregateException) inner = inner.InnerException;

					Logger.LogError(this.GetLogger(), "SolutionGrain", "SetSolutionPath", "Error:\n{0}\nInner:\n{1}", ex, inner);
					throw ex;
				}
			//});

			Logger.LogInfo(this.GetLogger(), "SolutionGrain", "SetSolutionPath", "Exit");
		}

        public async Task SetSolutionSourceAsync(string source)
        {
			await StatsHelper.RegisterMsg("SolutionGrain::SetSolutionSource", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionSource", "Enter");

            this.State.Source = source;
			this.State.SolutionPath = null;
            this.State.TestName = null;

            await this.WriteStateAsync();
			this.projectsReadyCount = 0;

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
				try
				{
					this.solutionManager = await OrleansSolutionManager.CreateFromSourceAsync(this, this.GrainFactory, this.State.Source);

					//await this.WaitForAllProjects();
				}
				catch (Exception ex)
				{
					var inner = ex;
					while (inner is AggregateException) inner = inner.InnerException;

					Logger.LogError(this.GetLogger(), "SolutionGrain", "SetSolutionSource", "Error:\n{0}\nInner:\n{1}", ex, inner);
					throw ex;
				}
			//});

            Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionSource", "Exit");
        }

		public async Task SetSolutionFromTestAsync(string testName)
		{
			await StatsHelper.RegisterMsg("SolutionGrain::SetSolutionFromTest", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionFromTest", "Enter");

			this.State.TestName = testName;
			this.State.SolutionPath = null;
            this.State.Source = null;

			await this.WriteStateAsync();
			this.projectsReadyCount = 0;

			//Task.Run(async () =>
			//await Task.Factory.StartNew(async () =>
			//{
				try
				{
					this.solutionManager = await OrleansSolutionManager.CreateFromTestAsync(this, this.GrainFactory, this.State.TestName);

					//await this.WaitForAllProjects();
				}
				catch (Exception ex)
				{
					var inner = ex;
					while (inner is AggregateException) inner = inner.InnerException;

					Logger.LogError(this.GetLogger(), "SolutionGrain", "SetSolutionFromTest", "Error:\n{0}\nInner:\n{1}", ex, inner);
					throw ex;
				}
			//});

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "SetSolutionFromTest", "Exit");
		}

		public Task<IProjectCodeProvider> GetProjectCodeProviderAsync(string assemblyName)
		{
			StatsHelper.RegisterMsg("SolutionGrain::GetProjectCodeProvider", this.GrainFactory);

			return this.solutionManager.GetProjectCodeProviderAsync(assemblyName);
		}

		public Task<IProjectCodeProvider> GetProjectCodeProviderAsync(MethodDescriptor methodDescriptor)
        {
			StatsHelper.RegisterMsg("SolutionGrain::GetProjectCodeProvider", this.GrainFactory);

            return this.solutionManager.GetProjectCodeProviderAsync(methodDescriptor);
        }

		public Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			//StatsHelper.RegisterMsg("SolutionGrain::GetMethodEntity:"+methodDescriptor, this.GrainFactory);
			StatsHelper.RegisterMsg("SolutionGrain::GetMethodEntity", this.GrainFactory);

			return this.solutionManager.GetMethodEntityAsync(methodDescriptor);
		}

		//public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
		//{
		//	StatsHelper.RegisterMsg("SolutionGrain::AddInstantiatedTypes", this.GrainFactory);

		//	return solutionManager.AddInstantiatedTypesAsync(types);
		//}

		//public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
		//{
		//	StatsHelper.RegisterMsg("SolutionGrain::GetInstantiatedTypes", this.GrainFactory);

		//	return this.solutionManager.GetInstantiatedTypesAsync();
		//}

        public async Task<IEnumerable<MethodDescriptor>> GetRootsAsync(AnalysisRootKind rootKind = AnalysisRootKind.Default)
        {
			await StatsHelper.RegisterMsg("SolutionGrain::GetRoots", this.GrainFactory);

			Logger.LogVerbose(this.GetLogger(), "SolutionGrain", "GetRoots", "Enter");
		
			var sw = new Stopwatch();
			sw.Start();
            var roots = await this.solutionManager.GetRootsAsync(rootKind);

			Logger.LogInfo(this.GetLogger(), "SolutionGrain", "GetRoots", "End Time elapsed {0}", sw.Elapsed);
			
			return roots; 
        }

		public Task<IEnumerable<IProjectCodeProvider>> GetProjectCodeProvidersAsync()
		{
			StatsHelper.RegisterMsg("SolutionGrain::GetProjectCodeProviders", this.GrainFactory);

			return this.solutionManager.GetProjectCodeProvidersAsync();
		}

		public Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			StatsHelper.RegisterMsg("SolutionGrain::GetModifications", this.GrainFactory);

			return this.solutionManager.GetModificationsAsync(modifiedDocuments);
        }

		public Task ReloadAsync()
		{
			StatsHelper.RegisterMsg("SolutionGrain::Reload", this.GrainFactory);

			return this.solutionManager.ReloadAsync();
        }

		public Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			StatsHelper.RegisterMsg("SolutionGrain::GetReachableMethodsAsync", this.GrainFactory);

			return this.solutionManager.GetReachableMethodsAsync();
		}

        public Task<int> GetReachableMethodsCountAsync()
        {
            StatsHelper.RegisterMsg("SolutionGrain::GetReachableMethodsCount", this.GrainFactory);

            return this.solutionManager.GetReachableMethodsCountAsync();
        }

        public async Task ForceDeactivationAsync()
		{
			//await StatsHelper.RegisterMsg("SolutionGrain::ForceDeactivation", this.GrainFactory);

			await this.solutionManager.ForceDeactivationOfProjects();
			await this.ClearStateAsync();

			//this.State.SolutionPath = null;
			//this.State.Source = null;
			//this.State.TestName = null;
			//await this.WriteStateAsync();

			this.DeactivateOnIdle();
		}

		public Task<MethodDescriptor> GetMethodDescriptorByIndexAsync(int index)
		{
			//StatsHelper.RegisterMsg("SolutionGrain::GetMethodDescriptorByIndexAsync", this.GrainFactory);

			return this.solutionManager.GetMethodDescriptorByIndexAsync(index);
		}

		public Task<MethodDescriptor> GetRandomMethodAsync()
		{
			//StatsHelper.RegisterMsg("SolutionGrain::GetRandomMethodAsync", this.GrainFactory);

			return this.solutionManager.GetRandomMethodAsync();
		}

		public Task<bool> IsReachableAsync(MethodDescriptor methodDescriptor)
		{
			//StatsHelper.RegisterMsg("SolutionGrain::IsReachable", this.GrainFactory);

			return this.solutionManager.IsReachableAsync(methodDescriptor);
        }

		public Task<EntityGrainStatus> GetStatusAsync()
		{
			var status = this.solutionManager != null &&
						 this.projectsReadyCount == this.solutionManager.ProjectsCount ?
							EntityGrainStatus.Ready :
							EntityGrainStatus.Busy;

			return Task.FromResult(status);
		}

        public Task<int> UpdateCounter(int value)
        {
            this.messagesCounter += value;
            return Task.FromResult(this.messagesCounter);
        }

		// TODO: remove this hack!
		//public Task<IEnumerable<string>> GetDrivesAsync()
		//{
		//	StatsHelper.RegisterMsg("SolutionGrain::GetDrives", this.GrainFactory);

		//	var drivers = DriveInfo.GetDrives().Select(d => d.Name).ToList();
		//	return Task.FromResult(drivers.AsEnumerable());
		//}
	}    
}
