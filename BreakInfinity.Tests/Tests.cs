using System;
using System.Text;
using NUnit.Framework;

namespace BreakInfinity.Tests
{
    [TestFixture]
    public class Tests
    {
        public static BigDouble TestValueExponent4 = new BigDouble("1.23456789e1234");
        public static BigDouble TestValueExponent1 = new BigDouble("1.234567893e3");

        [Test]
        public void TestMantissaWithDecimalPlaces()
        {
            Assert.That(TestValueExponent4.MantissaWithDecimalPlaces(0), Is.EqualTo(1));
            Assert.That(TestValueExponent4.MantissaWithDecimalPlaces(4), Is.EqualTo(1.2346));
        }

        [Test]
        public void TestToString()
        {
            Assert.That(TestValueExponent4.ToString(), Is.EqualTo("1.23456789e+1234"));
        }

        [Test]
        public void TestToExponential()
        {
            Assert.That(TestValueExponent4.ToExponential(0), Is.EqualTo("1e+1234"));
            Assert.That(TestValueExponent4.ToExponential(4), Is.EqualTo("1.2346e+1234"));
            Assert.That(TestValueExponent1.ToExponential(0), Is.EqualTo("1e+3"));
            Assert.That(TestValueExponent1.ToExponential(4), Is.EqualTo("1.2346e+3"));
        }

        [Test]
        public void TestToFixed()
        {
            var aLotOfZeroes = new StringBuilder(1226)
                .Insert(0, "0", 1226)
                .ToString();
            Assert.That(TestValueExponent4.ToFixed(0), Is.EqualTo("123456789" + aLotOfZeroes));
            Assert.That(TestValueExponent4.ToFixed(4), Is.EqualTo("123456789" + aLotOfZeroes + ".0000"));
            Assert.That(TestValueExponent1.ToFixed(0), Is.EqualTo("1235"));
            Assert.That(TestValueExponent1.ToFixed(4), Is.EqualTo("1234.5679"));
        }

        [Test]
        public void TestToPrecision()
        {
            Assert.That(TestValueExponent4.ToPrecision(0), Is.EqualTo("0e+1234"));
            Assert.That(TestValueExponent4.ToPrecision(4), Is.EqualTo("1.235e+1234"));
            Assert.That(TestValueExponent1.ToPrecision(0), Is.EqualTo("0e+3"));
            Assert.That(TestValueExponent1.ToPrecision(4), Is.EqualTo("1235"));
        }

        [Test]
        public void TestAdd()
        {
            var addSelf = TestValueExponent4.Add(TestValueExponent4);
            Assert.That(addSelf.Mantissa, Is.EqualTo(TestValueExponent4.Mantissa * 2));
            Assert.That(addSelf.Exponent, Is.EqualTo(TestValueExponent4.Exponent));
            var oneExponentLess = new BigDouble("1.23456789e1233");
            var addOneExponentLess = TestValueExponent4.Add(oneExponentLess);
            var expectedMantissa = TestValueExponent4.Mantissa + oneExponentLess.Mantissa / 10;
            Assert.That(addOneExponentLess.Mantissa, Is.EqualTo(expectedMantissa));
            Assert.That(addOneExponentLess.Exponent, Is.EqualTo(TestValueExponent4.Exponent));
            var aLotSmaller = new BigDouble("1.23456789e123");
            var addALotSmaller = TestValueExponent4.Add(aLotSmaller);
            Assert.That(addALotSmaller.Mantissa, Is.EqualTo(TestValueExponent4.Mantissa));
            Assert.That(addALotSmaller.Exponent, Is.EqualTo(TestValueExponent4.Exponent));
            var negative = new BigDouble("-1.23456789e1234");
            var addNegative = TestValueExponent4.Add(negative);
            Assert.That(addNegative.Mantissa, Is.EqualTo(0));
            Assert.That(addNegative.Exponent, Is.EqualTo(0));
            var addSmallNumbers = new BigDouble(299).Add(new BigDouble(18));
            Assert.That(addSmallNumbers.Mantissa, Is.EqualTo(3.17));
            Assert.That(addSmallNumbers.Exponent, Is.EqualTo(2));
        }

        [Test]
        public void TestCompareTo()
        {
            Assert.That(new BigDouble(299).CompareTo(300), Is.EqualTo(-1));
            Assert.That(new BigDouble(299).CompareTo(new BigDouble(299)), Is.EqualTo(0));
            Assert.That(new BigDouble(299).CompareTo("298"), Is.EqualTo(1));
            Assert.That(new BigDouble(0).CompareTo(0.0), Is.EqualTo(0));
        }

        [Test]
        public void TestEqTolerance()
        {
            Assert.That(new BigDouble(300).EqTolerance(new BigDouble(300)), Is.True);
            Assert.That(new BigDouble(300).EqTolerance(new BigDouble(300.0000005)), Is.False);
            Assert.That(new BigDouble(300).EqTolerance(new BigDouble(300.00000002)), Is.True);
            Assert.That(new BigDouble(300).EqTolerance(new BigDouble(300.0000005), 1e-8), Is.True);
        }

        [Test]
        [Repeat(10000)]
        public void TestDoubleCompatibility()
        {
            var first = BigDouble.RandomDecimalForTesting(100);
            var second = BigDouble.RandomDecimalForTesting(100);
            var aDouble = first.ToDouble();
            var bDouble = second.ToDouble();
            AssertEqual(first + second, aDouble + bDouble);
            AssertEqual(first - second, aDouble - bDouble);
            AssertEqual(first * second, aDouble * bDouble);
            AssertEqual(first / second, aDouble / bDouble);
            Assert.That(first.CompareTo(second), Is.EqualTo(aDouble.CompareTo(bDouble)));
            var smallNumber = BigDouble.RandomDecimalForTesting(2).Abs();
            var smallDouble = Math.Abs(smallNumber.ToDouble());
            AssertEqual(first.Log(smallDouble), Math.Log(aDouble, smallDouble));
            AssertEqual(first.Pow(smallDouble), Math.Pow(aDouble, smallDouble));
        }

        private static void AssertEqual(BigDouble a, double b)
        {
            if (BigDouble.IsFinite(a.ToDouble()) == !BigDouble.IsFinite(b))
            {
                Assert.Fail($"One of the values is finite, other is not: BigDouble {a.ToDouble()}, double {b}");
            }

            if (!BigDouble.IsFinite(b)) return;
            Assert.That(a.EqTolerance(b), Is.True);
        }
    }
}