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
using System.Globalization;

namespace DataWF.Data
{
    public class DBColumnNUInt32 : DBColumnNullable<uint>
    {
        public override void Read(DBTransaction transaction, DBItem row, int i)
        {
            var value = transaction.Reader.IsDBNull(i) ? (uint?)null : (uint)transaction.Reader.GetInt64(i);
            SetValue(row, value, DBSetValueMode.Loading);
        }

        public override F ReadAndSelect<F>(DBTransaction transaction, int i)
        {
            var value = (uint)transaction.Reader.GetInt32(i);
            return pullIndex?.SelectOne<F>(value);
        }

        public override string FormatQuery(uint? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture) ?? "null";
        }

        public override string FormatDisplay(uint? value)
        {
            return value?.ToString(Format, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        public override object GetParameterValue(uint? value)
        {
            if (value == null)
                return DBNull.Value;
            return (long)value.Value;
        }

        public override uint? Parse(object value)
        {
            if (value is uint typedValue)
                return typedValue;
            if (value == null || value == DBNull.Value)
                return (uint?)null;
            if (value is DBItem item)
                return GetReferenceId(item);

            return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
        }
    }
}