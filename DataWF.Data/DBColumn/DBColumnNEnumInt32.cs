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

using DataWF.Common;
using System;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DataWF.Data
{
    public class DBColumnNEnumInt32<T> : DBColumnNullable<T> where T : struct
    {
        public override void Read(DbDataReader reader, DBItem row, int i)
        {
            if (reader.IsDBNull(i))
            {
                SetValue(row, (T?)null, DBSetValueMode.Loading);
            }
            else
            {
                var value = reader.GetInt32(i);
                var enumValue = Unsafe.As<int, T>(ref value);
                SetValue(row, (T?)enumValue, DBSetValueMode.Loading);
            }
        }

        public override DBItem GetOrCreate(DbDataReader reader, int i, int typeIndex)
        {
            var value = reader.GetInt32(i);
            var enumValue = Unsafe.As<int, T>(ref value);
            return pullIndex.SelectOne(enumValue) ?? CreateLoadItem(typeIndex, enumValue);
        }

        public override string FormatQuery(T? value)
        {
            if (value == null)
                return "null";
            var val = value.Value;
            return Unsafe.As<T, int>(ref val).ToString(CultureInfo.InvariantCulture);
        }

        public override string FormatDisplay(T? value)
        {
            return value?.ToString() ?? string.Empty;
        }

        public override object GetParameterValue(T? value)
        {
            if (value == null)
                return DBNull.Value;
            var nnValue = (T)value;
            return Unsafe.As<T, int>(ref nnValue);
        }

        public override object GetParameterValue(object value)
        {
            if (value == null)
                return DBNull.Value;
            if (value is int oreadyValue)
                return oreadyValue;
            return base.GetParameterValue(value);
        }

        public override T? Parse(object value)
        {
            if (value is T typedValue)
                return typedValue;
            if (value == null || value == DBNull.Value)
                return default(T?);
            if (value is string stringValue && Enum.TryParse<T>(stringValue, out var parsed))
                return parsed;

            var convertedValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            return Unsafe.As<int, T>(ref convertedValue);
        }
    }
}