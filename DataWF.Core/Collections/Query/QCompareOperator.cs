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
using System.Text.RegularExpressions;

namespace DataWF.Collections.Query;

public class QCompareOperator<V> : QItem, IQCompareOperator
{
    public QCompareOperator()
    { }

    public QCompareOperator(CompareOperator compareOperator)
    {
        CompareOperator = compareOperator;
    }

    public CompareOperator CompareOperator { get; set; }


    public override bool IsFilled => true;

    public override string Format(object context = null)
    {
        return CompareOperator.Format();
    }

    public override object GetValue<T>(T row)
    {
        throw new NotSupportedException();
    }

    public virtual bool CheckItem<T>(T item)
    {
        if (item == null)
            return false; //comparer.Type == CompareTypes.Is && !comparer.Not;
        var param = Holder as IQCompareParameter;
        if (param == null)
            return false;
        var val1 = param.LeftItem.GetValue(item);
        var val2 = param.RightItem.GetValue(item);
        var comparer = param.Comparer;
        if (val1 == null)
            return (comparer.Type & CompareOperatorType.Is) == CompareOperatorType.Is ? !comparer.Not : val2 == null;
        else if (val2 == null)
            return (comparer.Type & CompareOperatorType.Is) == CompareOperatorType.Is ? comparer.Not : false;

        if (val1 is Enum)
            val1 = (int)val1;
        if (val2 is Enum)
            val2 = (int)val1;

        switch (comparer.Type)
        {
            case CompareOperatorType.IsNull:
                return val1.Equals(DBNull.Value) ? !comparer.Not : comparer.Not;
            case CompareOperatorType.Equal:
                return ListHelper.Equal(val1, val2);
            case CompareOperatorType.NotEqual:
                return !ListHelper.Equal(val1, val2);
            case CompareOperatorType.Like:
                {
                    var r = val2 is Regex regexValue ? regexValue : Helper.BuildLike(val2.ToString());
                    return r.IsMatch(val1.ToString());
                }
            case CompareOperatorType.NotLike:
                {
                    var r = val2 is Regex regexValue ? regexValue : Helper.BuildLike(val2.ToString());
                    return !r.IsMatch(val1.ToString());
                }
            case CompareOperatorType.In:
            case CompareOperatorType.NotIn:
                var list = val2 as IEnumerable;
                if (list != null)
                {
                    foreach (object s in list)
                    {
                        object comp = s;
                        if (comp is QItem qItem)
                            comp = qItem.GetValue(item);
                        if (comp.Equals(val1) && !comparer.Not)
                            return true;
                    }
                }
                return comparer.Not;
            case CompareOperatorType.Between:
            case CompareOperatorType.NotBetween:
                var between = val2 as IBetween;
                if (between == null)
                    throw new Exception($"Expect QBetween but Get {(val2 == null ? "null" : val2.GetType().FullName)}");
                return ListHelper.Compare(val1, between.Min.GetValue(item), (IComparer)null) >= 0
                                 && ListHelper.Compare(val1, between.Max.GetValue(item), (IComparer)null) <= 0;
            default:
                bool f = false;
                int rez = ListHelper.Compare(val1, val2, (IComparer)null);
                switch (comparer.Type)
                {
                    case CompareOperatorType.Greater:
                        f = rez > 0;
                        break;
                    case CompareOperatorType.GreaterOrEqual:
                        f = rez >= 0;
                        break;
                    case CompareOperatorType.Less:
                        f = rez < 0;
                        break;
                    case CompareOperatorType.LessOrEqual:
                        f = rez <= 0;
                        break;
                    default:
                        break;
                }
                return f;
        }
    }
}


