using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Orleans.Runtime.Host;
using System.Text;
using System.IO;
using Microsoft.Azure;
using RedDog.Storage.Files;
using Orleans.Runtime.Configuration;

namespace OrleansWorkerRole
{
	public class WorkerRole : RoleEntryPoint
	{
		private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

		private AzureSilo orleansAzureSilo;
		private const string DATA_CONNECTION_STRING_KEY = "DataConnectionString";
		private int instances;

		public override void Run()
		{
			Trace.TraceInformation("OrleansWorkerRole is running");

			try
			{
				this.Run(this.cancellationTokenSource.Token);
			}
			finally
			{
				this.runCompleteEvent.Set();
			}
		}

		public override bool OnStart()
		{
			if (!RoleEnvironment.IsEmulated)
			{
				try
				{
					CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
					var key = storageAccount.Credentials.ExportBase64EncodedKey();
					var storageAccountName = storageAccount.Credentials.AccountName;
					// Mount a drive.
					FilesMappedDrive.Mount("Y:", @"\\" + storageAccountName + @".file.core.windows.net\solutions", storageAccountName, key);
				}
				catch (Exception exc)
				{
					while (exc is AggregateException) exc = exc.InnerException;
					Trace.TraceError("Error trying to mount Azure File {0}", exc.ToString());
					WriteToTempFile(exc.ToString());
				}
			}

			// Set the maximum number of concurrent connections
			//ServicePointManager.DefaultConnectionLimit = 12;

			// For information on handling configuration changes
			// see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

			RoleEnvironment.Changing += RoleEnvironmentOnChanging;

			bool result = base.OnStart();

			Trace.TraceInformation("OrleansWorkerRole has been started");

			return result;
		}

		public override void OnStop()
		{
			Trace.TraceInformation("OrleansWorkerRole is stopping");

			this.cancellationTokenSource.Cancel();
			this.runCompleteEvent.WaitOne();

			if (orleansAzureSilo != null)
			{
				orleansAzureSilo.Stop();
				WriteToTempFile("OrleansWorkerRole has stopped");
			}

			base.OnStop();

			Trace.TraceInformation("OrleansWorkerRole has stopped");
		}

		private void Run(CancellationToken cancellationToken)
		{
			try
			{
				var config = new ClusterConfiguration();
				//config.StandardLoad();

				if (RoleEnvironment.IsEmulated)
				{
					config.LoadFromFile(@"OrleansLocalConfiguration.xml");
				}
				else
				{
					config.LoadFromFile(@"OrleansConfiguration.xml");
				}

				var ipAddr = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["OrleansSiloEndPoint"].IPEndpoint.Address.ToString();
				Environment.SetEnvironmentVariable("MyIPAddr", ipAddr);

				var instanceCount = RoleEnvironment.CurrentRoleInstance.Role.Instances.Count;
				Environment.SetEnvironmentVariable("MyInstanceCount", instanceCount.ToString());

				this.instances = instanceCount;

				// TODO: Delete Orleans Tables
				// To avoid double delete, check for existence
				//CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
				//table = tableClient.GetTableReference("OrleansGrainState");
				//table.DeleteIfExists();
				// Create the table client.
				//CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

				//CloudTable table = tableClient.GetTableReference("OrleansSiloStatistics");
				//table.DeleteIfExists();

				//CloudTable  table = tableClient.GetTableReference("OrleansClientStatistics");
				//table.DeleteIfExists();

				// It is IMPORTANT to start the silo not in OnStart but in Run.
				// Azure may not have the firewalls open yet (on the remote silos) at the OnStart phase.
				orleansAzureSilo = new AzureSilo();
				var ok = orleansAzureSilo.Start(config);

				if (ok)
				{
					Trace.TraceInformation("OrleansWorkerRole-OnStart Orleans silo started ok=" + ok, "Information");
					Trace.TraceInformation("OrleansWorkerRole is running");

					LogAnalysisVariables();

					orleansAzureSilo.Run(); // Call will block until silo is shutdown

					Trace.TraceInformation("OrleansWorkerRole stop running");
					WriteToTempFile("OrleansWorkerRole stop running");
					//SaveErrorToBlob("Orleans Silo stops!");
				}
				else
				{
					Trace.TraceError("Orleans Silo could not start");
					WriteToTempFile("Orleans Silo could not start");
					//SaveErrorToBlob("Orleans Silo could not start");
				}
			}
			catch (Exception ex)
			{
				while (ex is AggregateException) ex = ex.InnerException;
				Trace.TraceError("Error during initialization of OrleansWorkerRole {0}", ex.ToString());
				var excString = ex.ToString();
				WriteToTempFile(excString);
				//SaveErrorToBlob(excString);                
				throw ex;
			}
		}

		/// <summary>
		/// This event is called after configuration changes have been submited to Windows Azure but before they have been applied in this instance
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="RoleEnvironmentChangingEventArgs" /> instance containing the event data.</param>
		private void RoleEnvironmentOnChanging(object sender, RoleEnvironmentChangingEventArgs e)
		{
			// Implements the changes after restarting the role instance
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

		private void SaveErrorToBlob(string excString)
		{
			WriteToTempFile(excString);

			var errorFile = string.Format("error-{0}-{1}", RoleEnvironment.CurrentRoleInstance.Id, DateTime.UtcNow.Ticks);

			var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("DataConnectionString"));
			// Create the blob client.
			var blobClient = storageAccount.CreateCloudBlobClient();

			// Retrieve reference to a previously created container.
			var container = blobClient.GetContainerReference("errors");

			// Retrieve reference to a blob named "myblob".
			var blockBlob = container.GetBlockBlobReference(errorFile);

			using (var s = GenerateStreamFromString(excString))
			{
				blockBlob.UploadFromStream(s);

			}
		}

		private static void WriteToTempFile(string excString)
		{
			var errorFile = string.Format("error-{0}-{1}", RoleEnvironment.CurrentRoleInstance.Id, DateTime.UtcNow.Ticks);

			using (var file = new StreamWriter(@"C:\Temp\" + errorFile + ".txt"))
			{
				file.WriteLine("Logging:" + excString);
			}
		}

		private Stream GenerateStreamFromString(string s)
		{
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream);
			writer.Write(s);
			writer.Flush();
			stream.Position = 0;
			return stream;
		}
	}
}
