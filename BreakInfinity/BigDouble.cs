using System;
using System.Globalization;

namespace BreakInfinity
{
    public struct BigDouble : IComparable
    {
        //for example: if two exponents are more than 17 apart, consider adding them together pointless, just return the larger one
        private const int MaxSignificantDigits = 17;

        private const long ExpLimit = long.MaxValue;

        //the largest exponent that can appear in a Double, though not all mantissas are valid here.
        private const long DoubleExpMax = 308;

        //The smallest exponent that can appear in a Double, though not all mantissas are valid here.
        private const long DoubleExpMin = -324;

        //we need this lookup table because Math.pow(10, exponent) when exponent's absolute value is large is slightly inaccurate. you can fix it with the power of math... or just make a lookup table. faster AND simpler
        private static readonly double[] PowersOf10 = new double[632];

        static BigDouble()
        {
            var iActual = 0;
            for (var i = DoubleExpMin + 1; i <= DoubleExpMax; i++)
            {
                PowersOf10[iActual] = double.Parse("1e" + i, CultureInfo.InvariantCulture);
                ++iActual;
            }
        }

        private const int Indexof0Inpowersof10 = 323;

        public BigDouble(double mantissa, long exponent, bool normalize = true)
        {
            if (!normalize || mantissa >= 1 && mantissa < 10 || !IsFinite(mantissa))
            {
                Mantissa = mantissa;
                Exponent = exponent;
            }
            else if (IsZero(mantissa))
            {
                this = Zero;
            }
            else
            {
                var tempExponent = (long)Math.Floor(Math.Log10(Math.Abs(mantissa)));
                Mantissa = mantissa / PowersOf10[tempExponent + Indexof0Inpowersof10];
                Exponent = exponent + tempExponent;
            }
        }

        public BigDouble(BigDouble other)
        {
            Mantissa = other.Mantissa;
            Exponent = other.Exponent;
        }

        public BigDouble(double value)
        {
            //SAFETY: Handle Infinity and NaN in a somewhat meaningful way.
            if (double.IsNaN(value))
            {
                this = NaN;
            }
            else if (double.IsPositiveInfinity(value))
            {
                this = PositiveInfinity;
            }
            else if (double.IsNegativeInfinity(value))
            {
                this = NegativeInfinity;
            }
            else if (IsZero(value))
            {
                this = Zero;
            }
            else
            {
                var exponent = (long) Math.Floor(Math.Log10(Math.Abs(value)));
                double mantissa;
                //SAFETY: handle 5e-324, -5e-324 separately
                if (exponent == DoubleExpMin)
                {
                    mantissa = value * 10 / 1e-323;
                }
                else
                {
                    mantissa = value / PowersOf10[exponent + Indexof0Inpowersof10];
                }

                this = new BigDouble(mantissa, exponent);
            }
        }

        public double Mantissa { get; }

        public long Exponent { get; }

        public static BigDouble Zero { get; } = new BigDouble(0, 0);

        public static BigDouble One { get; } = new BigDouble(1, 0);

        public static BigDouble NaN { get; } = new BigDouble(double.NaN, long.MinValue);

        public static bool IsNaN(BigDouble value)
        {
            return double.IsNaN(value.Mantissa) || value.Exponent == long.MinValue;
        }

        public static BigDouble PositiveInfinity { get; } = new BigDouble(double.PositiveInfinity, 0);

        public static bool IsPositiveInfinity(BigDouble value)
        {
            return double.IsPositiveInfinity(value.Mantissa);
        }

        public static BigDouble NegativeInfinity { get; } = new BigDouble(double.NegativeInfinity, 0);

        public static bool IsNegativeInfinity(BigDouble value)
        {
            return double.IsNegativeInfinity(value.Mantissa);
        }

        public static bool IsInfinity(BigDouble value)
        {
            return double.IsInfinity(value.Mantissa);
        }

        public static BigDouble Parse(string value)
        {
            if (value.IndexOf('e') != -1)
            {
                var parts = value.Split('e');
                var mantissa = double.Parse(parts[0], CultureInfo.InvariantCulture);
                var exponent = long.Parse(parts[1], CultureInfo.InvariantCulture);
                return new BigDouble(mantissa, exponent);
            }

            if (value == "NaN")
            {
                return new BigDouble(double.NaN, long.MinValue, false);
            }

            var result = new BigDouble(double.Parse(value, CultureInfo.InvariantCulture));
            if (IsNaN(result))
            {
                throw new Exception("Invalid argument: " + value);
            }

            return result;
        }

        public double ToDouble()
        {
            //Problem: new Decimal(116).toNumber() returns 115.99999999999999.
            //TODO: How to fix in general case? It's clear that if toNumber() is VERY close to an integer, we want exactly the integer. But it's not clear how to specifically write that. So I'll just settle with 'exponent >= 0 and difference between rounded and not rounded < 1e-9' as a quick fix.

            //var result = this.mantissa*Math.pow(10, this.exponent);

            if (Exponent == long.MinValue)
            {
                return double.NaN;
            }

            if (Exponent > DoubleExpMax)
            {
                return Mantissa > 0 ? double.PositiveInfinity : double.NegativeInfinity;
            }

            if (Exponent < DoubleExpMin)
            {
                return 0.0;
            }

            //SAFETY: again, handle 5e-324, -5e-324 separately
            if (Exponent == DoubleExpMin)
            {
                return Mantissa > 0 ? 5e-324 : -5e-324;
            }

            var result = Mantissa * PowersOf10[Exponent + Indexof0Inpowersof10];
            if (!IsFinite(result) || Exponent < 0)
            {
                return result;
            }

            var resultrounded = Math.Round(result);
            if (Math.Abs(resultrounded - result) < 1e-10) return resultrounded;
            return result;
        }

        public double MantissaWithDecimalPlaces(int places)
        {
            // https://stackoverflow.com/a/37425022

            if (IsNaN(this)) return double.NaN;
            if (IsZero(Mantissa)) return 0;

            var len = places + 1;
            var numDigits = (int) Math.Ceiling(Math.Log10(Math.Abs(Mantissa)));
            var rounded = Math.Round(Mantissa * Math.Pow(10, len - numDigits)) * Math.Pow(10, numDigits - len);
            return double.Parse(ToFixed(rounded, Math.Max(len - numDigits, 0)), CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            if (IsNaN(this)) return "NaN";
            if (Exponent >= ExpLimit)
            {
                return Mantissa > 0 ? "Infinity" : "-Infinity";
            }

            if (Exponent <= -ExpLimit || IsZero(Mantissa))
            {
                return "0";
            }

            if (Exponent < 21 && Exponent > -7)
            {
                return ToDouble().ToString(CultureInfo.InvariantCulture);
            }

            return Mantissa.ToString(CultureInfo.InvariantCulture) + "e" + (Exponent >= 0 ? "+" : "") +
                   Exponent.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToFixed(double value, int places)
        {
            return value.ToString("F" + places, CultureInfo.InvariantCulture);
        }

        public string ToExponential(int places = MaxSignificantDigits)
        {
            // https://stackoverflow.com/a/37425022

            //TODO: Some unfixed cases:
            //new Decimal("1.2345e-999").toExponential()
            //"1.23450000000000015e-999"
            //new Decimal("1e-999").toExponential()
            //"1.000000000000000000e-999"
            //TBH I'm tempted to just say it's a feature. If you're doing pretty formatting then why don't you know how many decimal places you want...?

            if (IsNaN(this)) return "NaN";
            if (Exponent >= ExpLimit)
            {
                return Mantissa > 0 ? "Infinity" : "-Infinity";
            }

            if (Exponent <= -ExpLimit || IsZero(Mantissa))
            {
                return "0" + (places > 0 ? PadEnd(".", places + 1, '0') : "") + "e+0";
            }

            var len = places + 1;
            var numDigits = (int) Math.Ceiling(Math.Log10(Math.Abs(Mantissa)));
            var rounded = Math.Round(Mantissa * Math.Pow(10, len - numDigits)) * Math.Pow(10, numDigits - len);

            return ToFixed(rounded, Math.Max(len - numDigits, 0)) + "e" + (Exponent >= 0 ? "+" : "") + Exponent;
        }

        public string ToFixed(int places = MaxSignificantDigits)
        {
            if (IsNaN(this)) return "NaN";
            if (Exponent >= ExpLimit)
            {
                return Mantissa > 0 ? "Infinity" : "-Infinity";
            }

            if (Exponent <= -ExpLimit || IsZero(Mantissa))
            {
                return "0" + (places > 0 ? PadEnd(".", places + 1, '0') : "");
            }

            // two cases:
            // 1) exponent is 17 or greater: just print out mantissa with the appropriate number of zeroes after it
            // 2) exponent is 16 or less: use basic toFixed

            if (Exponent >= MaxSignificantDigits)
            {
                return PadEnd(Mantissa.ToString(CultureInfo.InvariantCulture).Replace(".", ""), (int) Exponent + 1,
                           '0') + (places > 0 ? PadEnd(".", places + 1, '0') : "");
            }

            return ToFixed(ToDouble(), places);
        }

        private static string PadEnd(string str, int maxLength, char fillString)
        {
            if (str == null)
            {
                str = "";
            }

            var length = str.Length;
            if (length >= maxLength)
            {
                return str;
            }

            var fillLen = maxLength - length;
            return str + new string(fillString, fillLen);
        }

        public string ToPrecision(int places = MaxSignificantDigits)
        {
            if (Exponent <= -7)
            {
                return ToExponential(places - 1);
            }

            if (places > Exponent)
            {
                return ToFixed((int) (places - Exponent - 1));
            }

            return ToExponential(places - 1);
        }

        public string ToStringWithDecimalPlaces(int places = MaxSignificantDigits)
        {
            return ToExponential(places);
        }

        public static BigDouble Abs(BigDouble value)
        {
            return new BigDouble(Math.Abs(value.Mantissa), value.Exponent);
        }

        public static BigDouble Negate(BigDouble value)
        {
            return new BigDouble(-value.Mantissa, value.Exponent);
        }

        public static int Sign(BigDouble value)
        {
            return Math.Sign(value.Mantissa);
        }

        public static BigDouble Round(BigDouble value)
        {
            if (value.Exponent < -1)
            {
                return Zero;
            }

            if (value.Exponent < MaxSignificantDigits)
            {
                return new BigDouble(Math.Round(value.ToDouble()));
            }

            return value;
        }

        public static BigDouble Floor(BigDouble value)
        {
            if (value.Exponent < -1)
            {
                return Math.Sign(value.Mantissa) >= 0 ? Zero : -One;
            }

            if (value.Exponent < MaxSignificantDigits)
            {
                return new BigDouble(Math.Floor(value.ToDouble()));
            }

            return value;
        }

        public static BigDouble Ceiling(BigDouble value)
        {
            if (value.Exponent < -1)
            {
                return Math.Sign(value.Mantissa) > 0 ? One : Zero;
            }

            if (value.Exponent < MaxSignificantDigits)
            {
                return new BigDouble(Math.Ceiling(value.ToDouble()));
            }

            return value;
        }

        public static BigDouble Truncate(BigDouble value)
        {
            if (value.Exponent < 0)
            {
                return Zero;
            }

            if (value.Exponent < MaxSignificantDigits)
            {
                return new BigDouble(Math.Truncate(value.ToDouble()));
            }

            return value;
        }

        public static BigDouble Add(BigDouble left, BigDouble right)
        {
            //figure out which is bigger, shrink the mantissa of the smaller by the difference in exponents, add mantissas, normalize and return

            //TODO: Optimizations and simplification may be possible, see https://github.com/Patashu/break_infinity.js/issues/8

            if (IsZero(left.Mantissa))
            {
                return right;
            }

            if (IsZero(left.Mantissa))
            {
                return left;
            }

            if (IsNaN(left) || IsInfinity(left))
            {
                return left;
            }

            if (IsNaN(right) || IsInfinity(right))
            {
                return right;
            }

            BigDouble biggerDecimal, smallerDecimal;
            if (left.Exponent >= right.Exponent)
            {
                biggerDecimal = left;
                smallerDecimal = right;
            }
            else
            {
                biggerDecimal = right;
                smallerDecimal = left;
            }

            if (biggerDecimal.Exponent - smallerDecimal.Exponent > MaxSignificantDigits)
            {
                return biggerDecimal;
            }

            //have to do this because adding numbers that were once integers but scaled down is imprecise.
            //Example: 299 + 18
            return new BigDouble(
                Math.Round(1e14 * biggerDecimal.Mantissa + 1e14 * smallerDecimal.Mantissa *
                           PowersOf10[smallerDecimal.Exponent - biggerDecimal.Exponent + Indexof0Inpowersof10]),
                biggerDecimal.Exponent - 14);
        }

        public static BigDouble Subtract(BigDouble left, BigDouble right)
        {
            return left + -right;
        }

        public static BigDouble Multiply(BigDouble left, BigDouble right)
        {
            // 2e3 * 4e5 = (2 * 4)e(3 + 5)
            return new BigDouble(left.Mantissa * right.Mantissa, left.Exponent + right.Exponent);
        }

        public static BigDouble Divide(BigDouble left, BigDouble right)
        {
            return left * Reciprocate(right);
        }

        public static BigDouble Reciprocate(BigDouble value)
        {
            return new BigDouble(1.0 / value.Mantissa, -value.Exponent);
        }

        public static implicit operator BigDouble(double value)
        {
            return new BigDouble(value);
        }

        public static implicit operator BigDouble(int value)
        {
            return new BigDouble(value);
        }

        public static implicit operator BigDouble(long value)
        {
            return new BigDouble(value);
        }

        public static implicit operator BigDouble(float value)
        {
            return new BigDouble(value);
        }

        public static BigDouble operator -(BigDouble value)
        {
            return Negate(value);
        }

        public static BigDouble operator +(BigDouble left, BigDouble right)
        {
            return Add(left, right);
        }

        public static BigDouble operator -(BigDouble left, BigDouble right)
        {
            return Subtract(left, right);
        }

        public static BigDouble operator *(BigDouble left, BigDouble right)
        {
            return Multiply(left, right);
        }

        public static BigDouble operator /(BigDouble left, BigDouble right)
        {
            return Divide(left, right);
        }

        public static BigDouble FromValue(object value)
        {
            switch (value)
            {
                case BigDouble bigDoubleValue:
                    return bigDoubleValue;
                case double doubleValue:
                    return new BigDouble(doubleValue);
                case string stringValue:
                    // Really?
                    return Parse(stringValue);
                case int intValue:
                    return new BigDouble(intValue);
                case long longvalue:
                    return new BigDouble(longvalue);
                case float floatValue:
                    return new BigDouble(floatValue);
            }

            throw new Exception("I have no idea what to do with this: " + value.GetType());
        }

        public int CompareTo(object other)
        {
            var value = FromValue(other);

            //TODO: sign(a-right) might be better? https://github.com/Patashu/break_infinity.js/issues/12

            if (IsZero(Mantissa))
            {
                if (IsZero(value.Mantissa))
                {
                    return 0;
                }

                if (value.Mantissa < 0)
                {
                    return 1;
                }

                if (value.Mantissa > 0)
                {
                    return -1;
                }
            }
            else if (IsZero(value.Mantissa))
            {
                if (Mantissa < 0)
                {
                    return -1;
                }

                if (Mantissa > 0)
                {
                    return 1;
                }
            }

            if (Mantissa > 0) //positive
            {
                if (value.Mantissa < 0)
                {
                    return 1;
                }

                if (Exponent > value.Exponent)
                {
                    return 1;
                }

                if (Exponent < value.Exponent)
                {
                    return -1;
                }

                if (Mantissa > value.Mantissa)
                {
                    return 1;
                }

                if (Mantissa < value.Mantissa)
                {
                    return -1;
                }

                return 0;
            }

            if (Mantissa < 0) // negative
            {
                if (value.Mantissa > 0)
                {
                    return -1;
                }

                if (Exponent > value.Exponent)
                {
                    return -1;
                }

                if (Exponent < value.Exponent)
                {
                    return 1;
                }

                if (Mantissa > value.Mantissa)
                {
                    return 1;
                }

                if (Mantissa < value.Mantissa)
                {
                    return -1;
                }

                return 0;
            }

            return 0;
        }

        public override bool Equals(object other)
        {
            var value = FromValue(other);
            return this == value;
        }

        public bool Equals(BigDouble other)
        {
            return Exponent == other.Exponent && AreEqual(Mantissa, other.Mantissa);
        }

        public override int GetHashCode()
        {
            return Mantissa.GetHashCode() + Exponent.GetHashCode() * 486187739;
        }

        public static bool operator ==(BigDouble left, BigDouble right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BigDouble left, BigDouble right)
        {
            return !(left == right);
        }

        public static bool operator <(BigDouble a, BigDouble b)
        {
            if (IsZero(a.Mantissa)) return b.Mantissa > 0;
            if (IsZero(b.Mantissa)) return a.Mantissa <= 0;
            if (a.Exponent == b.Exponent) return a.Mantissa < b.Mantissa;
            if (a.Mantissa > 0) return b.Mantissa > 0 && a.Exponent < b.Exponent;
            return b.Mantissa > 0 || a.Exponent > b.Exponent;
        }

        public static bool operator <=(BigDouble a, BigDouble b)
        {
            if (IsZero(a.Mantissa)) return b.Mantissa >= 0;
            if (IsZero(b.Mantissa)) return a.Mantissa <= 0;
            if (a.Exponent == b.Exponent) return a.Mantissa <= b.Mantissa;
            if (a.Mantissa > 0) return b.Mantissa > 0 && a.Exponent < b.Exponent;
            return b.Mantissa > 0 || a.Exponent > b.Exponent;
        }

        public static bool operator >(BigDouble a, BigDouble b)
        {
            if (IsZero(a.Mantissa)) return b.Mantissa < 0;
            if (IsZero(b.Mantissa)) return a.Mantissa > 0;
            if (a.Exponent == b.Exponent) return a.Mantissa > b.Mantissa;
            if (a.Mantissa > 0) return b.Mantissa < 0 || a.Exponent > b.Exponent;
            return b.Mantissa < 0 && a.Exponent < b.Exponent;
        }

        public static bool operator >=(BigDouble a, BigDouble b)
        {
            if (IsZero(a.Mantissa)) return b.Mantissa <= 0;
            if (IsZero(b.Mantissa)) return a.Mantissa > 0;
            if (a.Exponent == b.Exponent) return a.Mantissa >= b.Mantissa;
            if (a.Mantissa > 0) return b.Mantissa < 0 || a.Exponent > b.Exponent;
            return b.Mantissa < 0 && a.Exponent < b.Exponent;
        }

        public static BigDouble Max(BigDouble left, BigDouble right)
        {
            return left >= right ? left : right;
        }

        public static BigDouble Min(BigDouble left, BigDouble right)
        {
            return left <= right ? left : right;
        }

        //tolerance is a relative tolerance, multiplied by the greater of the magnitudes of the two arguments. For example, if you put in 1e-9, then any number closer to the larger number than (larger number)*1e-9 will be considered equal.
        // TODO: Maybe standard C# pattern of double comparison will suffice for BigDouble
        public static bool EqTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            // https://stackoverflow.com/a/33024979
            return Abs(a - b) <= Max(Abs(a), Abs(b)) * tolerance;
        }

        public static int CmpTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return EqTolerance(a, b, tolerance) ? 0 : a.CompareTo(b);
        }

        public static bool NeqTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return !EqTolerance(a, b, tolerance);
        }

        public static bool LtTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return !EqTolerance(a, b, tolerance) && a < b;
        }

        public static bool LteTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return EqTolerance(a, b, tolerance) || a < b;
        }

        public static bool GtTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return !EqTolerance(a, b, tolerance) && a > b;
        }

        public static bool GteTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return EqTolerance(a, b, tolerance) || a > b;
        }

        public static double AbsLog10(BigDouble value)
        {
            return value.Exponent + Math.Log10(Math.Abs(value.Mantissa));
        }

        public static double Log10(BigDouble value)
        {
            return value.Exponent + Math.Log10(value.Mantissa);
        }

        public static double Log(BigDouble value, double @base)
        {
            if (IsZero(@base))
            {
                return double.NaN;
            }

            //UN-SAFETY: Most incremental game cases are log(number := 1 or greater, base := 2 or greater). We assume this to be true and thus only need to return a number, not a Decimal, and don't do any other kind of error checking.
            return 2.30258509299404568402 / Math.Log(@base) * Log10(value);
        }

        public static double Log2(BigDouble value)
        {
            return 3.32192809488736234787 * Log10(value);
        }

        public static double Ln(BigDouble value)
        {
            return 2.30258509299404568402 * Log10(value);
        }

        public static BigDouble Pow10(double power)
        {
            return IsInteger(power)
                ? Pow10((long) power)
                : new BigDouble(Math.Pow(10, power % 1), (long) Math.Truncate(power));
        }

        public static BigDouble Pow10(long power)
        {
            return new BigDouble(1, power);
        }

        public static BigDouble Pow(BigDouble value, BigDouble power)
        {
            return Pow(value, power.ToDouble());
        }

        public static BigDouble Pow(BigDouble value, long power)
        {
            return value == 10
                ? Pow10(power)
                // TODO: overflows
                : new BigDouble(Math.Pow(value.Mantissa, power), value.Exponent * power);
        }

        public static BigDouble Pow(BigDouble value, double power)
        {
            // TODO: power can be greater that long.MaxValue, which can bring troubles in fast track
            return value == 10 && IsInteger(power) ? Pow10(power) : PowInternal(value, power);
        }

        private static BigDouble PowInternal(BigDouble value, double other)
        {
            //UN-SAFETY: Accuracy not guaranteed beyond ~9~11 decimal places.

            //TODO: Fast track seems about neutral for performance. It might become faster if an integer pow is implemented, or it might not be worth doing (see https://github.com/Patashu/break_infinity.js/issues/4 )

            //Fast track: If (this.exponent*value) is an integer and mantissa^value fits in a Number, we can do a very fast method.
            var temp = value.Exponent * other;
            double newMantissa;
            if (IsInteger(temp) && IsFinite(temp) && Math.Abs(temp) < ExpLimit)
            {
                newMantissa = Math.Pow(value.Mantissa, other);
                if (IsFinite(newMantissa))
                {
                    return new BigDouble(newMantissa, (long) temp);
                }
            }

            //Same speed and usually more accurate. (An arbitrary-precision version of this calculation is used in break_break_infinity.js, sacrificing performance for utter accuracy.)

            var newexponent = Math.Truncate(temp);
            var residue = temp - newexponent;
            newMantissa = Math.Pow(10, other * Math.Log10(value.Mantissa) + residue);
            if (IsFinite(newMantissa))
            {
                return new BigDouble(newMantissa, (long) newexponent);
            }

            //UN-SAFETY: This should return NaN when mantissa is negative and value is noninteger.
            var result = Pow10(other * AbsLog10(value)); //this is 2x faster and gives same values AFAIK
            if (Sign(value) == -1 && AreEqual(other % 2, 1))
            {
                return -result;
            }

            return result;
        }

        public static BigDouble Factorial(BigDouble value)
        {
            //Using Stirling's Approximation. https://en.wikipedia.org/wiki/Stirling%27s_approximation#Versions_suitable_for_calculators

            var n = value.ToDouble() + 1;

            return Pow(n / 2.71828182845904523536 * Math.Sqrt(n * Math.Sinh(1 / n) + 1 / (810 * Math.Pow(n, 6))), n) * Math.Sqrt(2 * 3.141592653589793238462 / n);
        }

        public static BigDouble Exp(BigDouble value)
        {
            return Pow(2.71828182845904523536, value);
        }

        public static BigDouble Sqrt(BigDouble value)
        {
            if (value.Mantissa < 0)
            {
                return new BigDouble(double.NaN);
            }

            if (value.Exponent % 2 != 0)
            {
                // mod of a negative number is negative, so != means '1 or -1'
                return new BigDouble(Math.Sqrt(value.Mantissa) * 3.16227766016838, (long) Math.Floor(value.Exponent / 2.0));
            }

            return new BigDouble(Math.Sqrt(value.Mantissa), (long) Math.Floor(value.Exponent / 2.0));
        }

        public static BigDouble Cbrt(BigDouble value)
        {
            var sign = 1;
            var mantissa = value.Mantissa;
            if (mantissa < 0)
            {
                sign = -1;
                mantissa = -mantissa;
            }

            var newmantissa = sign * Math.Pow(mantissa, 1 / 3.0);

            var mod = value.Exponent % 3;
            if (mod == 1 || mod == -1)
            {
                return new BigDouble(newmantissa * 2.1544346900318837, (long) Math.Floor(value.Exponent / 3.0));
            }

            if (mod != 0)
            {
                return new BigDouble(newmantissa * 4.6415888336127789, (long) Math.Floor(value.Exponent / 3.0));
            } //mod != 0 at this point means 'mod == 2 || mod == -2'

            return new BigDouble(newmantissa, (long) Math.Floor(value.Exponent / 3.0));
        }

        public static BigDouble Sinh(BigDouble value)
        {
            return (Exp(value) - Exp(-value)) / 2;
        }

        public static BigDouble Cosh(BigDouble value)
        {
            return (Exp(value) + Exp(-value)) / 2;
        }

        public static BigDouble Tanh(BigDouble value)
        {
            return Sinh(value) / Cosh(value);
        }

        public static double Asinh(BigDouble value)
        {
            return Ln(value + Sqrt(Pow(value, 2) + 1));
        }

        public static double Acosh(BigDouble value)
        {
            return Ln(value + Sqrt(Pow(value, 2) - 1));
        }

        public static double Atanh(BigDouble value)
        {
            if (Abs(value) >= 1) return double.NaN;
            return Ln((value + 1) / (One - value)) / 2;
        }

        private static readonly Random Random = new Random();

        public static BigDouble RandomDecimalForTesting(double absMaxExponent)
        {
            var random = Random;

            //NOTE: This doesn't follow any kind of sane random distribution, so use this for testing purposes only.
            //5% of the time, have a mantissa of 0
            if (random.NextDouble() * 20 < 1)
            {
                return new BigDouble(0, 0);
            }

            var mantissa = random.NextDouble() * 10;
            //10% of the time, have a simple mantissa
            if (random.NextDouble() * 10 < 1)
            {
                mantissa = Math.Round(mantissa);
            }

            mantissa *= Math.Sign(random.NextDouble() * 2 - 1);
            var exponent = (long) (Math.Floor(random.NextDouble() * absMaxExponent * 2) - absMaxExponent);
            return new BigDouble(mantissa, exponent);

            /*
                Examples:
                randomly test pow:

                var a = Decimal.randomDecimalForTesting(1000);
                var pow = Math.random()*20-10;
                if (Math.random()*2 < 1) { pow = Math.round(pow); }
                var result = Decimal.pow(a, pow);
                ["(" + a.toString() + ")^" + pow.toString(), result.toString()]
                randomly test add:
                var a = Decimal.randomDecimalForTesting(1000);
                var right = Decimal.randomDecimalForTesting(17);
                var c = a.mul(right);
                var result = a.add(c);
                [a.toString() + "+" + c.toString(), result.toString()]
            */
        }

        private static bool IsZero(double value)
        {
            return Math.Abs(value) < double.Epsilon;
        }

        private static bool AreEqual(double first, double second)
        {
            // TODO: Establish right tolerance
            return Math.Abs(first - second) < 1e-16;
        }

        private static bool IsInteger(double value)
        {
            return IsZero(Math.Abs(value % 1));
        }

        public static bool IsFinite(double value)
        {
            return !(double.IsNaN(value) || double.IsInfinity(value));
        }
    }

    public static class BigMath
    {
        /// <summary>
        /// If you're willing to spend 'resourcesAvailable' and want to buy something with
        /// exponentially increasing cost each purchase (start at priceStart, multiply by priceRatio,
        /// already own currentOwned), how much of it can you buy?
        /// <para>
        /// Adapted from Trimps source code.
        /// </para>
        /// </summary>
        public static BigDouble AffordGeometricSeries(BigDouble resourcesAvailable, BigDouble priceStart,
            BigDouble priceRatio, BigDouble currentOwned)
        {
            var actualStart = priceStart * BigDouble.Pow(priceRatio, currentOwned);

            //return Math.floor(log10(((resourcesAvailable / (priceStart * Math.pow(priceRatio, currentOwned))) * (priceRatio - 1)) + 1) / log10(priceRatio));

            return BigDouble.Floor(BigDouble.Log10(resourcesAvailable / actualStart * (priceRatio - 1) + 1) / BigDouble.Log10(priceRatio));
        }

        /// <summary>
        /// How much resource would it cost to buy (numItems) items if you already have currentOwned,
        /// the initial price is priceStart and it multiplies by priceRatio each purchase?
        /// </summary>
        public static BigDouble SumGeometricSeries(BigDouble numItems, BigDouble priceStart, BigDouble priceRatio,
            BigDouble currentOwned)
        {
            var actualStart = priceStart * BigDouble.Pow(priceRatio, currentOwned);

            return actualStart * (1 - BigDouble.Pow(priceRatio, numItems)) / (1 - priceRatio);
        }

        /// <summary>
        /// If you're willing to spend 'resourcesAvailable' and want to buy something with
        /// additively increasing cost each purchase (start at priceStart, add by priceAdd,
        /// already own currentOwned), how much of it can you buy?
        /// </summary>
        public static BigDouble AffordArithmeticSeries(BigDouble resourcesAvailable, BigDouble priceStart,
            BigDouble priceAdd, BigDouble currentOwned)
        {
            var actualStart = priceStart + currentOwned * priceAdd;

            //n = (-(a-d/2) + sqrt((a-d/2)^2+2dS))/d
            //where a is actualStart, d is priceAdd and S is resourcesAvailable
            //then floor it and you're done!

            var b = actualStart - priceAdd / 2;
            var b2 = BigDouble.Pow(b, 2);

            return BigDouble.Floor(
                (BigDouble.Sqrt(b2 + priceAdd * resourcesAvailable * 2) - b) / priceAdd
            );
        }

        /// <summary>
        /// How much resource would it cost to buy (numItems) items if you already have currentOwned,
        /// the initial price is priceStart and it adds priceAdd each purchase?
        /// <para>
        /// Adapted from http://www.mathwords.com/a/arithmetic_series.htm
        /// </para>
        /// </summary>
        public static BigDouble SumArithmeticSeries(BigDouble numItems, BigDouble priceStart, BigDouble priceAdd,
            BigDouble currentOwned)
        {
            var actualStart = priceStart + currentOwned * priceAdd;

            //(n/2)*(2*a+(n-1)*d)

            return numItems / 2 * (2 * actualStart + (numItems - 1) * priceAdd);
        }

        /// <summary>
        /// When comparing two purchases that cost (resource) and increase your resource/sec by (delta_RpS),
        /// the lowest efficiency score is the better one to purchase.
        /// <para>
        /// From Frozen Cookies: http://cookieclicker.wikia.com/wiki/Frozen_Cookies_(JavaScript_Add-on)#Efficiency.3F_What.27s_that.3F
        /// </para>
        /// </summary>
        public static BigDouble EfficiencyOfPurchase(BigDouble cost, BigDouble currentRpS, BigDouble deltaRpS)
        {
            return cost / currentRpS + cost / deltaRpS;
        }
    }

    public static class BigDoubleExtensions
    {
        public static BigDouble Abs(this BigDouble value)
        {
            return BigDouble.Abs(value);
        }

        public static BigDouble Negate(this BigDouble value)
        {
            return BigDouble.Negate(value);
        }

        public static int Sign(this BigDouble value)
        {
            return BigDouble.Sign(value);
        }

        public static BigDouble Round(this BigDouble value)
        {
            return BigDouble.Round(value);
        }

        public static BigDouble Floor(this BigDouble value)
        {
            return BigDouble.Floor(value);
        }

        public static BigDouble Ceiling(this BigDouble value)
        {
            return BigDouble.Ceiling(value);
        }

        public static BigDouble Truncate(this BigDouble value)
        {
            return BigDouble.Truncate(value);
        }

        public static BigDouble Add(this BigDouble value, BigDouble other)
        {
            return BigDouble.Add(value, other);
        }

        public static BigDouble Subtract(this BigDouble value, BigDouble other)
        {
            return BigDouble.Subtract(value, other);
        }

        public static BigDouble Multiply(this BigDouble value, BigDouble other)
        {
            return BigDouble.Multiply(value, other);
        }

        public static BigDouble Divide(this BigDouble value, BigDouble other)
        {
            return BigDouble.Divide(value, other);
        }

        public static BigDouble Reciprocate(this BigDouble value)
        {
            return BigDouble.Reciprocate(value);
        }

        public static BigDouble Max(this BigDouble value, BigDouble other)
        {
            return BigDouble.Max(value, other);
        }

        public static BigDouble Min(this BigDouble value, BigDouble other)
        {
            return BigDouble.Min(value, other);
        }

        public static double AbsLog10(this BigDouble value)
        {
            return BigDouble.AbsLog10(value);
        }

        public static double Log10(this BigDouble value)
        {
            return BigDouble.Log10(value);
        }

        public static double Log(this BigDouble value, double @base)
        {
            return BigDouble.Log(value, @base);
        }

        public static double Log2(this BigDouble value)
        {
            return BigDouble.Log2(value);
        }

        public static double Ln(this BigDouble value)
        {
            return BigDouble.Ln(value);
        }

        public static BigDouble Exp(this BigDouble value)
        {
            return BigDouble.Exp(value);
        }

        public static BigDouble Sinh(this BigDouble value)
        {
            return BigDouble.Sinh(value);
        }

        public static BigDouble Cosh(this BigDouble value)
        {
            return BigDouble.Cosh(value);
        }

        public static BigDouble Tanh(this BigDouble value)
        {
            return BigDouble.Tanh(value);
        }

        public static double Asinh(this BigDouble value)
        {
            return BigDouble.Asinh(value);
        }

        public static double Acosh(this BigDouble value)
        {
            return BigDouble.Acosh(value);
        }

        public static double Atanh(this BigDouble value)
        {
            return BigDouble.Atanh(value);
        }

        public static BigDouble Pow(this BigDouble value, BigDouble power)
        {
            return BigDouble.Pow(value, power);
        }

        public static BigDouble Pow(this BigDouble value, long power)
        {
            return BigDouble.Pow(value, power);
        }

        public static BigDouble Pow(this BigDouble value, double power)
        {
            return BigDouble.Pow(value, power);
        }

        public static BigDouble Factorial(this BigDouble value)
        {
            return BigDouble.Factorial(value);
        }

        public static BigDouble Sqrt(this BigDouble value)
        {
            return BigDouble.Sqrt(value);
        }

        public static BigDouble Cbrt(this BigDouble value)
        {
            return BigDouble.Cbrt(value);
        }

        public static BigDouble Sqr(this BigDouble value)
        {
            return BigDouble.Pow(value, 2);
        }

#if EXTENSIONS_EASTER_EGGS
        /// <summary>
        /// Joke function from Realm Grinder.
        /// </summary>
        public static BigDouble AscensionPenalty(this BigDouble value, double ascensions)
        {
            return Math.Abs(ascensions) < double.Epsilon ? value : BigDouble.Pow(value, Math.Pow(10, -ascensions));
        }

        /// <summary>
        /// Joke function from Cookie Clicker. It's an 'egg'.
        /// </summary>
        public static BigDouble Egg(this BigDouble value)
        {
            return value + 9;
        }
#endif
    }
}