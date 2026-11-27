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
    public class MathNDecimalSignEvaluator : IMathUnaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? leftParam) => leftParam is decimal value ? Math.Sign(value) : null;
    }

    public class MathNDecimalAbsEvaluator : IMathUnaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? leftParam) => leftParam is decimal value ? Math.Abs(value) : null;
    }

    public class MathNDecimalNegationEvaluator : IMathUnaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? leftParam) => -leftParam;
    }

    public class MathNDecimalBitwiseAndEvaluator : IMathBinaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? param1, decimal? param2) => (long)param1 & (long)param2;
    }

    public class MathNDecimalBitwiseNotEvaluator : IMathUnaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? param1) => ~(long)param1;
    }

    public class MathNDecimalBitwiseOrEvaluator : IMathBinaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? param1, decimal? param2) => (long)param1 | (long)param2;
    }

    public class MathNDecimalBitwiseXorEvaluator : IMathBinaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? param1, decimal? param2) => (long)param1 ^ (long)param2;
    }

    public class MathNDecimalDivideEvaluator : IMathBinaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? leftParam, decimal? rightParam) => leftParam / rightParam;
    }

    public class MathNDecimalExponentaEvaluator : IMathBinaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? leftParam, decimal? rightParam) => (decimal?)Math.Pow((double)leftParam, (double)rightParam);
    }

    public class MathNDecimalMinusEvaluator : IMathBinaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? leftParam, decimal? rightParam) => leftParam - rightParam;
    }

    public class MathNDecimalModuloEvaluator : IMathBinaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? leftParam, decimal? rightParam) => leftParam % rightParam;
    }

    public class MathNDecimalMultiplyEvaluator : IMathBinaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? leftParam, decimal? rightParam) => leftParam * rightParam;
    }

    public class MathNDecimalPlusEvaluator : IMathBinaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? leftParam, decimal? rightParam) => leftParam + rightParam;
    }

    public class MathNDecimalPowerEvaluator : IMathBinaryEvaluator<decimal?>
    {
        public decimal? Eval(decimal? leftParam, decimal? rightParam) => (decimal?)Math.Pow((double)leftParam, (double)rightParam);
    }
}



