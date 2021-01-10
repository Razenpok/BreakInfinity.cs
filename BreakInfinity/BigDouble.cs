using System;
using System.Globalization;
using Random = System.Random;

// I'm not sure if there's a "Yes, this is Unity" define symbol
// (#if UNITY doesn't seem to work). If you happen to know one - please create
// an issue here https://github.com/Razenpok/BreakInfinity.cs/issues.
#if UNITY_2017_1_OR_NEWER
using UnityEngine;
#endif

namespace BreakInfinity
{
#if UNITY_2017_1_OR_NEWER
    [Serializable]
#endif
    public struct BigDouble : IFormattable, IComparable, IComparable<BigDouble>, IEquatable<BigDouble>
    {
        public const double Tolerance = 1e-18;

        //for example: if two exponents are more than 17 apart, consider adding them together pointless, just return the larger one
        private const int MaxSignificantDigits = 17;

        private const long ExpLimit = long.MaxValue;

        //the largest exponent that can appear in a Double, though not all mantissas are valid here.
        private const long DoubleExpMax = 308;

        //The smallest exponent that can appear in a Double, though not all mantissas are valid here.
        private const long DoubleExpMin = -324;

#if UNITY_2017_1_OR_NEWER
        [SerializeField]
        private double mantissa;
        [SerializeField]
        private long exponent;
#else
        private double mantissa;
        private long exponent;
#endif

        // This constructor is used to prevent non-normalized values to be created via constructor.
        // ReSharper disable once UnusedParameter.Local
        private BigDouble(double mantissa, long exponent, PrivateConstructorArg _)
        {
            this.mantissa = mantissa;
            this.exponent = exponent;
        }

        public BigDouble(double mantissa, long exponent)
        {
            this = Normalize(mantissa, exponent);
        }

        public BigDouble(BigDouble other)
        {
            mantissa = other.mantissa;
            exponent = other.exponent;
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
                this = Normalize(value, 0);
            }
        }

        public static BigDouble Normalize(double mantissa, long exponent)
        {
            if (mantissa >= 1 && mantissa < 10 || !IsFinite(mantissa))
            {
                return FromMantissaExponentNoNormalize(mantissa, exponent);
            }
            if (IsZero(mantissa))
            {
                return Zero;
            }

            var tempExponent = (long)Math.Floor(Math.Log10(Math.Abs(mantissa)));
            //SAFETY: handle 5e-324, -5e-324 separately
            if (tempExponent == DoubleExpMin)
            {
                mantissa = mantissa * 10 / 1e-323;
            }
            else
            {
                mantissa = mantissa / PowersOf10.Lookup(tempExponent);
            }

            return FromMantissaExponentNoNormalize(mantissa, exponent + tempExponent);
        }

        public double Mantissa => mantissa;

        public long Exponent => exponent;

        public static BigDouble FromMantissaExponentNoNormalize(double mantissa, long exponent)
        {
            return new BigDouble(mantissa, exponent, new PrivateConstructorArg());
        }

        public static BigDouble Zero = FromMantissaExponentNoNormalize(0, 0);

        public static BigDouble One = FromMantissaExponentNoNormalize(1, 0);

        public static BigDouble NaN = FromMantissaExponentNoNormalize(double.NaN, long.MinValue);

        public static bool IsNaN(BigDouble value)
        {
            return double.IsNaN(value.Mantissa);
        }

        public static BigDouble PositiveInfinity = FromMantissaExponentNoNormalize(double.PositiveInfinity, 0);

        public static bool IsPositiveInfinity(BigDouble value)
        {
            return double.IsPositiveInfinity(value.Mantissa);
        }

        public static BigDouble NegativeInfinity = FromMantissaExponentNoNormalize(double.NegativeInfinity, 0);

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
                return Normalize(mantissa, exponent);
            }

            if (value == "NaN")
            {
                return NaN;
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
            if (IsNaN(this))
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

            var result = Mantissa * PowersOf10.Lookup(Exponent);
            if (!IsFinite(result) || Exponent < 0)
            {
                return result;
            }

            var resultrounded = Math.Round(result);
            if (Math.Abs(resultrounded - result) < 1e-10) return resultrounded;
            return result;
        }

        public override string ToString()
        {
            return BigNumber.FormatBigDouble(this, null, null);
        }

        public string ToString(string format)
        {
            return BigNumber.FormatBigDouble(this, format, null);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return BigNumber.FormatBigDouble(this, format, formatProvider);
        }

        public static BigDouble Abs(BigDouble value)
        {
            return FromMantissaExponentNoNormalize(Math.Abs(value.Mantissa), value.Exponent);
        }

        public static BigDouble Negate(BigDouble value)
        {
            return FromMantissaExponentNoNormalize(-value.Mantissa, value.Exponent);
        }

        public static int Sign(BigDouble value)
        {
            return Math.Sign(value.Mantissa);
        }

        public static BigDouble Round(BigDouble value)
        {
            if (IsNaN(value))
            {
                return value;
            }

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

        public static BigDouble Round(BigDouble value, MidpointRounding mode)
        {
            if (IsNaN(value))
            {
                return value;
            }

            if (value.Exponent < -1)
            {
                return Zero;
            }

            if (value.Exponent < MaxSignificantDigits)
            {
                return new BigDouble(Math.Round(value.ToDouble(), mode));
            }

            return value;
        }

        public static BigDouble Floor(BigDouble value)
        {
            if (IsNaN(value))
            {
                return value;
            }

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
            if (IsNaN(value))
            {
                return value;
            }

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
            if (IsNaN(value))
            {
                return value;
            }

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

            if (IsZero(right.Mantissa))
            {
                return left;
            }

            if (IsNaN(left) || IsNaN(right) || IsInfinity(left) || IsInfinity(right))
            {
                // Let Double handle these cases.
                return left.Mantissa + right.Mantissa;
            }

            BigDouble bigger, smaller;
            if (left.Exponent >= right.Exponent)
            {
                bigger = left;
                smaller = right;
            }
            else
            {
                bigger = right;
                smaller = left;
            }

            if (bigger.Exponent - smaller.Exponent > MaxSignificantDigits)
            {
                return bigger;
            }

            //have to do this because adding numbers that were once integers but scaled down is imprecise.
            //Example: 299 + 18
            return Normalize(
                Math.Round(1e14 * bigger.Mantissa + 1e14 * smaller.Mantissa *
                           PowersOf10.Lookup(smaller.Exponent - bigger.Exponent)),
                bigger.Exponent - 14);
        }

        public static BigDouble Subtract(BigDouble left, BigDouble right)
        {
            return left + -right;
        }

        public static BigDouble Multiply(BigDouble left, BigDouble right)
        {
            // 2e3 * 4e5 = (2 * 4)e(3 + 5)
            return Normalize(left.Mantissa * right.Mantissa, left.Exponent + right.Exponent);
        }

        public static BigDouble Divide(BigDouble left, BigDouble right)
        {
            return left * Reciprocate(right);
        }

        public static BigDouble Reciprocate(BigDouble value)
        {
            return Normalize(1.0 / value.Mantissa, -value.Exponent);
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

        public static BigDouble operator ++(BigDouble value)
        {
            return value.Add(1);
        }

        public static BigDouble operator --(BigDouble value)
        {
            return value.Subtract(1);
        }

        public int CompareTo(object other)
        {
            if (other == null)
            {
                return 1;
            }
            if (other is BigDouble)
            {
                return CompareTo((BigDouble) other);
            }
            throw new ArgumentException("The parameter must be a BigDouble.");
        }

        public int CompareTo(BigDouble other)
        {
            if (
                IsZero(Mantissa) || IsZero(other.Mantissa)
                || IsNaN(this) || IsNaN(other)
                || IsInfinity(this) || IsInfinity(other))
            {
                // Let Double handle these cases.
                return Mantissa.CompareTo(other.Mantissa);
            }
            if (Mantissa > 0 && other.Mantissa < 0)
            {
                return 1;
            }
            if (Mantissa < 0 && other.Mantissa > 0)
            {
                return -1;
            }

            var exponentComparison = Exponent.CompareTo(other.Exponent);
            return exponentComparison != 0
                ? (Mantissa > 0 ? exponentComparison : -exponentComparison)
                : Mantissa.CompareTo(other.Mantissa);
        }

        public override bool Equals(object other)
        {
            return other is BigDouble && Equals((BigDouble)other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Mantissa.GetHashCode() * 397) ^ Exponent.GetHashCode();
            }
        }

        public bool Equals(BigDouble other)
        {
            return !IsNaN(this) && !IsNaN(other) && (AreSameInfinity(this, other)
                || Exponent == other.Exponent && AreEqual(Mantissa, other.Mantissa));
        }

        /// <summary>
        /// Relative comparison with tolerance being adjusted with greatest exponent.
        /// <para>
        /// For example, if you put in 1e-9, then any number closer to the larger number
        /// than (larger number) * 1e-9 will be considered equal.
        /// </para>
        /// </summary>
        public bool Equals(BigDouble other, double tolerance)
        {
            return !IsNaN(this) && !IsNaN(other) && (AreSameInfinity(this, other)
                || Abs(this - other) <= Max(Abs(this), Abs(other)) * tolerance);
        }

        private static bool AreSameInfinity(BigDouble first, BigDouble second)
        {
            return IsPositiveInfinity(first) && IsPositiveInfinity(second)
                || IsNegativeInfinity(first) && IsNegativeInfinity(second);
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
            if (IsNaN(a) || IsNaN(b))
            {
                return false;
            }
            if (IsZero(a.Mantissa)) return b.Mantissa > 0;
            if (IsZero(b.Mantissa)) return a.Mantissa < 0;
            if (a.Exponent == b.Exponent) return a.Mantissa < b.Mantissa;
            if (a.Mantissa > 0) return b.Mantissa > 0 && a.Exponent < b.Exponent;
            return b.Mantissa > 0 || a.Exponent > b.Exponent;
        }

        public static bool operator <=(BigDouble a, BigDouble b)
        {
            if (IsNaN(a) || IsNaN(b))
            {
                return false;
            }

            return !(a > b);
        }

        public static bool operator >(BigDouble a, BigDouble b)
        {
            if (IsNaN(a) || IsNaN(b))
            {
                return false;
            }
            if (IsZero(a.Mantissa)) return b.Mantissa < 0;
            if (IsZero(b.Mantissa)) return a.Mantissa > 0;
            if (a.Exponent == b.Exponent) return a.Mantissa > b.Mantissa;
            if (a.Mantissa > 0) return b.Mantissa < 0 || a.Exponent > b.Exponent;
            return b.Mantissa < 0 && a.Exponent < b.Exponent;
        }

        public static bool operator >=(BigDouble a, BigDouble b)
        {
            if (IsNaN(a) || IsNaN(b))
            {
                return false;
            }

            return !(a < b);
        }

        public static BigDouble Max(BigDouble left, BigDouble right)
        {
            if (IsNaN(left) || IsNaN(right))
            {
                return NaN;
            }
            return left > right ? left : right;
        }

        public static BigDouble Min(BigDouble left, BigDouble right)
        {
            if (IsNaN(left) || IsNaN(right))
            {
                return NaN;
            }
            return left > right ? right : left;
        }

        public static double AbsLog10(BigDouble value)
        {
            return value.Exponent + Math.Log10(Math.Abs(value.Mantissa));
        }

        public static double Log10(BigDouble value)
        {
            return value.Exponent + Math.Log10(value.Mantissa);
        }

        public static double Log(BigDouble value, BigDouble @base)
        {
            return Log(value, @base.ToDouble());
        }

        public static double Log(BigDouble value, double @base)
        {
            if (IsZero(@base))
            {
                return double.NaN;
            }

            //UN-SAFETY: Most incremental game cases are log(number := 1 or greater, base := 2 or greater). We assume this to be true and thus only need to return a number, not a BigDouble, and don't do any other kind of error checking.
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
                : Normalize(Math.Pow(10, power % 1), (long) Math.Truncate(power));
        }

        public static BigDouble Pow10(long power)
        {
            return FromMantissaExponentNoNormalize(1, power);
        }

        public static BigDouble Pow(BigDouble value, BigDouble power)
        {
            return Pow(value, power.ToDouble());
        }

        public static BigDouble Pow(BigDouble value, long power)
        {
            if (Is10(value))
            {
                return Pow10(power);
            }

            var mantissa = Math.Pow(value.Mantissa, power);
            if (double.IsInfinity(mantissa))
            {
                // TODO: This is rather dumb, but works anyway
                // Power is too big for our mantissa, so we do multiple Pow with smaller powers.
                return Pow(Pow(value, 2), (double) power / 2);
            }

            return Normalize(mantissa, value.Exponent * power);
        }

        public static BigDouble Pow(BigDouble value, double power)
        {
            // TODO: power can be greater that long.MaxValue, which can bring troubles in fast track
            var powerIsInteger = IsInteger(power);
            if (value < 0 && !powerIsInteger)
            {
                return NaN;
            }
            return Is10(value) && powerIsInteger ? Pow10(power) : PowInternal(value, power);
        }

        private static bool Is10(BigDouble value)
        {
            return value.Exponent == 1 && value.Mantissa - 1 < double.Epsilon;
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
                    return Normalize(newMantissa, (long) temp);
                }
            }

            //Same speed and usually more accurate. (An arbitrary-precision version of this calculation is used in break_break_infinity.js, sacrificing performance for utter accuracy.)

            var newexponent = Math.Truncate(temp);
            var residue = temp - newexponent;
            newMantissa = Math.Pow(10, other * Math.Log10(value.Mantissa) + residue);
            if (IsFinite(newMantissa))
            {
                return Normalize(newMantissa, (long) newexponent);
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
                return Normalize(Math.Sqrt(value.Mantissa) * 3.16227766016838, (long) Math.Floor(value.Exponent / 2.0));
            }

            return Normalize(Math.Sqrt(value.Mantissa), (long) Math.Floor(value.Exponent / 2.0));
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
                return Normalize(newmantissa * 2.1544346900318837, (long) Math.Floor(value.Exponent / 3.0));
            }

            if (mod != 0)
            {
                return Normalize(newmantissa * 4.6415888336127789, (long) Math.Floor(value.Exponent / 3.0));
            } //mod != 0 at this point means 'mod == 2 || mod == -2'

            return Normalize(newmantissa, (long) Math.Floor(value.Exponent / 3.0));
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

        private static bool IsZero(double value)
        {
            return Math.Abs(value) < double.Epsilon;
        }

        private static bool AreEqual(double first, double second)
        {
            return Math.Abs(first - second) < Tolerance;
        }

        private static bool IsInteger(double value)
        {
            return IsZero(Math.Abs(value % 1));
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        /// <summary>
        /// The BigNumber class implements methods for formatting and parsing big numeric values.
        /// </summary>
        private static class BigNumber
        {
            public static string FormatBigDouble(BigDouble value, string format, IFormatProvider formatProvider)
            {
                if (IsNaN(value)) return "NaN";
                if (value.Exponent >= ExpLimit)
                {
                    return value.Mantissa > 0 ? "Infinity" : "-Infinity";
                }

                int formatDigits;
                var formatSpecifier = ParseFormatSpecifier(format, out formatDigits);
                switch (formatSpecifier)
                {
                    case 'R':
                    case 'G':
                        return FormatGeneral(value, formatDigits);
                    case 'E':
                        return FormatExponential(value, formatDigits);
                    case 'F':
                        return FormatFixed(value, formatDigits);
                }
                throw new FormatException($"Unknown string format '{formatSpecifier}'");
            }

            private static char ParseFormatSpecifier(string format, out int digits)
            {
                const char customFormat = (char) 0;
                digits = -1;
                if (string.IsNullOrEmpty(format))
                {
                    return 'R';
                }

                var i = 0;
                var ch = format[i];
                if ((ch < 'A' || ch > 'Z') && (ch < 'a' || ch > 'z'))
                {
                    return customFormat;
                }

                i++;
                var n = -1;

                if (i < format.Length && format[i] >= '0' && format[i] <= '9')
                {
                    n = format[i++] - '0';
                    while (i < format.Length && format[i] >= '0' && format[i] <= '9')
                    {
                        n = n * 10 + (format[i++] - '0');
                        if (n >= 10)
                            break;
                    }
                }

                if (i < format.Length && format[i] != '\0')
                {
                    return customFormat;
                }

                digits = n;
                return ch;
            }

            private static string FormatGeneral(BigDouble value, int places)
            {
                if (value.Exponent <= -ExpLimit || IsZero(value.Mantissa))
                {
                    return "0";
                }

                var format = places > 0 ? $"G{places}" : "G";
                if (value.Exponent < 21 && value.Exponent > -7)
                {
                    return value.ToDouble().ToString(format, CultureInfo.InvariantCulture);
                }

                return value.Mantissa.ToString(format, CultureInfo.InvariantCulture)
                       + "E" + (value.Exponent >= 0 ? "+" : "")
                       + value.Exponent.ToString(CultureInfo.InvariantCulture);
            }

            private static string ToFixed(double value, int places)
            {
                return value.ToString($"F{places}", CultureInfo.InvariantCulture);
            }

            private static string FormatExponential(BigDouble value, int places)
            {
                if (value.Exponent <= -ExpLimit || IsZero(value.Mantissa))
                {
                    return "0" + (places > 0 ? ".".PadRight(places + 1, '0') : "") + "E+0";
                }

                var len = (places >= 0 ? places : MaxSignificantDigits) + 1;
                var numDigits = (int)Math.Ceiling(Math.Log10(Math.Abs(value.Mantissa)));
                var rounded = Math.Round(value.Mantissa * Math.Pow(10, len - numDigits)) * Math.Pow(10, numDigits - len);

                var mantissa = ToFixed(rounded, Math.Max(len - numDigits, 0));
                if (mantissa != "0" && places < 0)
                {
                    mantissa = mantissa.TrimEnd('0', '.');
                }
                return mantissa + "E" + (value.Exponent >= 0 ? "+" : "")
                       + value.Exponent;
            }

            private static string FormatFixed(BigDouble value, int places)
            {
                if (places < 0)
                {
                    places = MaxSignificantDigits;
                }
                if (value.Exponent <= -ExpLimit || IsZero(value.Mantissa))
                {
                    return "0" + (places > 0 ? ".".PadRight(places + 1, '0') : "");
                }

                // two cases:
                // 1) exponent is 17 or greater: just print out mantissa with the appropriate number of zeroes after it
                // 2) exponent is 16 or less: use basic toFixed

                if (value.Exponent >= MaxSignificantDigits)
                {
                    // TODO: StringBuilder-optimizable
                    return value.Mantissa
                        .ToString(CultureInfo.InvariantCulture)
                        .Replace(".", "")
                        .PadRight((int)value.Exponent + 1, '0')
                        + (places > 0 ? ".".PadRight(places + 1, '0') : "");
                }
                return ToFixed(value.ToDouble(), places);
            }
        }

        /// <summary>
        /// We need this lookup table because Math.pow(10, exponent) when exponent's absolute value
        /// is large is slightly inaccurate. you can fix it with the power of math... or just make
        /// a lookup table. Faster AND simpler.
        /// </summary>
        private static class PowersOf10
        {
            private static double[] Powers { get; } = new double[DoubleExpMax - DoubleExpMin];

            private const long IndexOf0 = -DoubleExpMin - 1;

            static PowersOf10()
            {
                var index = 0;
                for (var i = 0; i < Powers.Length; i++)
                {
                    Powers[index++] = double.Parse("1e" + (i - IndexOf0), CultureInfo.InvariantCulture);
                }
            }

            public static double Lookup(long power)
            {
                return Powers[IndexOf0 + power];
            }
        }

        private struct PrivateConstructorArg { }
    }

    public static class BigMath
    {
        private static readonly Random Random = new Random();

        /// <summary>
        /// This doesn't follow any kind of sane random distribution, so use this for testing purposes only.
        /// <para>5% of the time, mantissa is 0.</para>
        /// <para>10% of the time, mantissa is round.</para>
        /// </summary>
        public static BigDouble RandomBigDouble(double absMaxExponent)
        {
            if (Random.NextDouble() * 20 < 1)
            {
                return BigDouble.Normalize(0, 0);
            }

            var mantissa = Random.NextDouble() * 10;
            if (Random.NextDouble() * 10 < 1)
            {
                mantissa = Math.Round(mantissa);
            }

            mantissa *= Math.Sign(Random.NextDouble() * 2 - 1);
            var exponent = (long)(Math.Floor(Random.NextDouble() * absMaxExponent * 2) - absMaxExponent);
            return BigDouble.Normalize(mantissa, exponent);
        }

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

        public static double Log(BigDouble value, BigDouble @base)
        {
            return BigDouble.Log(value, @base);
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
