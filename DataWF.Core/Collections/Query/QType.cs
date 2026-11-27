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
using System.ComponentModel;
using System.Data;
using DataWF.Collections.Query.Evaluators;

namespace DataWF.Collections.Query;

public class QType : QItem
{
    private const string StrDate = "date";
    private const string StrDateTime = "datetime";
    private const string StrTimestamp = "timestamp";
    private const string StrVarchar = "varchar";
    private const string StrNumber = "number";
    private const string StrDecimal = "decimal";
    private const string StrInt = "int";
    private const string StrInteger = "integer";
    private const string StrISmallInt = "smallint";
    private const string StrDouble = "double";
    private const string StrReal = "real";
    public static readonly QType None = new QType(DBDataType.None);
    // public static readonly QType Date = new QType(DBDataType.Date);
    // public static readonly QType String = new QType(DBDataType.String);
    // public static readonly QType Number = new QType(DBDataType.Number);
    // public static readonly QType Integer = new QType(DBDataType.Integer);
    // public static readonly QType DateTime = new QType(DBDataType.DateTime);
    
    [DefaultValue(DBDataType.String)]
    DBDataType type = DBDataType.String;
    
    [DefaultValue(0)]
    decimal size = 0;

    public QType()
    { }

    public QType(DBDataType type)
    {
        // TODO: Complete member initialization
        this.type = type;
    }

    public DBDataType Type
    {
        get => type;
        set => type = value;
    }

    public decimal Size
    {
        get => size;
        set => size = value;
    }

    public override bool IsFilled => true;

    public override object GetObjectValue<T>(T item) => type;
    public override object GetObjectValue(QTuple item) => type;

    public override string Format(object? context = null)
    {
        string rez = type switch
        {
            DBDataType.Decimal => StrNumber,
            DBDataType.Double => StrDouble,
            DBDataType.Float => StrReal,
            DBDataType.Int => StrInt,
            DBDataType.ShortInt => StrISmallInt,
            DBDataType.String => StrVarchar,
            DBDataType.Date => StrDate,
            DBDataType.DateTime => StrDateTime,
            DBDataType.TimeStamp => StrTimestamp,
            _ => ""
        };
        if (size > 0)
            rez += '(' + size.ToString() + ')';
        return rez;
    }

    public static QType ParseType(ReadOnlySpan<char> word)
    {
        if (MemoryExtensions.Equals(word, StrDate.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return new QType(DBDataType.Date);
        else if (MemoryExtensions.Equals(word, StrDateTime.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return new QType(DBDataType.DateTime);
        else if (MemoryExtensions.Equals(word, StrTimestamp.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return new QType(DBDataType.TimeStamp);
        else if (MemoryExtensions.Equals(word, StrVarchar.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return new QType(DBDataType.String);
        else if (MemoryExtensions.Equals(word, StrNumber.AsSpan(), StringComparison.OrdinalIgnoreCase)
            || MemoryExtensions.Equals(word, StrDecimal.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return new QType(DBDataType.Decimal);
        else if (MemoryExtensions.Equals(word, StrInt.AsSpan(), StringComparison.OrdinalIgnoreCase)
            || MemoryExtensions.Equals(word, StrInteger.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return new QType(DBDataType.Int);
        else if (MemoryExtensions.Equals(word, StrISmallInt.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return new QType(DBDataType.ShortInt);
        else if (MemoryExtensions.Equals(word, StrDouble.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return new QType(DBDataType.Double);
        else if (MemoryExtensions.Equals(word, StrReal.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return new QType(DBDataType.Float);
        else
            return None;
    }

    public override IQParameterFilter<T> GetFilter<T>(IQItem rightItem) => throw new NotImplementedException();

    public override bool Compare<T>(T item) => true;

    public override bool Compare(QTuple item) => true;

    public override bool Compare<T>(T item, CompareOperator comparer) => true;

    public override bool Compare(QTuple item, CompareOperator comparer) => true;

    public override bool Compare<T>(T item, IQItem rightItem, CompareOperator comparer) => true;

    public override bool Compare(QTuple item, IQItem rightItem, CompareOperator comparer) => true;
}
