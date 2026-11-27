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
    public class MathInt16SignEvaluator : IMathUnaryEvaluator<short>
    {
        public short Eval(short leftParam) => (short)Math.Sign(leftParam);
    }

    public class MathInt16AbsEvaluator : IMathUnaryEvaluator<short>
    {
        public short Eval(short leftParam) => Math.Abs(leftParam);
    }

    public class MathInt16NegationEvaluator : IMathUnaryEvaluator<short>
    {
        public short Eval(short leftParam) => (short)(-leftParam);
    }

    public class MathInt16BitwiseAndEvaluator : IMathBinaryEvaluator<short>
    {
        public short Eval(short param1, short param2) => (short)(param1 & param2);
    }

    public class MathInt16BitwiseNotEvaluator : IMathUnaryEvaluator<short>
    {
        public short Eval(short param1) => (short)~param1;
    }

    public class MathInt16BitwiseOrEvaluator : IMathBinaryEvaluator<short>
    {
        public short Eval(short param1, short param2) => (short)(param1 | param2);
    }

    public class MathInt16BitwiseXorEvaluator : IMathBinaryEvaluator<short>
    {
        public short Eval(short param1, short param2) => (short)(param1 ^ param2);
    }

    public class MathInt16DivideEvaluator : IMathBinaryEvaluator<short>
    {
        public short Eval(short leftParam, short rightParam) => (short)(leftParam / rightParam);
    }

    public class MathInt16ExponentaEvaluator : IMathBinaryEvaluator<short>
    {
        public short Eval(short leftParam, short rightParam) => (short)Math.Pow((double)leftParam, (double)rightParam);
    }

    public class MathInt16MinusEvaluator : IMathBinaryEvaluator<short>
    {
        public short Eval(short leftParam, short rightParam) => (short)(leftParam - rightParam);
    }

    public class MathInt16ModuloEvaluator : IMathBinaryEvaluator<short>
    {
        public short Eval(short leftParam, short rightParam) => (short)(leftParam % rightParam);
    }

    public class MathInt16MultiplyEvaluator : IMathBinaryEvaluator<short>
    {
        public short Eval(short leftParam, short rightParam) => (short)(leftParam * rightParam);
    }

    public class MathInt16PlusEvaluator : IMathBinaryEvaluator<short>
    {
        public short Eval(short leftParam, short rightParam) => (short)(leftParam + rightParam);
    }

    public class MathInt16PowerEvaluator : IMathBinaryEvaluator<short>
    {
        public short Eval(short leftParam, short rightParam) => (short)Math.Pow((double)leftParam, (double)rightParam);
    }
}



