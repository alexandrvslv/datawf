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
    public class DBColumnNUInt16 : DBColumnNullable<ushort>
    {
        public override void Read(DBTransaction transaction, DBItem row, int i)
        {
            var value = transaction.Reader.IsDBNull(i) ? (ushort?)null : (ushort)transaction.Reader.GetInt32(i);
            SetValue(row, value, DBSetValueMode.Loading);
        }

        public override F ReadAndSelect<F>(DBTransaction transaction, int i)
        {
            var value = (ushort)transaction.Reader.GetInt16(i);
            return PullIndex?.SelectOne<F>(value);
        }

        public override string FormatQuery(ushort? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture) ?? "null";
        }

        public override string FormatDisplay(ushort? value)
        {
            return value?.ToString(Format, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        public override object GetParameterValue(ushort? value)
        {
            if (value == null)
                return DBNull.Value;
            return (int)value.Value;
        }

        public override ushort? Parse(object value)
        {
            if (value is ushort typedValue)
                return typedValue;
            if (value == null || value == DBNull.Value)
                return (ushort?)null;
            if (value is DBItem item)
                return GetReferenceId(item);

            return Convert.ToUInt16(value, CultureInfo.InvariantCulture);
        }
    }
}