﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans;
using Common;

namespace OrleansInterfaces
{
	public interface IObservableEntityGrain
	{
		Task AddObserverAsync(IEntityGrainObserverNotifications observer);
		Task RemoveObserverAsync(IEntityGrainObserverNotifications observer);
	}

	public interface IEntityGrainObserver
	{
		Task StartObservingAsync(IObservableEntityGrain target);
		Task StopObservingAsync(IObservableEntityGrain target);
	}

	public interface IEntityGrainObserverNotifications : IGrainObserver
	{
		void OnStatusChanged(IObservableEntityGrain sender, EntityGrainStatus newStatus);
	}

	public interface IAnalysisObserver : IGrainObserver
	{
		void OnEffectsDispatcherStatusChanged(IEffectsDispatcherGrain sender, EffectsDispatcherStatus newStatus);
	}
}
