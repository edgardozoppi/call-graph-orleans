﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans.TestingHost;
using OrleansClient.Analysis;
using System;

namespace Tests
{
    [TestClass]
    public partial class OrleansTests
    {
		[TestInitialize]
		public void TestInitialize()
		{
			TestUtils.TestInitialize();
		}

		[TestCleanup]
		public void TestCleanup()
		{
			TestUtils.TestCleanup();
		}

		[TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestSimpleCallOnDemandOrleans()
        {
            BasicTests.TestSimpleCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

		[TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestRecursionOnDemandOrleans()
        {
            BasicTests.TestRecursion(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestIfOnDemandOrleans()
        {
            BasicTests.TestIf(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestVirtualCallViaSuperClassOnDemandOrleans()
        {
            BasicTests.TestVirtualCallViaSuperClass(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallViaInterfaceOnDemandOrleans()
        {
            BasicTests.TestCallViaInterface(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestForLoopOnDemandOrleans()
        {
            BasicTests.TestForLoop(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestFieldAccessOnDemandOrleans()
        {
            BasicTests.TestFieldAccess(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallStaticDelegateOnDemandOrleans()
        {
            BasicTests.TestCallStaticDelegate(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestCallInterfaceDelegateOnDemandOrleans()
        {
            BasicTests.TestCallInterfaceDelegate(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestClassesWithSameFieldNameOnDemandOrleans()
        {
            BasicTests.TestClassesWithSameFieldName(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestFieldLoadInCalleeOnDemandOrleans()
        {
            BasicTests.TestFieldLoadInCallee(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Soundness")]
        [TestCategory("OnDemandOrleans")]
        public void TestPropertyOnDemandOrleans()
        {
            BasicTests.TestProperty(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("OnDemandOrleans")]
		public void TestArrowMethodBodyOnDemandOrleans()
		{
			BasicTests.TestArrowMethodBody(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("OnDemandOrleans")]
		public void TestLambdaAsArgumentOnDemandOrleans()
		{
			BasicTests.TestLambdaAsArgument(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("OnDemandOrleans")]
		public void TestExtensionMethodCallOnDemandOrleans()
		{
			BasicTests.TestExtensionMethodCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("OnDemandOrleans")]
		public void TestStaticMethodCallOnDemandOrleans()
		{
			BasicTests.TestStaticMethodCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("OnDemandOrleans")]
		public void TestPropertyCallOnDemandOrleans()
		{
			BasicTests.TestPropertyCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("OnDemandOrleans")]
		public void TestInterfaceMethodCallOnDemandOrleans()
		{
			BasicTests.TestInterfaceMethodCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("OnDemandOrleans")]
		public void TestLambdaOnDemandOrleans()
		{
			BasicTests.TestLambda(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("OnDemandOrleans")]
		public void TestNamedParametersOnDemandOrleans()
		{
			BasicTests.TestNamedParameters(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("OnDemandOrleans")]
		public void TestGenericMethodOnDemandOrleans()
		{
			BasicTests.TestGenericMethod(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Generated")]
		public void LongGeneratedTestOrleansAsync1()
		{
			LongTests.LongGeneratedTestAsync1(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

        [TestMethod]
        [TestCategory("Generated")]
        public void LongGeneratedTestOrleansAsync2()
        {
			LongTests.LongGeneratedTestAsync2(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Generated")]
        public void LongGeneratedTestOrleansAsync3()
        {
			LongTests.LongGeneratedTestAsync3(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("VeryLongRunning")]
        public void LongGeneratedTestOrleansAsync4()
        {
			LongTests.LongGeneratedTestAsync4(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

        [TestMethod]
        [TestCategory("Solutions")]
        public void TestSolution1OnDemandOrleans()
        {
			SolutionTests.TestSolution1(AnalysisStrategyKind.ONDEMAND_ORLEANS);
        }

		[TestMethod]
		[TestCategory("Solutions")]
		public void TestRealSolution1OnDemandOrleans()
		{
			SolutionTests.TestRealSolution1(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Solutions")]
		[TestCategory("IncrementalOrleans")]
		public void TestSolution1IncrementalOnDemandOrleans()
		{
			SolutionTests.TestSolution1Incremental(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}
	}
}
