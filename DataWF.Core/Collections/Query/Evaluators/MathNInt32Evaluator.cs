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
    public class MathNInt32SignEvaluator : IMathUnaryEvaluator<int?>
    {
        public int? Eval(int? leftParam) => leftParam is int value ? Math.Sign(value) : null;
    }

    public class MathNInt32AbsEvaluator : IMathUnaryEvaluator<int?>
    {
        public int? Eval(int? leftParam) => leftParam is int value ? Math.Abs(value) : null;
    }

    public class MathNInt32NegationEvaluator : IMathUnaryEvaluator<int?>
    {
        public int? Eval(int? leftParam) => -leftParam;
    }

    public class MathNInt32BitwiseAndEvaluator : IMathBinaryEvaluator<int?>
    {
        public int? Eval(int? param1, int? param2) => param1 & param2;
    }

    public class MathNInt32BitwiseNotEvaluator : IMathUnaryEvaluator<int?>
    {
        public int? Eval(int? param1) => ~param1;
    }

    public class MathNInt32BitwiseOrEvaluator : IMathBinaryEvaluator<int?>
    {
        public int? Eval(int? param1, int? param2) => param1 | param2;
    }

    public class MathNInt32BitwiseXorEvaluator : IMathBinaryEvaluator<int?>
    {
        public int? Eval(int? param1, int? param2) => param1 ^ param2;
    }

    public class MathNInt32DivideEvaluator : IMathBinaryEvaluator<int?>
    {
        public int? Eval(int? leftParam, int? rightParam) => leftParam / rightParam;
    }

    public class MathNInt32ExponentaEvaluator : IMathBinaryEvaluator<int?>
    {
        public int? Eval(int? leftParam, int? rightParam) => (int?)Math.Pow((double)leftParam, (double)rightParam);
    }

    public class MathNInt32MinusEvaluator : IMathBinaryEvaluator<int?>
    {
        public int? Eval(int? leftParam, int? rightParam) => leftParam - rightParam;
    }

    public class MathNInt32ModuloEvaluator : IMathBinaryEvaluator<int?>
    {
        public int? Eval(int? leftParam, int? rightParam) => leftParam % rightParam;
    }

    public class MathNInt32MultiplyEvaluator : IMathBinaryEvaluator<int?>
    {
        public int? Eval(int? leftParam, int? rightParam) => leftParam * rightParam;
    }

    public class MathNInt32PlusEvaluator : IMathBinaryEvaluator<int?>
    {
        public int? Eval(int? leftParam, int? rightParam) => leftParam + rightParam;
    }

    public class MathNInt32PowerEvaluator : IMathBinaryEvaluator<int?>
    {
        public int? Eval(int? leftParam, int? rightParam) => (int?)Math.Pow((double)leftParam, (double)rightParam);
    }
}



