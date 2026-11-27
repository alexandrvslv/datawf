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
    public class MathNFloatSignEvaluator : IMathUnaryEvaluator<float?>
    {
        public float? Eval(float? leftParam) => leftParam is float value ? Math.Sign(value) : null;
    }

    public class MathNFloatAbsEvaluator : IMathUnaryEvaluator<float?>
    {
        public float? Eval(float? leftParam) => leftParam is float value ? Math.Abs(value) : null;
    }

    public class MathNFloatNegationEvaluator : IMathUnaryEvaluator<float?>
    {
        public float? Eval(float? leftParam) => -leftParam;
    }

    public class MathNFloatBitwiseAndEvaluator : IMathBinaryEvaluator<float?>
    {
        public float? Eval(float? param1, float? param2) => (int)param1 & (int)param2;
    }

    public class MathNFloatBitwiseNotEvaluator : IMathUnaryEvaluator<float?>
    {
        public float? Eval(float? param1) => ~(int)param1;
    }

    public class MathNFloatBitwiseOrEvaluator : IMathBinaryEvaluator<float?>
    {
        public float? Eval(float? param1, float? param2) => (int)param1 | (int)param2;
    }

    public class MathNFloatBitwiseXorEvaluator : IMathBinaryEvaluator<float?>
    {
        public float? Eval(float? param1, float? param2) => (int)param1 ^ (int)param2;
    }

    public class MathNFloatDivideEvaluator : IMathBinaryEvaluator<float?>
    {
        public float? Eval(float? leftParam, float? rightParam) => leftParam / rightParam;
    }

    public class MathNFloatExponentaEvaluator : IMathBinaryEvaluator<float?>
    {
        public float? Eval(float? leftParam, float? rightParam) => (float?)Math.Pow((double)leftParam, (double)rightParam);
    }

    public class MathNFloatMinusEvaluator : IMathBinaryEvaluator<float?>
    {
        public float? Eval(float? leftParam, float? rightParam) => leftParam - rightParam;
    }

    public class MathNFloatModuloEvaluator : IMathBinaryEvaluator<float?>
    {
        public float? Eval(float? leftParam, float? rightParam) => leftParam % rightParam;
    }

    public class MathNFloatMultiplyEvaluator : IMathBinaryEvaluator<float?>
    {
        public float? Eval(float? leftParam, float? rightParam) => leftParam * rightParam;
    }

    public class MathNFloatPlusEvaluator : IMathBinaryEvaluator<float?>
    {
        public float? Eval(float? leftParam, float? rightParam) => leftParam + rightParam;
    }

    public class MathNFloatPowerEvaluator : IMathBinaryEvaluator<float?>
    {
        public float? Eval(float? leftParam, float? rightParam) => (float?)Math.Pow((double)leftParam, (double)rightParam);
    }
}



