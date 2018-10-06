using System.Text;
using NUnit.Framework;

namespace BreakInfinity.Tests
{
    [TestFixture]
    public class Tests
    {
        public static BigDouble TestValueExponent4 = BigDouble.Parse("1.23456789e1234");
        public static BigDouble TestValueExponent1 = BigDouble.Parse("1.234567893e3");

        [Test]
        public void TestToString()
        {
            Assert.That(TestValueExponent4.ToString(), Is.EqualTo("1.23456789e+1234"));
        }

        [Test]
        public void TestToExponential()
        {
            Assert.That(TestValueExponent4.ToString("E0"), Is.EqualTo("1e+1234"));
            Assert.That(TestValueExponent4.ToString("E4"), Is.EqualTo("1.2346e+1234"));
            Assert.That(TestValueExponent1.ToString("E0"), Is.EqualTo("1e+3"));
            Assert.That(TestValueExponent1.ToString("E4"), Is.EqualTo("1.2346e+3"));
        }

        [Test]
        public void TestToFixed()
        {
            var aLotOfZeroes = new StringBuilder(1226)
                .Insert(0, "0", 1226)
                .ToString();
            Assert.That(TestValueExponent4.ToString("F0"), Is.EqualTo("123456789" + aLotOfZeroes));
            Assert.That(TestValueExponent4.ToString("F4"), Is.EqualTo("123456789" + aLotOfZeroes + ".0000"));
            Assert.That(TestValueExponent1.ToString("F0"), Is.EqualTo("1235"));
            Assert.That(TestValueExponent1.ToString("F4"), Is.EqualTo("1234.5679"));
        }
    }
}