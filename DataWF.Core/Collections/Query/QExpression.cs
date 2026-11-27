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

namespace DataWF.Collections.Query;

public class QExpression : QArray
{
    public QExpression()
    {
    }

    public override string Format(object context = null)
    {
        return base.Format(" ", context);
    }

    public override object GetValue<T>(T row)
    {
        object value = null;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is QMathOperator math)
            {
                var val1 = (i == 1) ? items[0].GetValue(row) : value;
                var val2 = items[i + 1].GetValue(row);
                value = math.MathOperator.Evaluator.Eval(GetDecimal(val1), GetDecimal(val2));
            }
        }
        return value;
    }

    public object Calc(MathOperatorType type, object val1, object val2)
    {
        if (val1 is string || val2 is string)
        {
            return string.Concat(val1, val2);
        }
        else
        {
            decimal obj1 = GetDecimal(val1);
            decimal obj2 = GetDecimal(val2);

            return type switch
            {
                MathOperatorType.Plus => obj1 + obj2,
                MathOperatorType.Minus => obj1 - obj2,
                MathOperatorType.Multiply => obj1 * obj2,
                MathOperatorType.Divide => obj2 == 0M ? 0M : obj1 / obj2,
                _ => throw new NotSupportedException()
            };
        }
    }

    public decimal GetDecimal(object obj)
    {
        return obj is decimal ? (decimal)obj
            : obj == null || obj == DBNull.Value ? 0M
            : System.Convert.ToDecimal(obj);
    }

}
