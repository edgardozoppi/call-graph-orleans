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


				// 100 commits OK
				@"C:\Users\Edgar\Repositories\ShareX\ShareX.sln", "OnDemandAsync", "3841eacd 441dc0bf cb6cc4a2 a9cb5a23 136ef977 4f6e969c 18450108 1219ef78 688ea9d6 26a32d2d 691d1e36 1a9205f9 0d6af0b3 204d6c2f 635ea7a2 b4362c09 38773f06 50495242 7fc5c515 89820bad 295749d7 c2736389 10d7476a 2fc7163e b01e7aa0 80ee2742 718dd711 de66d1e9 15e06f1f 4dccf81b 6bc7e0c4 5a03cad9 e19c60f8 3c8f5a1f 2d583800 0d524dca 7b306667 4b432f17 d653c02b 23fe2271 26fbf92f 0e76707a d98060f1 bbf00ed7 f8c57a4d 522b52a8 fa85ab2f aeec1ad4 815cc3ee a600de8e 096ab1c7 1c84e730 fddd843f 6d324d3f eee34635 27398925 9a3f8725 77de435a cc6df456 0b94f69b b3640f6f 1191f1b2 6f4854b4 ec034076 da2c9844 f7eda15a 08d91a25 cb91274d 240a8ceb 46e86d91 8dbbcad2 757e4b0e 468bccbc f5175e89 8131eeb7 009ee9d2 b4abf36b b2cbc035 6dd07cb8 2f15401d 2a5370c1 48681af9 0c36f823 6ee3e305 1a9888e0 1f593181 3741435f a51cdefb 226ccd9a 3e9d67dc cadd9c7f 4ea6ce88 308e20be 2967ef26 fefca5ee e802b050 6ef221a6 ce32d0bb 1bbd68c0 a19e6afe"

				// commit 191d2946: Changing top 5 methods with max callees.
				//@"C:\Users\Edgar\Repositories\ShareX\ShareX.sln", "OnDemandAsync", "3841eacd 191d2946"
				// commit 82cf2b98: Changing top 5 methods with min callees.
				//@"C:\Users\Edgar\Repositories\ShareX\ShareX.sln", "OnDemandAsync", "3841eacd 82cf2b98"

				// commit 6fc2d238: Changing top 5 methods with max callers.
				//@"C:\Users\Edgar\Repositories\ShareX\ShareX.sln", "OnDemandAsync", "3841eacd 6fc2d238"
				// commit 794a06ce: Changing top 5 methods with min callers.
				//@"C:\Users\Edgar\Repositories\ShareX\ShareX.sln", "OnDemandAsync", "3841eacd 794a06ce"

				// commit b6015579: Changing top 5 most influential methods.
				//@"C:\Users\Edgar\Repositories\ShareX\ShareX.sln", "OnDemandAsync", "3841eacd b6015579"
				// commit 217f7eee: Changing top 5 less influential methods.
				//@"C:\Users\Edgar\Repositories\ShareX\ShareX.sln", "OnDemandAsync", "3841eacd 217f7eee"
				

				//@"C:\Users\Edgar\Source\Repos\ILSpy\ILSpy.sln", "OnDemandAsync", "2726336b c10251d4 6a58560b 8cd31e30 68164194 411e6fac c59ed826 50d55b9b f267a787 1d029b36 30aa3bd4 596fca2b 5b4db073 22eb1a10"
				//@"C:\Users\Edgar\Source\Repos\ILSpy\ILSpy.sln", "OnDemandAsync", "c10251d4 6a58560b 8cd31e30 68164194 411e6fac c59ed826 50d55b9b f267a787 1d029b36 30aa3bd4 596fca2b 5b4db073 22eb1a10"


				// 100 commits OK
				//@"C:\Users\Edgar\Source\Repos\ILSpy\ILSpy.sln", "OnDemandAsync", "3d1d452e 2663416f 2a6d359e ec8fc657 30528cee 907aa62c 1d92df77 22520841 6a8908aa d92ba271 1f79b77c 43a2c9d0 b6cd1f3c 23817924 421ad617 14815abd 00b01777 5b1a540d adb8a987 e6b889ac 6774b3c3 385048f3 7689e99e e9740022 4eaafc7e 12e3ac81 ab4b9492 054f6a11 0e118f09 9dec6c80 f55a9301 ce023313 d5366140 d1026c46 36d61db3 a6fc52a6 11bb3060 4a13491f c0e07679 267c69cc 0fade5cb 15b776fa 5150cdce bedff74b 20c450c0 dd485b97 dfe70d53 4eb5e826 7273fe58 b2ef367c 63666813 19c819cd 51a97862 fe1b9dce 5bbec7df d50695c2 94d1d76e 760e02a5 0366ba18 507f8455 4ac1c2d0 db24116b 85bab79e 7e52b622 4c5f3839 4fa22d6c 5e79cf22 8d2116de 0d318eef b558f0c0 5bedb80a 284ddfad 19800c3b 53b2a70a 7eb557bf f51a8b98 557c5a50 1ce8349d 840ec04f 88887b7c d8a2d41e a8cdfc3d fa2f8ec2 c590d282 f6c60a62 0524b4a3 b3590ec2 5530f7ec b254ff66 c0582cf3 fc8825d8 6702488a ed3d4aba e871f7c0 66dee6c6 6343ab7c d3904598 64bd447d d0f9b567 68164194"
				//@"C:\Users\Edgar\Source\Repos\ILSpy\ILSpy.sln", "OnDemandAsync", "dde96674 92e8deaf a813dddc 3a89500e 2d26c776 ae75c57e 40819d2b fc45d476 c0effc81 40aaabe4 e921239b ddb6f969 d87a9bf1 ae7dd7c4 a674b4cd ae6c42c9 de6c39c0 2a5546c4 f116d508 51e4577c 2786ce7a bc7032a8 24dfd88b 3d1d452e 2663416f 2a6d359e ec8fc657 30528cee 907aa62c 1d92df77 22520841 6a8908aa d92ba271 1f79b77c 43a2c9d0 b6cd1f3c 23817924 421ad617 14815abd 00b01777 5b1a540d adb8a987 e6b889ac 6774b3c3 385048f3 7689e99e e9740022 4eaafc7e 12e3ac81 ab4b9492 054f6a11 0e118f09 9dec6c80 f55a9301 ce023313 d5366140 d1026c46 36d61db3 a6fc52a6 11bb3060 4a13491f c0e07679 267c69cc 0fade5cb 15b776fa 5150cdce bedff74b 20c450c0 dd485b97 dfe70d53 4eb5e826 7273fe58 b2ef367c 63666813 19c819cd 51a97862 fe1b9dce 5bbec7df d50695c2 94d1d76e 760e02a5 0366ba18 507f8455 4ac1c2d0 db24116b 85bab79e 7e52b622 4c5f3839 4fa22d6c 5e79cf22 8d2116de 0d318eef b558f0c0 5bedb80a 284ddfad 19800c3b 53b2a70a 7eb557bf f51a8b98 557c5a50"
				//@"C:\Users\Edgar\Repositories\ILSpy\ILSpy.sln", "OnDemandAsync", "ea98d8bd 48529053 c4092ac9 cd8c4e03 755fdee3 dde96674 92e8deaf a813dddc 3a89500e 2d26c776 ae75c57e 40819d2b fc45d476 c0effc81 40aaabe4 e921239b ddb6f969 d87a9bf1 ae7dd7c4 a674b4cd ae6c42c9 de6c39c0 2a5546c4 f116d508 51e4577c 2786ce7a bc7032a8 24dfd88b 3d1d452e 2663416f 2a6d359e ec8fc657 30528cee 907aa62c 1d92df77 22520841 6a8908aa d92ba271 1f79b77c 43a2c9d0 b6cd1f3c 23817924 421ad617 14815abd 00b01777 5b1a540d adb8a987 e6b889ac 6774b3c3 385048f3 7689e99e e9740022 4eaafc7e 12e3ac81 ab4b9492 054f6a11 0e118f09 9dec6c80 f55a9301 ce023313 d5366140 d1026c46 36d61db3 a6fc52a6 11bb3060 4a13491f c0e07679 267c69cc 0fade5cb 15b776fa 5150cdce bedff74b 20c450c0 dd485b97 dfe70d53 4eb5e826 7273fe58 b2ef367c 63666813 19c819cd 51a97862 fe1b9dce 5bbec7df d50695c2 94d1d76e 760e02a5 0366ba18 507f8455 4ac1c2d0 db24116b 85bab79e 7e52b622 4c5f3839 4fa22d6c 5e79cf22 8d2116de 0d318eef b558f0c0 5bedb80a 284ddfad"

				// commit 6af87cb5: Changing top 5 most influential methods.
				//@"C:\Users\Edgar\Repositories\ILSpy\ILSpy.sln", "OnDemandAsync", "ea98d8bd 6af87cb5"
				// commit 121406e1: Changing top 5 less influential methods.
				//@"C:\Users\Edgar\Repositories\ILSpy\ILSpy.sln", "OnDemandAsync", "ea98d8bd 121406e1"

				
				//@"C:\Users\Edgar\Repositories\Newtonsoft.Json\Src\Newtonsoft.Json.sln", "OnDemandAsync", "1a67b34d"

				//@"C:\Users\Edgar\Repositories\BenchmarkDotNet\BenchmarkDotNet.sln", "OnDemandAsync", "b4d68e93"

				//@"C:\azure-powershell\src\ResourceManager.ForRefactoringOnly.sln", "OnDemandAsync", "4485b9b4"

				// 100 commits OK
				//@"C:\azure-powershell\src\ResourceManager.ForRefactoringOnly.sln", "OnDemandAsync", "3f2656b6b 4255c69ab 3f92c7ebd bd1cfc24d 572f0bab7 765616a5b bc64d6be2 7e29780d2 708fb9202 9a0ebce33 3a1ce9111 1d57a6571 15fff78a7 d379b3701 b6f9c7ff5 03923b48d 5c1f6698a 55b0fde69 2ae86b83d 1e8e4b27f fdb8e8800 97a1cc774 95293243a 2fab2385f c3c665440 028010407 83140bd9e e523a2c0e a7d4aa05c 5d59da9ca 842678b1f 3c585e74a 361af2b6c 4da3ac532 98534cd7c 04199a4a4 4c05d4df3 709f18a27 25f5fe314 dd85df91b f54b25574 ada39c069 d7f2d2703 2c25a0ee4 f5a1824b0 9f7ddaa1f ff61ac81b c5fe01b06 269c919a4 fecca97d6 ebef27c14 c249497bc 6426d3956 784aeff82 e97364244 9c2229111 b3d641d7e 8a3589d13 f31049de1 893a2262a b83dda628 b2c97d4f4 ce2878326 cba5a0ff8 0db082fc8 f3bbb7dba 4b9699161 dc0ba3987 f3c185b8f ddf6bb097 8f841a098 b6f5de335 507dd0238 3349423f5 b5eeca6c4 563d13524 b98745aa8 15c88e6c3 feb5369a2 a543a174b 1535bf5a4 0e70b9726 0556b08cf 3730a9a17 eadcdf26e cc9860c9b 9e7bce83e 4edfa6a20 01f2ffec8 13f9592e3 ee3e6406e 6586a2bcf 50abf1336 7fec93d44 39fa88e92 7fca2c3ec 85e850219 f87828c89 3c97568a8 4485b9b42"
				//@"C:\azure-powershell\src\ResourceManager.ForRefactoringOnly.sln", "OnDemandAsync", "4a788ac83 85728e1dd 778996829 157095e62 a59a8b3d2 5f2924a27 cbd9c6de0 898f8899d 19db6a3e1 bfe98f08d 0595e8f01 0605f860d 1641ac1e6 3eb1938b9 acb6c53cc 77ee67436 885844e50 98c119a60 57e1fdc88 4270571bc 657c1b8fd 211ba0d82 b30b85736 673459434 284c48881 5599724e1 678d1c74a 21093caa9 5e74fb077 81040fd7b 5dc79acb1 5476ccffe bc1b9baa2 4f5f9a5b6 9f124a3bf b6dbc516f 11ea787d6 8d5cedc8c 4b8dd405a f91e06c45 a5ce1387f 56d97ab51 f2674a664 8d4d9a369 61ea79912 c200843f0 057008923 0e1387b36 108b4494e 701607f83 f9a288629 62647dc72 4e22d6ee9 5c9d426ff 1e6749497 4909ad037 cef53951d 1c7b9410e 80e9e9173 5ce4649d2 1f870bf0d 5c624bca2 11c238239 38f0e664d 74dafb406 ac71d7fe0 69044eaab 4d2f8d7de 93edfa5be 8d44f810c c3eff3585 8da5fe309 5c74dc57c 4b17c277f 50c3fc75a d7ee923f8 8d0528c9f e1653a22e 0873534a4 0716fc9cf bd1cfc24d 7e29780d2 fdb8e8800 97a1cc774 83140bd9e e523a2c0e 842678b1f 25f5fe314 dd85df91b 4b9699161 feb5369a2 a543a174b 3730a9a17 9e7bce83e 01f2ffec8 ee3e6406e 6586a2bcf 85e850219 f87828c89 4485b9b42"

				// commit 17b0fc71: Changing top 5 most influential methods.
				//@"C:\azure-powershell\src\ResourceManager.ForRefactoringOnly.sln", "OnDemandAsync", "4a788ac83 17b0fc71"
				// commit 16b66f72: Changing top 5 less influential methods.
				//@"C:\azure-powershell\src\ResourceManager.ForRefactoringOnly.sln", "OnDemandAsync", "4a788ac83 16b66f72"
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

					using (var logFile = File.CreateText(@"C:\Users\Edgar\Repositories\last_run.txt"))
					{
						System.Console.SetError(logFile);

						program.Initialize();
						//var callGraph = program.BuildCallGraph(solutionPath);
						//program.RunAnalysis(solutionPath);
						//program.RunAnalysis(solutionPath, commits);
						program.RunIncrementalAnalysis(solutionPath, commits);
						program.Cleanup();

						//callGraph.Save(outputPath);
					}
				}
				catch (Exception ex)
				{
					System.Console.WriteLine(ex);
				}
			}

			System.Console.WriteLine("Done!");
			System.Console.ReadKey();
		}

		private void RunAnalysis(string solutionPath, params string[] commits)
		{
			var timer = new Stopwatch();
			var solutionFolder = Path.GetDirectoryName(solutionPath);
			var solutionFileName = Path.GetFileName(solutionPath);
			var repoFolder = solutionFolder;
			var nugetPath = @"..\.nuget\nuget";

			if (solutionFileName == "ResourceManager.ForRefactoringOnly.sln")
			{
				repoFolder = Path.GetDirectoryName(repoFolder);
				nugetPath = Path.Combine("..", nugetPath);
			}

			nugetPath = Path.Combine(solutionFolder, nugetPath);
			nugetPath = Path.GetFullPath(nugetPath);

			System.Console.WriteLine(repoFolder);

			System.Console.Error.WriteLine(solutionFileName);
			System.Console.Error.Write("commit\tmodified documents\tmodified methods\tcompilation time\ttotal time\treachable methods");

			//CleanUpWorkingCopy(solutionFolder);

			for (var i = 0; i < commits.Length; ++i)
			{
				var commit = commits[i];

				System.Console.Error.Flush();
				System.Console.Error.WriteLine();
				System.Console.Error.Write(commit);

				System.Console.WriteLine(">> Analysis {0}: {1}", i, commit);

				RunAndPrintCommand(solutionFolder, "git", "checkout -f {0}", commit);
				//RunAndPrintCommand(solutionFolder, "git", "clean -ffdx");
				RunAndPrintCommand(solutionFolder, "git", "submodule update -f --init");

				if (solutionFileName == "ILSpy.sln")
				{
					var addinPath = Path.Combine(solutionFolder, "ILSpy.AddIn");

					if (Directory.Exists(addinPath))
					{
						Directory.Delete(addinPath, true);
					}
				}

				RunAndPrintCommand(solutionFolder, nugetPath, "restore \"{0}\"", solutionFileName);

				if (solutionFileName == "ResourceManager.ForRefactoringOnly.sln")
				{
					var program = @"C:\Users\Edgar\Repositories\build azure-powershell.bat";
					RunAndPrintCommand(solutionFolder, program, string.Empty);
				}

				this.RunAnalysis(solutionPath);

				/*
				var analyzer = this.RunAnalysis(solutionPath);

				System.Console.WriteLine("Generating call graph...");

				var callgraph = analyzer.GenerateCallGraphAsync().Result;

				System.Console.WriteLine("Sorting methods by callees...");

				var methods = (from node in callgraph.GetNodes()
							   let callees = callgraph.GetCallees(node)
							   let calleesCount = callees.Count()
							   orderby calleesCount descending
							   select new { Method = node, Count = calleesCount })
							   .ToList();

				System.Console.WriteLine("Saving...");
				var fileName = $@"C:\Users\Edgar\Repositories\callees {commit}.txt";

				using (var writter = File.CreateText(fileName))
				{
					foreach (var entry in methods)
					{
						writter.WriteLine("{0}\t{1}", entry.Count, entry.Method);
					}
				}

				System.Console.WriteLine("Sorting methods by callers...");

				methods = null;
				methods = (from node in callgraph.GetNodes()
						   let callers = callgraph.GetCallers(node)
						   let callersCount = callers.Count()
						   orderby callersCount descending
						   select new { Method = node, Count = callersCount })
						   .ToList();

				System.Console.WriteLine("Saving...");
				fileName = $@"C:\Users\Edgar\Repositories\callers {commit}.txt";

				using (var writter = File.CreateText(fileName))
				{
					foreach (var entry in methods)
					{
						writter.WriteLine("{0}\t{1}", entry.Count, entry.Method);
					}
				}
				*/
			}
		}

		private void RunIncrementalAnalysis(string solutionPath, params string[] commits)
		{
			var timer = new Stopwatch();
			var initialCommit = commits.FirstOrDefault();
			var solutionFolder = Path.GetDirectoryName(solutionPath);
			var solutionFileName = Path.GetFileName(solutionPath);
			var repoFolder = solutionFolder;
			var nugetPath = @"..\.nuget\nuget";

			if (solutionFileName == "ResourceManager.ForRefactoringOnly.sln")
			{
				repoFolder = Path.GetDirectoryName(repoFolder);
				nugetPath = Path.Combine("..", nugetPath);
			}

			nugetPath = Path.Combine(solutionFolder, nugetPath);
			nugetPath = Path.GetFullPath(nugetPath);

			if (initialCommit != null)
			{
				System.Console.WriteLine(repoFolder);

				System.Console.Error.WriteLine(solutionFileName);
				System.Console.Error.WriteLine("commit\tmodified documents\tmodified methods\tcompilation time\ttotal time\treachable methods");
				System.Console.Error.Write(initialCommit);

				//CleanUpWorkingCopy(solutionFolder);

				RunAndPrintCommand(solutionFolder, "git", "checkout -f {0}", initialCommit);
				//RunAndPrintCommand(solutionFolder, "git", "clean -ffdx");
				RunAndPrintCommand(solutionFolder, "git", "submodule update -f --init");

				if (solutionFileName == "ILSpy.sln")
				{
					var addinPath = Path.Combine(solutionFolder, "ILSpy.AddIn");

					if (Directory.Exists(addinPath))
					{
						Directory.Delete(addinPath, true);
					}
				}

				RunAndPrintCommand(solutionFolder, nugetPath, "restore \"{0}\"", solutionFileName);

				if (solutionFileName == "ResourceManager.ForRefactoringOnly.sln")
				{
					var program = @"C:\Users\Edgar\Repositories\build azure-powershell.bat";
					RunAndPrintCommand(solutionFolder, program, string.Empty);
				}
			}

			var analyzer = this.RunAnalysis(solutionPath);

			for (var i = 1; i < commits.Length; ++i)
			{
				var commit = commits[i];

				System.Console.Error.Flush();
				System.Console.Error.WriteLine();
				System.Console.Error.Write(commit);

				System.Console.WriteLine(">> Incremental analysis {0}: {1}", i, commit);

				var gitDiffResult = RunCommand(solutionFolder, "git", "diff --name-only {0}", commit);
				var gitDiffResultOutput = gitDiffResult.Output;

				RunAndPrintCommand(solutionFolder, "git", "checkout -f {0}", commit);
				//RunAndPrintCommand(solutionFolder, "git", "clean -ffdx");
				RunAndPrintCommand(solutionFolder, "git", "submodule update -f --init");

				string addinPath = null;

				if (solutionFileName == "ILSpy.sln")
				{
					addinPath = Path.Combine(solutionFolder, "ILSpy.AddIn");

					if (Directory.Exists(addinPath))
					{
						Directory.Delete(addinPath, true);
					}
				}

				RunAndPrintCommand(solutionFolder, nugetPath, "restore \"{0}\"", solutionFileName);

				if (solutionFileName == "ResourceManager.ForRefactoringOnly.sln")
				{
					var program = @"C:\Users\Edgar\Repositories\build azure-powershell.bat";
					RunAndPrintCommand(solutionFolder, program, string.Empty);
				}

				if (solutionFileName == "ILSpy.sln" && commit == "6af87cb5")
				{
					gitDiffResultOutput += "\n" + "NRefactory/ICSharpCode.NRefactory.CSharp/Ast/AstNode.cs" +
										   "\n" + "NRefactory/ICSharpCode.NRefactory.CSharp/Parser/mcs/cs-parser.cs" +
										   "\n" + "NRefactory/ICSharpCode.NRefactory.CSharp/Parser/mcs/location.cs" +
										   "\n" + "NRefactory/ICSharpCode.NRefactory.CSharp/Parser/mcs/report.cs";
				}

				var modifiedDocuments = gitDiffResultOutput
													 .Split('\n')
													 .Where(docPath => !string.IsNullOrEmpty(docPath))
													 .Where(docPath => docPath.EndsWith(".cs"))
													 .Select(docPath => docPath.Replace("/", @"\"))
													 .Select(docPath => Path.Combine(repoFolder, docPath))
													 .ToList();

				if (solutionFileName == "ILSpy.sln" && addinPath != null)
				{
					modifiedDocuments.RemoveAll(docPath => docPath.StartsWith(addinPath));
				}

				System.Console.WriteLine("Starting incremental analysis...");
				System.Console.WriteLine("Modified documents: {0}", modifiedDocuments.Count);

				System.Console.Error.Write("\t{0}", modifiedDocuments.Count);

				timer.Restart();

				analyzer.ApplyModificationsAsync(modifiedDocuments).Wait();

				timer.Stop();

				System.Console.WriteLine("Incremental analysis finish ({0} ms)", timer.ElapsedMilliseconds);

				System.Console.WriteLine();
				var color = System.Console.ForegroundColor;
				System.Console.ForegroundColor = ConsoleColor.White;
				var rootMethods = analyzer.SolutionManager.GetRootsAsync().Result;
				System.Console.WriteLine("Root methods={0} ({1})", rootMethods.Count(), analyzer.RootKind);

				var reachableMethodsCount = analyzer.SolutionManager.GetReachableMethodsCountAsync().Result;
				System.Console.WriteLine("Reachable methods={0}", reachableMethodsCount);
				System.Console.ForegroundColor = color;
				System.Console.WriteLine();

				System.Console.Error.Write("\t{0}", timer.ElapsedMilliseconds);
				System.Console.Error.Write("\t{0}", reachableMethodsCount);

				//var callGraph = analyzer.GenerateCallGraphAsync().Result;
				//callGraph.Save(CallGraphPath);
			}
		}

		private static void CleanUpWorkingCopy(string solutionFolder)
		{
			var directories = Directory.EnumerateDirectories(solutionFolder);

			foreach (var dir in directories)
			{
				if (dir.EndsWith(".git") || dir.EndsWith("packages")) continue;
				Directory.Delete(dir, true);
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

			System.Console.Error.Write("\t\t"); // modified documents, modified methods

			var analyzer = SolutionAnalyzer.CreateFromSolution(GrainClient.Instance, solutionPath);
			analyzer.RootKind = AnalysisRootKind.RootMethods;

			var timer = Stopwatch.StartNew();

			analyzer.AnalyzeAsync(strategyKind).Wait();

			timer.Stop();

			System.Console.WriteLine("Solution analysis finish ({0} ms)", timer.ElapsedMilliseconds);

			System.Console.WriteLine();
			var color = System.Console.ForegroundColor;
			System.Console.ForegroundColor = ConsoleColor.White;
			var rootMethods = analyzer.SolutionManager.GetRootsAsync().Result;
			System.Console.WriteLine("Root methods={0} ({1})", rootMethods.Count(), analyzer.RootKind);

			var reachableMethodsCount = analyzer.SolutionManager.GetReachableMethodsCountAsync().Result;
			System.Console.WriteLine("Reachable methods={0}", reachableMethodsCount);
			System.Console.ForegroundColor = color;
			System.Console.WriteLine();

			System.Console.Error.Write("\t{0}", timer.ElapsedMilliseconds);
			System.Console.Error.Write("\t{0}", reachableMethodsCount);

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
