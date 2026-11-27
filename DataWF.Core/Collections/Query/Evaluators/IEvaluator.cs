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

using System.Collections.Generic;
using System.Linq;

namespace DataWF.Collections.Query.Evaluators
{
    public interface IEvaluator
    {
        int ParametersCount => 1;
        object Eval(IEnumerable<object> leftParam);
    }

    public interface IEvaluator<R> : IEvaluator
    {
        new R Eval(IEnumerable<object> param);

        object IEvaluator.Eval(IEnumerable<object> param) => Eval(param);
    }

    public interface IEvaluator<P1, R> : IEvaluator<R>
    {
        R Eval(P1 param);
        R IEvaluator<R>.Eval(IEnumerable<object> param) => Eval((P1)param.ElementAt(0));
    }

    public interface IEvaluator<P1, P2, R> : IEvaluator<P1, R>
    {
        int IEvaluator.ParametersCount => 2;
        R Eval(P1 param1, P2 param2);
        R IEvaluator<P1, R>.Eval(P1 param1) => Eval(param1, default(P2));
        R IEvaluator<R>.Eval(IEnumerable<object> param) => Eval((P1)param.ElementAt(0), (P2)param.ElementAt(1));
    }

    public interface IEvaluator<P1, P2, P3, R> : IEvaluator<P1, P2, R>
    {
        int IEvaluator.ParametersCount => 3;
        R Eval(P1 param1, P2 param2, P3 param3);
        R IEvaluator<P1, P2, R>.Eval(P1 param1, P2 param2) => Eval(param1, param2, default(P3));
        R IEvaluator<R>.Eval(IEnumerable<object> param) => Eval((P1)param.ElementAt(0), (P2)param.ElementAt(1), (P3)param.ElementAt(2));
    }

    public interface IEvaluator<P1, P2, P3, P4, R> : IEvaluator<P1, P2, P3, R>
    {
        int IEvaluator.ParametersCount => 4;
        R Eval(P1 param1, P2 param2, P3 param3, P4 param4);
        R IEvaluator<P1, P2, P3, R>.Eval(P1 param1, P2 param2, P3 param3) => Eval(param1, param2, param3, default(P4));
        R IEvaluator<R>.Eval(IEnumerable<object> param) => Eval((P1)param.ElementAt(0), (P2)param.ElementAt(1), (P3)param.ElementAt(2), (P4)param.ElementAt(3));
    }

    public interface IMathUnaryEvaluator<T> : IEvaluator<T, T>
    {
    }

    public interface IMathBinaryEvaluator<T> : IEvaluator<T, T, T>
    {

    }

    public interface IMath2DecimalEvaluator : IMathBinaryEvaluator<decimal>
    {
    }

    public interface IMath1DecimalEvaluator : IMathUnaryEvaluator<decimal>
    {
    }

    public interface IMathStringEvaluator : IEvaluator<string, string, string>
    {
    }
}

