using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Common
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
        public static DoubleCalculator Default = new DoubleCalculator();
        private const double EPSILON = 0.0000001D;

        public double Add(double p1, double p2)
        {
            return p1 + p2;
        }

        public double Sub(double p1, double p2)
        {
            return p1 - p2;
        }

        public double Pow(double p1, double p2)
        {
            return p1 * p2;
        }

        public double Div(double p1, double p2)
        {
            return p1 / p2;
        }

        public int Compare(double p1, double p2)
        {
            return p1.CompareTo(p2);
        }

        public bool Greater(double p1, double p2)
        {
            return p1 > p2;
        }

        public bool GreaterOrEqual(double p1, double p2)
        {
            return p1 >= p2;
        }

        public bool Lower(double p1, double p2)
        {
            return p1 < p2;
        }

        public bool LowerOrEqual(double p1, double p2)
        {
            return p1 <= p2;
        }

        public bool Equal(double p1, double p2)
        {
            return Math.Abs(p1 - p2) < EPSILON;
        }
    }

    public class DecimalCalculator : Calculator<decimal>
    {
        public static DecimalCalculator Default = new DecimalCalculator();

        public decimal Add(decimal p1, decimal p2)
        {
            return p1 + p2;
        }

        public decimal Sub(decimal p1, decimal p2)
        {
            return p1 - p2;
        }

        public decimal Pow(decimal p1, decimal p2)
        {
            return p1 * p2;
        }

        public decimal Div(decimal p1, decimal p2)
        {
            return p1 / p2;
        }

        public int Compare(decimal p1, decimal p2)
        {
            return p1.CompareTo(p2);
        }

        public bool Greater(decimal p1, decimal p2)
        {
            return p1 > p2;
        }

        public bool GreaterOrEqual(decimal p1, decimal p2)
        {
            return p1 >= p2;
        }

        public bool Lower(decimal p1, decimal p2)
        {
            return p1 < p2;
        }

        public bool LowerOrEqual(decimal p1, decimal p2)
        {
            return p1 <= p2;
        }

        public bool Equal(decimal p1, decimal p2)
        {
            return p1 == p2;
        }
    }

    public class FloatCalculator : Calculator<float>
    {
        public static FloatCalculator Default = new FloatCalculator();
        private const float EPSILON = 0.000001F;

        public float Add(float p1, float p2)
        {
            return p1 + p2;
        }

        public float Sub(float p1, float p2)
        {
            return p1 - p2;
        }

        public float Pow(float p1, float p2)
        {
            return p1 * p2;
        }

        public float Div(float p1, float p2)
        {
            return p1 / p2;
        }

        public int Compare(float p1, float p2)
        {
            return p1.CompareTo(p2);
        }

        public bool Greater(float p1, float p2)
        {
            return p1 > p2;
        }

        public bool GreaterOrEqual(float p1, float p2)
        {
            return p1 >= p2;
        }

        public bool Lower(float p1, float p2)
        {
            return p1 < p2;
        }

        public bool LowerOrEqual(float p1, float p2)
        {
            return p1 <= p2;
        }

        public bool Equal(float p1, float p2)
        {
            return Math.Abs(p1 - p2) < EPSILON;
        }
    }

    public class IntCalculator : Calculator<int>
    {
        public static IntCalculator Default = new IntCalculator();

        public int Add(int p1, int p2)
        {
            return p1 + p2;
        }

        public int Sub(int p1, int p2)
        {
            return p1 - p2;
        }

        public int Pow(int p1, int p2)
        {
            return p1 * p2;
        }

        public int Div(int p1, int p2)
        {
            return p2 / p1;
        }

        public int Compare(int p1, int p2)
        {
            return p1.CompareTo(p2);
        }

        public bool Greater(int p1, int p2)
        {
            return p1 > p2;
        }

        public bool GreaterOrEqual(int p1, int p2)
        {
            return p1 >= p2;
        }

        public bool Lower(int p1, int p2)
        {
            return p1 < p2;
        }

        public bool LowerOrEqual(int p1, int p2)
        {
            return p1 <= p2;
        }

        public bool Equal(int p1, int p2)
        {
            return p1 == p2;
        }
    }
}
