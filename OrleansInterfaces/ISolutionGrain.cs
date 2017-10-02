using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Common;

namespace OrleansInterfaces
{
	public interface ISolutionGrain : IGrainWithStringKey, ISolutionManager, IEntityGrainObserver
    {
        Task SetSolutionPathAsync(string solutionPath);
        Task SetSolutionSourceAsync(string solutionSource);
		Task SetSolutionFromTestAsync(string testName); 
		Task ForceDeactivationAsync();
        Task<MethodDescriptor> GetMethodDescriptorByIndexAsync(int index);
		Task<EntityGrainStatus> GetStatusAsync();

		//Task<IEnumerable<string>> GetDrivesAsync();
        Task<int> UpdateCounter(int value);
	}
}
