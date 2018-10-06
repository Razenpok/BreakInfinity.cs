/*
    Copyright (c) 2011 Jeff Pasternack.  All rights reserved.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Text;

namespace BreakInfinity.Benchmarks.Quadruple
{
    /// <summary>
    /// Quad is a signed 128-bit floating point number, stored internally as a 64-bit significand (with the most significant bit as the sign bit) and
    /// a 64-bit signed exponent, with a value == significand * 2^exponent.  Quads have both a higher precision (64 vs. 53 effective significand bits)
    /// and a much higher range (64 vs. 11 exponent bits) than doubles, but also support NaN and PositiveInfinity/NegativeInfinity values and can be generally
    /// used as a drop-in replacement for doubles, much like double is a drop-in replacement for float.  Operations are checked and become +/- infinity in the
    /// event of overflow (values larger than ~8E+2776511644261678592) and 0 in the event of underflow (values less than ~4E-2776511644261678592).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exponents >= long.MaxValue - 64 and exponents &lt;= long.MinValue + 64 are reserved
    /// and constitute overflow and underflow, respectively.  Zero, PositiveInfinity, NegativeInfinity and NaN are
    /// defined by significand bits == 0 and an exponent of long.MinValue + 0, + 1, + 2, and + 3, respectively.
    /// </para>
    /// <para>
    /// Quad multiplication and division operators are slightly imprecise for the sake of efficiency; specifically,
    /// they may assign the wrong least significant bit, such that the precision is effectively only 63 bits rather than 64.
    /// </para>
    /// <para>
    /// For speed, consider using instance methods (like Multiply and Add) rather
    /// than the operators (like * and +) when possible, as the former are significantly faster (by as much as 50%).
    /// </para>
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("{ToString(),nq}")] //this attributes makes the debugger display the value without braces or quotes
    public struct Quad
    {
        #region Public constants

        /// <summary>
        /// 0.  Equivalent to (Quad)0.
        /// </summary>
        public static readonly Quad Zero = new Quad(0UL, long.MinValue); //there is only one zero; all other significands with exponent long.MinValue are invalid.

        /// <summary>
        /// 1.  Equivalent to (Quad)1.
        /// </summary>
        public static readonly Quad One = (Quad)1UL; //used for increment/decrement operators

        /// <summary>
        /// Positive infinity.  Equivalent to (Quad)double.PositiveInfinity.
        /// </summary>
        public static readonly Quad PositiveInfinity = new Quad(0UL, infinityExponent);

        /// <summary>
        /// Negative infinity.  Equivalent to (Quad)double.NegativeInfinity.
        /// </summary>
        public static readonly Quad NegativeInfinity = new Quad(0UL, negativeInfinityExponent);

        /// <summary>
        /// The Not-A-Number value.  Equivalent to (Quad)double.NaN.
        /// </summary>
        public static readonly Quad NaN = new Quad(0UL, notANumberExponent);

        /// <summary>
        /// The maximum value representable by a Quad, (2 - 1/(2^63)) * 2^(long.MaxValue-65)
        /// </summary>
        public static readonly Quad MaxValue = new Quad(~highestBit, exponentUpperBound);

        /// <summary>
        /// The minimum value representable by a Quad, -(2 - 1/(2^63)) * 2^(long.MaxValue-65)
        /// </summary>
        public static readonly Quad MinValue = new Quad(ulong.MaxValue, exponentUpperBound);

        /// <summary>
        /// The smallest positive value greater than zero representable by a Quad, 2^(long.MinValue+65)
        /// </summary>
        public static readonly Quad Epsilon = new Quad(0UL, exponentLowerBound);

        /// <summary>
        /// all the markers for an exponential number, for string parsing
        /// </summary>
        public static string[] ExponentialMarkers = new string[] { " e+", " E+", "E+", "e+", " e", " E", "E" };

        /// <summary>
        /// saves creating a new Quad every time we want to compare a quad to this constant
        /// </summary>
        public static Quad QuadDoubleMin = double.MinValue;

        /// <summary>
        /// saves creating a new Quad every time we want to compare a quad to this constant
        /// </summary>
        public static Quad QuadDoubleMax = double.MaxValue;

        //above this threshold Quad.ToString() is used instead of NumberFormatting custom class
        // < double.Max as numbers close to max fail to get non-infinite values from RoundToSignificantDigits()
        public static Quad ThresholdForFormatting = new Quad(double.MaxValue / 10);

        #endregion

        #region Public fields

        /// <summary>
        /// The first (most significant) bit of the significand is the sign bit; 0 for positive values, 1 for negative.
        /// The remainder of the bits represent the fractional part (after the binary point) of the significant; there is always an implicit "1"
        /// preceding the binary point, just as in IEEE's double specification.  For "special" values 0, PositiveInfinity, NegativeInfinity, and NaN,
        /// SignificantBits == 0.
        /// </summary>
        public ulong SignificandBits;

        /// <summary>
        /// The value of the Quad == (-1)^[first bit of significant] * 1.[last 63 bits of significand] * 2^exponent.
        /// Exponents >= long.MaxValue - 64 and exponents &lt;= long.MinValue + 64 are reserved.
        /// Exponents of long.MinValue + 0, + 1, + 2 and + 3 are used to represent 0, PositiveInfinity, NegativeInfinity, and NaN, respectively.
        /// </summary>
        public long Exponent;

        #endregion

        #region Constructors

        public Quad(int value)
        {
            this = value;
        }

        public Quad(double value)
        {
            this = value;
        }

        /// <summary>
        /// Creates a new Quad with the given significand bits and exponent.  The significand has a first (most significant) bit
        /// corresponding to the quad's sign (1 for positive, 0 for negative), and the rest of the bits correspond to the fractional
        /// part of the significand value (immediately after the binary point).  A "1" before the binary point is always implied.
        /// </summary>
        /// <param name="significand"></param>
        /// <param name="exponent"></param>
        public Quad(ulong significandBits, long exponent)
        {
            SignificandBits = significandBits;
            Exponent = exponent;
        }

        /// <summary>
        /// Creates a new Quad with the given significand value and exponent.
        /// </summary>
        /// <param name="significand"></param>
        /// <param name="exponent"></param>
        public Quad(long significand, long exponent)
        {
            if (significand == 0) //handle 0
            {
                SignificandBits = 0;
                Exponent = long.MinValue;
                return;
            }

            if (significand < 0)
            {
                if (significand == long.MinValue) //corner case
                {
                    SignificandBits = highestBit;
                    Exponent = 0;
                    return;
                }

                significand = -significand;
                SignificandBits = highestBit;
            }
            else
                SignificandBits = 0;

            int shift = nlz((ulong)significand); //we must normalize the value such that the most significant bit is 1
            SignificandBits |= ~highestBit & (((ulong)significand) << shift); //mask out the highest bit--it's implicit
            Exponent = exponent - shift;
        }

        #endregion

        #region Helper functions and constants

        #region "Special" arithmetic tables for zeros, infinities, and NaN's

        //first index = first argument to the operation; second index = second argument
        //One's are used as placeholders when dividing a finite by a finite; these will not be used as the actual result of division, of course.
        //arguments are in the order: 0, positive infinity, negative infinity, NaN, positive finite, negative finite
        private static readonly Quad[,] specialDivisionTable = new Quad[6, 6]
        {
            {NaN, Zero, Zero, NaN, Zero, Zero}, // 0 divided by something
            {PositiveInfinity, NaN, NaN, NaN, PositiveInfinity, NegativeInfinity}, // +inf divided by something
            {NegativeInfinity, NaN, NaN, NaN, NegativeInfinity, PositiveInfinity}, // -inf divided by something
            {NaN, NaN, NaN, NaN, NaN, NaN}, // NaN divided by something
            {PositiveInfinity, Zero, Zero, NaN, One, One}, //positive finite divided by something
            {NegativeInfinity, Zero, Zero, NaN, One, One} //negative finite divided by something
        };

        private static readonly Quad[,] specialMultiplicationTable = new Quad[6, 6]
        {
            {Zero, NaN, NaN, NaN, Zero, Zero}, // 0 * something
            {NaN, PositiveInfinity, NegativeInfinity, NaN, PositiveInfinity, NegativeInfinity}, // +inf * something
            {NaN, NegativeInfinity, PositiveInfinity, NaN, NegativeInfinity, PositiveInfinity}, // -inf * something
            {NaN, NaN, NaN, NaN, NaN, NaN}, // NaN * something
            {Zero, PositiveInfinity, NegativeInfinity, NaN, One, One}, //positive finite * something
            {Zero, NegativeInfinity, PositiveInfinity, NaN, One, One} //negative finite * something
        };

        private static readonly bool[,] specialGreaterThanTable = new bool[6, 6]
        {
            {false, false, true, false, false, true}, // 0 > something
            {true, false, true, false, true, true}, // +inf > something
            {false, false, false, false, false, false}, // -inf > something
            {false, false, false, false, false, false}, // NaN > something
            {true, false, true, false, false, true}, //positive finite > something
            {false, false, true, false, false, false} //negative finite > something
        };

        private static readonly bool[,] specialGreaterEqualThanTable = new bool[6, 6]
        {
            {true, false, true, false, false, true}, // 0 >= something
            {true, true, true, false, true, true}, // +inf >= something
            {false, false, true, false, false, false}, // -inf >= something
            {false, false, false, false, false, false}, // NaN >= something
            {true, false, true, false, false, true}, //positive finite >= something
            {false, false, true, false, false, false} //negative finite >= something
        };

        private static readonly bool[,] specialLessThanTable = new bool[6, 6]
        {
            {false, true, false, false, true, false}, // 0 < something
            {false, false, false, false, false, false}, // +inf < something
            {true, true, false, false, true, true}, // -inf < something
            {false, false, false, false, false, false}, // NaN < something
            {false, true, false, false, false, false}, //positive finite < something
            {true, true, false, false, true, false} //negative finite < something
        };

        private static readonly bool[,] specialLessEqualThanTable = new bool[6, 6]
        {
            {true, true, false, false, true, false}, // 0 < something
            {false, true, false, false, false, false}, // +inf < something
            {true, true, true, false, true, true}, // -inf < something
            {false, false, false, false, false, false}, // NaN < something
            {false, true, false, false, false, false}, //positive finite < something
            {true, true, false, false, true, false} //negative finite < something
        };

        private static readonly Quad[,] specialSubtractionTable = new Quad[6, 6]
        {
            {Zero, NegativeInfinity, PositiveInfinity, NaN, One, One}, //0 - something
            {PositiveInfinity, NaN, PositiveInfinity, NaN, PositiveInfinity, PositiveInfinity}, //+Infinity - something
            {NegativeInfinity, NegativeInfinity, NaN, NaN, NegativeInfinity, NegativeInfinity}, //-Infinity - something
            {NaN, NaN, NaN, NaN, NaN, NaN}, //NaN - something
            {One, NegativeInfinity, PositiveInfinity, NaN, One, One}, //+finite - something
            {One, NegativeInfinity, PositiveInfinity, NaN, One, One} //-finite - something
        };

        private static readonly Quad[,] specialAdditionTable = new Quad[6, 6]
        {
            {Zero, PositiveInfinity, NegativeInfinity, NaN, One, One}, //0 + something
            {PositiveInfinity, PositiveInfinity, NaN, NaN, PositiveInfinity, PositiveInfinity}, //+Infinity + something
            {NegativeInfinity, NaN, NegativeInfinity, NaN, NegativeInfinity, NegativeInfinity}, //-Infinity + something
            {NaN, NaN, NaN, NaN, NaN, NaN}, //NaN + something
            {One, PositiveInfinity, NegativeInfinity, NaN, One, One}, //+finite + something
            {One, PositiveInfinity, NegativeInfinity, NaN, One, One} //-finite + something
        };

        private static readonly double[] specialDoubleLogTable = new double[] { double.NegativeInfinity, double.PositiveInfinity, double.NaN, double.NaN };

        private static readonly string[] specialStringTable = new string[] { "0", "Infinity", "-Infinity", "NaN" };

        #endregion

        private const long zeroExponent = long.MinValue;
        private const long infinityExponent = long.MinValue + 1;
        private const long negativeInfinityExponent = long.MinValue + 2;
        private const long notANumberExponent = long.MinValue + 3;

        private const long exponentUpperBound = long.MaxValue - 65; //no exponent should be higher than this
        private const long exponentLowerBound = long.MinValue + 65; //no exponent should be lower than this

        private const double base2to10Multiplier = 0.30102999566398119521373889472449; //Math.Log(2) / Math.Log(10);
        private const ulong highestBit = 1UL << 63;
        private const ulong secondHighestBit = 1UL << 62;
        private const ulong lowWordMask = 0xffffffff; //lower 32 bits
        private const ulong highWordMask = 0xffffffff00000000; //upper 32 bits

        private const ulong b = 4294967296; // Number base (32 bits).

        private static readonly Quad e19 = (Quad)10000000000000000000UL;
        private static readonly Quad e10 = (Quad)10000000000UL;
        private static readonly Quad e5 = (Quad)100000UL;
        private static readonly Quad e3 = (Quad)1000UL;
        private static readonly Quad e1 = (Quad)10UL;

        //        private static readonly Quad en19 = One / e19;
        //        private static readonly Quad en10 = One / e10;
        //        private static readonly Quad en5 = One / e5;
        //        private static readonly Quad en3 = One / e3;
        //        private static readonly Quad en1 = One / e1;

        private static readonly Quad en18 = One / (Quad)1000000000000000000UL;
        private static readonly Quad en9 = One / (Quad)1000000000UL;
        private static readonly Quad en4 = One / (Quad)10000UL;
        private static readonly Quad en2 = One / (Quad)100UL;

        private static readonly double tenTo100 = Math.Pow(10, 100);
        private static readonly double tenTo10 = Math.Pow(10, 10);

        /// <summary>
        /// Returns the position of the highest set bit, counting from the most significant bit position (position 0).
        /// Returns 64 if no bit is set.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static int nlz(ulong x)
        {
            //Future work: might be faster with a huge, explicit nested if tree, or use of an 256-element per-byte array.

            int n;

            if (x == 0) return (64);
            n = 0;
            if (x <= 0x00000000FFFFFFFF)
            {
                n = n + 32;
                x = x << 32;
            }
            if (x <= 0x0000FFFFFFFFFFFF)
            {
                n = n + 16;
                x = x << 16;
            }
            if (x <= 0x00FFFFFFFFFFFFFF)
            {
                n = n + 8;
                x = x << 8;
            }
            if (x <= 0x0FFFFFFFFFFFFFFF)
            {
                n = n + 4;
                x = x << 4;
            }
            if (x <= 0x3FFFFFFFFFFFFFFF)
            {
                n = n + 2;
                x = x << 2;
            }
            if (x <= 0x7FFFFFFFFFFFFFFF)
            {
                n = n + 1;
            }
            return n;
        }

        #endregion

        #region Struct-modifying instance arithmetic functions

        public unsafe void Multiply(double multiplierDouble)
        {
            Quad multiplier;

            #region Parse the double

            // Implementation note: the use of goto is generally discouraged,
            // but here the idea is to copy-paste the casting call for double -> Quad
            // to avoid the expense of an additional function call
            // and the use of a single "return" goto target keeps things simple

            // Translate the double into sign, exponent and mantissa.
            //long bits = BitConverter.DoubleToInt64Bits(value); // doing an unsafe pointer-conversion to get the bits is faster
            ulong bits = *((ulong*)&multiplierDouble);

            // Note that the shift is sign-extended, hence the test against -1 not 1
            long exponent = (((long)bits >> 52) & 0x7ffL);
            ulong mantissa = (bits) & 0xfffffffffffffUL;

            if (exponent == 0x7ffL)
            {
                if (mantissa == 0)
                {
                    if (bits >= highestBit) //sign bit set?
                        multiplier = NegativeInfinity;
                    else
                        multiplier = PositiveInfinity;

                    goto Parsed;
                }
                else
                {
                    multiplier = NaN;
                    goto Parsed;
                }
            }

            // Subnormal numbers; exponent is effectively one higher,
            // but there's no extra normalisation bit in the mantissa
            if (exponent == 0)
            {
                if (mantissa == 0)
                {
                    multiplier = Zero;
                    goto Parsed;
                }
                exponent++;

                int firstSetPosition = nlz(mantissa);
                mantissa <<= firstSetPosition;
                exponent -= firstSetPosition;
            }
            else
            {
                mantissa = mantissa << 11;
                exponent -= 11;
            }

            exponent -= 1075;

            multiplier.SignificandBits = (highestBit & bits) | mantissa;
            multiplier.Exponent = exponent;

            Parsed:

            #endregion

            #region Multiply

            if (this.Exponent <= notANumberExponent) //zero/infinity/NaN * something
            {
                Quad result = specialMultiplicationTable[(int)(this.Exponent - zeroExponent), multiplier.Exponent > notANumberExponent ? (int)(4 + (multiplier.SignificandBits >> 63)) : (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }
            else if (multiplier.Exponent <= notANumberExponent) //finite * zero/infinity/NaN
            {
                Quad result = specialMultiplicationTable[(int)(4 + (this.SignificandBits >> 63)), (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }

            ulong high1 = (this.SignificandBits | highestBit) >> 32; //de-implicitize the 1
            ulong high2 = (multiplier.SignificandBits | highestBit) >> 32;

            //because the MSB of both significands is 1, the MSB of the result will also be 1, and the product of low bits on both significands is dropped (and thus we can skip its calculation)
            ulong significandBits = high1 * high2 + (((this.SignificandBits & lowWordMask) * high2) >> 32) + ((high1 * (multiplier.SignificandBits & lowWordMask)) >> 32);

            long qd2Exponent;
            long qd1Exponent = this.Exponent;
            if (significandBits < (1UL << 63))
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & highestBit) | ((significandBits << 1) & ~highestBit);
                qd2Exponent = multiplier.Exponent - 1 + 64;
                this.Exponent = this.Exponent + qd2Exponent;
            }
            else
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & highestBit) | (significandBits & ~highestBit);
                qd2Exponent = multiplier.Exponent + 64;
                this.Exponent = this.Exponent + qd2Exponent;
            }

            if (qd2Exponent < 0 && this.Exponent > qd1Exponent) //did the exponent get larger after adding something negative?
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }
            else if (qd2Exponent > 0 && this.Exponent < qd1Exponent) //did the exponent get smaller when it should have gotten larger?
            {
                this.SignificandBits = 0;
                this.Exponent = this.SignificandBits >= highestBit ? negativeInfinityExponent : infinityExponent; //overflow
            }
            else if (this.Exponent < exponentLowerBound) //check for underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }
            else if (this.Exponent > exponentUpperBound) //overflow
            {
                this.SignificandBits = 0;
                this.Exponent = this.SignificandBits >= highestBit ? negativeInfinityExponent : infinityExponent; //overflow
            }

            #endregion
        }

        public void Multiply(Quad multiplier)
        {
            if (this.Exponent <= notANumberExponent) //zero/infinity/NaN * something
            {
                Quad result = specialMultiplicationTable[(int)(this.Exponent - zeroExponent), multiplier.Exponent > notANumberExponent ? (int)(4 + (multiplier.SignificandBits >> 63)) : (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }
            else if (multiplier.Exponent <= notANumberExponent) //finite * zero/infinity/NaN
            {
                Quad result = specialMultiplicationTable[(int)(4 + (this.SignificandBits >> 63)), (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }

            ulong high1 = (this.SignificandBits | highestBit) >> 32; //de-implicitize the 1
            ulong high2 = (multiplier.SignificandBits | highestBit) >> 32;

            //because the MSB of both significands is 1, the MSB of the result will also be 1, and the product of low bits on both significands is dropped (and thus we can skip its calculation)
            ulong significandBits = high1 * high2 + (((this.SignificandBits & lowWordMask) * high2) >> 32) + ((high1 * (multiplier.SignificandBits & lowWordMask)) >> 32);

            long qd2Exponent;
            long qd1Exponent = this.Exponent;
            if (significandBits < (1UL << 63))
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & highestBit) | ((significandBits << 1) & ~highestBit);
                qd2Exponent = multiplier.Exponent - 1 + 64;
            }
            else
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & highestBit) | (significandBits & ~highestBit);
                qd2Exponent = multiplier.Exponent + 64;
            }

            this.Exponent = this.Exponent + qd2Exponent;

            if (qd2Exponent < 0 && this.Exponent > qd1Exponent) //did the exponent get larger after adding something negative?
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }
            else if (qd2Exponent > 0 && this.Exponent < qd1Exponent) //did the exponent get smaller when it should have gotten larger?
            {
                this.SignificandBits = 0;
                this.Exponent = this.SignificandBits >= highestBit ? negativeInfinityExponent : infinityExponent; //overflow
            }
            else if (this.Exponent < exponentLowerBound) //check for underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }
            else if (this.Exponent > exponentUpperBound) //overflow
            {
                this.SignificandBits = 0;
                this.Exponent = this.SignificandBits >= highestBit ? negativeInfinityExponent : infinityExponent; //overflow
            }

            #region Multiply with reduced branching (slightly faster?)

            //zeros
            ////if (this.Exponent == long.MinValue)// || multiplier.Exponent == long.MinValue)
            ////{
            ////    this.Exponent = long.MinValue;
            ////    this.Significand = 0;
            ////    return;
            ////}

            //ulong high1 = (this.Significand | highestBit ) >> 32; //de-implicitize the 1
            //ulong high2 = (multiplier.Significand | highestBit) >> 32;

            ////because the MSB of both significands is 1, the MSB of the result will also be 1, and the product of low bits on both significands is dropped (and thus we can skip its calculation)
            //ulong significandBits = high1 * high2 + (((this.Significand & lowWordMask) * high2) >> 32) + ((high1 * (multiplier.Significand & lowWordMask)) >> 32);

            //if (significandBits < (1UL << 63)) //first bit clear?
            //{
            //    long zeroMask = ((this.Exponent ^ -this.Exponent) & (multiplier.Exponent ^ -multiplier.Exponent)) >> 63;
            //    this.Significand = (ulong)zeroMask & ((this.Significand ^ multiplier.Significand) & highestBit) | ((significandBits << 1) & ~highestBit);
            //    this.Exponent = (zeroMask & (this.Exponent + multiplier.Exponent - 1 + 64)) | (~zeroMask & long.MinValue);
            //}
            //else
            //{
            //    this.Significand = ((this.Significand ^ multiplier.Significand) & highestBit) | (significandBits & ~highestBit);
            //    this.Exponent = this.Exponent + multiplier.Exponent + 64;
            //}

            ////long zeroMask = ((isZeroBit1 >> 63) & (isZeroBit2 >> 63));
            ////this.Significand = (ulong)zeroMask & ((this.Significand ^ multiplier.Significand) & highestBit) | ((significandBits << (int)(1 ^ (significandBits >> 63))) & ~highestBit);
            ////this.Exponent = (zeroMask & (this.Exponent + multiplier.Exponent - 1 + 64 + (long)(significandBits >> 63))) | (~zeroMask & long.MinValue);

            #endregion
        }

        /// <summary>
        /// Multiplies this Quad by a given multiplier, but does not check for underflow or overflow in the result.
        /// This is substantially (~20%) faster than the standard Multiply() method.
        /// </summary>
        /// <param name="multiplier"></param>
        public void MultiplyUnchecked(Quad multiplier)
        {
            if (this.Exponent <= notANumberExponent) //zero/infinity/NaN * something
            {
                Quad result = specialMultiplicationTable[(int)(this.Exponent - zeroExponent), multiplier.Exponent > notANumberExponent ? (int)(4 + (multiplier.SignificandBits >> 63)) : (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }
            else if (multiplier.Exponent <= notANumberExponent) //finite * zero/infinity/NaN
            {
                Quad result = specialMultiplicationTable[(int)(4 + (this.SignificandBits >> 63)), (int)(multiplier.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }

            ulong high1 = (this.SignificandBits | highestBit) >> 32; //de-implicitize the 1
            ulong high2 = (multiplier.SignificandBits | highestBit) >> 32;

            //because the MSB of both significands is 1, the MSB of the result will also be 1, and the product of low bits on both significands is dropped (and thus we can skip its calculation)
            ulong significandBits = high1 * high2 + (((this.SignificandBits & lowWordMask) * high2) >> 32) + ((high1 * (multiplier.SignificandBits & lowWordMask)) >> 32);

            long qd2Exponent;
            //            long qd1Exponent = this.Exponent;
            if (significandBits < (1UL << 63))
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & highestBit) | ((significandBits << 1) & ~highestBit);
                qd2Exponent = multiplier.Exponent - 1 + 64;
                this.Exponent = this.Exponent + qd2Exponent;
            }
            else
            {
                this.SignificandBits = ((this.SignificandBits ^ multiplier.SignificandBits) & highestBit) | (significandBits & ~highestBit);
                qd2Exponent = multiplier.Exponent + 64;
                this.Exponent = this.Exponent + qd2Exponent;
            }
        }

        public unsafe void Add(double valueDouble)
        {
            #region Parse the double

            // Implementation note: the use of goto is generally discouraged,
            // but here the idea is to copy-paste the casting call for double -> Quad
            // to avoid the expense of an additional function call
            // and the use of a single "return" goto target keeps things simple

            Quad value;
            {
                // Translate the double into sign, exponent and mantissa.
                //long bits = BitConverter.DoubleToInt64Bits(value); // doing an unsafe pointer-conversion to get the bits is faster
                ulong bits = *((ulong*)&valueDouble);

                // Note that the shift is sign-extended, hence the test against -1 not 1
                long exponent = (((long)bits >> 52) & 0x7ffL);
                ulong mantissa = (bits) & 0xfffffffffffffUL;

                if (exponent == 0x7ffL)
                {
                    if (mantissa == 0)
                    {
                        if (bits >= highestBit) //sign bit set?
                            value = NegativeInfinity;
                        else
                            value = PositiveInfinity;

                        goto Parsed;
                    }
                    else
                    {
                        value = NaN;
                        goto Parsed;
                    }
                }

                // Subnormal numbers; exponent is effectively one higher,
                // but there's no extra normalisation bit in the mantissa
                if (exponent == 0)
                {
                    if (mantissa == 0)
                    {
                        value = Zero;
                        goto Parsed;
                    }
                    exponent++;

                    int firstSetPosition = nlz(mantissa);
                    mantissa <<= firstSetPosition;
                    exponent -= firstSetPosition;
                }
                else
                {
                    mantissa = mantissa << 11;
                    exponent -= 11;
                }

                exponent -= 1075;

                value.SignificandBits = (highestBit & bits) | mantissa;
                value.Exponent = exponent;
            }
            Parsed:

            #endregion

            #region Addition

            {
                if (this.Exponent <= notANumberExponent) //zero or infinity or NaN + something
                {
                    if (this.Exponent == zeroExponent)
                    {
                        this.SignificandBits = value.SignificandBits;
                        this.Exponent = value.Exponent;
                    }
                    else
                    {
                        Quad result = specialAdditionTable[(int)(this.Exponent - zeroExponent), value.Exponent > notANumberExponent ? (int)(4 + (value.SignificandBits >> 63)) : (int)(value.Exponent - zeroExponent)];
                        this.SignificandBits = result.SignificandBits;
                        this.Exponent = result.Exponent;
                    }

                    return;
                }
                else if (value.Exponent <= notANumberExponent) //finite + (infinity or NaN)
                {
                    if (value.Exponent != zeroExponent)
                    {
                        Quad result = specialAdditionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(value.Exponent - zeroExponent)];
                        this.SignificandBits = result.SignificandBits;
                        this.Exponent = result.Exponent;
                    }
                    return; //if value == 0, no need to change
                }

                if ((this.SignificandBits ^ value.SignificandBits) >= highestBit) //this and value have different signs--use subtraction instead
                {
                    Subtract(new Quad(value.SignificandBits ^ highestBit, value.Exponent));
                    return;
                }

                if (this.Exponent > value.Exponent)
                {
                    if (this.Exponent >= value.Exponent + 64)
                        return; //value too small to make a difference
                    else
                    {
                        ulong bits = (this.SignificandBits | highestBit) + ((value.SignificandBits | highestBit) >> (int)(this.Exponent - value.Exponent));

                        if (bits < highestBit) //this can only happen in an overflow
                        {
                            this.SignificandBits = (this.SignificandBits & highestBit) | (bits >> 1);
                            this.Exponent = this.Exponent + 1;
                        }
                        else
                        {
                            this.SignificandBits = (this.SignificandBits & highestBit) | (bits & ~highestBit);
                            //this.Exponent = this.Exponent; //exponent stays the same
                        }
                    }
                }
                else if (this.Exponent < value.Exponent)
                {
                    if (value.Exponent >= this.Exponent + 64)
                    {
                        this.SignificandBits = value.SignificandBits; //too small to matter
                        this.Exponent = value.Exponent;
                    }
                    else
                    {
                        ulong bits = (value.SignificandBits | highestBit) + ((this.SignificandBits | highestBit) >> (int)(value.Exponent - this.Exponent));

                        if (bits < highestBit) //this can only happen in an overflow
                        {
                            this.SignificandBits = (value.SignificandBits & highestBit) | (bits >> 1);
                            this.Exponent = value.Exponent + 1;
                        }
                        else
                        {
                            this.SignificandBits = (value.SignificandBits & highestBit) | (bits & ~highestBit);
                            this.Exponent = value.Exponent;
                        }
                    }
                }
                else //expDiff == 0
                {
                    //the MSB must have the same sign, so the MSB will become 0, and logical overflow is guaranteed in this situation (so we can shift right and increment the exponent).
                    this.SignificandBits = ((this.SignificandBits + value.SignificandBits) >> 1) | (this.SignificandBits & highestBit);
                    this.Exponent = this.Exponent + 1;
                }
            }

            #endregion
        }


        public void Add(Quad value)
        {
            #region Addition

            if (this.Exponent <= notANumberExponent) //zero or infinity or NaN + something
            {
                if (this.Exponent == zeroExponent)
                {
                    this.SignificandBits = value.SignificandBits;
                    this.Exponent = value.Exponent;
                }
                else
                {
                    Quad result = specialAdditionTable[(int)(this.Exponent - zeroExponent), value.Exponent > notANumberExponent ? (int)(4 + (value.SignificandBits >> 63)) : (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }

                return;
            }
            else if (value.Exponent <= notANumberExponent) //finite + (infinity or NaN)
            {
                if (value.Exponent != zeroExponent)
                {
                    Quad result = specialAdditionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }
                return; //if value == 0, no need to change
            }

            if ((this.SignificandBits ^ value.SignificandBits) >= highestBit) //this and value have different signs--use subtraction instead
            {
                Subtract(new Quad(value.SignificandBits ^ highestBit, value.Exponent));
                return;
            }

            if (this.Exponent > value.Exponent)
            {
                if (this.Exponent >= value.Exponent + 64)
                    return; //value too small to make a difference
                else
                {
                    ulong bits = (this.SignificandBits | highestBit) + ((value.SignificandBits | highestBit) >> (int)(this.Exponent - value.Exponent));

                    if (bits < highestBit) //this can only happen in an overflow
                    {
                        this.SignificandBits = (this.SignificandBits & highestBit) | (bits >> 1);
                        this.Exponent = this.Exponent + 1;
                    }
                    else
                    {
                        this.SignificandBits = (this.SignificandBits & highestBit) | (bits & ~highestBit);
                        //this.Exponent = this.Exponent; //exponent stays the same
                    }
                }
            }
            else if (this.Exponent < value.Exponent)
            {
                if (value.Exponent >= this.Exponent + 64)
                {
                    this.SignificandBits = value.SignificandBits; //too small to matter
                    this.Exponent = value.Exponent;
                }
                else
                {
                    ulong bits = (value.SignificandBits | highestBit) + ((this.SignificandBits | highestBit) >> (int)(value.Exponent - this.Exponent));

                    if (bits < highestBit) //this can only happen in an overflow
                    {
                        this.SignificandBits = (value.SignificandBits & highestBit) | (bits >> 1);
                        this.Exponent = value.Exponent + 1;
                    }
                    else
                    {
                        this.SignificandBits = (value.SignificandBits & highestBit) | (bits & ~highestBit);
                        this.Exponent = value.Exponent;
                    }
                }
            }
            else //expDiff == 0
            {
                //the MSB must have the same sign, so the MSB will become 0, and logical overflow is guaranteed in this situation (so we can shift right and increment the exponent).
                this.SignificandBits = ((this.SignificandBits + value.SignificandBits) >> 1) | (this.SignificandBits & highestBit);
                this.Exponent = this.Exponent + 1;
            }

            #endregion
        }

        public unsafe void Subtract(double valueDouble)
        {
            #region Parse the double

            // Implementation note: the use of goto is generally discouraged,
            // but here the idea is to copy-paste the casting call for double -> Quad
            // to avoid the expense of an additional function call
            // and the use of a single "return" goto target keeps things simple

            Quad value;
            {
                // Translate the double into sign, exponent and mantissa.
                //long bits = BitConverter.DoubleToInt64Bits(value); // doing an unsafe pointer-conversion to get the bits is faster
                ulong bits = *((ulong*)&valueDouble);

                // Note that the shift is sign-extended, hence the test against -1 not 1
                long exponent = (((long)bits >> 52) & 0x7ffL);
                ulong mantissa = (bits) & 0xfffffffffffffUL;

                if (exponent == 0x7ffL)
                {
                    if (mantissa == 0)
                    {
                        if (bits >= highestBit) //sign bit set?
                            value = NegativeInfinity;
                        else
                            value = PositiveInfinity;

                        goto Parsed;
                    }
                    else
                    {
                        value = NaN;
                        goto Parsed;
                    }
                }

                // Subnormal numbers; exponent is effectively one higher,
                // but there's no extra normalisation bit in the mantissa
                if (exponent == 0)
                {
                    if (mantissa == 0)
                    {
                        value = Zero;
                        goto Parsed;
                    }
                    exponent++;

                    int firstSetPosition = nlz(mantissa);
                    mantissa <<= firstSetPosition;
                    exponent -= firstSetPosition;
                }
                else
                {
                    mantissa = mantissa << 11;
                    exponent -= 11;
                }

                exponent -= 1075;

                value.SignificandBits = (highestBit & bits) | mantissa;
                value.Exponent = exponent;
            }
            Parsed:

            #endregion

            #region Subtraction

            if (this.Exponent <= notANumberExponent) //infinity or NaN - something
            {
                if (this.Exponent == zeroExponent)
                {
                    this.SignificandBits = value.SignificandBits ^ highestBit; //negate value
                    this.Exponent = value.Exponent;
                }
                else
                {
                    Quad result = specialSubtractionTable[(int)(this.Exponent - zeroExponent), value.Exponent > notANumberExponent ? (int)(4 + (value.SignificandBits >> 63)) : (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }

                return;
            }
            else if (value.Exponent <= notANumberExponent) //finite - (infinity or NaN)
            {
                if (value.Exponent != zeroExponent)
                {
                    Quad result = specialSubtractionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }

                return;
            }

            if ((this.SignificandBits ^ value.SignificandBits) >= highestBit) //this and value have different signs--use addition instead
            {
                this.Add(new Quad(value.SignificandBits ^ highestBit, value.Exponent));
                return;
            }

            if (this.Exponent > value.Exponent)
            {
                if (this.Exponent >= value.Exponent + 64)
                    return; //value too small to make a difference
                else
                {
                    ulong bits = (this.SignificandBits | highestBit) - ((value.SignificandBits | highestBit) >> (int)(this.Exponent - value.Exponent));

                    //make sure MSB is 1
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~highestBit) | (this.SignificandBits & highestBit);
                    this.Exponent = this.Exponent - highestBitPos;
                }
            }
            else if (this.Exponent < value.Exponent) //must subtract our significand from value, and switch the sign
            {
                if (value.Exponent >= this.Exponent + 64)
                {
                    this.SignificandBits = value.SignificandBits ^ highestBit;
                    this.Exponent = value.Exponent;
                    return;
                }
                else
                {
                    ulong bits = (value.SignificandBits | highestBit) - ((this.SignificandBits | highestBit) >> (int)(value.Exponent - this.Exponent));

                    //make sure MSB is 1
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~highestBit) | (~value.SignificandBits & highestBit);
                    this.Exponent = value.Exponent - highestBitPos;
                }
            }
            else // (this.Exponent == value.Exponent)
            {
                if (value.SignificandBits > this.SignificandBits) //must switch sign
                {
                    ulong bits = value.SignificandBits - this.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~highestBit) | (~value.SignificandBits & highestBit);
                    this.Exponent = value.Exponent - highestBitPos;
                }
                else if (value.SignificandBits < this.SignificandBits) //sign remains the same
                {
                    ulong bits = this.SignificandBits - value.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~highestBit) | (this.SignificandBits & highestBit);
                    this.Exponent = this.Exponent - highestBitPos;
                }
                else //this == value
                {
                    //result is 0
                    this.SignificandBits = 0;
                    this.Exponent = zeroExponent;
                    return;
                }
            }

            if (this.Exponent < exponentLowerBound) //catch underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }

            #endregion
        }

        public void Subtract(Quad value)
        {
            #region Subtraction

            if (this.Exponent <= notANumberExponent) //infinity or NaN - something
            {
                if (this.Exponent == zeroExponent)
                {
                    this.SignificandBits = value.SignificandBits ^ highestBit; //negate value
                    this.Exponent = value.Exponent;
                }
                else
                {
                    Quad result = specialSubtractionTable[(int)(this.Exponent - zeroExponent), value.Exponent > notANumberExponent ? (int)(4 + (value.SignificandBits >> 63)) : (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }

                return;
            }
            else if (value.Exponent <= notANumberExponent) //finite - (infinity or NaN)
            {
                if (value.Exponent != zeroExponent)
                {
                    Quad result = specialSubtractionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(value.Exponent - zeroExponent)];
                    this.SignificandBits = result.SignificandBits;
                    this.Exponent = result.Exponent;
                }

                return;
            }

            if ((this.SignificandBits ^ value.SignificandBits) >= highestBit) //this and value have different signs--use addition instead
            {
                this.Add(new Quad(value.SignificandBits ^ highestBit, value.Exponent));
                return;
            }

            if (this.Exponent > value.Exponent)
            {
                if (this.Exponent >= value.Exponent + 64)
                    return; //value too small to make a difference
                else
                {
                    ulong bits = (this.SignificandBits | highestBit) - ((value.SignificandBits | highestBit) >> (int)(this.Exponent - value.Exponent));

                    //make sure MSB is 1
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~highestBit) | (this.SignificandBits & highestBit);
                    this.Exponent = this.Exponent - highestBitPos;
                }
            }
            else if (this.Exponent < value.Exponent) //must subtract our significand from value, and switch the sign
            {
                if (value.Exponent >= this.Exponent + 64)
                {
                    this.SignificandBits = value.SignificandBits ^ highestBit;
                    this.Exponent = value.Exponent;
                    return;
                }
                else
                {
                    ulong bits = (value.SignificandBits | highestBit) - ((this.SignificandBits | highestBit) >> (int)(value.Exponent - this.Exponent));

                    //make sure MSB is 1
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~highestBit) | (~value.SignificandBits & highestBit);
                    this.Exponent = value.Exponent - highestBitPos;
                }
            }
            else // (this.Exponent == value.Exponent)
            {
                if (value.SignificandBits > this.SignificandBits) //must switch sign
                {
                    ulong bits = value.SignificandBits - this.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~highestBit) | (~value.SignificandBits & highestBit);
                    this.Exponent = value.Exponent - highestBitPos;
                }
                else if (value.SignificandBits < this.SignificandBits) //sign remains the same
                {
                    ulong bits = this.SignificandBits - value.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    this.SignificandBits = ((bits << highestBitPos) & ~highestBit) | (this.SignificandBits & highestBit);
                    this.Exponent = this.Exponent - highestBitPos;
                }
                else //this == value
                {
                    //result is 0
                    this.SignificandBits = 0;
                    this.Exponent = zeroExponent;
                    return;
                }
            }

            if (this.Exponent < exponentLowerBound) //catch underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent;
            }

            #endregion
        }

        public unsafe void Divide(double divisorDouble)
        {
            #region Parse the double

            // Implementation note: the use of goto is generally discouraged,
            // but here the idea is to copy-paste the casting call for double -> Quad
            // to avoid the expense of an additional function call
            // and the use of a single "return" goto target keeps things simple

            Quad divisor;
            {
                // Translate the double into sign, exponent and mantissa.
                //long bits = BitConverter.DoubleToInt64Bits(divisor); // doing an unsafe pointer-conversion to get the bits is faster
                ulong bits = *((ulong*)&divisorDouble);

                // Note that the shift is sign-extended, hence the test against -1 not 1
                long exponent = (((long)bits >> 52) & 0x7ffL);
                ulong mantissa = (bits) & 0xfffffffffffffUL;

                if (exponent == 0x7ffL)
                {
                    if (mantissa == 0)
                    {
                        if (bits >= highestBit) //sign bit set?
                            divisor = NegativeInfinity;
                        else
                            divisor = PositiveInfinity;

                        goto Parsed;
                    }
                    else
                    {
                        divisor = NaN;
                        goto Parsed;
                    }
                }

                // Subnormal numbers; exponent is effectively one higher,
                // but there's no extra normalisation bit in the mantissa
                if (exponent == 0)
                {
                    if (mantissa == 0)
                    {
                        divisor = Zero;
                        goto Parsed;
                    }
                    exponent++;

                    int firstSetPosition = nlz(mantissa);
                    mantissa <<= firstSetPosition;
                    exponent -= firstSetPosition;
                }
                else
                {
                    mantissa = mantissa << 11;
                    exponent -= 11;
                }

                exponent -= 1075;

                divisor.SignificandBits = (highestBit & bits) | mantissa;
                divisor.Exponent = exponent;
            }
            Parsed:

            #endregion

            #region Division

            if (this.Exponent <= notANumberExponent) //zero/infinity/NaN divided by something
            {
                Quad result = specialDivisionTable[(int)(this.Exponent - zeroExponent), divisor.Exponent > notANumberExponent ? (int)(4 + (divisor.SignificandBits >> 63)) : (int)(divisor.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }
            else if (divisor.Exponent <= notANumberExponent) //finite divided by zero/infinity/NaN
            {
                Quad result = specialDivisionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(divisor.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }

            ulong un1 = 0, // Norm. dividend LSD's.
                vn1,
                vn0, // Norm. divisor digits.
                q1,
                q0, // Quotient digits.
                un21, // Dividend digit pairs.
                rhat; // A remainder.

            //result.Significand = highestBit & (this.Significand ^ divisor.Significand); //determine the sign bit

            //this.Significand |= highestBit; //de-implicitize the 1 before the binary point
            //divisor.Significand |= highestBit;

            long adjExponent = 0;
            ulong thisAdjSignificand = this.SignificandBits | highestBit;
            ulong divisorAdjSignificand = divisor.SignificandBits | highestBit;

            if (thisAdjSignificand >= divisorAdjSignificand)
            {
                //need to make this's significand smaller than divisor's
                adjExponent = 1;
                un1 = (this.SignificandBits & 1) << 31;
                thisAdjSignificand = thisAdjSignificand >> 1;
            }

            vn1 = divisorAdjSignificand >> 32; // Break divisor up into
            vn0 = divisor.SignificandBits & 0xFFFFFFFF; // two 32-bit digits.

            q1 = thisAdjSignificand / vn1; // Compute the first
            rhat = thisAdjSignificand - q1 * vn1; // quotient digit, q1.
            again1:
            if (q1 >= b || q1 * vn0 > b * rhat + un1)
            {
                q1 = q1 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again1;
            }

            un21 = thisAdjSignificand * b + un1 - q1 * divisorAdjSignificand; // Multiply and subtract.

            q0 = un21 / vn1; // Compute the second
            rhat = un21 - q0 * vn1; // quotient digit, q0.
            again2:
            if (q0 >= b || q0 * vn0 > b * rhat)
            {
                q0 = q0 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again2;
            }

            thisAdjSignificand = q1 * b + q0; //convenient place to store intermediate result

            //if (this.Significand == 0) //the final significand should never be 0
            //    result.Exponent = 0;
            //else

            long originalExponent;
            long divisorExponent;

            if (thisAdjSignificand < (1UL << 63))
            {
                this.SignificandBits = (~highestBit & (thisAdjSignificand << 1)) | ((this.SignificandBits ^ divisor.SignificandBits) & highestBit);

                originalExponent = this.Exponent - 1 + adjExponent;
                divisorExponent = divisor.Exponent + 64;
            }
            else
            {
                this.SignificandBits = (~highestBit & thisAdjSignificand) | ((this.SignificandBits ^ divisor.SignificandBits) & highestBit);

                originalExponent = this.Exponent + adjExponent;
                divisorExponent = divisor.Exponent + 64;
            }

            this.Exponent = originalExponent - divisorExponent;

            //now check for underflow or overflow
            if (divisorExponent > 0 && this.Exponent > originalExponent) //underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent; //new value is 0
            }
            else if (divisorExponent < 0 && this.Exponent < originalExponent) //overflow
            {
                this.SignificandBits = 0; // (this.SignificandBits & highestBit);
                this.Exponent = this.SignificandBits >= highestBit ? negativeInfinityExponent : infinityExponent;
            }
            else if (this.Exponent < exponentLowerBound)
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent; //new value is 0
            }
            else if (this.Exponent > exponentUpperBound)
            {
                this.SignificandBits = 0; // (this.SignificandBits & highestBit);
                this.Exponent = this.SignificandBits >= highestBit ? negativeInfinityExponent : infinityExponent;
            }

            #endregion
        }

        public void Divide(Quad divisor)
        {
            #region Division

            if (this.Exponent <= notANumberExponent) //zero/infinity/NaN divided by something
            {
                Quad result = specialDivisionTable[(int)(this.Exponent - zeroExponent), divisor.Exponent > notANumberExponent ? (int)(4 + (divisor.SignificandBits >> 63)) : (int)(divisor.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }
            else if (divisor.Exponent <= notANumberExponent) //finite divided by zero/infinity/NaN
            {
                Quad result = specialDivisionTable[(int)(4 + (this.SignificandBits >> 63)), (int)(divisor.Exponent - zeroExponent)];
                this.SignificandBits = result.SignificandBits;
                this.Exponent = result.Exponent;
                return;
            }

            ulong un1 = 0, // Norm. dividend LSD's.
                vn1,
                vn0, // Norm. divisor digits.
                q1,
                q0, // Quotient digits.
                un21, // Dividend digit pairs.
                rhat; // A remainder.

            //result.Significand = highestBit & (this.Significand ^ divisor.Significand); //determine the sign bit

            //this.Significand |= highestBit; //de-implicitize the 1 before the binary point
            //divisor.Significand |= highestBit;

            long adjExponent = 0;
            ulong thisAdjSignificand = this.SignificandBits | highestBit;
            ulong divisorAdjSignificand = divisor.SignificandBits | highestBit;

            if (thisAdjSignificand >= divisorAdjSignificand)
            {
                //need to make this's significand smaller than divisor's
                adjExponent = 1;
                un1 = (this.SignificandBits & 1) << 31;
                thisAdjSignificand = thisAdjSignificand >> 1;
            }

            vn1 = divisorAdjSignificand >> 32; // Break divisor up into
            vn0 = divisor.SignificandBits & 0xFFFFFFFF; // two 32-bit digits.

            q1 = thisAdjSignificand / vn1; // Compute the first
            rhat = thisAdjSignificand - q1 * vn1; // quotient digit, q1.
            again1:
            if (q1 >= b || q1 * vn0 > b * rhat + un1)
            {
                q1 = q1 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again1;
            }

            un21 = thisAdjSignificand * b + un1 - q1 * divisorAdjSignificand; // Multiply and subtract.

            q0 = un21 / vn1; // Compute the second
            rhat = un21 - q0 * vn1; // quotient digit, q0.
            again2:
            if (q0 >= b || q0 * vn0 > b * rhat)
            {
                q0 = q0 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again2;
            }

            thisAdjSignificand = q1 * b + q0; //convenient place to store intermediate result

            //if (this.Significand == 0) //the final significand should never be 0
            //    result.Exponent = 0;
            //else

            long originalExponent;
            long divisorExponent;

            if (thisAdjSignificand < (1UL << 63))
            {
                this.SignificandBits = (~highestBit & (thisAdjSignificand << 1)) | ((this.SignificandBits ^ divisor.SignificandBits) & highestBit);

                originalExponent = this.Exponent - 1 + adjExponent;
                divisorExponent = divisor.Exponent + 64;
            }
            else
            {
                this.SignificandBits = (~highestBit & thisAdjSignificand) | ((this.SignificandBits ^ divisor.SignificandBits) & highestBit);

                originalExponent = this.Exponent + adjExponent;
                divisorExponent = divisor.Exponent + 64;
            }

            this.Exponent = originalExponent - divisorExponent;

            //now check for underflow or overflow
            if (divisorExponent > 0 && this.Exponent > originalExponent) //underflow
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent; //new value is 0
            }
            else if (divisorExponent < 0 && this.Exponent < originalExponent) //overflow
            {
                this.SignificandBits = 0; // (this.SignificandBits & highestBit);
                this.Exponent = this.SignificandBits >= highestBit ? negativeInfinityExponent : infinityExponent;
            }
            else if (this.Exponent < exponentLowerBound)
            {
                this.SignificandBits = 0;
                this.Exponent = zeroExponent; //new value is 0
            }
            else if (this.Exponent > exponentUpperBound)
            {
                this.SignificandBits = 0; // (this.SignificandBits & highestBit);
                this.Exponent = this.SignificandBits >= highestBit ? negativeInfinityExponent : infinityExponent;
            }

            #endregion
        }

        #endregion

        #region Operators

        /// <summary>
        /// Efficiently multiplies the Quad by 2^shift.
        /// </summary>
        /// <param name="qd"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public static Quad operator <<(Quad qd, int shift)
        {
            if (qd.Exponent <= notANumberExponent)
                return qd; //finite * infinity == infinity, finite * NaN == NaN, finite * 0 == 0
            else
                return new Quad(qd.SignificandBits, qd.Exponent + shift);
        }

        /// <summary>
        /// Efficiently divides the Quad by 2^shift.
        /// </summary>
        /// <param name="qd"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public static Quad operator >>(Quad qd, int shift)
        {
            if (qd.Exponent <= notANumberExponent)
                return qd; //infinity / finite == infinity, NaN / finite == NaN, 0 / finite == 0
            else
                return new Quad(qd.SignificandBits, qd.Exponent - shift);
        }

        /// <summary>
        /// Efficiently multiplies the Quad by 2^shift.
        /// </summary>
        /// <param name="qd"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public static Quad LeftShift(Quad qd, long shift)
        {
            if (qd.Exponent <= notANumberExponent)
                return qd; //finite * infinity == infinity, finite * NaN == NaN, finite * 0 == 0
            else
                return new Quad(qd.SignificandBits, qd.Exponent + shift);
        }

        /// <summary>
        /// Efficiently divides the Quad by 2^shift.
        /// </summary>
        /// <param name="qd"></param>
        /// <param name="shift"></param>
        /// <returns></returns>
        public static Quad RightShift(Quad qd, long shift)
        {
            if (qd.Exponent <= notANumberExponent)
                return qd; //infinity / finite == infinity, NaN / finite == NaN, 0 / finite == 0
            else
                return new Quad(qd.SignificandBits, qd.Exponent - shift);
        }

        /// <summary>
        /// Divides one Quad by another and returns the result
        /// </summary>
        /// <param name="qd1"></param>
        /// <param name="qd2"></param>
        /// <returns></returns>
        /// <remarks>
        /// This code is a heavily modified derivation of a division routine given by http://www.hackersdelight.org/HDcode/divlu.c.txt ,
        /// which has a very liberal (public domain-like) license attached: http://www.hackersdelight.org/permissions.htm
        /// </remarks>
        public static Quad operator /(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN divided by something
                return specialDivisionTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite divided by zero/infinity/NaN
                return specialDivisionTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            if (qd2.Exponent == long.MinValue)
                throw new DivideByZeroException();
            else if (qd1.Exponent == long.MinValue)
                return Zero;

            ulong un1 = 0, // Norm. dividend LSD's.
                vn1,
                vn0, // Norm. divisor digits.
                q1,
                q0, // Quotient digits.
                un21, // Dividend digit pairs.
                rhat; // A remainder.

            long adjExponent = 0;
            ulong qd1AdjSignificand = qd1.SignificandBits | highestBit; //de-implicitize the 1 before the binary point
            ulong qd2AdjSignificand = qd2.SignificandBits | highestBit; //de-implicitize the 1 before the binary point

            if (qd1AdjSignificand >= qd2AdjSignificand)
            {
                // need to make qd1's significand smaller than qd2's
                // If we were faithful to the original code this method derives from,
                // we would branch on qd1AdjSignificand > qd2AdjSignificand instead.
                // However, this results in undesirable results like (in binary) 11/11 = 0.11111...,
                // where the result should be 1.0.  Thus, we branch on >=, which prevents this problem.
                adjExponent = 1;
                un1 = (qd1.SignificandBits & 1) << 31;
                qd1AdjSignificand = qd1AdjSignificand >> 1;
            }

            vn1 = qd2AdjSignificand >> 32; // Break divisor up into
            vn0 = qd2.SignificandBits & 0xFFFFFFFF; // two 32-bit digits.

            q1 = qd1AdjSignificand / vn1; // Compute the first
            rhat = qd1AdjSignificand - q1 * vn1; // quotient digit, q1.
            again1:
            if (q1 >= b || q1 * vn0 > b * rhat + un1)
            {
                q1 = q1 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again1;
            }

            un21 = qd1AdjSignificand * b + un1 - q1 * qd2AdjSignificand; // Multiply and subtract.

            q0 = un21 / vn1; // Compute the second
            rhat = un21 - q0 * vn1; // quotient digit, q0.
            again2:
            if (q0 >= b || q0 * vn0 > b * rhat)
            {
                q0 = q0 - 1;
                rhat = rhat + vn1;
                if (rhat < b) goto again2;
            }

            qd1AdjSignificand = q1 * b + q0; //convenient place to store intermediate result

            //if (qd1.Significand == 0) //the final significand should never be 0
            //    result.Exponent = 0;
            //else

            long originalExponent;
            long divisorExponent;
            Quad result;

            if (qd1AdjSignificand < (1UL << 63))
            {
                result.SignificandBits = (~highestBit & (qd1AdjSignificand << 1)) | ((qd1.SignificandBits ^ qd2.SignificandBits) & highestBit);

                originalExponent = qd1.Exponent - 1 + adjExponent;
                divisorExponent = qd2.Exponent + 64;
            }
            else
            {
                result.SignificandBits = (~highestBit & qd1AdjSignificand) | ((qd1.SignificandBits ^ qd2.SignificandBits) & highestBit);

                originalExponent = qd1.Exponent + adjExponent;
                divisorExponent = qd2.Exponent + 64;
            }

            result.Exponent = originalExponent - divisorExponent;

            //now check for underflow or overflow
            if (divisorExponent > 0 && result.Exponent > originalExponent) //underflow
                return Zero;
            else if (divisorExponent < 0 && result.Exponent < originalExponent) //overflow
                return result.SignificandBits >= highestBit ? NegativeInfinity : PositiveInfinity;
            else if (result.Exponent < exponentLowerBound)
                return Zero;
            else if (result.Exponent > exponentUpperBound)
                return result.SignificandBits >= highestBit ? NegativeInfinity : PositiveInfinity;
            else
                return result;
        }

        /// <summary>
        /// Divides two numbers and gets the remainder.
        /// This is equivalent to qd1 - (qd2 * Truncate(qd1 / qd2)).
        /// </summary>
        /// <param name="qd1"></param>
        /// <param name="qd2"></param>
        /// <returns></returns>
        public static Quad operator %(Quad qd1, Quad qd2)
        {
            if (qd2.Exponent == infinityExponent || qd2.Exponent == negativeInfinityExponent)
            {
                if (qd1.Exponent == infinityExponent || qd1.Exponent == negativeInfinityExponent)
                    return NaN;
                else
                    return qd1;
            }

            return qd1 - (qd2 * Truncate(qd1 / qd2));
        }

        public static Quad operator -(Quad qd)
        {
            if (qd.Exponent <= notANumberExponent)
            {
                if (qd.Exponent == infinityExponent) return NegativeInfinity;
                else if (qd.Exponent == negativeInfinityExponent) return PositiveInfinity;
                else return qd;
            }
            else
                return new Quad(qd.SignificandBits ^ highestBit, qd.Exponent); //just swap the sign bit
        }

        public static Quad operator +(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero or infinity or NaN + something
            {
                if (qd1.Exponent == zeroExponent) return qd2;
                else return specialAdditionTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            }
            else if (qd2.Exponent <= notANumberExponent) //finite + (infinity or NaN)
            {
                if (qd2.Exponent == zeroExponent) return qd1;
                else return specialAdditionTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];
            }

            if ((qd1.SignificandBits ^ qd2.SignificandBits) >= highestBit) //qd1 and qd2 have different signs--use subtraction instead
            {
                return qd1 - new Quad(qd2.SignificandBits ^ highestBit, qd2.Exponent);
            }

            Quad result;
            if (qd1.Exponent > qd2.Exponent)
            {
                if (qd1.Exponent >= qd2.Exponent + 64)
                    return qd1; //qd2 too small to make a difference
                else
                {
                    ulong bits = (qd1.SignificandBits | highestBit) + ((qd2.SignificandBits | highestBit) >> (int)(qd1.Exponent - qd2.Exponent));

                    if (bits < highestBit) //this can only happen in an overflow
                        result = new Quad((qd1.SignificandBits & highestBit) | (bits >> 1), qd1.Exponent + 1);
                    else
                        return new Quad((qd1.SignificandBits & highestBit) | (bits & ~highestBit), qd1.Exponent);
                }
            }
            else if (qd1.Exponent < qd2.Exponent)
            {
                if (qd2.Exponent >= qd1.Exponent + 64)
                    return qd2; //qd1 too small to matter
                else
                {
                    ulong bits = (qd2.SignificandBits | highestBit) + ((qd1.SignificandBits | highestBit) >> (int)(qd2.Exponent - qd1.Exponent));

                    if (bits < highestBit) //this can only happen in an overflow
                        result = new Quad((qd2.SignificandBits & highestBit) | (bits >> 1), qd2.Exponent + 1);
                    else
                        return new Quad((qd2.SignificandBits & highestBit) | (bits & ~highestBit), qd2.Exponent);
                }
            }
            else //expDiff == 0
            {
                //the MSB must have the same sign, so the MSB will become 0, and logical overflow is guaranteed in this situation (so we can shift right and increment the exponent).
                result = new Quad(((qd1.SignificandBits + qd2.SignificandBits) >> 1) | (qd1.SignificandBits & highestBit), qd1.Exponent + 1);
            }

            if (result.Exponent > exponentUpperBound) //overflow check
                return result.SignificandBits >= highestBit ? NegativeInfinity : PositiveInfinity;
            else
                return result;
        }

        public static Quad operator -(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //infinity or NaN - something
            {
                if (qd1.Exponent == zeroExponent) return -qd2;
                else return specialSubtractionTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            }
            else if (qd2.Exponent <= notANumberExponent) //finite - (infinity or NaN)
            {
                if (qd2.Exponent == zeroExponent) return qd1;
                else return specialSubtractionTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];
            }

            if ((qd1.SignificandBits ^ qd2.SignificandBits) >= highestBit) //qd1 and qd2 have different signs--use addition instead
            {
                return qd1 + new Quad(qd2.SignificandBits ^ highestBit, qd2.Exponent);
            }

            Quad result;
            if (qd1.Exponent > qd2.Exponent)
            {
                if (qd1.Exponent >= qd2.Exponent + 64)
                    return qd1; //qd2 too small to make a difference
                else
                {
                    ulong bits = (qd1.SignificandBits | highestBit) - ((qd2.SignificandBits | highestBit) >> (int)(qd1.Exponent - qd2.Exponent));

                    //make sure MSB is 1
                    int highestBitPos = nlz(bits);
                    result = new Quad(((bits << highestBitPos) & ~highestBit) | (qd1.SignificandBits & highestBit), qd1.Exponent - highestBitPos);
                }
            }
            else if (qd1.Exponent < qd2.Exponent) //must subtract qd1's significand from qd2, and switch the sign
            {
                if (qd2.Exponent >= qd1.Exponent + 64)
                    return new Quad(qd2.SignificandBits ^ highestBit, qd2.Exponent); //qd1 too small to matter, switch sign of qd2 and return

                ulong bits = (qd2.SignificandBits | highestBit) - ((qd1.SignificandBits | highestBit) >> (int)(qd2.Exponent - qd1.Exponent));

                //make sure MSB is 1
                int highestBitPos = nlz(bits);
                result = new Quad(((bits << highestBitPos) & ~highestBit) | (~qd2.SignificandBits & highestBit), qd2.Exponent - highestBitPos);
            }
            else // (qd1.Exponent == qd2.Exponent)
            {
                if (qd2.SignificandBits > qd1.SignificandBits) //must switch sign
                {
                    ulong bits = qd2.SignificandBits - qd1.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    result = new Quad(((bits << highestBitPos) & ~highestBit) | (~qd2.SignificandBits & highestBit), qd2.Exponent - highestBitPos);
                }
                else if (qd2.SignificandBits < qd1.SignificandBits) //sign remains the same
                {
                    ulong bits = qd1.SignificandBits - qd2.SignificandBits; //notice that we don't worry about de-implicitizing the MSB--it'd be eliminated by subtraction anyway
                    int highestBitPos = nlz(bits);
                    result = new Quad(((bits << highestBitPos) & ~highestBit) | (qd1.SignificandBits & highestBit), qd1.Exponent - highestBitPos);
                }
                else //qd1 == qd2
                    return Zero;
            }

            if (result.Exponent < exponentLowerBound) //handle underflow
                return Zero;
            else
                return result;
        }

        public static Quad operator *(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN * something
                return specialMultiplicationTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite * zero/infinity/NaN
                return specialMultiplicationTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            ulong high1 = (qd1.SignificandBits | highestBit) >> 32; //de-implicitize the 1
            ulong high2 = (qd2.SignificandBits | highestBit) >> 32;

            //because the MSB of both significands is 1, the MSB of the result will also be 1, and the product of low bits on both significands is dropped (and thus we can skip its calculation)
            ulong significandBits = high1 * high2 + (((qd1.SignificandBits & lowWordMask) * high2) >> 32) + ((high1 * (qd2.SignificandBits & lowWordMask)) >> 32);

            long qd2Exponent;
            Quad result;
            if (significandBits < (1UL << 63))
            {
                qd2Exponent = qd2.Exponent - 1 + 64;
                result = new Quad(((qd1.SignificandBits ^ qd2.SignificandBits) & highestBit) | ((significandBits << 1) & ~highestBit), qd1.Exponent + qd2Exponent);
            }
            else
            {
                qd2Exponent = qd2.Exponent + 64;
                result = new Quad(((qd1.SignificandBits ^ qd2.SignificandBits) & highestBit) | (significandBits & ~highestBit), qd1.Exponent + qd2Exponent);
            }

            if (qd2Exponent < 0 && result.Exponent > qd1.Exponent) //did the exponent get larger after adding something negative?
                return Zero; //underflow
            else if (qd2Exponent > 0 && result.Exponent < qd1.Exponent) //did the exponent get smaller when it should have gotten larger?
                return result.SignificandBits >= highestBit ? NegativeInfinity : PositiveInfinity; //overflow
            else if (result.Exponent < exponentLowerBound) //check for underflow
                return Zero;
            else if (result.Exponent > exponentUpperBound) //overflow
                return result.SignificandBits >= highestBit ? NegativeInfinity : PositiveInfinity; //overflow
            else
                return result;
        }

        public static Quad operator ++(Quad qd)
        {
            return qd + One;
        }

        public static Quad operator --(Quad qd)
        {
            return qd - One;
        }

        #endregion

        #region Comparison

        /// <summary>
        /// Determines if qd1 is the same value as qd2.
        /// The same rules for doubles are used, e.g. PositiveInfinity == PositiveInfinity, but NaN != NaN.
        /// </summary>
        public static bool operator ==(Quad qd1, Quad qd2)
        {
            return (qd1.SignificandBits == qd2.SignificandBits && qd1.Exponent == qd2.Exponent && qd1.Exponent != notANumberExponent); // || (qd1.Exponent == long.MinValue && qd2.Exponent == long.MinValue);
        }

        /// <summary>
        /// Determines if qd1 is different from qd2.
        /// Always true if qd1 or qd2 is NaN.  False if both qd1 and qd2 are infinity with the same polarity (e.g. PositiveInfinities).
        /// </summary>
        public static bool operator !=(Quad qd1, Quad qd2)
        {
            return (qd1.SignificandBits != qd2.SignificandBits || qd1.Exponent != qd2.Exponent || qd1.Exponent == notANumberExponent); // && (qd1.Exponent != long.MinValue || qd2.Exponent != long.MinValue);
        }

        public static bool operator >(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN * something
                return specialGreaterThanTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite * zero/infinity/NaN
                return specialGreaterThanTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            //There is probably a faster way to accomplish this by cleverly exploiting signed longs
            switch ((qd1.SignificandBits & highestBit) | ((qd2.SignificandBits & highestBit) >> 1))
            {
                case highestBit: //qd1 is negative, qd2 positive
                    return false;
                case secondHighestBit: //qd1 positive, qd2 negative
                    return true;
                case highestBit | secondHighestBit: //both negative
                    return qd1.Exponent < qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits < qd2.SignificandBits);
                default: //both positive
                    return qd1.Exponent > qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits > qd2.SignificandBits);
            }
        }

        public static bool operator <(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN * something
                return specialLessThanTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite * zero/infinity/NaN
                return specialLessThanTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            switch ((qd1.SignificandBits & highestBit) | ((qd2.SignificandBits & highestBit) >> 1))
            {
                case highestBit: //qd1 is negative, qd2 positive
                    return true;
                case secondHighestBit: //qd1 positive, qd2 negative
                    return false;
                case highestBit | secondHighestBit: //both negative
                    return qd1.Exponent > qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits > qd2.SignificandBits);
                default: //both positive
                    return qd1.Exponent < qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits < qd2.SignificandBits);
            }
        }

        public static bool operator >=(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN * something
                return specialGreaterEqualThanTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite * zero/infinity/NaN
                return specialGreaterEqualThanTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            switch ((qd1.SignificandBits & highestBit) | ((qd2.SignificandBits & highestBit) >> 1))
            {
                case highestBit: //qd1 is negative, qd2 positive
                    return false;
                case secondHighestBit: //qd1 positive, qd2 negative
                    return true;
                case highestBit | secondHighestBit: //both negative
                    return qd1.Exponent < qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits <= qd2.SignificandBits);
                default: //both positive
                    return qd1.Exponent > qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits >= qd2.SignificandBits);
            }
        }

        public static bool operator <=(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent <= notANumberExponent) //zero/infinity/NaN * something
                return specialLessEqualThanTable[(int)(qd1.Exponent - zeroExponent), qd2.Exponent > notANumberExponent ? (int)(4 + (qd2.SignificandBits >> 63)) : (int)(qd2.Exponent - zeroExponent)];
            else if (qd2.Exponent <= notANumberExponent) //finite * zero/infinity/NaN
                return specialLessEqualThanTable[(int)(4 + (qd1.SignificandBits >> 63)), (int)(qd2.Exponent - zeroExponent)];

            switch ((qd1.SignificandBits & highestBit) | ((qd2.SignificandBits & highestBit) >> 1))
            {
                case highestBit: //qd1 is negative, qd2 positive
                    return true;
                case secondHighestBit: //qd1 positive, qd2 negative
                    return false;
                case highestBit | secondHighestBit: //both negative
                    return qd1.Exponent > qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits >= qd2.SignificandBits);
                default: //both positive
                    return qd1.Exponent < qd2.Exponent || (qd1.Exponent == qd2.Exponent && qd1.SignificandBits <= qd2.SignificandBits);
            }
        }

        #endregion

        #region String parsing

        /// <summary>
        /// Parses decimal number strings in the form of "1234.5678" OR "123E+123" OR "123 e10 etc.".
        ///  </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static Quad Parse(string number)
        {
            if (number.Equals(specialStringTable[1], StringComparison.OrdinalIgnoreCase))
                return PositiveInfinity;
            if (number.Equals(specialStringTable[2], StringComparison.OrdinalIgnoreCase))
                return NegativeInfinity;
            if (number.Equals(specialStringTable[3], StringComparison.OrdinalIgnoreCase))
                return NaN;

            //replace various exponential markers with a common one
            foreach (var s in ExponentialMarkers)
            {
                number = number.Replace(s, "e");
            }

            var sciNotationMarker = number.IndexOf("e");
            if (sciNotationMarker != -1)
            {
                //number assumed to be in format 3.24e34
                Quad result = double.Parse(number.Substring(0, sciNotationMarker));
                var exponentPart = long.Parse(number.Substring(sciNotationMarker + 1, number.Length - sciNotationMarker - 1));
                //protection for player entering massive numbers
                exponentPart = Math.Min(exponentPart, 999999999);
                while (exponentPart > 0)
                {
                    if (exponentPart > 100)
                    {
                        result.Multiply(tenTo100);
                        exponentPart -= 100;
                    }
                    else if (exponentPart > 10)
                    {
                        result.Multiply(tenTo10);
                        exponentPart -= 10;
                    }
                    else
                    {
                        result.Multiply(10);
                        exponentPart -= 1;
                    }
                }

                return result;
            }
            else
            {
                //Can piggyback on BigInteger's parser for this, but this is inefficient.
                //Smarter way is to break the numeric string into chunks and parse each of them using long's parse method, then combine.

                bool negative = number.StartsWith("-");
                if (negative) number = number.Substring(1);

                string left = number, right = null;
                int decimalPoint = number.IndexOf('.');
                if (decimalPoint >= 0)
                {
                    left = number.Substring(0, decimalPoint);
                    right = number.Substring(decimalPoint + 1);
                }

                long leftInt = long.Parse(left);

                Quad result = (Quad)leftInt;
                if (right != null)
                {
                    long rightInt = long.Parse(right);
                    Quad fractional = (Quad)rightInt;

                    // we implicitly multiplied the stuff right of the decimal point by 10^(right.length) to get an integer;
                    // now we must reverse that and add this quantity to our results.
                    result += fractional * (Quad.Pow(new Quad(10L, 0), -right.Length));
                }

                return negative ? -result : result;
            }
        }

        #endregion

        #region Math functions

        /// <summary>
        /// Removes any fractional part of the provided value (rounding down for positive numbers, and rounding up for negative numbers)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Quad Truncate(Quad value)
        {
            if (value.Exponent <= notANumberExponent) return value;

            if (value.Exponent <= -64) return Zero;
            else if (value.Exponent >= 0) return value;
            else
            {
                //clear least significant "-value.exponent" bits that come after the binary point by shifting
                return new Quad((value.SignificandBits >> (int)(-value.Exponent)) << (int)(-value.Exponent), value.Exponent);
            }
        }

        /// <summary>
        /// Returns only the fractional part of the provided value.  Equivalent to value % 1.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Quad Fraction(Quad value)
        {
            if (value.Exponent >= 0) return Zero; //no fraction
            else if (value.Exponent <= -64)
            {
                if (value.Exponent == infinityExponent || value.Exponent == negativeInfinityExponent)
                    return NaN;
                else
                    return value; //all fraction (or zero or NaN)
            }
            else
            {
                //clear most significant 64+value.exponent bits before the binary point
                ulong bits = (value.SignificandBits << (int)(64 + value.Exponent)) >> (int)(64 + value.Exponent);
                if (bits == 0) return Zero; //value is an integer

                int shift = nlz(bits); //renormalize

                return new Quad((~highestBit & (bits << shift)) | (highestBit & value.SignificandBits), value.Exponent - shift);
            }
        }

        /// <summary>
        /// Calculates the log (base 2) of a Quad.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double Log2(Quad value)
        {
            if (value.SignificandBits >= highestBit) return double.NaN;
            if (value.Exponent <= notANumberExponent) return specialDoubleLogTable[(int)(value.Exponent - zeroExponent)];

            return Math.Log(value.SignificandBits | highestBit, 2) + value.Exponent;
        }

        /// <summary>
        /// Calculates the natural log (base e) of a Quad.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double Log(Quad value)
        {
            if (value.SignificandBits >= highestBit) return double.NaN;
            if (value.Exponent <= notANumberExponent) return specialDoubleLogTable[(int)(value.Exponent - zeroExponent)];

            return Math.Log(value.SignificandBits | highestBit) + value.Exponent * 0.69314718055994530941723212145818;
        }

        /// <summary>
        /// Raise a Quad to a given exponent.  Pow returns 1 for x^0 for all x >= 0.  An exception is thrown
        /// if 0 is raised to a negative exponent (implying division by 0), or if a negative value is raised
        /// by a non-integer exponent (yielding an imaginary number).
        /// </summary>
        public static Quad Pow(Quad value, double exponent)
        {
            if (value.Exponent <= notANumberExponent)
            {
                //check NaN
                if (value.Exponent == notANumberExponent || double.IsNaN(exponent)) return NaN;

                //anything ^ 0 == 1
                if (exponent == 0) return One;

                //0 ^ y
                if (value.Exponent == zeroExponent)
                    return exponent < 0 ? PositiveInfinity : Zero;

                //PositiveInfinity ^ y
                if (value.Exponent == infinityExponent)
                    return exponent < 0 ? Zero : PositiveInfinity;

                if (value.Exponent == negativeInfinityExponent)
                    return Math.Pow(double.NegativeInfinity, exponent); //lots of weird special cases
            }

            if (double.IsNaN(exponent)) return NaN;
            if (double.IsInfinity(exponent))
            {
                if (value < -2)
                    return Math.Pow(-2, exponent);
                else if (value > 2)
                    return Math.Pow(2, exponent);
                else
                    return Math.Pow((double)value, exponent);
            }

            if (value == 0) return Zero;

            if (exponent == 0) return One;

            if (value.SignificandBits >= highestBit && exponent % 1 != 0)
                return NaN; //result is an imaginary number--negative value raised to non-integer exponent

            var significand = new Quad(value.SignificandBits, -63);

            //Math.Pow will fail with large exponents so use custom method for whole number exponents
            Quad result;
            if (exponent % 1 == 0)
            {
                result = QuadPow(significand, (long)exponent);
            }
            else
            {
                result = Math.Pow((double)significand, exponent);
            }

            var resultExponent = (value.Exponent + 63) * exponent; //exponents multiply

            result *= Math.Pow(2, resultExponent % 1); //push the fractional exponent into the significand

            result.Exponent += (long)resultExponent;

            return result;
        }

        private static Quad QuadPow(Quad value, long power)
        {
            if (power == 0)
            {
                return 1;
            }

            if (power < 0)
            {
                value = 1.0 / value;
                power = -power;
            }

            var ret = QuadPow(value, power / 2);
            ret = ret * ret;
            if (power % 2 != 0)
            {
                ret = ret * value;
            }

            return ret;
        }


        public static Quad Max(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent == notANumberExponent) return NaN;
            else return qd1 > qd2 ? qd1 : qd2;
        }

        public static Quad Min(Quad qd1, Quad qd2)
        {
            if (qd1.Exponent == notANumberExponent) return NaN;
            else return qd1 < qd2 ? qd1 : qd2;
        }

        public static Quad Abs(Quad qd)
        {
            if (qd.Exponent == negativeInfinityExponent) return PositiveInfinity;
            else return new Quad(qd.SignificandBits & ~highestBit, qd.Exponent); //clear the sign bit
        }

        public static Quad Floor(Quad value)
        {
            if (value.Exponent > 0)
            {
                return value;
            }

            if (value.Exponent <= -64) // tiny number,
            {
                return 0;
            }

            var significand = value.SignificandBits | highestBit; //make explicit the implicit bit
            return (significand >> (int)(-value.Exponent));
        }

        public static Quad Ceil(Quad quad)
        {
            var fraction = Fraction(quad);
            if (fraction == 0)
            {
                return quad;
            }
            return quad + (1 - fraction);
        }

        /// <summary>
        /// Round quad to integer, uses Quad.Floor/Ceil depending on fractional value
        /// </summary>
        public static Quad Round(Quad quad)
        {
            var fraction = Fraction(quad);
            if (fraction == 0)
            {
                return quad;
            }

            return fraction < 0.5 ? Floor(quad) : Ceil(quad);
        }

        #endregion

        #region IsInfinity/IsNaN static test methods

        public static bool IsNaN(Quad quad)
        {
            return quad.Exponent == notANumberExponent;
        }

        public static bool IsInfinity(Quad quad)
        {
            return quad.Exponent == infinityExponent || quad.Exponent == negativeInfinityExponent;
        }

        public static bool IsPositiveInfinity(Quad quad)
        {
            return quad.Exponent == infinityExponent;
        }

        public static bool IsNegativeInfinity(Quad quad)
        {
            return quad.Exponent == negativeInfinityExponent;
        }

        #endregion

        #region Casts

        public static explicit operator ulong(Quad value)
        {
            if (value.Exponent == negativeInfinityExponent
                || value.Exponent == infinityExponent)
                throw new InvalidCastException("Cannot cast infinity to 64-bit unsigned integer");
            else if (value.Exponent == notANumberExponent)
                throw new InvalidCastException("Cannot cast NaN to 64-bit unsigned integer");

            if (value.SignificandBits >= highestBit) throw new ArgumentOutOfRangeException("Cannot convert negative value to ulong");

            if (value.Exponent > 0)
                throw new InvalidCastException("Value too large to fit in 64-bit unsigned integer");

            if (value.Exponent <= -64) return 0;

            return (highestBit | value.SignificandBits) >> (int)(-value.Exponent);
        }

        public static explicit operator long(Quad value)
        {
            if (value.Exponent == negativeInfinityExponent
                || value.Exponent == infinityExponent)
                throw new InvalidCastException("Cannot cast infinity to 64-bit signed integer");
            else if (value.Exponent == notANumberExponent)
                throw new InvalidCastException("Cannot cast NaN to 64-bit signed integer");

            if (value.SignificandBits == highestBit && value.Exponent == 0) //corner case
                return long.MinValue;

            if (value.Exponent >= 0)
                throw new InvalidCastException("Value too large to fit in 64-bit signed integer");

            if (value.Exponent <= -64) return 0;

            if (value.SignificandBits >= highestBit) //negative
                return -(long)(value.SignificandBits >> (int)(-value.Exponent));
            else
                return (long)((value.SignificandBits | highestBit) >> (int)(-value.Exponent));
        }

        public static unsafe explicit operator double(Quad value)
        {
            switch (value.Exponent)
            {
                case zeroExponent: return 0;
                case infinityExponent: return double.PositiveInfinity;
                case negativeInfinityExponent: return double.NegativeInfinity;
                case notANumberExponent: return double.NaN;
            }

            if (value.Exponent <= -1086)
            {
                if (value.Exponent > -1086 - 52) //can create subnormal double value
                {
                    ulong bits = (value.SignificandBits & highestBit) | ((value.SignificandBits | highestBit) >> (int)(-value.Exponent - 1086 + 12));
                    return *((double*)&bits);
                }
                else
                    return 0;
            }
            else
            {
                ulong bits = (ulong)(value.Exponent + 1086);
                if (bits >= 0x7ffUL) return value.SignificandBits >= highestBit ? double.NegativeInfinity : double.PositiveInfinity; //too large

                bits = (value.SignificandBits & highestBit) | (bits << 52) | (value.SignificandBits & (~highestBit)) >> 11;

                return *((double*)&bits);
            }
        }

        /// <summary>
        /// Converts a 64-bit unsigned integer into a Quad.  No data can be lost, nor will any exception be thrown, by this cast;
        /// however, it is marked explicit in order to avoid ambiguity with the implicit long-to-Quad cast operator.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static explicit operator Quad(ulong value)
        {
            if (value == 0) return Zero;
            int firstSetPosition = nlz(value);
            return new Quad((value << firstSetPosition) & ~highestBit, -firstSetPosition);
        }

        public static implicit operator Quad(long value)
        {
            return new Quad(value, 0);
        }

        public static unsafe implicit operator Quad(double value)
        {
            // Translate the double into sign, exponent and mantissa.
            //long bits = BitConverter.DoubleToInt64Bits(value); // doing an unsafe pointer-conversion to get the bits is faster
            ulong bits = *((ulong*)&value);

            // Note that the shift is sign-extended, hence the test against -1 not 1
            long exponent = (((long)bits >> 52) & 0x7ffL);
            ulong mantissa = (bits) & 0xfffffffffffffUL;

            if (exponent == 0x7ffL)
            {
                if (mantissa == 0)
                {
                    if (bits >= highestBit) //sign bit set?
                        return NegativeInfinity;
                    else
                        return PositiveInfinity;
                }
                else
                    return NaN;
            }

            // Subnormal numbers; exponent is effectively one higher,
            // but there's no extra normalisation bit in the mantissa
            if (exponent == 0)
            {
                if (mantissa == 0) return Zero;
                exponent++;

                int firstSetPosition = nlz(mantissa);
                mantissa <<= firstSetPosition;
                exponent -= firstSetPosition;
            }
            else
            {
                mantissa = mantissa << 11;
                exponent -= 11;
            }

            exponent -= 1075;

            return new Quad((highestBit & bits) | mantissa, exponent);
        }

        #endregion

        public string ToScientificString(int significantDigits)
        {
            if (this == 0)
            {
                return "0 e0";
            }

            var dVal = (double)new Quad(SignificandBits, -61);
            var dExp = base2to10Multiplier * (Exponent + 61);

            var sign = "";
            if (dVal < 0)
            {
                sign = "-";
                dVal = -dVal;
            }

            if (dExp >= 0)
                dVal *= Math.Pow(10, (dExp % 1));
            else
                dVal *= Math.Pow(10, -((-dExp) % 1));

            var iExp = (long)Math.Truncate(dExp);

            while (dVal >= 10)
            {
                iExp++;
                dVal /= 10;
            }

            while (dVal < 1)
            {
                iExp--;
                dVal *= 10;
            }

            return sign + dVal.RoundToSignificantDigits(significantDigits).ToString(System.Globalization.CultureInfo.InvariantCulture) + " e" + iExp;
        }

        #region ToString

        /// <summary>
        /// Returns this number as a decimal, or in scientific notation where a decimal would be excessively long.
        /// Equivalent to ToString(QuadrupleFormat.ScientificApproximate).
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(QuadrupleStringFormat.ScientificApproximate);
        }

        /// <summary>
        /// Obtains a string representation for this Quad according to the specified format.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <remarks>
        /// ScientificExact returns the value in scientific notation as accurately as possible, but is still subject to imprecision due to the conversion from
        /// binary to decimal and the divisions or multiplications used in the conversion.  It does not use rounding, which can lead to odd-looking outputs
        /// that would otherwise be rounded by double.ToString() or the ScientificApproximate format (which uses double.ToString()).  For example, 0.1 will be rendered
        /// as the string "9.9999999999999999981e-2".
        /// </remarks>
        public string ToString(QuadrupleStringFormat format)
        {
            if (Exponent <= notANumberExponent) return specialStringTable[(int)(Exponent - zeroExponent)];

            switch (format)
            {
                case QuadrupleStringFormat.HexExponential:
                    if (SignificandBits >= highestBit)
                        return "-" + SignificandBits.ToString("x") + "*2^" + (Exponent >= 0 ? Exponent.ToString("x") : "-" + (-Exponent).ToString("x"));
                    else
                        return (SignificandBits | highestBit).ToString("x") + "*2^" + (Exponent >= 0 ? Exponent.ToString("x") : "-" + (-Exponent).ToString("x"));

                case QuadrupleStringFormat.DecimalExponential:
                    if (SignificandBits >= highestBit)
                        return "-" + SignificandBits.ToString() + "*2^" + Exponent.ToString();
                    else
                        return (SignificandBits | highestBit).ToString() + "*2^" + Exponent.ToString();

                case QuadrupleStringFormat.ScientificApproximate:
                    if (this > QuadDoubleMin && this < ThresholdForFormatting)
                    {
                        //can be represented as double (albeit with a precision loss)
                        return ((double)this).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }

                    double dVal = (double)new Quad(SignificandBits, -61);
                    double dExp = base2to10Multiplier * (Exponent + 61);

                    string sign = "";
                    if (dVal < 0)
                    {
                        sign = "-";
                        dVal = -dVal;
                    }

                    if (dExp >= 0)
                        dVal *= Math.Pow(10, (dExp % 1));
                    else
                        dVal *= Math.Pow(10, -((-dExp) % 1));

                    long iExp = (long)Math.Truncate(dExp);

                    while (dVal >= 10)
                    {
                        iExp++;
                        dVal /= 10;
                    }
                    while (dVal < 1)
                    {
                        iExp--;
                        dVal *= 10;
                    }

                    if (iExp >= -10 && iExp < 0)
                    {
                        string dValString = dVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        if (dValString[1] != '.')
                            goto returnScientific; //unexpected formatting; use default behavior.
                        else
                            return sign + "0." + new string('0', (int)((-iExp) - 1)) + dVal.ToString(System.Globalization.CultureInfo.InvariantCulture).Remove(1, 1);
                    }
                    else if (iExp >= 0 && iExp <= 10)
                    {
                        string dValString = dVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        if (dValString[1] != '.')
                            goto returnScientific; //unexpected formating; use default behavior.
                        else
                        {
                            dValString = dValString.Remove(1, 1);
                            if (iExp < dValString.Length - 1)
                                return sign + dValString.Substring(0, 1 + (int)iExp) + "." + dValString.Substring(1 + (int)iExp);
                            else
                                return sign + dValString + new string('0', (int)iExp - (dValString.Length - 1)) + ".0";
                        }
                    }

                    returnScientific:
                    return sign + dVal.RoundToSignificantDigits(3).ToString(System.Globalization.CultureInfo.InvariantCulture) + " e" + iExp;

                case QuadrupleStringFormat.ScientificExact:
                    if (this == Zero) return "0";
                    if (Fraction(this) == Zero && this.Exponent <= 0) //integer value that we can output directly
                        return (this.SignificandBits >= highestBit ? "-" : "") + ((this.SignificandBits | highestBit) >> (int)(-this.Exponent)).ToString();

                    Quad absValue = Abs(this);

                    long e = 0;
                    if (absValue < One)
                    {
                        while (true)
                        {
                            if (absValue < en18)
                            {
                                absValue.Multiply(e19);
                                e -= 19;
                            }
                            else if (absValue < en9)
                            {
                                absValue.Multiply(e10);
                                e -= 10;
                            }
                            else if (absValue < en4)
                            {
                                absValue.Multiply(e5);
                                e -= 5;
                            }
                            else if (absValue < en2)
                            {
                                absValue.Multiply(e3);
                                e -= 3;
                            }
                            else if (absValue < One)
                            {
                                absValue.Multiply(e1);
                                e -= 1;
                            }
                            else
                                break;
                        }
                    }
                    else
                    {
                        while (true)
                        {
                            if (absValue >= e19)
                            {
                                absValue.Divide(e19);
                                e += 19;
                            }
                            else if (absValue >= e10)
                            {
                                absValue.Divide(e10);
                                e += 10;
                            }
                            else if (absValue >= e5)
                            {
                                absValue.Divide(e5);
                                e += 5;
                            }
                            else if (absValue >= e3)
                            {
                                absValue.Divide(e3);
                                e += 3;
                            }
                            else if (absValue >= e1)
                            {
                                absValue.Divide(e1);
                                e += 1;
                            }
                            else
                                break;
                        }
                    }

                    //absValue is now in the interval [1,10)
                    StringBuilder result = new StringBuilder();

                    result.Append(IntegerString(absValue, 1) + ".");

                    while ((absValue = Fraction(absValue)) > Zero)
                    {
                        absValue.Multiply(e19);
                        result.Append(IntegerString(absValue, 19));
                    }

                    string resultString = result.ToString().TrimEnd('0'); //trim excess 0's at the end
                    if (resultString[resultString.Length - 1] == '.') resultString += "0"; //e.g. 1.0 instead of 1.

                    return (this.SignificandBits >= highestBit ? "-" : "") + resultString + "e" + (e >= 0 ? "+" : "") + e;

                default:
                    throw new ArgumentException("Unknown format requested");
            }
        }

        /// <summary>
        /// Retrieves the integer portion of the quad as a string,
        /// assuming that the quad's value is less than long.MaxValue.
        /// No sign ("-") is prepended to the result in the case of negative values.
        /// </summary>
        /// <returns></returns>
        private static string IntegerString(Quad quad, int digits)
        {
            if (quad.Exponent > 0) throw new ArgumentOutOfRangeException("The given quad is larger than long.MaxValue");
            if (quad.Exponent <= -64) return "0";

            ulong significand = quad.SignificandBits | highestBit; //make explicit the implicit bit
            return (significand >> (int)(-quad.Exponent)).ToString(new string('0', digits));
        }

        #endregion

        #region GetHashCode and Equals

        public override int GetHashCode()
        {
            int expHash = Exponent.GetHashCode();
            return SignificandBits.GetHashCode() ^ (expHash << 16 | expHash >> 16); //rotate expHash's bits 16 places
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            try
            {
                return this == (Quad)obj;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    public enum QuadrupleStringFormat
    {
        HexExponential,
        ScientificApproximate,
        ScientificExact,
        DecimalExponential
    }

    public static class DoubleExtensions
    {
        public static double RoundToSignificantDigits(this double value, int digits)
        {
            return value;
        }
    }
}