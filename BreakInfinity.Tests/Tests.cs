using System;
using System.Globalization;

namespace BreakInfinity.Tests
{
    public class Tests
    {
        public static void Main()
        {
            Console.WriteLine(new BigDouble("1.23456789e1234").MantissaWithDecimalPlaces(0));
            Console.WriteLine(new BigDouble("1.23456789e1234").MantissaWithDecimalPlaces(4));
            Console.WriteLine("...");
            Console.WriteLine(new BigDouble("1.23456789e1234").ToString());
            Console.WriteLine("...");
            Console.WriteLine(new BigDouble("1.23456789e1234").ToExponential(0));
            Console.WriteLine(new BigDouble("1.23456789e1234").ToExponential(4));
            Console.WriteLine(new BigDouble("1.23456789e3").ToExponential(0));
            Console.WriteLine(new BigDouble("1.23456789e3").ToExponential(4));
            Console.WriteLine("...");
            Console.WriteLine(new BigDouble("1.23456789e1234").ToFixed(0));
            Console.WriteLine(new BigDouble("1.23456789e1234").ToFixed(4));
            Console.WriteLine(new BigDouble("1.23456789e3").ToFixed(0));
            Console.WriteLine(new BigDouble("1.23456789e3").ToFixed(4));
            Console.WriteLine("...");
            Console.WriteLine(new BigDouble("1.23456789e1234").ToPrecision(0));
            Console.WriteLine(new BigDouble("1.23456789e1234").ToPrecision(4));
            Console.WriteLine(new BigDouble("1.23456789e3").ToPrecision(0));
            Console.WriteLine(new BigDouble("1.23456789e3").ToPrecision(4));
            Console.WriteLine("...");
            Console.WriteLine(
                new BigDouble("1.23456789e1234").Add(
                    new BigDouble("1.23456789e1234")));
            Console.WriteLine(
                new BigDouble("1.23456789e1234").Add(
                    new BigDouble("1.23456789e123")));
            Console.WriteLine(
                new BigDouble("1.23456789e1234").Add(
                    new BigDouble("1.23456789e1233")));
            Console.WriteLine(
                new BigDouble("1.23456789e1234").Add(
                    new BigDouble("-1.23456789e1234")));
            Console.WriteLine(new BigDouble(299).Add(new BigDouble(18)));
            Console.WriteLine("...");
            Console.WriteLine(new BigDouble(299).CompareTo(300));
            Console.WriteLine(new BigDouble(299).CompareTo(new BigDouble(299)));
            Console.WriteLine(new BigDouble(299).CompareTo("298"));
            Console.WriteLine(new BigDouble(0).CompareTo(0.0));
            Console.WriteLine("...");
            Console.WriteLine(
                new BigDouble(300).EqTolerance(new BigDouble(300)));
            Console.WriteLine(
                new BigDouble(300).EqTolerance(new BigDouble(300.0000005)));
            Console.WriteLine(
                new BigDouble(300).EqTolerance(new BigDouble(300.00000002)));
            Console.WriteLine(
                new BigDouble(300).EqTolerance(new BigDouble(300.0000005),
                    1e-8));

            for (var i = 0; i < 10000; ++i)
            {
                var a = BigDouble.RandomDecimalForTesting(100);
                var b = BigDouble.RandomDecimalForTesting(100);
                var aDouble = a.ToDouble();
                var bDouble = b.ToDouble();
                var smallNumber = BigDouble.RandomDecimalForTesting(2);
                var smallDouble = smallNumber.ToDouble();
                Assert(a.ToString() + "+" + b.ToString() + "=" + (a + b).ToString(),
                    EqualEnough(a + b, aDouble + bDouble));
                Assert(a.ToString() + "-" + b.ToString() + "=" + (a - b).ToString(),
                    EqualEnough(a - b, aDouble - bDouble));
                Assert(a.ToString() + "*" + b.ToString() + "=" + (a * b).ToString(),
                    EqualEnough(a * b, aDouble * bDouble));
                Assert(a.ToString() + "/" + b.ToString() + "=" + (a / b).ToString(),
                    EqualEnough(a / b, aDouble / bDouble));
                Assert(a.ToString() + " cmp " + b.ToString() + " = " + a.CompareTo(b),
                    a.CompareTo(b) == aDouble.CompareTo(bDouble));
                Assert(a.ToString() + " log " + smallNumber.ToString() + " = " + a.Log(smallDouble),
                    EqualEnough(a.Log(smallDouble), Math.Log(aDouble, smallDouble)));
                Assert(a.ToString() + " pow " + smallNumber.ToString() + " = " + a.Pow(smallDouble).ToString(),
                    EqualEnough(a.Pow(smallDouble), Math.Pow(aDouble, smallDouble)));
            }
        }

        public static bool EqualEnough(BigDouble a, double b)
        {
            try
            {
                return !BigDouble.IsFinite(a.ToDouble()) &&
                       !BigDouble.IsFinite(b) || a.EqTolerance(b) || Math.Abs(a.Exponent) > 300;
            }
            catch (Exception e)
            {
                Console.WriteLine(a.ToString() + ", " + b.ToString(CultureInfo.InvariantCulture) + ", " + e);
                return false;
            }
        }

        public static void Assert(string message, bool result)
        {
            if (!result)
            {
                Console.WriteLine(message);
            }
        }
    }
}