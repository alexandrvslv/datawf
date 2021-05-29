﻿//  The MIT License (MIT)
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
using DataWF.Data;
using System;
using System.Globalization;
using System.Text.Json;

namespace DataWF.Data
{
    public class DBColumnNBool : DBColumnNullable<bool>
    {
        public override void Read(DBTransaction transaction, DBItem row, int i)
        {
            var value = transaction.Reader.IsDBNull(i) ? (bool?)null : transaction.Reader.GetBoolean(i);
            SetValue(row, value, DBSetValueMode.Loading);
        }

        public override F ReadAndSelect<F>(DBTransaction transaction, int i)
        {
            var value = transaction.Reader.GetBoolean(i);
            return ((IPullOutIndex<F, bool?>)pullIndex).SelectOne(value);
        }

        public override string FormatQuery(bool? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture).ToLower() ?? "null";
        }

        public override string FormatDisplay(bool? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        }

        public override bool? Parse(object value)
        {
            if (value is bool typedValue)
                return typedValue;
            if (value == null || value == DBNull.Value)
                return null;
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        public override void Read<E>(ref Utf8JsonReader reader, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, bool?> valueInvoker)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    valueInvoker.SetValue(element, null);
                else
                    valueInvoker.SetValue(element, reader.GetBoolean());
            }
            else
            {
                base.Read(ref reader, element, options);
            }
        }

        public override void Write<E>(Utf8JsonWriter writer, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, bool?> valueInvoker)
            {
                var value = valueInvoker.GetValue(element);
                if (value == null)
                    writer.WriteNull(JsonName);
                else
                    writer.WriteBoolean(JsonName, value.Value);
            }
            else
                base.Write(writer, element, options);
        }
    }
}