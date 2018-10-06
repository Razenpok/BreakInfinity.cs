using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BreakInfinity.Tests
{
    [TestFixture]
    public class DoubleCompatibilityTests
    {
        [Test]
        public void SpecialValuesEquality()
        {
            Assert.That(BigDouble.NaN.Equals(double.NaN));
            Assert.That(BigDouble.PositiveInfinity.Equals(double.PositiveInfinity));
            Assert.That(BigDouble.NegativeInfinity.Equals(double.NegativeInfinity));
        }

        [Test]
        [TestCaseSource(nameof(FundamentalTestCases))]
        [TestCaseSource(nameof(GeneralTestCases))]
        public void Add(TestCase testCase)
        {
            testCase.AssertEqual((d1, d2) => d1 + d2, (bd1, bd2) => bd1 + bd2);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalTestCases))]
        [TestCaseSource(nameof(GeneralTestCases))]
        public void Subtract(TestCase testCase)
        {
            testCase.AssertEqual((d1, d2) => d1 - d2, (bd1, bd2) => bd1 - bd2);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalTestCases))]
        [TestCaseSource(nameof(GeneralTestCases))]
        public void Multiply(TestCase testCase)
        {
            testCase.AssertEqual((d1, d2) => d1 * d2, (bd1, bd2) => bd1 * bd2);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalTestCases))]
        [TestCaseSource(nameof(GeneralTestCases))]
        public void Divide(TestCase testCase)
        {
            testCase.AssertEqual((d1, d2) => d1 / d2, (bd1, bd2) => bd1 / bd2);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalTestCases))]
        [TestCaseSource(nameof(GeneralTestCases))]
        public void CompareTo(TestCase testCase)
        {
            testCase.AssertEqual((d1, d2) => d1.CompareTo(d2), (bd1, bd2) => bd1.CompareTo(bd2));
        }

        [Test]
        [TestCaseSource(nameof(FundamentalTestCases))]
        [TestCaseSource(nameof(GeneralTestCases))]
        public void Log(TestCase testCase)
        {
            testCase.AssertEqual(Math.Log, (bd1, bd2) => BigDouble.Log(bd1, bd2));
        }

        [Test]
        [TestCaseSource(nameof(FundamentalTestCases))]
        [TestCaseSource(nameof(GeneralTestCases))]
        public void Pow(TestCase testCase)
        {
            testCase.AssertEqual(Math.Pow, BigDouble.Pow);
        }

        private static IEnumerable<TestCaseData> GeneralTestCases()
        {
            return new TestCaseCombinator()
                .Value("0", 0)
                .Value("Integer", 345)
                .Value("Negative integer", -745)
                .Value("Big integer", 123456789)
                .Value("Big negative integer", -987654321)
                .Value("Small integer", 4)
                .Value("Small negative integer", -5)
                .Value("Big value", 3.7e63)
                .Value("Big negative value", -7.3e36)
                .Value("Really big value", 7.23e222)
                .Value("Really big negative value", -2.23e201)
                .Value("Small value", 5.323e-47)
                .Value("Small negative value", -8.252e-21)
                .Value("Really small value", 1.98e-241)
                .Value("Really small negative value", -6.79e-215)
                .GenerateTestCases();
        }

        private static IEnumerable<TestCaseData> FundamentalTestCases()
        {
            return new TestCaseCombinator()
                .Value("0", 0)
                .Value("1", 1)
                .Value("-1", -1)
                .Value("∞", double.PositiveInfinity)
                .Value("-∞", double.NegativeInfinity)
                .Value("NaN", double.NaN)
                .GenerateTestCases();
        }

        private class TestCaseCombinator
        {
            private readonly List<TestCaseValue> values = new List<TestCaseValue>();

            public TestCaseCombinator Value(string name, double value)
            {
                values.Add(new TestCaseValue(name, value, BigDouble.Tolerance));
                return this;
            }

            public TestCaseCombinator LowPrecisionValue(string name, double value)
            {
                values.Add(new TestCaseValue(name, value, 1E-9));
                return this;
            }

            public IEnumerable<TestCaseData> GenerateTestCases()
            {
                var current = 0;
                while (current < values.Count)
                {
                    for (var i = current; i < values.Count; i++)
                    {
                        foreach (var testCaseData in Permutate(values[current], values[i]))
                        {
                            yield return testCaseData;
                        }
                    }

                    current++;
                }
            }

            private static IEnumerable<TestCaseData> Permutate(TestCaseValue first, TestCaseValue second)
            {
                yield return TestCaseData(first, second);
                if (first != second)
                {
                    yield return TestCaseData(second, first);
                }
            }

            private static TestCaseData TestCaseData(TestCaseValue first, TestCaseValue second)
            {
                var testCase = new TestCase(first.Value, second.Value, Math.Max(first.Precision, second.Precision));
                return new TestCaseData(testCase).SetName($"{first.Name}, {second.Name}");
            }

            private class TestCaseValue
            {
                public string Name { get; }
                public double Value { get; }
                public double Precision { get; }

                public TestCaseValue(string name, double value, double precision)
                {
                    Name = name;
                    Value = value;
                    Precision = precision;
                }
            }
        }

        public class TestCase
        {
            private readonly (double first, double second) doubles;
            private readonly (BigDouble first, BigDouble second) bigDoubles;
            private readonly double precision;

            public TestCase(double first, double second, double precision = BigDouble.Tolerance)
            {
                doubles = (first, second);
                bigDoubles = (first, second);
                this.precision = precision;
            }

            public void AssertEqual(Func<double, double, double> doubleOperation,
                Func<BigDouble, BigDouble, BigDouble> bigDoubleOperation)
            {
                var doubleResult = doubleOperation(doubles.first, doubles.second);
                var bigDoubleResult = bigDoubleOperation(bigDoubles.first, bigDoubles.second);
                if (IsOutsideDoubleRange(bigDoubleResult))
                {
                    Assert.Ignore("Result is not in range of possible Double values");
                }
                Assert.That(bigDoubleResult.Equals(doubleResult, precision),
                    $"Double result {doubleResult} is not equals BigDouble result {bigDoubleResult}");
            }

            private static bool IsOutsideDoubleRange(BigDouble bigDouble)
            {
                if (BigDouble.IsNaN(bigDouble) || BigDouble.IsInfinity(bigDouble))
                {
                    return false;
                }

                return bigDouble.Exponent > Math.Log10(double.MaxValue)
                    || bigDouble.Exponent < Math.Log10(double.Epsilon);
            }
        }
    }
}
