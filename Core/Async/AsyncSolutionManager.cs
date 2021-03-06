﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrleansClient.Roslyn;
using Microsoft.CodeAnalysis;
using System.Linq;
using OrleansClient;
using System.Threading;
using Common;

using AssemblyName = System.String;

namespace OrleansClient.Analysis
{
	internal class AsyncSolutionManager : SolutionManager
	{
		private IDictionary<AssemblyName, IProjectCodeProvider> projectProviders;
		private IDictionary<AssemblyName, IProjectCodeProvider> newProjectProviders;

		private AsyncSolutionManager()
		{
			this.projectProviders = new Dictionary<AssemblyName, IProjectCodeProvider>();

			Utils.UseCacheWhenReadingProjects = true;
		}

		public static async Task<AsyncSolutionManager> CreateFromSolutionAsync(string solutionPath)
		{
			var manager = new AsyncSolutionManager();
			await manager.LoadSolutionAsync(solutionPath);
			return manager;
		}

		public static async Task<AsyncSolutionManager> CreateFromSourceAsync(string source)
		{
			var manager = new AsyncSolutionManager();
			await manager.LoadSourceAsync(source);
			return manager;
		}

		public static async Task<AsyncSolutionManager> CreateFromTestAsync(string testName)
		{
			var manager = new AsyncSolutionManager();
			await manager.LoadTestAsync(testName);
			return manager;
		}

		protected override ICollection<AssemblyName> Assemblies
		{
			get { return this.ProjectProviders.Keys; }
		}

		private IDictionary<AssemblyName, IProjectCodeProvider> ProjectProviders
		{
			get { return useNewFieldsVersion ? newProjectProviders : projectProviders; }
		}

		protected override async Task CreateProjectCodeProviderAsync(string projectPath, AssemblyName assemblyName)
		{
			if (this.ProjectProviders.ContainsKey(assemblyName))
			{
				var message = string.Format("Same assembly name used in more than one project: {0}", assemblyName);
				Console.WriteLine(message);
				return;
				//throw new Exception(message);
			}

			var provider = await AsyncProjectCodeProvider.CreateFromProjectAsync(projectPath, this);

            lock (this.ProjectProviders)
            {
				//// TODO: Hack! remove these lines!
				//if (this.ProjectProviders.ContainsKey(assemblyName))
				//{
				//	var oldProvider = this.ProjectProviders[assemblyName];
				//	this.ProjectProviders.Remove(assemblyName);
				//}

				this.ProjectProviders.Add(assemblyName, provider);
            }
		}

		protected override async Task CreateProjectCodeProviderFromSourceAsync(string source, AssemblyName assemblyName)
		{
			if (this.ProjectProviders.ContainsKey(assemblyName))
			{
				var message = string.Format("Same assembly name used in more than one project: {0}", assemblyName);
				Console.WriteLine(message);
				return;
				//throw new Exception(message);
			}

			var provider = await AsyncProjectCodeProvider.CreateFromSourceAsync(source, assemblyName, this);
			this.ProjectProviders.Add(assemblyName, provider);
		}

		protected override async Task CreateProjectCodeProviderFromTestAsync(string testName, AssemblyName assemblyName)
		{
			if (this.ProjectProviders.ContainsKey(assemblyName))
			{
				var message = string.Format("Same assembly name used in more than one project: {0}", assemblyName);
				Console.WriteLine(message);
				return;
				//throw new Exception(message);
			}

			var provider = await AsyncProjectCodeProvider.CreateFromTestAsync(testName, assemblyName, this);
			this.ProjectProviders.Add(assemblyName, provider);
		}

		public override Task<IProjectCodeProvider> GetProjectCodeProviderAsync(AssemblyName assemblyName)
		{
			IProjectCodeProvider provider = null;

			if (!this.ProjectProviders.TryGetValue(assemblyName, out provider))
			{
				provider = this.GetDummyProjectCodeProvider();
				this.ProjectProviders.Add(assemblyName, provider);
			}

			return Task.FromResult(provider);
		}

		private IProjectCodeProvider GetDummyProjectCodeProvider()
		{
			var provider = new AsyncDummyProjectCodeProvider();
			return provider;
		}

		public override async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			var projectProvider = await this.GetProjectCodeProviderAsync(methodDescriptor);
			var methodEntity = await projectProvider.GetMethodEntityAsync(methodDescriptor);

			return methodEntity;
		}

		public override Task<IEnumerable<MethodModification>> GetModificationsAsync(IEnumerable<string> modifiedDocuments)
		{
			this.newProjectProviders = new Dictionary<AssemblyName, IProjectCodeProvider>(this.ProjectProviders);
			return base.GetModificationsAsync(modifiedDocuments);
		}

		public override async Task ReloadAsync()
		{
			if (newProjectProviders != null)
			{
				this.projectProviders = newProjectProviders;
				this.newProjectProviders = null;
			}

			await base.ReloadAsync();
		}
    }
}
