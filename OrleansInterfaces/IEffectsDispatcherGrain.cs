using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Orleans.Streams;
using Common;

namespace OrleansInterfaces
{
	public interface IEffectsDispatcherGrain : IGrainWithGuidKey, IEffectsDispatcher, IAsyncObserver<PropagationEffects>
	{
		Task Subscribe(IAnalysisObserver observer);
		Task Unsubscribe(IAnalysisObserver observer);
		Task ForceDeactivationAsync();
	}
}
