// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using OrleansClient.Analysis;
using SolutionTraversal.CallGraph;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Configuration;
using System.Diagnostics;
using System.Collections.Generic;
using Common;
using Orleans;

namespace Console
{
    class Program
    {
		private AnalysisStrategyKind strategyKind;
		private AppDomain hostDomain;
		private static OrleansHostWrapper hostWrapper;

		public Program(AnalysisStrategyKind strategyKind)
		{
			this.strategyKind = strategyKind;
		}

        static void Main(string[] args)
        {
			//// This is to generate big synthetic tests in x64 to avoid getting OutOfMemory exceptions
			//var a = new OrleansClient.Tests.CallGraphGenerator();
			//a.GenerateSyntheticSolution();
			//Console.WriteLine("Done!");
			//Console.ReadKey();
			//return;

			args = new string[]
			{
				//@"..\..\..\ConsoleApplication1\ConsoleApplication1.sln", "OnDemandAsync"
				//@"..\..\..\ConsoleApplication1\ConsoleApplication1.sln", "OnDemandOrleans"
				//@"..\..\..\TestsSolutions\LongTest2\LongTest2.sln", "OnDemandAsync"
                //@"C:\Users\diegog\Temp\newSynthetic\synthetic-1000\test.sln", "OnDemandOrleans"
				//@"C:\Users\Edgar\Projects\Call-Graph-Builder\TestsSolutions\synthetic-1000\test.sln", "OnDemandOrleans"
                //@"C:\Users\diegog\Temp\newSynthetic\synthetic-1000000\test.sln", "OnDemandOrleans"
				//@"C:\Users\t-edzopp\Desktop\Roslyn\Roslyn.sln", "OnDemandAsync"
				//@"C:\Users\t-edzopp\Desktop\Roslyn\Roslyn.sln", "OnDemandOrleans"
				//@"C:\Users\t-edzopp\Desktop\ArcusClientPrototype\src\ArcusClient\data\Coby\Coby.sln", "OnDemandAsync"
                //@"C:\Users\t-digarb\Source\Coby\Coby.sln", "OnDemandAsync"
                //@"C:\Users\t-edzopp\Desktop\ArcusClientPrototype\src\ArcusClient\data\Coby\Coby.sln", "OnDemandOrleans"
				
				//@"C:\Users\Edgar\Projects\Test projects\de4dot\de4dot.sln", "OnDemandAsync"
				//@"C:\Users\Edgar\Projects\Test projects\RestSharp\RestSharp.sln", "OnDemandAsync"
				//@"C:\Users\Edgar\Projects\Test projects\buildtools\src\BuildTools.sln", "OnDemandAsync"
				//@"C:\Users\Edgar\Projects\Test projects\codeformatter\src\CodeFormatter.sln", "OnDemandAsync" // works!
				//@"C:\Users\Edgar\Projects\Test projects\Json\Src\Newtonsoft.Json.sln", "OnDemandAsync" // with errors
				//@"C:\azure-powershell\src\ResourceManager.ForRefactoringOnly.sln", "OnDemandAsync"
                //@"C:\Users\Edgar\Projects\Call-Graph-Builder\RealSolutions\codeformatter\src\CodeFormatter.sln", "OnDemandAsync"
				//@"C:\Users\Edgar\Projects\Call-Graph-Builder\RealSolutions\ShareX\ShareX.sln", "OnDemandOrleans"
				//@"C:\Users\Edgar\Projects\Test projects\ShareX\ShareX.sln", "OnDemandAsync"
				//@"C:\Users\Edgar\Projects\Test projects\ILSpy\ILSpy.sln", "OnDemandAsync"
				//@"C:\azure-powershell\src\ResourceManager.ForRefactoringOnly.sln", "OnDemandAsync"

				//@"C:\Users\Edgar\Source\Repos\ShareX\ShareX.sln", "OnDemandAsync", "0697738e ee540696 f6559ae2 c8dcc271 73136fbe f98307c6 6ecd7045 b3df7e49 95ff7c91 26513fa0 5c6da480 6a7e2b79 8adfe534 8b163231 265eeaad 58e9db8c"
				//@"C:\Users\Edgar\Source\Repos\ShareX\ShareX.sln", "OnDemandAsync", "0697738e 0bf1f3f8 673324e6 4e721fc8 341784cc aaace76f 538acf30 cd76008e 1a2ea5bd 1c54b0de 5c8f91f5 6d637402 40eda16a c2546a28 0fd54925 6ad97797 b1a5ac3e 22fd8840 2b864b30 408f1d2d 1e0398cc a19e6afe"

				@"C:\Users\Edgar\Source\Repos\ILSpy\ILSpy.sln", "OnDemandAsync", "2726336b c10251d4 6a58560b 8cd31e30 68164194 411e6fac c59ed826 50d55b9b f267a787 1d029b36 30aa3bd4 596fca2b 5b4db073 22eb1a10"
			};

			//// This is to compute solution statistics
			//OrleansClient.Statistics.SolutionStats.ComputeSolutionStats(args[0]);
			//Console.WriteLine("Done!");
			//Console.ReadKey();
			//return;

			if (args.Length >= 2)
			{
				try
				{
					var solutionPath = args[0];
					var strategyName = args[1];
					var commitList = args[2];
					var outputPath = Path.ChangeExtension(solutionPath, ".dgml");
					var strategyKind = SolutionAnalyzer.StringToAnalysisStrategy(strategyName);
					var commits = commitList.Split(' ');
					var program = new Program(strategyKind);

					program.Initialize();
					//var callGraph = program.BuildCallGraph(solutionPath);
					//program.RunAnalysis(solutionPath);
					program.RunIncrementalAnalysis(solutionPath, commits);
					program.Cleanup();

					//callGraph.Save(outputPath);
				}
				catch (Exception ex)
				{
					System.Console.WriteLine(ex);
				}
			}

			System.Console.WriteLine("Done!");
			System.Console.ReadKey();
        }

		private void RunIncrementalAnalysis(string solutionPath, params string[] commits)
		{
			var solutionFolder = Path.GetDirectoryName(solutionPath);
			var solutionFileName = Path.GetFileName(solutionPath);
			var nugetPath = Path.GetFullPath(Path.Combine(solutionFolder, @"..\.nuget\nuget"));
			var initialCommit = commits.FirstOrDefault();

			if (initialCommit != null)
			{
				System.Console.WriteLine(solutionFolder);
				RunAndPrintCommand(solutionFolder, "git", "checkout {0}", initialCommit);
				RunAndPrintCommand(solutionFolder, nugetPath, "restore \"{0}\"", solutionFileName);
			}

			var analyzer = this.RunAnalysis(solutionPath);

			for (var i = 1; i < commits.Length; ++i)
			{
				var commit = commits[i];
				var gitDiffResult = RunCommand(solutionFolder, "git", "diff --name-only {0}", commit);

				RunAndPrintCommand(solutionFolder, "git", "checkout {0}", commit);
				RunAndPrintCommand(solutionFolder, nugetPath, "restore \"{0}\"", solutionFileName);

				var modifiedDocuments = gitDiffResult.Output
													 .Split('\n')
													 .Where(docPath => !string.IsNullOrEmpty(docPath))
													 .Select(docPath => docPath.Replace("/", @"\"))
													 .Select(docPath => Path.Combine(solutionFolder, docPath))
													 .ToList();

				System.Console.WriteLine("Starting incremental analysis...");
				System.Console.WriteLine("Modified documents: {0}", modifiedDocuments.Count());

				analyzer.ApplyModificationsAsync(modifiedDocuments).Wait();

				System.Console.WriteLine("Incremental analysis finish");

				System.Console.WriteLine();
				var color = System.Console.ForegroundColor;
				System.Console.ForegroundColor = ConsoleColor.White;
				var rootMethods = analyzer.SolutionManager.GetRootsAsync().Result;
				System.Console.WriteLine("Root methods={0} ({1})", rootMethods.Count(), analyzer.RootKind);

				var reachableMethodsCount = analyzer.SolutionManager.GetReachableMethodsCountAsync().Result;
				System.Console.WriteLine("Reachable methods={0}", reachableMethodsCount);
				System.Console.ForegroundColor = color;
				System.Console.WriteLine();

				//var callGraph = analyzer.GenerateCallGraphAsync().Result;
				//callGraph.Save(CallGraphPath);
			}
		}

		private CommandResult RunAndPrintCommand(string workingDirectory, string program, string command, params string[] args)
		{
			var result = RunCommand(workingDirectory, program, command, args);

			var color = System.Console.ForegroundColor;
			System.Console.ForegroundColor = ConsoleColor.DarkGreen;
			System.Console.WriteLine(result);
			System.Console.ForegroundColor = color;

			return result;
		}

		private CommandResult RunCommand(string workingDirectory, string program, string command, params string[] args)
		{
			command = string.Format(command, args);
			var result = CommandRunner.Run(program, workingDirectory, command);

			var color = System.Console.ForegroundColor;
			System.Console.ForegroundColor = ConsoleColor.DarkGreen;
			System.Console.WriteLine("{0} {1}", program, command);
			System.Console.ForegroundColor = color;

			return result;
		}

		private SolutionAnalyzer RunAnalysis(string solutionPath)
		{
			System.Console.WriteLine("Analyzing solution...");

			var analyzer = SolutionAnalyzer.CreateFromSolution(GrainClient.Instance, solutionPath);
			analyzer.RootKind = AnalysisRootKind.RootMethods;
			analyzer.AnalyzeAsync(strategyKind).Wait();

			System.Console.WriteLine("Solution analysis finish");

			System.Console.WriteLine();
			var color = System.Console.ForegroundColor;
			System.Console.ForegroundColor = ConsoleColor.White;
			var rootMethods = analyzer.SolutionManager.GetRootsAsync().Result;
			System.Console.WriteLine("Root methods={0} ({1})", rootMethods.Count(), analyzer.RootKind);

			var reachableMethodsCount = analyzer.SolutionManager.GetReachableMethodsCountAsync().Result;
			System.Console.WriteLine("Reachable methods={0}", reachableMethodsCount);
			System.Console.ForegroundColor = color;
			System.Console.WriteLine();

			return analyzer;
		}

		private CallGraph<MethodDescriptor, LocationDescriptor> BuildCallGraph(string solutionPath)
        {
			var analyzer = this.RunAnalysis(solutionPath);

			System.Console.WriteLine("Generating call graph...");

			var callgraph = analyzer.GenerateCallGraphAsync().Result;

			//var reachableMethods = analyzer.SolutionManager.GetReachableMethodsAsync().Result;
			//var reachableMethods2 = reachableMethods;
			//var reachableMethods2 = callgraph.GetReachableMethods();
			//System.Console.WriteLine("Reachable methods={0}", reachableMethods2.Count());

			//// TODO: Remove these lines
			//var newMethods = reachableMethods2.Except(reachableMethods).ToList();
			//var missingMethods = reachableMethods.Except(reachableMethods2).ToList();

			//var allMethods = OrleansClient.Statistics.SolutionStats.ComputeSolutionStats(solutionPath);
			//missingMethods = allMethods.Except(reachableMethods2).ToList();

			//allMethods = allMethods.OrderByDescending(m => m.Name).ToList();
			//missingMethods = missingMethods.OrderByDescending(m => m.Name).ToList();

			return callgraph;
		}

		public void Initialize()
		{
			if (strategyKind != AnalysisStrategyKind.ONDEMAND_ORLEANS) return;
			System.Console.WriteLine("Initializing Orleans silo...");

			var applicationPath = Environment.CurrentDirectory;

			var appDomainSetup = new AppDomainSetup
			{
				AppDomainInitializer = InitSilo,
				ApplicationBase = applicationPath,
				ApplicationName = "CallGraphGeneration",
				AppDomainInitializerArguments = new string[] { },
				ConfigurationFile = "CallGraphGeneration.exe.config"
			};

			// set up the Orleans silo
			hostDomain = AppDomain.CreateDomain("OrleansHost", null, appDomainSetup);

			var xmlConfig = "ClientConfigurationForTesting.xml";
			Contract.Assert(File.Exists(xmlConfig), "Can't find " + xmlConfig);

			GrainClient.Initialize(xmlConfig);
			System.Console.WriteLine("Orleans silo initialized successfully");
		}

		public void Cleanup()
		{
			if (strategyKind != AnalysisStrategyKind.ONDEMAND_ORLEANS) return;

			hostDomain.DoCallBack(ShutdownSilo);
		}

		private static void InitSilo(string[] args)
		{
			hostWrapper = new OrleansHostWrapper();
			hostWrapper.Init();
			var ok = hostWrapper.Run();

			if (!ok)
			{
				System.Console.WriteLine("Failed to initialize Orleans silo");
			}
		}

		private static void ShutdownSilo()
		{
			if (hostWrapper != null)
			{
				hostWrapper.Dispose();
				GC.SuppressFinalize(hostWrapper);
			}
		}
    }
}
