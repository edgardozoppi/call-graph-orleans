using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console
{
	class CommandResult
	{
		public string Error { get; private set; }
		public string Output { get; private set; }

		public CommandResult(string output, string error)
		{
			this.Output = output;
			this.Error = error;
		}

		public override string ToString()
		{
			var separator = string.IsNullOrWhiteSpace(this.Output) || string.IsNullOrWhiteSpace(this.Error) ?
				string.Empty : "\n";

			return string.Format("{0}{1}{2}", this.Output, separator, this.Error);
		}
	}

	static class CommandRunner
	{
		public static CommandResult Run(string programPath, string workingDirectory, string command)
		{
			// This is not needed if git is in the PATH.
			if (programPath == "git")
			{
				programPath = @"C:\Program Files\Git\cmd\git.exe";
			}

			//var programFilesDirectory = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");
			//var outputTempFilePath = Path.GetTempFileName();

			var process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					//FileName = "git.exe",
					//Arguments = "diff --name-only",
					//FileName = "cmd",
					FileName = programPath,
					Arguments = command,
					WorkingDirectory = workingDirectory,
					UseShellExecute = false,
					//RedirectStandardInput = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				}
			};

			process.Start();
			//process.StandardInput.WriteLine();

			var output = process.StandardOutput.ReadToEnd();
			var error = process.StandardError.ReadToEnd();
			process.WaitForExit();

			//var output = File.ReadAllText(outputTempFilePath);
			return new CommandResult(output, error);
		}
	}
}
