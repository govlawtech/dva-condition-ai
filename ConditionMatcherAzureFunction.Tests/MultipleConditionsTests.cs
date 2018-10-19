using System;
using System.Collections.Generic;
using ConditionMatcherAzureFunction.Interfaces;
using ConditionMatcherAzureFunction.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConditionMatcherAzureFunction.Tests
{
    [TestClass]
    public class MultipleConditionsTests
    {
        private IMultipleConditionIdentifier _underTest = new MultipleConditionIdentifier();

        private List<Tuple<string, int>> _mockLuisResultsMultipleConditions = new List<Tuple<string, int>>()
        {
            new Tuple<string, int>("a", 99),
            new Tuple<string, int>("b", 95),
            new Tuple<string, int>("c", 80)
        };

        private List<Tuple<string, int>> _mockLuisResultsOneCondition = new List<Tuple<string, int>>()
        {
            new Tuple<string, int>("a", 99),
            new Tuple<string, int>("b", 10),
            new Tuple<string, int>("c", 10)
        };


        [TestMethod]
        public void MultipleAiConditions()
        {
            var testData = "depression";
            var result = _underTest.IsPossiblyMultipleCondition(testData, _mockLuisResultsMultipleConditions);
            Assert.IsTrue(result.IsMultipleCondition);
            Console.WriteLine(result.Reason);
        }

        [TestMethod]
        public void AndConjunctionShouldBeFlagged()
        {
            var testData = "depression and anxiety";
            var result = _underTest.IsPossiblyMultipleCondition(testData, new List<Tuple<string, int>>());
            Assert.IsTrue(result.IsMultipleCondition);
            Console.WriteLine(result.Reason);
        }

        [TestMethod]
        public void ShouldNotBeFlagged()
        {
            var testData = "depression";
            var result = _underTest.IsPossiblyMultipleCondition(testData, _mockLuisResultsOneCondition);
            Assert.IsTrue(!result.IsMultipleCondition);
            Console.WriteLine(result.Reason);
        }

        [TestMethod]
        public void ShouldBeFlaggedBecauseMultipleCommas()
        {
            var testData = "depression, anxiety, ptsd";
            var result = _underTest.IsPossiblyMultipleCondition(testData, _mockLuisResultsOneCondition);
            Assert.IsTrue(result.IsMultipleCondition);
            Console.WriteLine(result.Reason);
        }

        [TestMethod]
        public void ShouldNotBeFlaggedBecauseNotConjuntion()
        {
            var testData = "hand problem";
            var result = _underTest.IsPossiblyMultipleCondition(testData, _mockLuisResultsOneCondition);
            Assert.IsTrue(!result.IsMultipleCondition);
        }
    }
}
