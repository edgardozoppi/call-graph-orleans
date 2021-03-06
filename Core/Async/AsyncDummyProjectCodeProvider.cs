﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using OrleansClient.Analysis;
using System.IO;
using Common;

namespace OrleansClient.Roslyn
{
	public class AsyncDummyProjectCodeProvider : DummyProjectCodeProvider
	{
		private IDictionary<MethodDescriptor, IMethodEntityWithPropagator> methodEntities;

		public AsyncDummyProjectCodeProvider()
        {
			this.methodEntities = new Dictionary<MethodDescriptor, IMethodEntityWithPropagator>();
		}

		public override async Task<IMethodEntityWithPropagator> GetMethodEntityAsync(MethodDescriptor methodDescriptor)
		{
			IMethodEntityWithPropagator result;

			if (!this.methodEntities.TryGetValue(methodDescriptor, out result))
			{
				var methodEntity = await this.CreateMethodEntityAsync(methodDescriptor) as MethodEntity;

				result = new MethodEntityWithPropagator(methodEntity, this);
				this.methodEntities.Add(methodDescriptor, result);
			}

			return result;
		}

		public override Task<IEnumerable<MethodDescriptor>> GetReachableMethodsAsync()
		{
			return Task.FromResult(methodEntities.Keys.AsEnumerable());
		}

        public override Task<int> GetReachableMethodsCountAsync()
        {
            return Task.FromResult(methodEntities.Keys.Count);
        }

		public override Task<MethodDescriptor> GetRandomMethodAsync()
		{
			var random = new Random();
			var randomIndex = random.Next(methodEntities.Count);
			var method = methodEntities.Keys.ElementAt(randomIndex);

			return Task.FromResult(method);
		}

		public override Task<bool> IsReachableAsync(MethodDescriptor methodDescriptor)
		{
			return Task.FromResult(methodEntities.ContainsKey(methodDescriptor));
		}
	}
}
