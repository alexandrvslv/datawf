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

namespace DataWF.Collections.Query.Evaluators;

public class FunctionConvertEvaluator : IEvaluator<object, object, object>
{
    public object Eval(object param1, object param2)
    {
        if (param1 is string strParam)
        {
            return strParam switch
            {
                "string" => param2?.ToString()!,
                "short" => Convert.ToInt16(param2),
                "int" => Convert.ToInt32(param2),
                "long" => Convert.ToInt64(param2),
                "decimal" => Convert.ToDecimal(param2),
                "double" => Convert.ToDouble(param2),
                "float" => Convert.ToSingle(param2),
                "DateOnly" => DateOnly.FromDateTime(Convert.ToDateTime(param2)),
                "DateTime" => Convert.ToDateTime(param2),
                "DateTimeOffset" => new DateTimeOffset(Convert.ToDateTime(param2)),
                _ => null!
            };
        }
        if (param1 is DBDataType dbType)
        {
            return dbType switch
            {
                DBDataType.String => param2?.ToString()!,
                DBDataType.ShortInt => Convert.ToInt16(param2),
                DBDataType.Int => Convert.ToInt32(param2),
                DBDataType.BigInt => Convert.ToInt64(param2),
                DBDataType.Decimal => Convert.ToDecimal(param2),
                DBDataType.Double => Convert.ToDouble(param2),
                DBDataType.Float => Convert.ToSingle(param2),
                DBDataType.Date => DateOnly.FromDateTime(Convert.ToDateTime(param2)),
                DBDataType.DateTime => Convert.ToDateTime(param2),
                DBDataType.TimeStamp => new DateTimeOffset(Convert.ToDateTime(param2)),
                _ => null!
            };
        }
        throw new NotSupportedException($"Conversion from {param1} is not supported.");
    }
}


