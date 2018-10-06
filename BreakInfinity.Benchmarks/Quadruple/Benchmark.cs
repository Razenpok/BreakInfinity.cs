using BenchmarkDotNet.Attributes;

namespace BreakInfinity.Benchmarks.Quadruple
{
    public class BigDoubleVsQuad
    {
        private BigDouble firstBigDouble;
        private BigDouble secondBigDouble;
        private Quad firstQuad;
        private Quad secondQuad;
        private double smallDouble;

        [GlobalSetup]
        public void Setup()
        {
            firstBigDouble = BigMath.RandomBigDouble(100);
            firstQuad = new Quad(firstBigDouble.ToDouble());
            secondBigDouble = BigMath.RandomBigDouble(100);
            secondQuad = new Quad(secondBigDouble.ToDouble());
            smallDouble = BigMath.RandomBigDouble(2).ToDouble();
        }

        [Benchmark]
        public Quad QuadAdd() => firstQuad + secondQuad;

        [Benchmark]
        public BigDouble BigDoubleAdd() => firstBigDouble + secondBigDouble;

        [Benchmark]
        public Quad QuadSubtract() => firstQuad - secondQuad;

        [Benchmark]
        public BigDouble BigDoubleSubtract() => firstBigDouble - secondBigDouble;

        [Benchmark]
        public Quad QuadMultiply() => firstQuad * secondQuad;

        [Benchmark]
        public BigDouble BigDoubleMultiply() => firstBigDouble * secondBigDouble;

        [Benchmark]
        public Quad QuadDivide() => firstQuad / secondQuad;

        [Benchmark]
        public BigDouble BigDoubleDivide() => firstBigDouble / secondBigDouble;

        [Benchmark]
        public Quad QuadPow() => Quad.Pow(firstQuad, smallDouble);

        [Benchmark]
        public BigDouble BigDoublePow() => BigDouble.Pow(firstBigDouble, smallDouble);
    }
}
