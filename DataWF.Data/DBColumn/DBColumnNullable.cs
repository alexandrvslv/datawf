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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            {
                var typedValue = typedColumn.GetValue(item);
                return typedColumn.Equal(typedValue, default(T)) ? null : typedValue;
            }
            return base.GetReferenceId(item);
        }


        protected override DBItem LoadReference(T? id, DBLoadParam param)
        {
            if (ReferenceTable.PrimaryKey is DBColumn<T> typedColumn)
                return ReferenceTable.LoadByKey<DBItem, T>((T)id, typedColumn, param).FirstOrDefault();
            return base.LoadReference(id, param);
        }

        public override void Copy(DBItem fromItem, DBColumn fromColumn, DBItem toItem, DBSetValueMode mode = DBSetValueMode.Default)
        {
            if (fromColumn is DBColumn<T> typedColumn)
                SetValue(toItem, typedColumn.GetValue(fromItem), mode);
            else
                base.Copy(fromItem, fromColumn, toItem, mode);

        }

        public override object GetParameterValue(T? value)
        {
            if (value == null)
                return DBNull.Value;

            return value.Value;
        }

        public override string FormatQuery(T? value)
        {
            if (value == null)
                return "null";

            return base.FormatQuery(value.Value);
        }

        public override string FormatDisplay(T? value)
        {
            if (value == null)
                return string.Empty;

            var val = value.Value;
            if (IsReference)
            {
                DBItem temp = LoadReference(value, DBLoadParam.Load);
                return temp?.ToString() ?? "<new or empty>";
            }
            if (val is IFormattable formattable)
            {
                return formattable.ToString(Format, CultureInfo.InvariantCulture);
            }
            return val.ToString();
        }

        public override bool CheckIn(DBItem item, T? val1, object val2, bool not)
        {
            if (val2 is IEnumerable<T> typedList)
            {
                foreach (T entry in typedList)
                {
                    if (Equal(entry, val1) && !not)
                        return true;
                }
                return not;
            }
            return base.CheckIn(item, val1, val2, not);
        }

        public override T? Parse(object value)
        {
            return base.Parse(value);
        }

        public override void Write(BinaryInvokerWriter writer, object element)
        {
            if (element is DBItem item)
            {
                T? value = GetValue(item);
                if (value == null)
                    writer.WriteNull();
                else
                    TypedSerializer.Write(writer, value, null, null);
            }
            else
                base.Write(writer, element);
        }

        public override void Write<E>(BinaryInvokerWriter writer, E element)
        {
            if (element is DBItem item)
            {
                T? value = GetValue(item);
                if (value == null)
                    writer.WriteNull();
                else
                    TypedSerializer.Write(writer, value, null, null);
            }
            else
                base.Write(writer, element);
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