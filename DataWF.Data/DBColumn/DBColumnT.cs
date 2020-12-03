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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBColumn<T> : DBColumn, IInvoker<DBItem, T>
    {
        public new static readonly DBColumn<T> EmptyKey = new DBColumn<T>();

        protected GenericPull<T> pull;
        private IValuedInvoker<T> typedPropertyInvoker;

        public DBColumn() : base()
        {
            DataType = typeof(T);
            TypedSerializer = (IElementSerializer<T>)TypeHelper.GetSerializer(DataType);
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override Pull Pull
        {
            get => pull;
            internal set
            {
                if (pull != value)
                {
                    pull = (GenericPull<T>)value;
                }
            }
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public override IInvoker PropertyInvoker
        {
            get => base.PropertyInvoker;
            set
            {
                base.PropertyInvoker = value;
                typedPropertyInvoker = value as IValuedInvoker<T>;
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public IElementSerializer<T> TypedSerializer { get; set; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override IElementSerializer Serializer { get => TypedSerializer; }

        public override bool Equal(object oldValue, object newValue)
        {
            return Equal((T)oldValue, (T)newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool Equal(T oldValue, T newValue)
        {
            return EqualityComparer<T>.Default.Equals(oldValue, newValue);
        }

        public override void Copy(int fromIndex, int toIndex)
        {
            var value = pull.GetValue(fromIndex);
            pull.SetValue(toIndex, value);
        }

        public override void Copy(DBItem fromItem, DBItem toItem, DBSetValueMode mode = DBSetValueMode.Default)
        {
            SetValue(toItem, GetValue(fromItem), mode);
        }

        public override void Copy(DBItem fromItem, DBColumn fromColumn, DBItem toItem, DBSetValueMode mode = DBSetValueMode.Default)
        {
            SetValue(toItem, ((DBColumn<T>)fromColumn).GetValue(fromItem), mode);
        }

        public override bool IsNull(DBItem item)
        {
            return Equal(GetValue(item), default(T));
        }

        public override bool IsNull(int index)
        {
            return Equal(pull.GetValue(index), default(T));
        }

        public override void Clear(DBItem item, DBSetValueMode mode = DBSetValueMode.Default)
        {
            SetValue(item, default(T), mode);
        }

        public override void Clear(int index)
        {
            pull.SetValue(index, default(T));
        }

        public override object GetValue(object item)
        {
            return GetValue((DBItem)item);
        }

        public virtual T GetValue(DBItem item)
        {
            if (pull != null)
                return pull.GetValue(item.block, item.blockIndex);
            else if (typedPropertyInvoker != null)
                return typedPropertyInvoker.GetValue(item);
            return default(T);
        }

        public override void SetValue(object item, object value)
        {
            SetValue((DBItem)item, (T)value);
        }

        public virtual void SetValue(DBItem item, T value)
        {
            if (pull != null)
            {
                pull.SetValue(item.block, item.blockIndex, value);
            }
            else if (typedPropertyInvoker != null)
            {
                typedPropertyInvoker.SetValue(item, value);
            }
            else
            {
                PropertyInvoker?.SetValue(item, value);
            }
        }

        public override void SetValue(DBItem item, object value, DBSetValueMode mode)
        {
            SetValue(item, (T)value, mode);
        }

        public void SetValue(DBItem item, T value, DBSetValueMode mode)
        {
            if (mode == DBSetValueMode.Loading && !item.Attached)
            {
                SetValue(item, value);
                return;
            }

            var check = mode == DBSetValueMode.Default && ColumnType == DBColumnTypes.Default;
            var oldValue = GetValue(item);

            if (Equal(oldValue, value))
            {
                return;
            }
            if (check)
            {
                item.Backup();
            }

            item.OnPropertyChanging<T>(PropertyName ?? Name, this, oldValue);

            SetValue(item, value);

            item.OnPropertyChanged<T>(PropertyName ?? Name, this, value);

            if (check)
            {
                item.CheckState();
            }
        }

        public override R GetReference<R>(DBItem item, DBLoadParam param)
        {
            if (!IsReference)
                return null;
            if (IsPrimaryKey)
                return (R)item;
            T value = typedPropertyInvoker.GetValue(item);
            if (Equal(value, default(T)))
                return null;
            return (R)ReferenceTable.LoadItemById(value, param);
        }

        public override R GetReference<R>(DBItem item, ref R reference, DBLoadParam param)
        {
            T value = typedPropertyInvoker.GetValue(item);
            var id = reference == null ? default(T) : reference.GetValue<T>(reference.Table.PrimaryKey);
            if (!Equal(value, default(T)) && Equal(value, id))
                return reference;

            return reference = value == null ? (R)null : (R)ReferenceTable.LoadItemById(value, param);
        }

        public override void SetReference<R>(DBItem item, R reference)
        {
            var id = reference == null ? default(T) : reference.GetValue<T>(reference.Table.PrimaryKey);
            typedPropertyInvoker.SetValue(item, id);
        }

        public override object GetParameterValue(DBItem item)
        {
            return GetValue(item);
        }

        public override string FormatValue(DBItem item)
        {
            return FormatValue(GetValue(item));
        }

        public override string FormatValue(object val)
        {
            return FormatValue((T)val);
        }

        public virtual string FormatValue(T val)
        {
            //if value passed to format is null
            if (val == null)
                return string.Empty;
            if (IsReference)
            {
                DBItem temp = ReferenceTable.LoadItemById(val);
                return temp == null ? "<new or empty>" : temp.ToString();
            }
            return val.ToString();;
        }

        public override bool GetOld(DBItem item, out object obj)
        {
            if (GetOld(item, out T value))
            {
                obj = value;
                return true;
            }
            obj = null;
            return false;
        }

        public bool GetOld(DBItem item, out T old)
        {
            old = default(T);
            if (pull == null)
                return false;

            if (item.oldHandler is int handler)
            {
                old = pull.GetValue(handler);
                return !Equal(old, pull.GetValue(item.block, item.blockIndex));
            }
            return (item.UpdateState & DBUpdateState.Insert) != 0 ? !Equal(old, pull.GetValue(item.block, item.blockIndex)) : false;
        }

        public override void Reject(DBItem item, DBSetValueMode mode = DBSetValueMode.Default)
        {
            if (GetOld(item, out var value))
            {
                SetValue(item, value, mode);
            }
        }

        protected internal override void CheckPull()
        {
            if (!Containers.Any())
                return;

            if (ColumnType == DBColumnTypes.Expression
                || ColumnType == DBColumnTypes.Code)
            {
                return;
            }

            if (Pull != null &&
                (Pull.ItemType != DataType))
            {
                Pull.Clear();
                Pull = null;
            }
            if (Pull == null && Table != null)
            {
                Pull = CreatePull();
            }
            else if (Pull.BlockSize != Table.BlockSize)
            {
                Pull.BlockSize = Table.BlockSize;
            }
        }

        protected internal override PullIndex CreatePullIndex()
        {
            return PullIndexFactory.Create(Pull, typeof(DBItem), DataType, Table.DefaultComparer);
        }

        public override bool CheckItem(DBItem item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return ListHelper.CheckItemT(GetValue(item), typedValue, comparer, (IComparer<T>)comparision);
        }

        public virtual Pull CreatePull()
        {
            return new PullArray<T>(Table.BlockSize);
        }

        public override void Clear()
        {
            Pull?.Clear();
        }

        public override void Read(DBTransaction transaction, DBItem row, int i)
        {
            if (row.Attached && row.UpdateState != DBUpdateState.Default && row.GetOld(this, out _))
            {
                return;
            }
            var value = transaction.Reader.IsDBNull(i) ? default(T) : transaction.Reader.GetFieldValue<T>(i);
            SetValue(row, value, DBSetValueMode.Loading);
        }

        public override F ReadAndSelect<F>(DBTransaction transaction, int i)
        {
            var value = transaction.Reader.GetFieldValue<T>(i);
            return Table.GetPullIndex<T>(this)?.SelectOne<F>(value);
        }

        public override void Write(BinaryInvokerWriter writer, object element)
        {
            if (element is DBItem item)
            {
                T value = GetValue(item);
                TypedSerializer.Write(writer, value, null, null);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void Write<E>(BinaryInvokerWriter writer, E element)
        {
            if (element is DBItem item)
            {
                T value = GetValue(item);
                TypedSerializer.Write(writer, value, null, null);
            }
            else
            {
                Write(writer, (object)element);
            }
        }

        public override void Read(BinaryInvokerReader reader, object element, TypeSerializeInfo itemInfo)
        {
            var token = reader.ReadToken();
            if (element is DBItem item)
            {
                if (token == BinaryToken.Null)
                {
                    SetValue(item, default(T));
                }
                else
                {
                    T value = TypedSerializer.Read(reader, default(T), null, null);
                    SetValue(item, value);
                }
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void Read<E>(BinaryInvokerReader reader, E element, TypeSerializeInfo itemInfo)
        {
            if (element is DBItem item)
            {
                var token = reader.ReadToken();
                if (token == BinaryToken.Null)
                {
                    SetValue(item, default(T));
                }
                else
                {
                    T value = TypedSerializer.Read(reader, default(T), null, null);
                    SetValue(item, value);
                }
            }
            else
            {
                Read(reader, (object)element, itemInfo);
            }
        }

        public override void Write(XmlInvokerWriter writer, object element)
        {
            if (PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                writer.WriteStart(this);
                TypedSerializer.Write(writer, valueInvoker.GetValue(element), null);
                writer.WriteEnd(this);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void Write<E>(XmlInvokerWriter writer, E element)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                writer.WriteStart(this);
                TypedSerializer.Write(writer, valueInvoker.GetValue(element), null);
                writer.WriteEnd(this);
            }
            else
            {
                Write(writer, (object)element);
            }
        }

        public override void Read(XmlInvokerReader reader, object element, TypeSerializeInfo itemInfo)
        {
            if (PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                valueInvoker.SetValue(element, TypedSerializer.Read(reader, value, itemInfo));
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void Read<E>(XmlInvokerReader reader, E element, TypeSerializeInfo itemInfo)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                T value = valueInvoker.GetValue(element);
                valueInvoker.SetValue(element, TypedSerializer.Read(reader, value, itemInfo));
            }
            else
            {
                Read(reader, (object)element, itemInfo);
            }
        }

        public override void Write(Utf8JsonWriter writer, object element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                writer.WritePropertyName(valueInvoker.JsonName);
                JsonSerializer.Serialize(writer, valueInvoker.GetValue(element), options);
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void Write<E>(Utf8JsonWriter writer, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                writer.WritePropertyName(valueInvoker.JsonName);
                JsonSerializer.Serialize(writer, valueInvoker.GetValue(element), options);
            }
            else
            {
                Write(writer, (object)element, options);
            }
        }

        public override void Read(ref Utf8JsonReader reader, object element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IValuedInvoker<T> valueInvoker)
            {
                valueInvoker.SetValue(element, JsonSerializer.Deserialize<T>(ref reader, options));
            }
            else
            {
                throw new Exception("Wrong Property Invoker");
            }
        }

        public override void Read<E>(ref Utf8JsonReader reader, E element, JsonSerializerOptions options = null)
        {
            if (PropertyInvoker is IInvoker<E, T> valueInvoker)
            {
                valueInvoker.SetValue(element, JsonSerializer.Deserialize<T>(ref reader, options));
            }
            else
            {
                Read(ref reader, (object)element, options);
            }
        }

    }
}