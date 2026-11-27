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
    public class MathNInt64SignEvaluator : IMathUnaryEvaluator<long?>
    {
        public long? Eval(long? leftParam) => leftParam is long value ? Math.Sign(value) : null;
    }

    public class MathNInt64AbsEvaluator : IMathUnaryEvaluator<long?>
    {
        public long? Eval(long? leftParam) => leftParam is long value ? Math.Abs(value) : null;
    }

    public class MathNInt64NegationEvaluator : IMathUnaryEvaluator<long?>
    {
        public long? Eval(long? leftParam) => -leftParam;
    }

    public class MathNInt64BitwiseAndEvaluator : IMathBinaryEvaluator<long?>
    {
        public long? Eval(long? param1, long? param2) => param1 & param2;
    }

    public class MathNInt64BitwiseNotEvaluator : IMathUnaryEvaluator<long?>
    {
        public long? Eval(long? param1) => ~param1;
    }

    public class MathNInt64BitwiseOrEvaluator : IMathBinaryEvaluator<long?>
    {
        public long? Eval(long? param1, long? param2) => param1 | param2;
    }

    public class MathNInt64BitwiseXorEvaluator : IMathBinaryEvaluator<long?>
    {
        public long? Eval(long? param1, long? param2) => param1 ^ param2;
    }

    public class MathNInt64DivideEvaluator : IMathBinaryEvaluator<long?>
    {
        public long? Eval(long? leftParam, long? rightParam) => leftParam / rightParam;
    }

    public class MathNInt64ExponentaEvaluator : IMathBinaryEvaluator<long?>
    {
        public long? Eval(long? leftParam, long? rightParam) => (long?)Math.Pow((double)leftParam, (double)rightParam);
    }

    public class MathNInt64MinusEvaluator : IMathBinaryEvaluator<long?>
    {
        public long? Eval(long? leftParam, long? rightParam) => leftParam - rightParam;
    }

    public class MathNInt64ModuloEvaluator : IMathBinaryEvaluator<long?>
    {
        public long? Eval(long? leftParam, long? rightParam) => leftParam % rightParam;
    }

    public class MathNInt64MultiplyEvaluator : IMathBinaryEvaluator<long?>
    {
        public long? Eval(long? leftParam, long? rightParam) => leftParam * rightParam;
    }

    public class MathNInt64PlusEvaluator : IMathBinaryEvaluator<long?>
    {
        public long? Eval(long? leftParam, long? rightParam) => leftParam + rightParam;
    }

    public class MathNInt64PowerEvaluator : IMathBinaryEvaluator<long?>
    {
        public long? Eval(long? leftParam, long? rightParam) => (long?)Math.Pow((double)leftParam, (double)rightParam);
    }
}



