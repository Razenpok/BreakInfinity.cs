using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace BreakInfinity.Benchmarks
{
    public class DoublevsBigDouble
    {
        private readonly BigDouble first;
        private readonly BigDouble second;
        private readonly double firstDouble;
        private readonly double secondDouble;

        public DoublevsBigDouble()
        {
            first = BigDouble.RandomDecimalForTesting(100);
            second = BigDouble.RandomDecimalForTesting(100);
            firstDouble = first.ToDouble();
            secondDouble = second.ToDouble();
        }

        [Benchmark]
        public double DoubleAdd() => firstDouble + secondDouble;

        [Benchmark]
        public BigDouble BigDoubleAdd() => first + second;

        [Benchmark]
        public double DoubleSubtract() => firstDouble - secondDouble;

        [Benchmark]
        public BigDouble BigDoubleSubtract() => first - second;

        [Benchmark]
        public double DoubleMultiply() => firstDouble * secondDouble;

        [Benchmark]
        public BigDouble BigDoubleMultiply() => first * second;

        [Benchmark]
        public double DoubleDivide() => firstDouble / secondDouble;

        [Benchmark]
        public BigDouble BigDoubleDivide() => first / second;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<DoublevsBigDouble>();
        }
    }
}
