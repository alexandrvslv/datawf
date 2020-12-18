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
using System.Text.Json;

namespace DataWF.Data
{
    public class DBColumnNUInt8 : DBColumnNullable<byte>
    {
        public override void Read(DBTransaction transaction, DBItem row, int i)
        {
            var value = transaction.Reader.IsDBNull(i) ? (byte?)null : transaction.Reader.GetByte(i);
            SetValue(row, value, DBSetValueMode.Loading);
        }

        public override string FormatQuery(byte? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture) ?? "null";
        }

        public override string FormatDisplay(byte? value)
        {
            return value?.ToString(Format, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        public override F ReadAndSelect<F>(DBTransaction transaction, int i)
        {
            var value = transaction.Reader.GetByte(i);
            return ((IPullOutIndex<F, byte?>)pullIndex).SelectOne(value);
        }

        public override byte? Parse(object value)
        {
            if (value is byte typedValue)
                return typedValue;
            if (value == null || value == DBNull.Value)
                return (byte?)null;
            if (value is DBItem item)
                return GetReferenceId(item);

            return Convert.ToByte(value, CultureInfo.InvariantCulture);
        }

        public override void Read<E>(ref Utf8JsonReader reader, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, byte?> valueInvoker)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    valueInvoker.SetValue(element, null);
                else if (reader.TryGetByte(out var value))
                    valueInvoker.SetValue(element, value);
                else
                    base.Read(ref reader, element, options);
            }
            else
            {
                base.Read(ref reader, element, options);
            }
        }

        public override void Write<E>(Utf8JsonWriter writer, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, byte?> valueInvoker)
            {
                var value = valueInvoker.GetValue(element);
                if (value == null)
                    writer.WriteNull(JsonName);
                else
                    writer.WriteNumber(JsonName, value.Value);
            }
            else
                base.Write(writer, element, options);
        }
    }
}