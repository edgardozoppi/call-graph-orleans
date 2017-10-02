using Orleans;
using OrleansInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansClient.Statistics
{
	[Serializable]
	public class StatsContext
	{
		public bool IsClient { get; set; }
		public string IPAddr { get; set; }
		public DateTime TimeStamp { get; set; }
	}

	public static class StatsHelper
	{
		public const string STATS_GRAIN = "Stats";
		public const string CALLER_ADDR_CONTEXT = "CallerAddr";
		public const string SILO_ADDR = "MyIPAddr";
		public const string IS_ORLEANS_CLIENT = "ISORLEANSCLIENT";

		public static IStatsGrain GetStatGrain(IGrainFactory grainFactory)
		{
			var statGrain = grainFactory.GetGrain<IStatsGrain>(STATS_GRAIN);
			return statGrain;
		}

		public static Task RegisterMsg(string msg, IGrainFactory grainFactory)
		{
#if COMPUTE_STATS
			var statGrain = GetStatGrain(grainFactory);
			var context = RequestContext.Get(StatsHelper.CALLER_ADDR_CONTEXT) as StatsContext;
            if (context != null)
            {
                var callerAddr = context.IPAddr;
                var calleeAddr = GetMyIPAddr();
                var isClient = context.IsClient;
                // TODO: This time diffenrence may not work well if machines are not coordinated. 
                // It may be better to use a stopwatch both in the caller and callee and substract the difference
                // But for that we would need to include a stopwtach in the grain wrapper and another stopwatch in the grain 
                // Then, the stat grain needs to compute the difference. We may need another method and the end of the grain call for that
                // Something like RegisterEndMsg 
                var timeDiff = DateTime.UtcNow.Subtract(context.TimeStamp).TotalMilliseconds;
                return statGrain.RegisterMessage(msg, callerAddr, calleeAddr, isClient, timeDiff);
            }
            else
            {
                return Task.CompletedTask;
            }
#else
			return Task.CompletedTask;
#endif
		}

		public static Task RegisterActivation(string grainClass, IGrainFactory grainFactory)
		{
#if COMPUTE_STATS
			var statGrain = GetStatGrain(grainFactory);
			var calleeAddr = GetMyIPAddr();
			return statGrain.RegisterActivation(grainClass, calleeAddr);
#else
			return Task.CompletedTask;
#endif
		}

		public static Task RegisterPropagationUpdates(int updates, int worklistSize, IGrainFactory grainFactory)
		{
#if COMPUTE_STATS
            /*
			var statGrain = GetStatGrain(grainFactory);
			return statGrain.AddUpdatesCounter(updates, worklistSize);
            */
            return Task.CompletedTask;

#else
			return Task.CompletedTask;
#endif
		}

		public static Task RegisterDeactivation(string grainClass, IGrainFactory grainFactory)
		{
#if COMPUTE_STATS
			var statGrain = GetStatGrain(grainFactory);
			var calleeAddr = GetMyIPAddr();
			return statGrain.RegisterActivation(grainClass, calleeAddr);
#else
			return Task.CompletedTask;
#endif
		}

		public static StatsContext CreateMyIPAddrContext()
		{
			var addr = GetMyIPAddr();
			return new StatsContext()
			{
				IsClient = IsOrleansClient(),
				IPAddr = addr,
				TimeStamp = DateTime.UtcNow,
			};
		}

		private static bool IsOrleansClient()
		{
			var isClient = Environment.GetEnvironmentVariable(IS_ORLEANS_CLIENT);
			if (isClient == null)
			{
				return false;
			}
			return true;
		}

		public static string GetMyIPAddr()
		{
			//IPHostEntry host;
			//string localIP = "?";
			//host = Dns.GetHostEntry(Dns.GetHostName());
			//foreach (IPAddress ip in host.AddressList)
			//{
			//	if (ip.AddressFamily.ToString() == "InterNetwork")
			//	{
			//		localIP = ip.ToString();
			//	}
			//}
			//return localIP;

			//RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["YourInternalEndpoint"].IPEndpoint.Address;

			var myIP = Environment.GetEnvironmentVariable(SILO_ADDR);
			if (myIP == null)
			{
				myIP = "Unknown";
			}
			return myIP;

			//if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
			//{
			//	return null;
			//}

			//var host = Dns.GetHostEntry(Dns.GetHostName());

			//return host
			//	.AddressList
			//	.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
			//	.ToString();
		}
	}
}
