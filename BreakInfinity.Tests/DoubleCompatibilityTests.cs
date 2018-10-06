using System;
using System.Collections.Generic;
using System.Linq;
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
        [TestCaseSource(nameof(FundamentalBinaryTestCases))]
        [TestCaseSource(nameof(GeneralBinaryTestCases))]
        public void Add(BinaryTestCase testCase)
        {
            testCase.AssertEqual((d1, d2) => d1 + d2, (bd1, bd2) => bd1 + bd2);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalBinaryTestCases))]
        [TestCaseSource(nameof(GeneralBinaryTestCases))]
        public void Subtract(BinaryTestCase testCase)
        {
            testCase.AssertEqual((d1, d2) => d1 - d2, (bd1, bd2) => bd1 - bd2);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalBinaryTestCases))]
        [TestCaseSource(nameof(GeneralBinaryTestCases))]
        public void Multiply(BinaryTestCase testCase)
        {
            testCase.AssertEqual((d1, d2) => d1 * d2, (bd1, bd2) => bd1 * bd2);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalBinaryTestCases))]
        [TestCaseSource(nameof(GeneralBinaryTestCases))]
        public void Divide(BinaryTestCase testCase)
        {
            testCase.AssertEqual((d1, d2) => d1 / d2, (bd1, bd2) => bd1 / bd2);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Negate(UnaryTestCase testCase)
        {
            testCase.AssertEqual(d => -d, bd => -bd);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalBinaryTestCases))]
        [TestCaseSource(nameof(GeneralBinaryTestCases))]
        public void CompareTo(BinaryTestCase testCase)
        {
            testCase.AssertEqual((d1, d2) => d1.CompareTo(d2), (bd1, bd2) => bd1.CompareTo(bd2));
        }

        [Test]
        [TestCaseSource(nameof(FundamentalBinaryTestCases))]
        [TestCaseSource(nameof(GeneralBinaryTestCases))]
        public void Log(BinaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Log, (bd1, bd2) => BigDouble.Log(bd1, bd2));
        }

        [Test]
        [TestCaseSource(nameof(FundamentalBinaryTestCases))]
        [TestCaseSource(nameof(GeneralBinaryTestCases))]
        public void Pow(BinaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Pow, BigDouble.Pow);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Abs(UnaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Abs, BigDouble.Abs);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Ceiling(UnaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Ceiling, BigDouble.Ceiling);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Cosh(UnaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Cosh, BigDouble.Cosh);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Exp(UnaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Exp, BigDouble.Exp);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Floor(UnaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Floor, BigDouble.Floor);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Log10(UnaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Log10, d => BigDouble.Log10(d));
        }

        [Test]
        [TestCaseSource(nameof(FundamentalBinaryTestCases))]
        [TestCaseSource(nameof(GeneralBinaryTestCases))]
        public void Max(BinaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Max, BigDouble.Max);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalBinaryTestCases))]
        [TestCaseSource(nameof(GeneralBinaryTestCases))]
        public void Min(BinaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Min, BigDouble.Min);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Round(UnaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Round, BigDouble.Round);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Sign(UnaryTestCase testCase)
        {
            testCase.AssertEqual(d => Math.Sign(d), d => BigDouble.Sign(d));
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Sinh(UnaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Sinh, BigDouble.Sinh);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Tanh(UnaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Tanh, BigDouble.Tanh);
        }

        [Test]
        [TestCaseSource(nameof(FundamentalUnaryTestCases))]
        [TestCaseSource(nameof(GeneralUnaryTestCases))]
        public void Truncate(UnaryTestCase testCase)
        {
            testCase.AssertEqual(Math.Truncate, BigDouble.Truncate);
        }

        private static IEnumerable<TestCaseData> GeneralUnaryTestCases()
        {
            return GeneralTestCaseCombinator.UnaryTestCases;
        }

        private static IEnumerable<TestCaseData> FundamentalUnaryTestCases()
        {
            return FundamentalTestCaseCombinator.UnaryTestCases;
        }

        private static IEnumerable<TestCaseData> GeneralBinaryTestCases()
        {
            return GeneralTestCaseCombinator.BinaryTestCases;
        }

        private static IEnumerable<TestCaseData> FundamentalBinaryTestCases()
        {
            return FundamentalTestCaseCombinator.BinaryTestCases;
        }

        private static readonly TestCaseCombinator GeneralTestCaseCombinator = new TestCaseCombinator()
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
            .Value("Really small negative value", -6.79e-215);

        private static readonly TestCaseCombinator FundamentalTestCaseCombinator = new TestCaseCombinator()
            .Value("0", 0)
            .Value("1", 1)
            .Value("-1", -1)
            .Value("∞", double.PositiveInfinity)
            .Value("-∞", double.NegativeInfinity)
            .Value("NaN", double.NaN);

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

            public IEnumerable<TestCaseData> UnaryTestCases
            {
                get
                {
                    return values
                        .Select(v => new TestCaseData(new UnaryTestCase(v.Value, v.Precision)).SetName(v.Name));
                }
            }

            public IEnumerable<TestCaseData> BinaryTestCases
            {
                get
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
                var testCase = new BinaryTestCase(first.Value, second.Value, Math.Max(first.Precision, second.Precision));
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

        public static void AssertEqual(BigDouble bigDouble, double @double, double precision)
        {
            if (IsOutsideDoubleRange(bigDouble))
            {
                Assert.Ignore("Result is not in range of possible Double values");
            }
            Assert.That(bigDouble.Equals(@double, precision),
                $"Double result {@double} is not equals BigDouble result {bigDouble}");
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

        public class UnaryTestCase
        {
            private readonly double @double;
            private readonly BigDouble bigDouble;
            private readonly double precision;

            public UnaryTestCase(double @double, double precision = BigDouble.Tolerance)
            {
                this.@double = @double;
                bigDouble = @double;
                this.precision = precision;
            }

            public void AssertEqual(Func<double, double> doubleOperation,
                Func<BigDouble, BigDouble> bigDoubleOperation)
            {
                var doubleResult = doubleOperation(@double);
                var bigDoubleResult = bigDoubleOperation(bigDouble);
                DoubleCompatibilityTests.AssertEqual(bigDoubleResult, doubleResult, precision);
            }
        }

        public class BinaryTestCase
        {
            private readonly (double first, double second) doubles;
            private readonly (BigDouble first, BigDouble second) bigDoubles;
            private readonly double precision;

            public BinaryTestCase(double first, double second, double precision = BigDouble.Tolerance)
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
                DoubleCompatibilityTests.AssertEqual(bigDoubleResult, doubleResult, precision);
            }
        }
    }
}
