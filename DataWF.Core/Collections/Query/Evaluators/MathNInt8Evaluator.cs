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
    public class MathNInt8SignEvaluator : IMathUnaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? leftParam) => leftParam is sbyte value ? (sbyte?)Math.Sign(value) : null;
    }

    public class MathNInt8AbsEvaluator : IMathUnaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? leftParam) => leftParam is sbyte value ? Math.Abs(value) : null;
    }

    public class MathNInt8NegationEvaluator : IMathUnaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? leftParam) => (sbyte?)-leftParam;
    }

    public class MathNInt8BitwiseAndEvaluator : IMathBinaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? param1, sbyte? param2) => (sbyte?)(param1 & param2);
    }

    public class MathNInt8BitwiseNotEvaluator : IMathUnaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? param1) => (sbyte?)(~param1);
    }

    public class MathNInt8BitwiseOrEvaluator : IMathBinaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? param1, sbyte? param2) => (sbyte?)(param1 | param2);
    }

    public class MathNInt8BitwiseXorEvaluator : IMathBinaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? param1, sbyte? param2) => (sbyte?)(param1 ^ param2);
    }

    public class MathNInt8DivideEvaluator : IMathBinaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? leftParam, sbyte? rightParam) => (sbyte?)(leftParam / rightParam);
    }

    public class MathNInt8ExponentaEvaluator : IMathBinaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? leftParam, sbyte? rightParam) => (sbyte?)Math.Pow((double)leftParam, (double)rightParam);
    }

    public class MathNInt8MinusEvaluator : IMathBinaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? leftParam, sbyte? rightParam) => (sbyte?)(leftParam - rightParam);
    }

    public class MathNInt8ModuloEvaluator : IMathBinaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? leftParam, sbyte? rightParam) => (sbyte?)(leftParam % rightParam);
    }

    public class MathNInt8MultiplyEvaluator : IMathBinaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? leftParam, sbyte? rightParam) => (sbyte?)(leftParam * rightParam);
    }

    public class MathNInt8PlusEvaluator : IMathBinaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? leftParam, sbyte? rightParam) => (sbyte?)(leftParam + rightParam);
    }

    public class MathNInt8PowerEvaluator : IMathBinaryEvaluator<sbyte?>
    {
        public sbyte? Eval(sbyte? leftParam, sbyte? rightParam) => (sbyte?)Math.Pow((double)leftParam, (double)rightParam);
    }
}



