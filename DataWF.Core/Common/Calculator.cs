using System;

namespace DataWF
{
    public interface Calculator<T>
    {
        T Add(T p1, T p2);
        T Sub(T p1, T p2);
        T Pow(T p1, T p2);
        T Div(T p1, T p2);
        int Compare(T p1, T p2);
        bool Greater(T p1, T p2);
        bool GreaterOrEqual(T p1, T p2);
        bool Lower(T p1, T p2);
        bool LowerOrEqual(T p1, T p2);
        bool Equal(T p1, T p2);
    }

    public class DoubleCalculator : Calculator<double>
    {
        public static DoubleCalculator Default = new();
        private const double EPSILON = 0.0000001D;

        public double Add(double p1, double p2) => p1 + p2;

        public double Sub(double p1, double p2) => p1 - p2;

        public double Pow(double p1, double p2) => p1 * p2;

        public double Div(double p1, double p2) => p1 / p2;

        public int Compare(double p1, double p2) => p1.CompareTo(p2);

        public bool Greater(double p1, double p2) => p1 > p2;

        public bool GreaterOrEqual(double p1, double p2) => p1 >= p2;

        public bool Lower(double p1, double p2) => p1 < p2;

        public bool LowerOrEqual(double p1, double p2) => p1 <= p2;

        public bool Equal(double p1, double p2) => Math.Abs(p1 - p2) < EPSILON;
    }

    public class DecimalCalculator : Calculator<decimal>
    {
        public static DecimalCalculator Default = new();

        public decimal Add(decimal p1, decimal p2) => p1 + p2;

        public decimal Sub(decimal p1, decimal p2) => p1 - p2;

        public decimal Pow(decimal p1, decimal p2) => p1 * p2;

        public decimal Div(decimal p1, decimal p2) => p1 / p2;

        public int Compare(decimal p1, decimal p2) => p1.CompareTo(p2);

        public bool Greater(decimal p1, decimal p2) => p1 > p2;

        public bool GreaterOrEqual(decimal p1, decimal p2) => p1 >= p2;

        public bool Lower(decimal p1, decimal p2) => p1 < p2;

        public bool LowerOrEqual(decimal p1, decimal p2) => p1 <= p2;

        public bool Equal(decimal p1, decimal p2) => p1 == p2;
    }

    public class FloatCalculator : Calculator<float>
    {
        public static FloatCalculator Default = new();
        private const float EPSILON = 0.000001F;

        public float Add(float p1, float p2) => p1 + p2;

        public float Sub(float p1, float p2) => p1 - p2;

        public float Pow(float p1, float p2) => p1 * p2;

        public float Div(float p1, float p2) => p1 / p2;

        public int Compare(float p1, float p2) => p1.CompareTo(p2);

        public bool Greater(float p1, float p2) => p1 > p2;

        public bool GreaterOrEqual(float p1, float p2) => p1 >= p2;

        public bool Lower(float p1, float p2) => p1 < p2;

        public bool LowerOrEqual(float p1, float p2) => p1 <= p2;

        public bool Equal(float p1, float p2) => Math.Abs(p1 - p2) < EPSILON;
    }

    public class IntCalculator : Calculator<int>
    {
        public static IntCalculator Default = new();

        public int Add(int p1, int p2) => p1 + p2;

        public int Sub(int p1, int p2) => p1 - p2;

        public int Pow(int p1, int p2) => p1 * p2;

        public int Div(int p1, int p2) => p2 / p1;

        public int Compare(int p1, int p2) => p1.CompareTo(p2);

        public bool Greater(int p1, int p2) => p1 > p2;

        public bool GreaterOrEqual(int p1, int p2) => p1 >= p2;

        public bool Lower(int p1, int p2) => p1 < p2;

        public bool LowerOrEqual(int p1, int p2) => p1 <= p2;

        public bool Equal(int p1, int p2) => p1 == p2;
    }

    public class LongCalculator : Calculator<long>
    {
        public static LongCalculator Default = new();

        public long Add(long p1, long p2) => p1 + p2;

        public long Sub(long p1, long p2) => p1 - p2;

        public long Pow(long p1, long p2) => p1 * p2;

        public long Div(long p1, long p2) => p2 / p1;

        public int Compare(long p1, long p2) => p1.CompareTo(p2);

        public bool Greater(long p1, long p2) => p1 > p2;

        public bool GreaterOrEqual(long p1, long p2) => p1 >= p2;

        public bool Lower(long p1, long p2) => p1 < p2;

        public bool LowerOrEqual(long p1, long p2) => p1 <= p2;

        public bool Equal(long p1, long p2) => p1 == p2;
    }
}
