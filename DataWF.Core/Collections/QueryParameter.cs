using DataWF.Common.Collections.Query;
using DataWF.Common.Collections.Query.Operators;
using DataWF.Common.Invokers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Common.Collections
{
    public class QueryParameter<T, V> : IQueryParameter<T, V>, INotifyPropertyChanged, INamed
    {
        private object value;
        private CompareOperator comparer = CompareOperator.Equal;
        private LogicOperator logic = LogicOperator.And;
        private string name;
        private IInvoker<T, V> invoker;
        private object typedValue;
        private bool isEnabled = true;
        private bool emptyFormat;
        private QueryGroup group = QueryGroup.None;

        public QueryParameter()
        { }

        public QueryParameter(IInvoker invoker)
        {
            Invoker = (IInvoker<T, V>)invoker;
        }

        public QueryParameter(string property)
        {
            Name = property;
            if (Invoker?.DataType == typeof(string))
            {
                Comparer = CompareOperator.Like;
            }
        }

        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    if (invoker != null && invoker.Name != value)
                    {
                        invoker = null;
                    }

                    name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FormatName
        {
            get; set;
        }

        IInvoker IQParameter.LeftProperty
        {
            get => Invoker;
            set => Invoker = (IInvoker<T, V>)value;
        }

        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, XmlIgnore]
        public IInvoker<T, V> Invoker
        {
            get => invoker ?? (invoker = (IInvoker<T, V>)EmitInvoker.Initialize<T>(Name));
            set
            {
                invoker = value;
                Name = invoker?.Name;
            }
        }

        public bool IsGlobal
        {
            get; set;
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (value != isEnabled)
                {
                    isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool FormatIgnore { get; set; } = false;

        public bool FormatEmpty
        {
            get => emptyFormat ? true : (Comparer.Type & CompareOperatorType.Is) != CompareOperatorType.Is
                    && Comparer.Type != CompareOperatorType.Distinct
                  && (RightValue == null || (RightValue is string strFilter && strFilter.Length == 0)
                  || string.IsNullOrEmpty(FormatValue(RightValue, Comparer)));
            set => emptyFormat = value;
        }

        public object RightValue
        {
            get => value;
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    TypedValue = Helper.ParseParameter(value, Comparer, Invoker?.DataType);
                    OnPropertyChanged();
                }
            }
        }

        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, XmlIgnore]
        public object TypedValue
        {
            get => typedValue;
            set => typedValue = value;
        }

        public CompareOperator Comparer
        {
            get => comparer;
            set
            {
                if (comparer != value)
                {
                    comparer = value;
                    OnPropertyChanged();
                }
            }
        }

        public LogicOperator Logic
        {
            get => logic;
            set
            {
                if (logic != value)
                {
                    logic = value;
                    OnPropertyChanged();
                }
            }
        }

        public QueryGroup Group { get => group; set => group = value; }

        IComparer IQParameter.Comparision { get => (IComparer)Comparision; set => Comparision = (IComparer<V>)value; }

        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, XmlIgnore]
        public IComparer<V> Comparision { get; set; }

        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, XmlIgnore]
        public object Tag { get; set; }

        public bool AlwaysTrue { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Format(bool logic = true)
        {
            var sb = new StringBuilder();
            Format(sb, logic);
            return sb.ToString();
        }

        public void Format(StringBuilder builder, bool logic = true)
        {
            if (logic)
            {
                builder.Append($" {Logic.Format()} ");
            }
            builder.Append((Group & QueryGroup.Begin) != 0 ? "(" : "");
            if (Comparer.Type == CompareOperatorType.Distinct)
            {
                builder.Append($"{Comparer.Format()}({FormatName ?? Name})");
            }
            else
            {
                builder.Append($"{FormatName ?? Name} {Comparer.Format()} {FormatValue(RightValue, Comparer)}");
            }
            builder.Append((Group & QueryGroup.End) != 0 ? ")" : "");
        }

        private string FormatValue(object value, CompareOperator comparer)
        {
            string result;
            if (Invoker == null)
            {
                result = value.ToString();
            }
            else if (value == null)
            {
                result = "null";
            }
            else if (value is IQueryFormatable formatable)
            {
                result = formatable.Format();
            }
            else if (value is DateTime date)
            {
                result = $"'{date.ToString("yyyy.MM.dd")}'";
            }
            else if (value is string stringValue)
            {
                if ((comparer.Type & CompareOperatorType.Like) == CompareOperatorType.Like
                    && stringValue.IndexOf('%') < 0)
                    result = $"'%{stringValue}%'";
                else if ((comparer.Type & CompareOperatorType.In) == CompareOperatorType.In
                    && Invoker?.DataType != typeof(string))
                    result = stringValue;
                else
                    result = $"'{stringValue}'";
            }
            else if (value is IEnumerable enumerable)
            {
                var casted = enumerable.Cast<object>();
                result = casted.Count() == 0
                    ? string.Empty
                    : string.Join(", ", casted.Select(p => FormatValue(p, CompareOperator.Equal)));
            }
            else
            {
                result = value.ToString();
            }
            if (comparer.Type == CompareOperatorType.In)
            {
                result = string.Concat('(', result, ')');
            }
            return result;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{(IsEnabled ? "On" : "Off")} {Logic} {Name} {Comparer} {FormatValue(RightValue, Comparer)}";
        }

        public bool Compare(object item)
        {
            return Compare((T)item);
        }

        public bool Compare(T item)
        {
            return ListHelper.CheckItem<V>(Invoker.GetValue(item), TypedValue, Comparer, Comparision);
        }

        public bool CheckValue(object value)
        {
            return CheckValue((V)value);
        }

        public bool CheckValue(V value)
        {
            return ListHelper.CheckItem<V>(value, TypedValue, Comparer, Comparision);
        }

        public IEnumerable Select(IEnumerable items)
        {
            return Search((IEnumerable<T>)items);
        }

        public IEnumerable<T> Search(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                if (item != null && Compare(item))
                {
                    yield return item;
                }
            }
        }

        public IEnumerable Select(IEnumerable items, IListIndexes indexes = null)
        {
            return Select((IEnumerable<T>)items, (IListIndexes<T>)indexes);
        }

        public IEnumerable<T> Select(IEnumerable<T> items, IListIndexes<T> indexes = null)
        {
            if (AlwaysTrue)
            {
                return items;
            }

            if (Comparer.Type == CompareOperatorType.Distinct)
            {
                return Distinct(items);
            }

            var index = (IListIndex<T>)indexes?.GetIndex(Name);
            if (index != null)
            {
                return index.Select(this);
            }
            return Search(items);
        }

        public IEnumerable Distinct(IEnumerable items)
        {
            return Distinct((IEnumerable<T>)items);
        }

        public IEnumerable<T> Distinct(IEnumerable<T> items)
        {
            var oldValue = default(V);
            var list = items.ToList();
            ListHelper.QuickSort(list, Invoker.CreateComparer<T>(ListSortDirection.Descending));
            foreach (var item in list)
            {
                var newValue = Invoker.GetValue(item);
                if (!ListHelper.Equal(newValue, oldValue))
                {
                    oldValue = newValue;
                    yield return item;
                }
            }
        }

        public QueryParameter<T, V> WithTag(object tag)
        {
            Tag = tag;
            return this;
        }

        IQueryParameter<T, V> IQueryParameter<T, V>.WithTag(object tag) => WithTag(tag);

        IQParameter<T> IQParameter<T>.WithTag(object tag) => WithTag(tag);

        IQParameter IQParameter.WithTag(object tag) => WithTag(tag);
    }

    [Flags]
    public enum QueryGroup
    {
        None = 0,
        Begin = 1,
        End = 2
    }
}

