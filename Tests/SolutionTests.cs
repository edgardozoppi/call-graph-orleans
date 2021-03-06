﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using OrleansClient.Analysis;
using Common;

namespace Tests
{
    [TestClass]
    public class SolutionTests
    {
		[TestMethod]
        [TestCategory("Solutions")]
        public void TestSolution1OnDemandAsync()
        {
            TestSolution1(AnalysisStrategyKind.ONDEMAND_ASYNC);
        }

		[TestMethod]
		[TestCategory("Solutions")]
		public void TestRealSolution1OnDemandAsync()
		{
			TestRealSolution1(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
		[TestCategory("Solutions")]
		[TestCategory("IncrementalAsync")]
		public void TestSolution1IncrementalOnDemandAsync()
		{
			TestSolution1Incremental(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		// This is not a test method to run directly, use the above methods instead
		public static void TestSolution1(AnalysisStrategyKind strategy)
        {
			var solutionPath = @"ConsoleApplication1\base\ConsoleApplication1.sln";
			solutionPath = TestUtils.GetTestSolutionPath(solutionPath);

			TestUtils.AnalyzeSolution(solutionPath,
				(s, callgraph) =>
				{
					//callgraph.Save("solution1.dot");
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
					// Fails is I use only Contains with hascode!
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ClassLibrary1", "RemoteClass1", "ClassLibrary1"), "Bar", false), callgraph)); // ClassLibrary
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "LocalClass2", "ConsoleApplication1"), "Bar"), callgraph)); // ConsoleApplication1
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
				},
				strategy);
        }

		// This is not a test method to run directly, use the above methods instead
		public static void TestRealSolution1(AnalysisStrategyKind strategy)
		{
			//var solutionPath = @"buildtools\src\BuildTools.sln";
			var solutionPath = @"codeformatter\src\CodeFormatter.sln";
			//var solutionPath = @"azure-powershell\src\ResourceManager.ForRefactoringOnly.sln";
			solutionPath = TestUtils.GetRealTestSolutionPath(solutionPath);

			TestUtils.AnalyzeSolution(solutionPath,
				(s, callgraph) =>
				{
					var reachableMethods = callgraph.GetReachableMethods();
					Debug.WriteLine("Reachable methods: {0}", reachableMethods.Count);
					Assert.IsTrue(reachableMethods.Count > 0);
				},
				strategy);
		}

		// This is not a test method to run directly, use the above methods instead
		public static void TestSolution1Incremental(AnalysisStrategyKind strategy)
		{
			var solutionPath = @"ConsoleApplication1\base\ConsoleApplication1.sln";
			solutionPath = TestUtils.GetTestSolutionPath(solutionPath);

			var baseFolder = Path.GetDirectoryName(solutionPath);
			var testRootFolder = Directory.GetParent(baseFolder).FullName;
			var currentFolder= Path.Combine(testRootFolder, "current");
			var changesFolder = Path.Combine(testRootFolder, "changes");

			solutionPath = solutionPath.Replace(baseFolder, currentFolder);

			TestUtils.CopyFiles(baseFolder, currentFolder);

			TestUtils.AnalyzeSolution(solutionPath,
				(s, callgraph) =>
				{
					//callgraph.Save("solution1.dot");
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
																																										// Fails is I use only Contains with hascode!
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ClassLibrary1", "RemoteClass1", "ClassLibrary1"), "Bar", false), callgraph)); // ClassLibrary
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "LocalClass2", "ConsoleApplication1"), "Bar"), callgraph)); // ConsoleApplication1
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "CallBar"), callgraph)); // ConsoleApplication1
                    Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test3", "ConsoleApplication1"), "DoTest"), callgraph)); // ConsoleApplication1
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "DoTest"), callgraph));
                },
				(s) =>
				{
					var modifications = TestUtils.CopyFiles(changesFolder, currentFolder);

					s.ApplyModificationsAsync(modifications).Wait();
				},
				(s, callgraph) =>
				{
					Assert.IsTrue(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test4", "ConsoleApplication1"), "DoTest"), callgraph)); 

					Assert.IsFalse(s.IsReachable(new MethodDescriptor(new TypeDescriptor("ConsoleApplication1", "Test", "ConsoleApplication1"), "DoTest"), callgraph));
                },
				strategy);
		}

		[TestMethod]
        [TestCategory("Solutions")]
        public void TestPrecisionVsFindReferences()
        {
			var solutionPath = @"ConsoleApplication1\base\ConsoleApplication1.sln";
			solutionPath = TestUtils.GetTestSolutionPath(solutionPath);

			TestUtils.CompareWithRoslyn(solutionPath);
        }      
    }
}
