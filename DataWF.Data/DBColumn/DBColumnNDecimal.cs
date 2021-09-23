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
using System.Text.Json;

namespace DataWF.Data
{
    public class DBColumnNDecimal : DBColumnNullable<decimal>
    {
        public override void Read(DbDataReader reader, DBItem row, int i)
        {
            var value = reader.IsDBNull(i) ? (decimal?)null : reader.GetDecimal(i);
            SetValue(row, value, DBSetValueMode.Loading);
        }

        public override DBItem GetOrCreate(DbDataReader reader, int i, int typeIndex)
        {
            var value = reader.GetDecimal(i);
            return pullIndex.SelectOne(value) ?? CreateLoadItem(typeIndex, value);
        }

        public override string FormatQuery(decimal? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture).ToLower() ?? "null";
        }

        public override string FormatDisplay(decimal? value)
        {
            return value?.ToString(Format, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        public override decimal? Parse(object value)
        {
            if (value is decimal typedValue)
                return typedValue;
            if (value == null || value == DBNull.Value)
                return null;
            if (value is DBItem item)
                return GetReferenceId(item);

            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        public override void Read<E>(ref Utf8JsonReader reader, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, decimal?> valueInvoker)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    valueInvoker.SetValue(element, null);
                else if (reader.TryGetDecimal(out var value))
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
            if (PropertyInvoker is IInvoker<E, decimal?> valueInvoker)
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