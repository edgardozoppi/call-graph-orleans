using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrleansClient
{
	public enum LogSeverity
	{
		Verbose,
		Info,
		Warning,
		Error
	}

	sealed public class Logger
	{
		private static readonly object syncObject = new object();
		private static Logger instance;

		private string _filename;

		public Logger(string filename)
		{
			_filename = filename;
			File.Delete(filename);
		}

		public static Logger Instance
		{
			get
			{
				lock (syncObject)
				{
					if (instance == null)
					{
						var filename = @"log.txt";
						instance = new Logger(filename);
					}
				}

				return instance;
			}
        }

        public static void LogVerbose(string type, string method, string format, params object[] arguments)
        {
#if DEBUG
			Instance.Log(LogSeverity.Verbose, type, method, format, arguments);
#endif
		}

        public static void LogWarning(string type, string method, string format, params object[] arguments)
        {
#if DEBUG
			Instance.Log(LogSeverity.Warning, type, method, format, arguments);
#endif
		}

		public static void LogError(string type, string method, string format, params object[] arguments)
        {
			Instance.Log(LogSeverity.Error, type, method, format, arguments);
		}

		public static void LogInfo(string type, string method, string format, params object[] arguments)
		{
#if DEBUG
			Instance.Log(LogSeverity.Info, type, method, format, arguments);
#endif
		}

		public static void LogInfo(string format, params object[] arguments)
		{
#if DEBUG
			var message = string.Format(format, arguments);

			Instance.Log(LogSeverity.Info, message);
#endif
		}

		public void Log(LogSeverity severity, string type, string method, string format, params object[] arguments)
		{
#if DEBUG
			var threadId = Thread.CurrentThread.ManagedThreadId;
			var message = string.Format(format, arguments);

			message = string.Format("{0}[{1}] {2}::{3}: {4}", DateTime.UtcNow, threadId, type, method, message);
			Log(severity, message);
#endif
		}

		private void Log(LogSeverity severity, string message)
		{
			if (severity >= LogSeverity.Warning)
			{
				//Debug.WriteLine(message);
				Console.WriteLine(message);
			}

			lock (syncObject)
			{
				try
				{
					using (var writer = File.AppendText(_filename))
					{
						writer.WriteLine(message);
					}
				}
				catch (Exception e)
				{
					Debug.WriteLine("Error writing log");
				}
			}
		}
	}
}
