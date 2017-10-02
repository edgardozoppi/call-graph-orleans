using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace WebAPI
{
	public class WebRole : RoleEntryPoint
	{
		public override bool OnStart()
		{
			Trace.WriteLine("WebAPI-OnStart");

			// For information on handling configuration changes
			// see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
			RoleEnvironment.Changing += RoleEnvironmentChanging;

			var ok = base.OnStart();

			Trace.WriteLine("WebAPI-OnStart completed with OK=" + ok);
			WriteToTempFile("WebAPI-OnStart completed with OK=" + ok);
			return ok;
		}

		public override void OnStop()
		{
			Trace.WriteLine("WebAPI-OnStop");
			WriteToTempFile("WebAPI-OnStop");

			base.OnStop();
		}

		public override void Run()
		{
			Trace.WriteLine("WebAPI-Run");
			WriteToTempFile("WebAPI-Run");

			try
			{
				LogAnalysisVariables();

				base.Run();
			}
			catch (Exception exc)
			{
				WriteToTempFile("Run() failed with " + exc.ToString());

				Trace.WriteLine("Run() failed with " + exc.ToString());
			}
		}

		private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
		{
			WriteToTempFile("RoleEnvironmentChanging:" + e.Cancel);

			// If a configuration setting is changing
			foreach (RoleEnvironmentConfigurationSettingChange settingChange in e.Changes.Where(x => x is RoleEnvironmentTopologyChange))
			{
				// Set e.Cancel to true to restart this role instance
				e.Cancel = true;
				return;
			}
		}

		private void LogAnalysisVariables()
		{
			var message = new StringBuilder();

			message.AppendFormat("DispatcherInactiveThreshold = {0}\n", Common.AnalysisConstants.DispatcherInactiveThreshold);
			message.AppendFormat("DispatcherIdleThreshold = {0}\n", Common.AnalysisConstants.DispatcherIdleThreshold);
			message.AppendFormat("DispatcherTimerPeriod = {0}\n", Common.AnalysisConstants.DispatcherTimerPeriod);
			message.AppendFormat("WaitForTerminationDelay = {0}\n", Common.AnalysisConstants.WaitForTerminationDelay);
			message.AppendFormat("StreamsPerInstance = {0}\n", Common.AnalysisConstants.StreamsPerInstance);
			message.AppendFormat("InstanceCount = {0}\n", Common.AnalysisConstants.InstanceCount);
			message.AppendFormat("StreamCount = {0}\n", Common.AnalysisConstants.StreamCount);

			WriteToTempFile(message.ToString());
		}

		private static void WriteToTempFile(string excString)
		{
			var errorFile = string.Format("error-{0}-{1}", RoleEnvironment.CurrentRoleInstance.Id, DateTime.UtcNow.Ticks);

			using (StreamWriter file = new StreamWriter(@"C:\Temp\" + errorFile + ".txt"))
			{
				file.WriteLine("Logging:" + excString);
			}
		}
	}
}
