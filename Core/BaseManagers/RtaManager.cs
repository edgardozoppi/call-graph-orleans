using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrleansClient.Roslyn;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Threading;
using Common;

namespace OrleansClient.Analysis
{
    internal class RtaManager : IRtaManager
    {
		protected ISet<TypeDescriptor> instantiatedTypes;

		public RtaManager()
		{
			this.instantiatedTypes = new HashSet<TypeDescriptor>();
		}

        /// <summary>
        /// For RTA analysis
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public Task AddInstantiatedTypesAsync(IEnumerable<TypeDescriptor> types)
        {
            this.instantiatedTypes.UnionWith(types);
			return Task.CompletedTask;
        }

		/// <summary>
		/// For RTA analysis
		/// </summary>
		/// <param name="types"></param>
		/// <returns></returns>
        public Task<ISet<TypeDescriptor>> GetInstantiatedTypesAsync()
        {
			return Task.FromResult(this.instantiatedTypes);
        }
	}
}
