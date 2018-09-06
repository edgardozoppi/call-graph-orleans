// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.Providers;
using OrleansInterfaces;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Net;

namespace OrleansClient.Analysis
{
	//public interface IStatsState : IGrainState
	public class StatsState
	{
		public Dictionary<string,Dictionary<string, long>> SiloSentMsgs { get; set; }
		public Dictionary<string,Dictionary<string, long>> SiloRecvMsgs { get; set; }
		public Dictionary<string, Dictionary<string, long>> SiloActivations { get; set; }
		public Dictionary<string, Dictionary<string, long>> SiloDeactivations { get; set; }
		public ISet<string> GrainClasses { get; set; }

		public Dictionary<string, long> SiloClientMsgs { get; set; }
		//Dictionary<string, long> SiloLocalRecvMsgs { get; set; }

		//Dictionary<string, long> SiloNetworkSentMsgs { get; set; }
		//Dictionary<string, long> SiloLocalRecvMsgs { get; set; }

	}

	public class LatencyInfo
	{
		public double AccumulattedTimeDifference { get; set; }
		public double MaxLatency  { get; set; }
		public string MaxLatencyMsg { get; set; }
	}

	//[StorageProvider(ProviderName = "AzureTableStore")]
	//public class StatsGrain : Grain<IStatsState>, IStatsGrain
	public class StatsGrain : Grain, IStatsGrain
    {
		private StatsState State;
		private Dictionary<string,long> operationCounter;
		private long messages;
		private LatencyInfo latencyInfo;
		private long memoryUsage;
        private long clientMessages;
        private string lastMessage;
        private int updatesCounter;
        private int worklistSizeCounter;

        private Task WriteStateAsync()
		{
			return Task.CompletedTask;
		}

		private Task ClearStateAsync()
		{
			return Task.CompletedTask;
		}

        public override Task OnActivateAsync()
        {
			this.State = new StatsState();

            Logger.LogVerbose("StatsGrain", "OnActivate","Enter");

			this.State.SiloSentMsgs = new Dictionary<string, Dictionary<string, long>>();
			this.State.SiloRecvMsgs = new Dictionary<string, Dictionary<string, long>>();

			this.State.SiloActivations = new Dictionary<string, Dictionary<string, long>>();
			this.State.SiloDeactivations = new Dictionary<string, Dictionary<string, long>>();

			this.State.GrainClasses  = new HashSet<string>();
            this.State.SiloClientMsgs = new Dictionary<string, long>();

            this.operationCounter = new Dictionary<string,long>();
			this.messages = 0;
            this.clientMessages = 0;
            this.updatesCounter = 0;
            this.worklistSizeCounter = 0;
			this.latencyInfo = new LatencyInfo
			{
				AccumulattedTimeDifference = 0,
				MaxLatency = 0,
				MaxLatencyMsg = ""
			};

			Logger.LogVerbose("StatsGrain", "OnActivate", "Exit");
			return Task.CompletedTask;
		}

		public Task RegisterMessage(string message, string senderAddr, string receiverAddr, bool isClient, double timeDiff)
		{
            // This is wrong: we need to get the memory as a parameter, if not we are measuring the Silo that contains the stat grain
            //var currentMemoryUsage = System.GC.GetTotalMemory(false);
            //if(currentMemoryUsage>this.memoryUsage)
            //{
            //	this.memoryUsage = currentMemoryUsage;
            //}
            this.memoryUsage = 0;

            Logger.LogInfo("StatGrain", "Register Msg", "Addr1:{0} Addr2:{1} {2}" ,senderAddr,receiverAddr,message);
			AddToMap(this.State.SiloSentMsgs, senderAddr, receiverAddr);
			AddToMap(this.State.SiloRecvMsgs, receiverAddr, senderAddr);
     		IncrementCounter(message, this.operationCounter);

			this.messages++;
            if (isClient)
            {
                this.clientMessages++;
                IncrementCounter(receiverAddr, this.State.SiloClientMsgs);

            }
			this.latencyInfo.AccumulattedTimeDifference += timeDiff;
			if(timeDiff>this.latencyInfo.MaxLatency)
			{
				this.latencyInfo.MaxLatency= timeDiff;
				this.latencyInfo.MaxLatencyMsg= message;
			}
            this.lastMessage = message;

			return this.WriteStateAsync();
		}

		public Task RegisterActivation(string grainClass, string calleeAddr)
		{
			AddToMap(this.State.SiloActivations, calleeAddr, grainClass);
			this.State.GrainClasses.Add(grainClass);

			return this.WriteStateAsync();
		}

		public Task RegisterDeactivation(string msg, string calleeAddr)
		{
			AddToMap(this.State.SiloDeactivations, calleeAddr, msg);

			return this.WriteStateAsync();
		}

		private void AddToMap(Dictionary<string, Dictionary<string, long>> silosStatMap, string siloAddr, string key)
		{
			Dictionary<string, long> siloStat = null;
			if(!silosStatMap.TryGetValue(siloAddr,out siloStat))
			{
				siloStat = new Dictionary<string, long>();
				silosStatMap[siloAddr] = siloStat;
			}

			IncrementCounter(key, siloStat);
		}

		private static void IncrementCounter(string key, Dictionary<string, long> counterMap)
		{
			if (counterMap.ContainsKey(key))
			{
				counterMap[key]++;
			}
			else
			{
				counterMap[key] = 1;
			}
		}
		
		public async Task ResetStats()
		{
			await this.ClearStateAsync();

			this.State.SiloSentMsgs.Clear();
			this.State.SiloRecvMsgs.Clear();
			this.State.SiloActivations.Clear();
			this.State.SiloDeactivations.Clear();
            this.State.SiloClientMsgs.Clear();
			this.operationCounter.Clear();
			this.State.GrainClasses.Clear();
            this.messages = 0;
            this.clientMessages = 0;
            this.memoryUsage = 0;
            this.operationCounter.Clear();
            this.updatesCounter = 0;

            this.latencyInfo = new LatencyInfo()
            {
                AccumulattedTimeDifference = 0,
                MaxLatency = 0,
                MaxLatencyMsg = ""
            };
			await this.WriteStateAsync();
		}

		public Task<IEnumerable<string>> GetSilos()
		{
			return Task.FromResult(this.State.SiloActivations.Keys.AsEnumerable());
		}

		public Task<Dictionary<string, long>> GetSiloSentMsgs(string siloAddr)
		{
			Dictionary<string, long> result;
			if(!this.State.SiloSentMsgs.TryGetValue(siloAddr, out result))
			{
				result = new Dictionary<string, long>();
			}
			return Task.FromResult(result);
		}
		public Task<Dictionary<string, long>> GetSiloReceivedMsgs(string siloAddr)
		{
			Dictionary<string, long> result;
			if (!this.State.SiloRecvMsgs.TryGetValue(siloAddr, out result))
			{
				result = new Dictionary<string, long>();
			}
			return Task.FromResult(result);
		}
		public async Task<long> GetSiloLocalMsgs(string siloAddr)
		{
			var siloSent = await this.GetSiloSentMsgs(siloAddr);
			var total = siloSent.Where(item => item.Key.Equals(siloAddr)).Sum(item => item.Value);
			return total;
		}
		public async Task<long> GetSiloNetworkSentMsgs(string siloAddr)
		{
			var siloSent = await GetSiloSentMsgs(siloAddr);
			var total = siloSent.Where(item => !item.Key.Equals(siloAddr)).Sum(item => item.Value);
			return total;
		}

		public async Task<long> GetSiloNetworkReceivedMsgs(string siloAddr)
		{
			var siloRcv = await GetSiloReceivedMsgs(siloAddr);
			var total = siloRcv.Where(item => !item.Key.Equals(siloAddr)).Sum(item => item.Value);
			return total;
		}

		public async Task<long> GetTotalSentMsgs(string siloAddr)
		{
			var siloStat = await GetSiloSentMsgs(siloAddr);
			var total = siloStat.Sum(item => item.Value); ;
			return total;
		}
		public async Task<long> GetTotalReceivedMsgs(string siloAddr)
		{
			var siloStat = await GetSiloReceivedMsgs(siloAddr);
			var total = siloStat.Sum(item => item.Value);
			return total;
		}

        public  Task<long> GetTotalClientMsgsPerSilo(string siloAddr)
        {
            var total = 0L;
            if(!this.State.SiloClientMsgs.TryGetValue(siloAddr, out total))
            {
                total = 0;
            }
            return Task.FromResult(total);
        }


        public async Task<long> GetActivations(string grainClass)
		{
			return await SumSiloPerCategory(grainClass, this.State.SiloActivations);
		}

		public Task<Dictionary<string, long>> GetActivationsPerSilo(string siloAddr)
		{
			return GetDictForSilo(siloAddr,this.State.SiloActivations);
		}

		public Task<Dictionary<string, long>> GetDeactivationsPerSilo(string siloAddr)
		{
			return GetDictForSilo(siloAddr,this.State.SiloDeactivations);
		}

		public async Task<long> GetDeactivations(string grainClass)
		{
			return await SumSiloPerCategory(grainClass, this.State.SiloDeactivations);
		}

		public Task<IEnumerable<string>> GetGrainClasses()
		{
			return Task.FromResult(this.State.GrainClasses.AsEnumerable());
		}

		private async Task<long> SumSiloPerCategory(string grainClass,  Dictionary<string, Dictionary<string, long>> silosStatMap)
		{
			var total = 0L;
			foreach (var siloAddr in this.State.SiloActivations.Keys)
			{
				var siloActivations = await GetDictForSilo(siloAddr, silosStatMap);
				var siloTotal = 0L;
				if (siloActivations.TryGetValue(grainClass, out siloTotal))
				{
					total += siloTotal;
				}
			}
			return total;
		}

		private Task<Dictionary<string, long>> GetDictForSilo(string siloAddr, Dictionary<string, Dictionary<string, long>> silosStatMap)
		{
			Dictionary<string, long> result;
			if (!silosStatMap.TryGetValue(siloAddr, out result))
			{
				result = new Dictionary<string, long>();
			}
			return Task.FromResult(result);
		}

		public Task<double> GetAverageLatency()
		{
			return Task.FromResult(this.latencyInfo.AccumulattedTimeDifference / this.messages);
		}
        public Task<double> GetMaxLatency()
        {
            return Task.FromResult(this.latencyInfo.MaxLatency);
        }
        public Task<string> GetMaxLatencyMsg()
        {
            return Task.FromResult(this.latencyInfo.MaxLatencyMsg);
        }
        public Task<long> GetTotalMessages()
		{
			return Task.FromResult(this.messages);
		}
        public Task<long> GetTotalClientMessages()
        {
            return Task.FromResult(this.clientMessages);
        }

        public Task<long> GetSiloMemoryUsage(string addrString)
		{
			return Task.FromResult(this.memoryUsage);
		}

        public Task<string> GetLastMessage()
        {
            return Task.FromResult(this.lastMessage);
        }
        public Task<Dictionary<string,long>> GetOperationCounters()
        {
            return Task.FromResult(this.operationCounter);
        }
        public Task AddUpdatesCounter(int updates, int wlSize)
        {
            this.updatesCounter += updates;
            this.worklistSizeCounter += wlSize;
            return Task.CompletedTask;
        }
        public Task<Tuple<int,int>> GetUpdatesAndReset()
        {
            var updates = this.updatesCounter;
            var wlSize = this.worklistSizeCounter;
            this.updatesCounter = 0;
            this.worklistSizeCounter = 0; 
            return Task.FromResult(new Tuple<int,int>(updates,wlSize));
        }
    }
}
