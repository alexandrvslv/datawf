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
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DataWF.Data
{
    public class DBColumnEnumInt64<T> : DBColumn<T> where T : struct
    {
        public override void Read(DBTransaction transaction, DBItem row, int i)
        {
            var value = transaction.Reader.IsDBNull(i) ? default(long) : transaction.Reader.GetInt64(i);
            var enumValue = Unsafe.As<long, T>(ref value);
            SetValue(row, enumValue, DBSetValueMode.Loading);
        }

        public override F ReadAndSelect<F>(DBTransaction transaction, int i)
        {
            var value = transaction.Reader.GetInt64(i);
            var enumValue = Unsafe.As<long, T>(ref value);
            return ((IPullOutIndex<F, T>)pullIndex).SelectOne(value);
        }

        public override string FormatQuery(T value)
        {
            return Unsafe.As<T, long>(ref value).ToString(CultureInfo.InvariantCulture);
        }

        public override string FormatDisplay(T value)
        {
            return value.ToString();
        }

        public override object GetParameterValue(T value)
        {
            return Unsafe.As<T, long>(ref value);
        }

        public override object GetParameterValue(object value)
        {
            if (value is long readyValue)
                return readyValue;
            return base.GetParameterValue(value);
        }
        
        public override T Parse(object value)
        {
            if (value is T typedValue)
                return typedValue;
            if (value == null || value == DBNull.Value)
                return default(T);
            if (value is string stringValue && Enum.TryParse<T>(stringValue, out var parsed))
                return parsed;

            var convertedValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            return Unsafe.As<long, T>(ref convertedValue);
        }
    }
}