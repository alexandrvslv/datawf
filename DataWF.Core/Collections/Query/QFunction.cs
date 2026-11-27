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
using System.Collections;
using System.Data;
using System.Linq;
using System.Text;
using DataWF.Collections.Query.Evaluators;

namespace DataWF.Collections.Query;

public class QFunction : QArray
{
    protected Function function;

    public QFunction()
    { }

    public QFunction(Function function)
    {
        Function = function;
    }

    public QFunction(Function function, IEnumerable parameters)
        : base(parameters)
    {
        Function = function;
    }

    public Function Function
    {
        get => function;
        set => function = value;
    }

    public override object GetValue<T>(T item)
    {
        QItem qItem = items.Count == 0 ? null : items[0];
        var parameters = items.Select(p => p.GetValue(item));
        return function.Evaluate(parameters);
    }

    public override object GetValue(QTuple item)
    {
        QItem qItem = items.Count == 0 ? null : items[0];
        var parameters = items.Select(p => p.GetValue(item));
        return function.Evaluate(parameters);
    }

    public override bool Compare(QTuple item, IQItem rightItem, CompareOperator comparer)
    {
        return base.Compare(item, rightItem, comparer);
    }
    // {
    //     switch (function.Type)
    //     {
    //         case FunctionType.Convert:
    //             {
    //                 var paramType = items[0] as QType;
    //                 var val = items[1].GetValue(item);

    //                 if (paramType.Type == DBDataType.Date)
    //                 {
    //                     if (val == DBNull.Value)
    //                         return val;
    //                     else if (!(val is DateTime))
    //                         val = DateTime.Parse(val.ToString());
    //                     return ((DateTime)val).Date;
    //                 }
    //                 else if (paramType.Type == DBDataType.String)
    //                 {
    //                     string temp = string.Empty;
    //                     if (val == DBNull.Value)
    //                         return string.Empty;
    //                     else if (val is DateTime)
    //                     {
    //                         string format = "yyyy-MM-dd HH:mm:ss.fff";
    //                         if (items.Count == 3)
    //                         {
    //                             string f = items[2].GetValue(item)?.ToString();
    //                             if (f == "112")
    //                                 format = "yyyyMMdd";
    //                             else if (f == "12")
    //                                 format = "yyMMdd";
    //                             else if (f == "6")
    //                                 format = "dd MMM yyyy";
    //                         }
    //                         temp = ((DateTime)val).ToString(format);
    //                     }
    //                     else
    //                         temp = System.FormatQuery(val);
    //                     if (paramType.Size > 0 && temp.Length > paramType.Size)
    //                         temp = temp.Substring(0, (int)paramType.Size);
    //                     return temp;
    //                 }
    //                 else
    //                 {
    //                 }

    //                 break;
    //             }
    //         case FunctionType.parse:
    //             {
    //                 var param = items[0].GetValue(item)?.ToString().Trim('\'');
    //                 var local = items.Count > 3
    //                     ? CultureInfo.GetCultureInfo(items[4].GetValue(item)?.ToString().Trim('\''))
    //                     : CultureInfo.CurrentCulture;
    //                 var t = items[2] as QType;
    //                 if (t.Type == DBDataType.Date || t.Type == DBDataType.DateTime)
    //                 {
    //                     return DateTime.Parse(param, local);
    //                 }
    //                 else if (t.Type == DBDataType.Decimal)
    //                 {
    //                     return decimal.Parse(param, local);
    //                 }

    //                 break;
    //             }
    //         case FunctionType.DateName:
    //             {
    //                 var param = items[0].GetValue(item)?.ToString();
    //                 var val = items[1].GetValue(item);
    //                 if (!(val is DateTime))
    //                     val = DateTime.Parse(val.ToString());
    //                 return ((DateTime)val).ToString(param);
    //             }

    //         case FunctionType.None:
    //             break;
    //         case FunctionType.Cast:
    //             break;
    //         case FunctionType.To_Char:
    //             break;
    //         case FunctionType.To_Date:
    //             break;
    //         case FunctionType.Format:
    //             break;
    //         case FunctionType.Group:
    //             break;
    //     }
    //     return null;
    // }

    public override string Format(object? context = null)
    {
        var rez = new StringBuilder();
        rez.Append(function.ToString());
        rez.Append('(');
        QItem prev = null;
        foreach (var item in items)
        {
            if (!items.IsFirst(item)
                && !(item is QCompareOperatorType)
                && !(prev is QCompareOperatorType))
                rez.Append(", ");
            rez.Append(item.Format(context));
            prev = item;
        }
        rez.Append(')');
        return rez.ToString();
    }


}

