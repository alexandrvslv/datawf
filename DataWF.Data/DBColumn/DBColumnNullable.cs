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
using DataWF.Data;
using System;
using System.Globalization;
using System.Text.Json;

namespace DataWF.Data
{
    public class DBColumnNullable<T> : DBColumn<T?> where T : struct
    {
        public override bool Equal(object oldValue, object newValue)
        {
            return base.Equal(oldValue == null ? null : oldValue is T? ? (T?)oldValue : (T?)(T)oldValue,
                newValue == null ? null : newValue is T? ? (T?)newValue : (T?)(T)newValue);
        }

        public override bool Equal(T? oldValue, T? newValue)
        {
            return Nullable.Equals<T>(oldValue, newValue);
        }

        protected override T? GetReferenceId(DBItem item)
        {
            if (item.Table.PrimaryKey is DBColumn<T> typedColumn)
                return typedColumn.GetValue(item);
            return base.GetReferenceId(item);
        }


        protected override DBItem LoadReference(T? id, DBLoadParam param)
        {
            if (ReferenceTable.PrimaryKey is DBColumn<T>)
                return ReferenceTable.LoadItemById((T)id, param);
            return base.LoadReference(id, param);
        }

        public override object GetParameterValue(DBItem item)
        {
            var value = GetValue(item);
            if (value == null)
                return DBNull.Value;
            return (T)value;
        }

        public override string FormatValue(T? value)
        {
            if (value is T val)
            {
                if (IsReference)
                {
                    DBItem temp = ReferenceTable.LoadItemById(value);
                    return temp?.ToString() ?? "<new or empty>";
                }
                if (Format != null && val is IFormattable formattable)
                {
                    return formattable.ToString(Format, CultureInfo.InvariantCulture);
                }

                return val.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        public override T? Parse(object value)
        {
            return base.Parse(value);
        }

        public override void Write<E>(Utf8JsonWriter writer, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, T?> valueInvoker)
            {
                var value = valueInvoker.GetValue(element);
                if (value == null)
                    writer.WriteNull(JsonName);
                else
                {
                    writer.WritePropertyName(JsonName);
                    JsonSerializer.Serialize(writer, (T)value, options);
                }
            }
            else
            {
                Write(writer, (object)element, options);
            }
        }

        public override void Write(Utf8JsonWriter writer, object element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IValuedInvoker<T?> valueInvoker)
            {
                var value = valueInvoker.GetValue(element);
                if (value == null)
                    writer.WriteNull(JsonName);
                else
                {
                    writer.WritePropertyName(JsonName);
                    JsonSerializer.Serialize(writer, (T)value, options);
                }
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }
    }
}