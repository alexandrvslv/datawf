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
using System;
using System.Data.Common;
using System.Text.Json;

namespace DataWF.Data
{
    public sealed class DBColumnString : DBColumnReferenceType<string>
    {
        public override bool Equal(string oldValue, string newValue)
        {
            return string.Equals(oldValue, newValue, StringComparison.Ordinal);
        }

        public override void Read(DbDataReader reader, DBItem row, int i)
        {
            var value = reader.IsDBNull(i) ? null : reader.GetString(i);
            SetValue(row, value, DBSetValueMode.Loading);
        }

        public override DBItem GetOrCreate(DbDataReader reader, int i, int typeIndex)
        {
            var value = reader.GetString(i);
            return pullIndex.SelectOne(value) ?? Table.NewItem(DBUpdateState.Default, false, typeIndex);
        }

        public override string FormatQuery(string value)
        {
            if (value == null)
                return "null";

            return $"'{value.Replace("'", "''")}'";
        }

        public override string Parse(object value)
        {
            return Helper.TextBinaryFormat(value);
        }

        public override void Read<E>(ref Utf8JsonReader reader, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, string> valueInvoker)
            {
                valueInvoker.SetValue(element, reader.GetString());
            }
            else
            {
                base.Read(ref reader, element, options);
            }
        }

        public override void Write<E>(Utf8JsonWriter writer, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, string> valueInvoker)
            {
                var text = valueInvoker.GetValue(element);
                if (text == null)
                    writer.WriteNull(JsonName);
                else
                    writer.WriteString(JsonName, text);
            }
            else
                base.Write(writer, element, options);
        }
    }
}