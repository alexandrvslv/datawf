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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBColumn<T> : DBColumn, IInvoker<DBItem, T>, IValuedInvoker<T>
    {
        public new static readonly DBColumn<T> EmptyKey = new DBColumn<T>();
        private static readonly char[] trimEntry = new char[] { ' ', '\'' };
        protected GenericPull<T> pull;
        protected IPullOutIndex<DBItem, T> pullIndex;
        private IInvoker propertyInvoker;
        private IValuedInvoker<T> typedPropertyInvoker;
        private IEqualityComparer<T> equalityComparer;

        public DBColumn() : base()
        {
            DataType = typeof(T);
            TypedSerializer = (IElementSerializer<T>)TypeHelper.GetSerializer(DataType);
            equalityComparer = ListHelperEqualityComparer<T>.Default;
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override int SizeOfDataType => TypedSerializer.SizeOfType;

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override Pull Pull
        {
            get => pull;
            internal set
            {
                if (pull != value)
                {
                    pull = (GenericPull<T>)value;
                    CheckPullIndex();
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override IPullIndex PullIndex
        {
            get => pullIndex;
            internal set
            {
                if (pullIndex != value)
                {
                    pullIndex = (IPullOutIndex<DBItem, T>)value;
                }
            }
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public override IInvoker PropertyInvoker
        {
            get => propertyInvoker ?? this;
            set
            {
                propertyInvoker = value;
                typedPropertyInvoker = value as IValuedInvoker<T>;
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public IElementSerializer<T> TypedSerializer { get; set; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override IElementSerializer Serializer { get => TypedSerializer; }

        protected internal override void CheckPullIndex()
        {
            if (pullIndex != null && pullIndex.BasePull != Pull)
            {
                pullIndex.Dispose();
                pullIndex = null;
            }
            if (pullIndex == null && Pull != null && (IsPrimaryKey
                || (Keys & DBColumnKeys.Indexing) == DBColumnKeys.Indexing
                || (Keys & DBColumnKeys.Reference) == DBColumnKeys.Reference))
                PullIndex = CreatePullIndex();
        }

        public void RemovePullIndex()
        {
            if (pullIndex != null)
            {
                pullIndex.Dispose();
                pullIndex = null;
            }
        }

        protected internal override PullIndex CreatePullIndex()
        {
            return PullIndexFactory.Create(Pull, Table.ItemType.Type, DataType, Table.DefaultComparer);
        }

        public override void AddIndex<F>(F item)
        {
            ((IPullInIndex<F, T>)pullIndex)?.Add(item);
        }

        public override void RemoveIndex<F>(F item)
        {
            ((IPullInIndex<F, T>)pullIndex)?.Remove(item);
        }

        public override IEnumerable<TT> SelectIndex<TT>(object value, CompareType comparer)
        {
            return ((IPullOutIndex<TT, T>)pullIndex).Select(value, comparer);
        }

        public override bool Equal(object oldValue, object newValue)
        {
            return Equal((T)oldValue, (T)newValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool Equal(T oldValue, T newValue)
        {
            return equalityComparer.Equals(oldValue, newValue);
        }

        public override void Copy(PullHandler fromIndex, PullHandler toIndex)
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
            if (fromColumn is DBColumn<T> typedColumn)
                SetValue(toItem, typedColumn.GetValue(fromItem), mode);
            else
                SetValue(toItem, fromColumn.GetValue(fromItem), mode);
        }

        public override bool IsEmpty(DBItem item)
        {
            return Equal(GetValue(item), default(T));
        }

        public override bool IsEmpty(PullHandler handler)
        {
            return Equal(pull.GetValue(handler), default(T));
        }

        public override void SetId(DBItem item, long id)
        {
            SetValue(item, id, DBSetValueMode.Default);
        }

        public override void Clear(DBItem item, DBSetValueMode mode = DBSetValueMode.Default)
        {
            SetValue(item, default(T), mode);
        }

        public override void Clear(PullHandler handler)
        {
            pull.SetValue(handler, default(T));
        }

        public override object GetValue(object item)
        {
            return GetValue((DBItem)item);
        }

        public virtual T GetValue(DBItem item)
        {
            if (pull != null)
                return pull.GetValue(in item.handler);
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
                pull.SetValue(in item.handler, value);
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
            SetValue(item, Parse(value), mode);
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
            return (R)LoadReference(value, param);
        }

        public override R GetReference<R>(DBItem item, ref R reference, DBLoadParam param)
        {
            T value = typedPropertyInvoker.GetValue(item);
            var isEmpty = Equal(value, default(T));
            var id = reference == null ? default(T) : GetReferenceId(reference);
            if (!isEmpty && Equal(value, id))
                return reference;

            return reference = isEmpty ? (R)null : (R)LoadReference(value, param);
        }

        public override void SetReference<R>(DBItem item, R reference)
        {
            var id = reference == null ? default(T) : GetReferenceId(reference);
            typedPropertyInvoker.SetValue(item, id);
        }

        public override object GetParameterValue(DBItem item)
        {
            return GetParameterValue(GetValue(item));
        }

        public virtual object GetParameterValue(T value)
        {
            return value;
        }

        public override object GetParameterValue(object value)
        {
            if (value is T typedValue)
                return GetParameterValue(typedValue);
            else
                return GetParameterValue(Parse(value));
        }

        public override string FormatQuery(DBItem item)
        {
            return FormatQuery(GetValue(item));
        }

        public override string FormatQuery(object value)
        {
            if (value is T typedValue)
                return FormatQuery(typedValue);
            else
                return FormatQuery(Parse(value));
        }

        public virtual string FormatQuery(T value)
        {
            if (Equal(value, default(T)))
                return "null";
            if (value is IFormattable formattable)
            {
                return formattable.ToString(Format, CultureInfo.InvariantCulture);
            }
            return $"'{value}'";
        }

        public override string FormatDisplay(DBItem item)
        {
            return FormatDisplay(GetValue(item));
        }

        public override string FormatDisplay(object value)
        {
            if (value is T typedValue)
                return FormatDisplay(typedValue);
            else
                return FormatDisplay(Parse(value));
        }

        public virtual string FormatDisplay(T value)
        {
            if (Equal(value, default(T)))
                return string.Empty;

            if (IsReference)
            {
                DBItem temp = LoadReference(value, DBLoadParam.None);
                return temp == null ? "<new or empty>" : temp.ToString();
            }
            if (value is IFormattable formattable)
            {
                return formattable.ToString(Format, CultureInfo.InvariantCulture);
            }
            return value.ToString();
        }

        public override object ParseValue(object value)
        {
            return Parse(value);
        }

        public virtual T Parse(object value)
        {
            if (value is T typeValue)
                return typeValue;
            else if (value == null || value == DBNull.Value)
                return default(T);
            else if (value is DBItem item)
                return GetReferenceId(item);
            return (T)Helper.Parse(value, DataType);
        }

        protected virtual T GetReferenceId(DBItem item)
        {
            if (item.Table.PrimaryKey is DBColumn<T> typedColumn)
                return typedColumn.GetValue(item);
            return (T)item.PrimaryId;
        }

        protected virtual DBItem LoadReference(T id, DBLoadParam param)
        {
            if (ReferenceTable.PrimaryKey is DBColumn<T> typedColumn)
                return ReferenceTable.LoadItemByKey(id, typedColumn, param);
            return ReferenceTable.LoadItemByKey((object)id, ReferenceTable.PrimaryKey, param);
        }

        public override DBItem LoadByKey(DBItem item, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            return Table.LoadItemByKey(GetValue(item), this, param, cols, transaction);
        }

        public override DBItem LoadByKey(object key, DBLoadParam param = DBLoadParam.Load, IEnumerable<DBColumn> cols = null, DBTransaction transaction = null)
        {
            return Table.LoadItemByKey(Parse(key), this, param, cols, transaction);
        }

        public override bool IsChanged(DBItem item)
        {
            return GetOld(item, out T _);
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

            if (item.oldHandler is PullHandler handler)
            {
                old = pull.GetValue(handler);
                return !Equal(old, pull.GetValue(item.handler));
            }
            return (item.UpdateState & DBUpdateState.Insert) != 0 ? !Equal(old, pull.GetValue(item.handler)) : false;
        }

        public override void Reject(DBItem item, DBSetValueMode mode = DBSetValueMode.Default)
        {
            if (GetOld(item, out T value))
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

        public virtual Pull CreatePull()
        {
            return new PullArray<T>(Table.BlockSize);
        }

        public override void Clear()
        {
            PullIndex?.Clear();
            Pull?.Clear();
        }

        public override void Read(DBTransaction transaction, DBItem row, int i)
        {
            var value = transaction.Reader.IsDBNull(i) ? default(T) : transaction.Reader.GetFieldValue<T>(i);
            SetValue(row, value, DBSetValueMode.Loading);
        }

        public override F ReadAndSelect<F>(DBTransaction transaction, int i)
        {
            var value = transaction.Reader.GetFieldValue<T>(i);
            return ((IPullOutIndex<F, T>)pullIndex).SelectOne(value);
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
                writer.WritePropertyName(JsonName);
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
                writer.WritePropertyName(JsonName);
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

        public override bool CheckItem(DBItem item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return CheckItem(item, GetValue(item), typedValue, comparer);
        }

        public override bool CheckItem(DBItem item, object val2, CompareType comparer)
        {
            return CheckItem(item, GetValue(item), val2, comparer);
        }

        public bool CheckItem(DBItem item, T val2, CompareType comparer)
        {
            return CheckItem(item, GetValue(item), val2, comparer);
        }

        public bool CheckItem(DBItem item, T val1, T val2, CompareType comparer)
        {
            switch (comparer.Type)
            {
                case CompareTypes.Is:
                    return Equal(val1, default(T)) ? !comparer.Not : comparer.Not;
                case CompareTypes.Equal:
                    return Equal(val1, val2) ? !comparer.Not : comparer.Not;
                case CompareTypes.Greater:
                    return ListHelper.Compare<T>(val1, val2) > 0;
                case CompareTypes.GreaterOrEqual:
                    return ListHelper.Compare<T>(val1, val2) >= 0;
                case CompareTypes.Less:
                    return ListHelper.Compare<T>(val1, val2) < 0;
                case CompareTypes.LessOrEqual:
                    return ListHelper.Compare<T>(val1, val2) <= 0;
                default:
                    return true;
            }
        }

        public bool CheckItem(DBItem item, T val1, object val2, CompareType comparer)
        {
            if (item == null)
                return false;
            if (val2 is QQuery query2)
                val2 = item.Table.SelectValues(item, query2, comparer);

            switch (comparer.Type)
            {
                case CompareTypes.Like:
                    var r = val2 is Regex ? (Regex)val2 : Helper.BuildLike(val2.ToString());
                    return val1 != null && r.IsMatch(val1.ToString()) ? !comparer.Not : comparer.Not;
                case CompareTypes.In:
                    if (val2 is string)
                        val2 = val2.ToString().Split(QQuery.CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
                    return CheckIn(item, val1, val2, comparer.Not);
                case CompareTypes.Between:
                    var between = val2 as QBetween;
                    if (between == null)
                        throw new Exception($"Expect QBetween but Get {(val2 == null ? "null" : val2.GetType().FullName)}");
                    return ListHelper.Compare<T>(val1, (T)between.Min.GetValue(item)) >= 0
                                     && ListHelper.Compare<T>(val1, (T)between.Max.GetValue(item)) <= 0;
                default:
                    return CheckItem(item, val1, Parse(val2), comparer);
            }
        }

        public virtual bool CheckIn(DBItem item, T val1, object val2, bool not)
        {
            if (val2 is IEnumerable<T> typedList)
            {
                foreach (T entry in typedList)
                {
                    if (Equal(entry, val1) && !not)
                        return true;
                }
            }
            else if (val2 is IEnumerable list)
            {
                foreach (object entry in list)
                {
                    object comp = entry;
                    if (comp is QItem qItem)
                        comp = qItem.GetValue(item);
                    if (comp is string)
                        comp = ((string)comp).Trim(trimEntry);

                    if (comp.Equals(val1) && !not)
                        return true;
                }
            }
            return not;
        }

        public virtual IEnumerable<TT> Search<TT>(CompareType comparer, T value, IEnumerable<TT> list) where TT : DBItem
        {
            foreach (TT item in list)
            {
                if (CheckItem(item, value, comparer))
                    yield return item;
            }
        }

        public virtual IEnumerable<TT> Search<TT>(CompareType comparer, DBColumn<T> column, IEnumerable<TT> list) where TT : DBItem
        {
            foreach (TT item in list)
            {
                if (CheckItem(item, column.GetValue(item), comparer))
                    yield return item;
            }
        }

        public override IEnumerable<TT> Search<TT>(CompareType comparer, DBColumn column, IEnumerable<TT> list)
        {
            if (column is DBColumn<T> typedColumn)
                return Search(comparer, typedColumn, list);
            return base.Search(comparer, column, list);
        }

        public override IEnumerable<TT> Search<TT>(CompareType comparer, object value, IEnumerable<TT> list)
        {
            if (value is T typedValue)
                return Search(comparer, typedValue, list);
            if (value == null)
                return Search(comparer, default(T), list);
            return base.Search(comparer, value, list);
        }

        public override IEnumerable Distinct(IEnumerable<DBItem> enumerable)
        {
            var result = new List<T>();
            foreach (var item in enumerable)
            {
                var value = GetValue(item);
                int index = ListHelper.BinarySearch<T>(result, value, null);
                if (index < 0)
                {
                    result.Insert(-index - 1, value);
                }
            }
            return result;
        }

        T IValuedInvoker<T>.GetValue(object target)
        {
            return GetValue((DBItem)target);
        }

        public void SetValue(object target, T value)
        {
            SetValue((DBItem)target, value);
        }
    }
}