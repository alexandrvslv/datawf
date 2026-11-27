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

namespace DataWF.Collections.Query.Evaluators;

public class FunctionCoalesceEvaluator : IEvaluator<object, object, object, object, string>
{
    public FunctionCoalesceEvaluator()
    {
    }

    public string Eval(IEnumerable<object> param) => param.Select(x => x.ToString()).FirstOrDefault(x => x != null);

    public string Eval(object param1, object param2) => (param1 ?? param2)?.ToString();

    public string Eval(object param1, object param2, object param3) => (param1 ?? param2 ?? param3)?.ToString();

    public string Eval(object param1, object param2, object param3, object param4) => (param1 ?? param2 ?? param3 ?? param4)?.ToString();
}

