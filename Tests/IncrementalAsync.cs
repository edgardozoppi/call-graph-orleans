// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OrleansClient.Analysis;
using System;

namespace Tests
{
	[TestClass]
	public class IncrementalAsyncTests
	{
		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalAsync")]
		public void TestRemoveMethodSimpleCallOnDemandAsync()
		{
			BasicTests.TestRemoveMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalAsync")]
		public void TestUpdateMethodSimpleCallOnDemandAsync()
		{
			BasicTests.TestUpdateMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalAsync")]
		public void TestAddMethodSimpleCallOnDemandAsync()
		{
			BasicTests.TestAddMethodSimpleCall(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalAsync")]
		public void TestUpdateMethodStarOnDemandAsync()
		{
			BasicTests.TestUpdateMethodStar(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalAsync")]
		public void TestAddMethodOverrideOnDemandAsync()
		{
			BasicTests.TestAddMethodOverride(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalAsync")]
		public void TestSoundness1OnDemandAsync()
		{
			BasicTests.TestSoundness1(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalAsync")]
		public void TestSoundness2OnDemandAsync()
		{
			BasicTests.TestSoundness2(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}

		[TestMethod]
		[TestCategory("Soundness")]
		[TestCategory("IncrementalAsync")]
		public void TestSoundness3OnDemandAsync()
		{
			BasicTests.TestSoundness3(AnalysisStrategyKind.ONDEMAND_ASYNC);
		}
	}
}
