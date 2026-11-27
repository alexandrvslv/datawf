//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

using System;

namespace DataWF.Collections.Query.Evaluators
{
    public class MathNDoubleSignEvaluator : IMathUnaryEvaluator<double?>
    {
        public double? Eval(double? leftParam) => leftParam is double value ? Math.Sign(value) : null;
    }

    public class MathNDoubleAbsEvaluator : IMathUnaryEvaluator<double?>
    {
        public double? Eval(double? leftParam) => leftParam is double value ? Math.Abs(value) : null;
    }

    public class MathNDoubleNegationEvaluator : IMathUnaryEvaluator<double?>
    {
        public double? Eval(double? leftParam) => -leftParam;
    }

    public class MathNDoubleBitwiseAndEvaluator : IMathBinaryEvaluator<double?>
    {
        public double? Eval(double? param1, double? param2) => (long)param1 & (long)param2;
    }

    public class MathNDoubleBitwiseNotEvaluator : IMathUnaryEvaluator<double?>
    {
        public double? Eval(double? param1) => ~(long)param1;
    }

    public class MathNDoubleBitwiseOrEvaluator : IMathBinaryEvaluator<double?>
    {
        public double? Eval(double? param1, double? param2) => (long)param1 | (long)param2;
    }

    public class MathNDoubleBitwiseXorEvaluator : IMathBinaryEvaluator<double?>
    {
        public double? Eval(double? param1, double? param2) => (long)param1 ^ (long)param2;
    }

    public class MathNDoubleDivideEvaluator : IMathBinaryEvaluator<double?>
    {
        public double? Eval(double? leftParam, double? rightParam) => leftParam / rightParam;
    }

    public class MathNDoubleExponentaEvaluator : IMathBinaryEvaluator<double?>
    {
        public double? Eval(double? leftParam, double? rightParam) => (double?)Math.Pow((double)leftParam, (double)rightParam);
    }

    public class MathNDoubleMinusEvaluator : IMathBinaryEvaluator<double?>
    {
        public double? Eval(double? leftParam, double? rightParam) => leftParam - rightParam;
    }

    public class MathNDoubleModuloEvaluator : IMathBinaryEvaluator<double?>
    {
        public double? Eval(double? leftParam, double? rightParam) => leftParam % rightParam;
    }

    public class MathNDoubleMultiplyEvaluator : IMathBinaryEvaluator<double?>
    {
        public double? Eval(double? leftParam, double? rightParam) => leftParam * rightParam;
    }

    public class MathNDoublePlusEvaluator : IMathBinaryEvaluator<double?>
    {
        public double? Eval(double? leftParam, double? rightParam) => leftParam + rightParam;
    }

    public class MathNDoublePowerEvaluator : IMathBinaryEvaluator<double?>
    {
        public double? Eval(double? leftParam, double? rightParam) => (double?)Math.Pow((double)leftParam, (double)rightParam);
    }
}



