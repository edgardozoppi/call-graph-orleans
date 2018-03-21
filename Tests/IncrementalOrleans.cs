// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans.TestingHost;
using OrleansClient.Analysis;
using System;

namespace Tests
{
	[TestClass]
	public class IncrementalOrleansTests
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
		[TestCategory("IncrementalOrleans")]
		public void TestRemoveMethodSimpleCallOnDemandOrleans()
		{
			BasicTests.TestRemoveMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalOrleans")]
		public void TestAddMethodSimpleCallOnDemandOrleans()
		{
			BasicTests.TestAddMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalOrleans")]
		public void TestUpdateMethodSimpleCallOnDemandOrleans()
		{
			BasicTests.TestUpdateMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalOrleans")]
		public void TestSoundness1OnDemandOrleans()
		{
			BasicTests.TestSoundness1(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalOrleans")]
		public void TestSoundness2OnDemandOrleans()
		{
			BasicTests.TestSoundness2(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalOrleans")]
		public void TestSoundness3OnDemandOrleans()
		{
			BasicTests.TestSoundness3(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalOrleans")]
		public void TestSoundness4OnDemandOrleans()
		{
			BasicTests.TestSoundness4(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalOrleans")]
		public void TestSoundness5OnDemandOrleans()
		{
			BasicTests.TestSoundness5(AnalysisStrategyKind.ONDEMAND_ORLEANS);
		}
	}
}
