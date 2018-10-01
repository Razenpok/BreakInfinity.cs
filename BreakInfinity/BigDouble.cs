using System;
using System.Globalization;

namespace BreakInfinity
{
    public class BigDouble : IComparable
    {
        //for example: if two exponents are more than 17 apart, consider adding them together pointless, just return the larger one
        private const int MaxSignificantDigits = 17;

        //TODO: Increase to Decimal or BigInteger?
        private const long ExpLimit = (long) 9e15;

        //the largest exponent that can appear in a Number, though not all mantissas are valid here.
        private const long NumberExpMax = 308;

        //The smallest exponent that can appear in a Number, though not all mantissas are valid here.
        private const long NumberExpMin = -324;

        //we need this lookup table because Math.pow(10, exponent) when exponent's absolute value is large is slightly inaccurate. you can fix it with the power of math... or just make a lookup table. faster AND simpler
        private static readonly double[] PowersOf10 = new double[632];

        static BigDouble()
        {
            var iActual = 0;
            for (var i = NumberExpMin + 1; i <= NumberExpMax; i++)
            {
                PowersOf10[iActual] = double.Parse("1e" + i, CultureInfo.InvariantCulture);
                ++iActual;
            }
        }

        private const int Indexof0Inpowersof10 = 323;

        public double Mantissa = double.NaN;
        public long Exponent = long.MinValue; //TODO: Increase to Decimal or BigInteger?

        public double M
        {
            get => Mantissa;
            set => Mantissa = value;
        }

        public long E
        {
            get => Exponent;
            set => Exponent = value;
        }

        #region Helper Static Functions

        public static bool IsFinite(double value)
        {
            return !(double.IsNaN(value) || double.IsInfinity(value));
        }

        public static string ToFixed(double value, int places)
        {
            return value.ToString("F" + places, CultureInfo.InvariantCulture);
        }

        public static string PadEnd(string str, int maxLength, char fillString)
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

        #endregion

        private BigDouble Normalize()
        {
            //When mantissa is very denormalized, use this to normalize much faster.

            //TODO: I'm worried about mantissa being negative 0 here which is why I set it again, but it may never matter
            if (Mantissa == 0)
            {
                Mantissa = 0;
                Exponent = 0;
                return this;
            }

            if (Mantissa >= 1 && Mantissa < 10)
            {
                return this;
            }

            if (!IsFinite(Mantissa))
            {
                return this;
            }

            var tempExponent = (long) Math.Floor(Math.Log10(Math.Abs(Mantissa)));
            Mantissa = Mantissa / PowersOf10[tempExponent + Indexof0Inpowersof10];
            Exponent += tempExponent;

            return this;
        }

        private BigDouble() { }

        public BigDouble(double mantissa, long exponent)
        {
            Mantissa = mantissa;
            Exponent = exponent;
            Normalize();
        }

        public static BigDouble FromMantissaExponent_NoNormalize(double mantissa, long exponent)
        {
            //Well, you know what you're doing!
            var result = new BigDouble
            {
                Mantissa = mantissa,
                Exponent = exponent
            };
            return result;
        }

        public BigDouble(BigDouble other)
        {
            Mantissa = other.Mantissa;
            Exponent = other.Exponent;
        }

        private BigDouble FromNumber(double value)
        {
            //SAFETY: Handle Infinity and NaN in a somewhat meaningful way.
            if (double.IsNaN(value))
            {
                Mantissa = double.NaN;
                Exponent = long.MinValue;
            }
            else if (double.IsPositiveInfinity(value))
            {
                Mantissa = 1;
                Exponent = ExpLimit;
            }
            else if (double.IsNegativeInfinity(value))
            {
                Mantissa = -1;
                Exponent = ExpLimit;
            }
            else if (value == 0)
            {
                Mantissa = 0;
                Exponent = 0;
            }
            else
            {
                Exponent = (long) Math.Floor(Math.Log10(Math.Abs(value)));
                //SAFETY: handle 5e-324, -5e-324 separately
                if (Exponent == NumberExpMin)
                {
                    Mantissa = value * 10 / 1e-323;
                }
                else
                {
                    Mantissa = value / PowersOf10[Exponent + Indexof0Inpowersof10];
                }

                Normalize(); //SAFETY: Prevent weirdness.
            }

            return this;
        }

        public BigDouble(double value)
        {
            FromNumber(value);
        }

        public BigDouble(string value)
        {
            if (value.IndexOf('e') != -1)
            {
                var parts = value.Split('e');
                Mantissa = double.Parse(parts[0], CultureInfo.InvariantCulture);
                Exponent = long.Parse(parts[1], CultureInfo.InvariantCulture);
                Normalize(); //Non-normalized mantissas can easily get here, so this is mandatory.
            }
            else if (value == "NaN")
            {
                Mantissa = double.NaN;
                Exponent = long.MinValue;
            }
            else
            {
                FromNumber(double.Parse(value, CultureInfo.InvariantCulture));
                if (double.IsNaN(Mantissa))
                {
                    throw new Exception("[DecimalError] Invalid argument: " + value);
                }
            }
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

            if (Exponent > NumberExpMax)
            {
                return Mantissa > 0 ? double.PositiveInfinity : double.NegativeInfinity;
            }

            if (Exponent < NumberExpMin)
            {
                return 0.0;
            }

            //SAFETY: again, handle 5e-324, -5e-324 separately
            if (Exponent == NumberExpMin)
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

            if (double.IsNaN(Mantissa) || Exponent == long.MinValue) return double.NaN;
            if (Mantissa == 0) return 0;

            var len = places + 1;
            var numDigits = (int) Math.Ceiling(Math.Log10(Math.Abs(Mantissa)));
            var rounded = Math.Round(Mantissa * Math.Pow(10, len - numDigits)) * Math.Pow(10, numDigits - len);
            return double.Parse(ToFixed(rounded, Math.Max(len - numDigits, 0)), CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            if (double.IsNaN(Mantissa) || Exponent == long.MinValue) return "NaN";
            if (Exponent >= ExpLimit)
            {
                return Mantissa > 0 ? "Infinity" : "-Infinity";
            }

            if (Exponent <= -ExpLimit || Mantissa == 0)
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

        public string ToExponential(int places = MaxSignificantDigits)
        {
            // https://stackoverflow.com/a/37425022

            //TODO: Some unfixed cases:
            //new Decimal("1.2345e-999").toExponential()
            //"1.23450000000000015e-999"
            //new Decimal("1e-999").toExponential()
            //"1.000000000000000000e-999"
            //TBH I'm tempted to just say it's a feature. If you're doing pretty formatting then why don't you know how many decimal places you want...?

            if (double.IsNaN(Mantissa) || Exponent == long.MinValue) return "NaN";
            if (Exponent >= ExpLimit)
            {
                return Mantissa > 0 ? "Infinity" : "-Infinity";
            }

            if (Exponent <= -ExpLimit || Mantissa == 0)
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
            if (double.IsNaN(Mantissa) || Exponent == long.MinValue) return "NaN";
            if (Exponent >= ExpLimit)
            {
                return Mantissa > 0 ? "Infinity" : "-Infinity";
            }

            if (Exponent <= -ExpLimit || Mantissa == 0)
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

        public BigDouble Abs()
        {
            return new BigDouble(Math.Abs(Mantissa), Exponent);
        }

        public static BigDouble Abs(BigDouble v)
        {
            return v.Abs();
        }

        public BigDouble Neg()
        {
            return new BigDouble(-Mantissa, Exponent);
        }

        public static BigDouble Neg(BigDouble v)
        {
            return v.Neg();
        }

        public BigDouble Negate()
        {
            return Neg();
        }

        public static BigDouble Negate(BigDouble v)
        {
            return v.Negate();
        }

        public BigDouble Negated()
        {
            return Neg();
        }

        public static BigDouble Negated(BigDouble v)
        {
            return v.Negated();
        }

        public int Sgn()
        {
            return Math.Sign(Mantissa);
        }

        public static BigDouble Sgn(BigDouble v)
        {
            return v.Sgn();
        }

        public int Sign()
        {
            return Sgn();
        }

        public static BigDouble Sign(BigDouble v)
        {
            return v.Sign();
        }

        public BigDouble Round()
        {
            if (Exponent < -1)
            {
                return new BigDouble(0);
            }

            if (Exponent < MaxSignificantDigits)
            {
                return new BigDouble(Math.Round(ToDouble()));
            }

            return this;
        }

        public static BigDouble Round(BigDouble v)
        {
            return v.Round();
        }

        public BigDouble Floor()
        {
            if (Exponent < -1)
            {
                return Math.Sign(Mantissa) >= 0 ? new BigDouble(0) : new BigDouble(-1);
            }

            if (Exponent < MaxSignificantDigits)
            {
                return new BigDouble(Math.Floor(ToDouble()));
            }

            return this;
        }

        public static BigDouble Floor(BigDouble v)
        {
            return v.Floor();
        }

        public BigDouble Ceiling()
        {
            if (Exponent < -1)
            {
                return Math.Sign(Mantissa) > 0 ? new BigDouble(1) : new BigDouble(0);
            }

            if (Exponent < MaxSignificantDigits)
            {
                return new BigDouble(Math.Ceiling(ToDouble()));
            }

            return this;
        }

        public static BigDouble Ceiling(BigDouble v)
        {
            return v.Ceiling();
        }

        public BigDouble Ceil()
        {
            return Ceiling();
        }

        public static BigDouble Ceil(BigDouble v)
        {
            return v.Ceil();
        }

        public BigDouble Truncate()
        {
            if (Exponent < 0)
            {
                return new BigDouble(0);
            }

            if (Exponent < MaxSignificantDigits)
            {
                return new BigDouble(Math.Truncate(ToDouble()));
            }

            return this;
        }

        public static BigDouble Truncate(BigDouble v)
        {
            return v.Truncate();
        }

        public BigDouble Trunc()
        {
            return Truncate();
        }

        public static BigDouble Trunc(BigDouble v)
        {
            return v.Trunc();
        }

        public BigDouble Add(BigDouble value)
        {
            //figure out which is bigger, shrink the mantissa of the smaller by the difference in exponents, add mantissas, normalize and return

            //TODO: Optimizations and simplification may be possible, see https://github.com/Patashu/break_infinity.js/issues/8

            if (Mantissa == 0)
            {
                return value;
            }

            if (value.Mantissa == 0)
            {
                return this;
            }

            if (!IsFinite(Mantissa))
            {
                return this;
            }

            if (!IsFinite(value.Mantissa))
            {
                return value;
            }

            BigDouble biggerDecimal, smallerDecimal;
            if (Exponent >= value.Exponent)
            {
                biggerDecimal = this;
                smallerDecimal = value;
            }
            else
            {
                biggerDecimal = value;
                smallerDecimal = this;
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

        public BigDouble Add(double value)
        {
            return Add(new BigDouble(value));
        }

        public BigDouble Add(string value)
        {
            return Add(new BigDouble(value));
        }

        public static BigDouble Add(BigDouble a, BigDouble b)
        {
            return a.Add(b);
        }

        public BigDouble Plus(BigDouble value)
        {
            return Add(value);
        }

        public BigDouble Plus(double value)
        {
            return Add(new BigDouble(value));
        }

        public BigDouble Plus(string value)
        {
            return Add(new BigDouble(value));
        }

        public BigDouble Sub(BigDouble value)
        {
            return Add(value.Neg());
        }

        public BigDouble Sub(double value)
        {
            return Sub(new BigDouble(value));
        }

        public BigDouble Sub(string value)
        {
            return Sub(new BigDouble(value));
        }

        public static BigDouble Sub(BigDouble a, BigDouble b)
        {
            return a.Sub(b);
        }

        public BigDouble Subtract(BigDouble value)
        {
            return Sub(value);
        }

        public BigDouble Subtract(double value)
        {
            return Sub(new BigDouble(value));
        }

        public BigDouble Subtract(string value)
        {
            return Sub(new BigDouble(value));
        }

        public BigDouble Minus(BigDouble value)
        {
            return Sub(value);
        }

        public BigDouble Minus(double value)
        {
            return Sub(new BigDouble(value));
        }

        public BigDouble Minus(string value)
        {
            return Sub(new BigDouble(value));
        }

        public BigDouble Mul(BigDouble value)
        {
            /*
            a_1*10^b_1 * a_2*10^b_2
            = a_1*a_2*10^(b_1+b_2)
            */
            return new BigDouble(Mantissa * value.Mantissa, Exponent + value.Exponent);
        }

        public BigDouble Mul(double value)
        {
            return Mul(new BigDouble(value));
        }

        public BigDouble Mul(string value)
        {
            return Mul(new BigDouble(value));
        }

        public static BigDouble Mul(BigDouble a, BigDouble b)
        {
            return a.Mul(b);
        }

        public BigDouble Multiply(BigDouble value)
        {
            return Mul(value);
        }

        public BigDouble Multiply(double value)
        {
            return Mul(new BigDouble(value));
        }

        public BigDouble Multiply(string value)
        {
            return Mul(new BigDouble(value));
        }

        public BigDouble Times(BigDouble value)
        {
            return Mul(value);
        }

        public BigDouble Times(double value)
        {
            return Mul(new BigDouble(value));
        }

        public BigDouble Times(string value)
        {
            return Mul(new BigDouble(value));
        }

        public BigDouble Div(BigDouble value)
        {
            return Mul(value.Recip());
        }

        public BigDouble Div(double value)
        {
            return Div(new BigDouble(value));
        }

        public BigDouble Div(string value)
        {
            return Div(new BigDouble(value));
        }

        public static BigDouble Div(BigDouble a, BigDouble b)
        {
            return a.Div(b);
        }

        public BigDouble Divide(BigDouble value)
        {
            return Div(value);
        }

        public BigDouble Divide(double value)
        {
            return Div(new BigDouble(value));
        }

        public BigDouble Divide(string value)
        {
            return Div(new BigDouble(value));
        }

        public BigDouble DivideBy(BigDouble value)
        {
            return Div(value);
        }

        public BigDouble DivideBy(double value)
        {
            return Div(new BigDouble(value));
        }

        public BigDouble DivideBy(string value)
        {
            return Div(new BigDouble(value));
        }

        public BigDouble DividedBy(BigDouble value)
        {
            return Div(value);
        }

        public BigDouble DividedBy(double value)
        {
            return Div(new BigDouble(value));
        }

        public BigDouble DividedBy(string value)
        {
            return Div(new BigDouble(value));
        }

        public BigDouble Recip()
        {
            return new BigDouble(1.0 / Mantissa, -Exponent);
        }

        public static BigDouble Recip(BigDouble v)
        {
            return v.Recip();
        }

        public BigDouble Reciprocal()
        {
            return Recip();
        }

        public static BigDouble Reciprocal(BigDouble v)
        {
            return v.Recip();
        }

        public BigDouble Reciprocate()
        {
            return Recip();
        }

        public static BigDouble Reciprocate(BigDouble v)
        {
            return v.Recip();
        }

        public static implicit operator BigDouble(double value)
        {
            return new BigDouble(value);
        }

        public static implicit operator BigDouble(string value)
        {
            return new BigDouble(value);
        }

        public static implicit operator BigDouble(int value)
        {
            return new BigDouble(Convert.ToDouble(value));
        }

        public static implicit operator BigDouble(long value)
        {
            return new BigDouble(Convert.ToDouble(value));
        }

        public static implicit operator BigDouble(float value)
        {
            return new BigDouble(Convert.ToDouble(value));
        }

        public static BigDouble operator +(BigDouble a, BigDouble b)
        {
            return a.Add(b);
        }

        public static BigDouble operator -(BigDouble a, BigDouble b)
        {
            return a.Sub(b);
        }

        public static BigDouble operator *(BigDouble a, BigDouble b)
        {
            return a.Mul(b);
        }

        public static BigDouble operator /(BigDouble a, BigDouble b)
        {
            return a.Div(b);
        }

        public static BigDouble FromValue(object value)
        {
            switch (value)
            {
                case BigDouble bigDoubleValue:
                    return bigDoubleValue;
                case string stringValue:
                    return new BigDouble(stringValue);
                case double doubleValue:
                    return new BigDouble(doubleValue);
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

            //TODO: sign(a-b) might be better? https://github.com/Patashu/break_infinity.js/issues/12

            /*
            from smallest to largest:

            -3e100
            -1e100
            -3e99
            -1e99
            -3e0
            -1e0
            -3e-99
            -1e-99
            -3e-100
            -1e-100
            0
            1e-100
            3e-100
            1e-99
            3e-99
            1e0
            3e0
            1e99
            3e99
            1e100
            3e100

            */

            if (Mantissa == 0)
            {
                if (value.Mantissa == 0)
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
            else if (value.Mantissa == 0)
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

        public override int GetHashCode()
        {
            return Mantissa.GetHashCode() + Exponent.GetHashCode() * 486187739;
        }

        public static bool operator ==(BigDouble a, BigDouble b)
        {
            return a.Exponent == b.Exponent && a.Mantissa == b.Mantissa;
        }

        public static bool operator !=(BigDouble a, BigDouble b)
        {
            return a.Exponent != b.Exponent || a.Mantissa != b.Mantissa;
        }

        public static bool operator <(BigDouble a, BigDouble b)
        {
            if (a.Mantissa == 0) return b.Mantissa > 0;
            if (b.Mantissa == 0) return a.Mantissa <= 0;
            if (a.Exponent == b.Exponent) return a.Mantissa < b.Mantissa;
            if (a.Mantissa > 0) return b.Mantissa > 0 && a.Exponent < b.Exponent;
            return b.Mantissa > 0 || a.Exponent > b.Exponent;
        }

        public static bool operator <=(BigDouble a, BigDouble b)
        {
            if (a.Mantissa == 0) return b.Mantissa >= 0;
            if (b.Mantissa == 0) return a.Mantissa <= 0;
            if (a.Exponent == b.Exponent) return a.Mantissa <= b.Mantissa;
            if (a.Mantissa > 0) return b.Mantissa > 0 && a.Exponent < b.Exponent;
            return b.Mantissa > 0 || a.Exponent > b.Exponent;
        }

        public static bool operator >(BigDouble a, BigDouble b)
        {
            if (a.Mantissa == 0) return b.Mantissa < 0;
            if (b.Mantissa == 0) return a.Mantissa > 0;
            if (a.Exponent == b.Exponent) return a.Mantissa > b.Mantissa;
            if (a.Mantissa > 0) return b.Mantissa < 0 || a.Exponent > b.Exponent;
            return b.Mantissa < 0 && a.Exponent < b.Exponent;
        }

        public static bool operator >=(BigDouble a, BigDouble b)
        {
            if (a.Mantissa == 0) return b.Mantissa <= 0;
            if (b.Mantissa == 0) return a.Mantissa > 0;
            if (a.Exponent == b.Exponent) return a.Mantissa >= b.Mantissa;
            if (a.Mantissa > 0) return b.Mantissa < 0 || a.Exponent > b.Exponent;
            return b.Mantissa < 0 && a.Exponent < b.Exponent;
        }

        public BigDouble Max(BigDouble value)
        {
            return this >= value ? this : value;
        }

        public static BigDouble Max(BigDouble a, BigDouble b)
        {
            return a.Max(b);
        }

        public BigDouble Min(BigDouble value)
        {
            return this <= value ? this : value;
        }

        public static BigDouble Min(BigDouble a, BigDouble b)
        {
            return a.Min(b);
        }

        //tolerance is a relative tolerance, multiplied by the greater of the magnitudes of the two arguments. For example, if you put in 1e-9, then any number closer to the larger number than (larger number)*1e-9 will be considered equal.
        public bool EqTolerance(BigDouble value, double tolerance = 1e-9)
        {
            // https://stackoverflow.com/a/33024979
            //return abs(a-b) <= tolerance * max(abs(a), abs(b))

            return (this - value).Abs() <= Max(Abs(), value.Abs()).Mul(tolerance);
        }

        public static bool EqTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return a.EqTolerance(b, tolerance);
        }

        public int CmpTolerance(BigDouble value, double tolerance = 1e-9)
        {
            return EqTolerance(value, tolerance) ? 0 : CompareTo(value);
        }

        public static int CmpTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return a.CmpTolerance(b, tolerance);
        }

        public bool NeqTolerance(BigDouble value, double tolerance = 1e-9)
        {
            return !EqTolerance(value, tolerance);
        }

        public static bool NeqTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return a.NeqTolerance(b, tolerance);
        }

        public bool LtTolerance(BigDouble value, double tolerance = 1e-9)
        {
            return !EqTolerance(value, tolerance) && this < value;
        }

        public static bool LtTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return a.LtTolerance(b, tolerance);
        }

        public bool LteTolerance(BigDouble value, double tolerance = 1e-9)
        {
            return EqTolerance(value, tolerance) || this < value;
        }

        public static bool LteTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return a.LteTolerance(b, tolerance);
        }

        public bool GtTolerance(BigDouble value, double tolerance = 1e-9)
        {
            return !EqTolerance(value, tolerance) && this > value;
        }

        public static bool GtTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return a.GtTolerance(b, tolerance);
        }

        public bool GteTolerance(BigDouble value, double tolerance = 1e-9)
        {
            return EqTolerance(value, tolerance) || this > value;
        }

        public static bool GteTolerance(BigDouble a, BigDouble b, double tolerance = 1e-9)
        {
            return a.GteTolerance(b, tolerance);
        }

        public double AbsLog10()
        {
            return Exponent + Math.Log10(Math.Abs(Mantissa));
        }

        public double Log10()
        {
            return Exponent + Math.Log10(Mantissa);
        }

        public static double Log10(BigDouble value)
        {
            return value.Log10();
        }

        public double Log(double b)
        {
            if (b == 0)
            {
                return double.NaN;
            }

            //UN-SAFETY: Most incremental game cases are log(number := 1 or greater, base := 2 or greater). We assume this to be true and thus only need to return a number, not a Decimal, and don't do any other kind of error checking.
            return 2.30258509299404568402 / Math.Log(b) * Log10();
        }

        public static double Log(BigDouble value, double b)
        {
            return value.Log(b);
        }

        public double Log2()
        {
            return 3.32192809488736234787 * Log10();
        }

        public static double Log2(BigDouble value)
        {
            return value.Log2();
        }

        public double Ln()
        {
            return 2.30258509299404568402 * Log10();
        }

        public static double Ln(BigDouble value)
        {
            return value.Ln();
        }

        public double Logarithm(double b)
        {
            return Log(b);
        }

        public static double Logarithm(BigDouble value, double b)
        {
            return value.Logarithm(b);
        }

        public BigDouble Pow(BigDouble value)
        {
            return Pow(value.ToDouble());
        }

        public BigDouble Pow(double value)
        {
            //UN-SAFETY: Accuracy not guaranteed beyond ~9~11 decimal places.

            //TODO: Fast track seems about neutral for performance. It might become faster if an integer pow is implemented, or it might not be worth doing (see https://github.com/Patashu/break_infinity.js/issues/4 )

            //Fast track: If (this.exponent*value) is an integer and mantissa^value fits in a Number, we can do a very fast method.
            var temp = Exponent * value;
            var newMantissa = double.NaN;
            if (Math.Truncate(temp) == temp && IsFinite(temp) && Math.Abs(temp) < ExpLimit)
            {
                newMantissa = Math.Pow(Mantissa, value);
                if (IsFinite(newMantissa))
                {
                    return new BigDouble(newMantissa, (long) temp);
                }
            }

            //Same speed and usually more accurate. (An arbitrary-precision version of this calculation is used in break_break_infinity.js, sacrificing performance for utter accuracy.)

            var newexponent = Math.Truncate(temp);
            var residue = temp - newexponent;
            newMantissa = Math.Pow(10, value * Math.Log10(Mantissa) + residue);
            if (IsFinite(newMantissa))
            {
                return new BigDouble(newMantissa, (long) newexponent);
            }

            //UN-SAFETY: This should return NaN when mantissa is negative and value is noninteger.
            var result = Pow10(value * AbsLog10()); //this is 2x faster and gives same values AFAIK
            if (Sign() == -1 && value % 2 == 1)
            {
                return result.Neg();
            }

            return result;
        }

        public static BigDouble Pow10(double value)
        {
            if (value == Math.Truncate(value))
            {
                return FromMantissaExponent_NoNormalize(1, (long) value);
            }

            return new BigDouble(Math.Pow(10, value % 1), (long) Math.Truncate(value));
        }

        public BigDouble PowBase(BigDouble value)
        {
            return value.Pow(this);
        }

        public static BigDouble Pow(BigDouble value, BigDouble other)
        {
            return Pow(value, other.ToDouble());
        }

        public static BigDouble Pow(BigDouble value, double other)
        {
            //Fast track: 10^integer
            if (value == 10 && other == Math.Truncate(other))
            {
                return new BigDouble(1, (long) other);
            }

            return value.Pow(other);
        }

        public BigDouble Factorial()
        {
            //Using Stirling's Approximation. https://en.wikipedia.org/wiki/Stirling%27s_approximation#Versions_suitable_for_calculators

            var n = ToDouble() + 1;

            return Pow(n / 2.71828182845904523536 * Math.Sqrt(n * Math.Sinh(1 / n) + 1 / (810 * Math.Pow(n, 6))), n)
                .Mul(Math.Sqrt(2 * 3.141592653589793238462 / n));
        }

        public BigDouble Exp()
        {
            return Pow(2.71828182845904523536, this);
        }

        public static BigDouble Exp(BigDouble value)
        {
            return value.Exp();
        }

        public BigDouble Sqr()
        {
            return new BigDouble(Math.Pow(Mantissa, 2), Exponent * 2);
        }

        public static BigDouble Sqr(BigDouble value)
        {
            return value.Sqr();
        }

        public BigDouble Sqrt()
        {
            if (Mantissa < 0)
            {
                return new BigDouble(double.NaN);
            }

            if (Exponent % 2 != 0)
            {
                // mod of a negative number is negative, so != means '1 or -1'
                return new BigDouble(Math.Sqrt(Mantissa) * 3.16227766016838, (long) Math.Floor((double) Exponent / 2));
            }

            return new BigDouble(Math.Sqrt(Mantissa), (long) Math.Floor((double) Exponent / 2));
        }

        public static BigDouble Sqrt(BigDouble value)
        {
            return value.Sqrt();
        }

        public BigDouble Cube()
        {
            return new BigDouble(Math.Pow(Mantissa, 3), Exponent * 3);
        }

        public static BigDouble Cube(BigDouble value)
        {
            return value.Cube();
        }

        public BigDouble Cbrt()
        {
            var sign = 1;
            var mantissa = Mantissa;
            if (mantissa < 0)
            {
                sign = -1;
                mantissa = -mantissa;
            }

            ;
            var newmantissa = sign * Math.Pow(mantissa, 1 / 3);

            var mod = Exponent % 3;
            if (mod == 1 || mod == -1)
            {
                return new BigDouble(newmantissa * 2.1544346900318837, (long) Math.Floor((double) Exponent / 3));
            }

            if (mod != 0)
            {
                return new BigDouble(newmantissa * 4.6415888336127789, (long) Math.Floor((double) Exponent / 3));
            } //mod != 0 at this point means 'mod == 2 || mod == -2'

            return new BigDouble(newmantissa, (long) Math.Floor((double) Exponent / 3));
        }

        public static BigDouble Cbrt(BigDouble value)
        {
            return value.Cbrt();
        }

        //Some hyperbolic trig functions that happen to be easy
        public BigDouble Sinh()
        {
            return Exp().Sub(Negate().Exp()).Div(2);
        }

        public BigDouble Cosh()
        {
            return Exp().Add(Negate().Exp()).Div(2);
        }

        public BigDouble Tanh()
        {
            return Sinh().Div(Cosh());
        }

        public double Asinh()
        {
            return Ln(Add(Sqr().Add(1).Sqrt()));
        }

        public double Acosh()
        {
            return Ln(Add(Sqr().Sub(1).Sqrt()));
        }

        public double Atanh()
        {
            if (Abs() >= 1) return double.NaN;
            return Ln(Add(1).Div(new BigDouble(1).Sub(this))) / 2;
        }

        //If you're willing to spend 'resourcesAvailable' and want to buy something with exponentially increasing cost each purchase (start at priceStart, multiply by priceRatio, already own currentOwned), how much of it can you buy? Adapted from Trimps source code.
        public static BigDouble AffordGeometricSeries(BigDouble resourcesAvailable, BigDouble priceStart,
            BigDouble priceRatio, BigDouble currentOwned)
        {
            var actualStart = priceStart.Mul(Pow(priceRatio, currentOwned));

            //return Math.floor(log10(((resourcesAvailable / (priceStart * Math.pow(priceRatio, currentOwned))) * (priceRatio - 1)) + 1) / log10(priceRatio));

            return Floor(Log10(resourcesAvailable.Div(actualStart).Mul(priceRatio.Sub(1)).Add(1)) / Log10(priceRatio));
        }

        //How much resource would it cost to buy (numItems) items if you already have currentOwned, the initial price is priceStart and it multiplies by priceRatio each purchase?
        public static BigDouble SumGeometricSeries(BigDouble numItems, BigDouble priceStart, BigDouble priceRatio,
            BigDouble currentOwned)
        {
            var actualStart = priceStart.Mul(Pow(priceRatio, currentOwned));

            return actualStart.Mul(Sub(1, Pow(priceRatio, numItems))).Div(Sub(1, priceRatio));
        }

        //If you're willing to spend 'resourcesAvailable' and want to buy something with additively increasing cost each purchase (start at priceStart, add by priceAdd, already own currentOwned), how much of it can you buy?
        public static BigDouble AffordArithmeticSeries(BigDouble resourcesAvailable, BigDouble priceStart,
            BigDouble priceAdd, BigDouble currentOwned)
        {
            var actualStart = priceStart.Add(Mul(currentOwned, priceAdd));

            //n = (-(a-d/2) + sqrt((a-d/2)^2+2dS))/d
            //where a is actualStart, d is priceAdd and S is resourcesAvailable
            //then floor it and you're done!

            var b = actualStart.Sub(priceAdd.Div(2));
            var b2 = b.Pow(2);

            return Floor(
                b.Neg().Add(Sqrt(b2.Add(Mul(priceAdd, resourcesAvailable).Mul(2)))).Div(priceAdd)
            );
        }

        //How much resource would it cost to buy (numItems) items if you already have currentOwned, the initial price is priceStart and it adds priceAdd each purchase? Adapted from http://www.mathwords.com/a/arithmetic_series.htm
        public static BigDouble SumArithmeticSeries(BigDouble numItems, BigDouble priceStart, BigDouble priceAdd,
            BigDouble currentOwned)
        {
            var actualStart = priceStart.Add(Mul(currentOwned, priceAdd));

            //(n/2)*(2*a+(n-1)*d)

            return Div(numItems, 2).Mul(Mul(2, actualStart).Add(numItems.Sub(1).Mul(priceAdd)));
        }

        //Joke function from Realm Grinder
        public BigDouble AscensionPenalty(double ascensions)
        {
            if (ascensions == 0) return this;
            return Pow(Math.Pow(10, -ascensions));
        }

        //When comparing two purchases that cost (resource) and increase your resource/sec by (delta_RpS), the lowest efficiency score is the better one to purchase. From Frozen Cookies: http://cookieclicker.wikia.com/wiki/Frozen_Cookies_(JavaScript_Add-on)#Efficiency.3F_What.27s_that.3F
        public static BigDouble EfficiencyOfPurchase(BigDouble cost, BigDouble currentRpS, BigDouble deltaRpS)
        {
            return Add(cost.Div(currentRpS), cost.Div(deltaRpS));
        }

        //Joke function from Cookie Clicker. It's 'egg'
        public BigDouble Egg()
        {
            return Add(9);
        }

        //  Porting some function from Decimal.js
        public bool LessThanOrEqualTo(BigDouble other)
        {
            return CompareTo(other) < 1;
        }

        public bool LessThan(BigDouble other)
        {
            return CompareTo(other) < 0;
        }

        public bool GreaterThanOrEqualTo(BigDouble other)
        {
            return CompareTo(other) > -1;
        }

        public bool GreaterThan(BigDouble other)
        {
            return CompareTo(other) > 0;
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
                var b = Decimal.randomDecimalForTesting(17);
                var c = a.mul(b);
                var result = a.add(c);
                [a.toString() + "+" + c.toString(), result.toString()]
            */
        }
    }
}