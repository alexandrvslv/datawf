using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class QueryParameter<T> : IQueryParameter, INotifyPropertyChanged, INamed
    {
        private object value;
        private CompareType comparer = CompareType.Equal;
        private LogicType logic = LogicType.And;
        private string name;
        private IInvoker invoker;
        private object typedValue;
        private bool isEnabled = true;
        private bool emptyFormat;
        private QueryGroup group = QueryGroup.None;

        public QueryParameter()
        { }

        public QueryParameter(IInvoker invoker)
        {
            Invoker = invoker;
        }

        public QueryParameter(string property)
        {
            Name = property;
            if (Invoker?.DataType == typeof(string))
            {
                Comparer = CompareType.Like;
            }
        }

        public string Name
        {
            get { return name; }
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

        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, XmlIgnore]
        public IInvoker Invoker
        {
            get { return invoker ?? (invoker = EmitInvoker.Initialize<T>(Name)); }
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
            get { return isEnabled; }
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
            get
            {
                return emptyFormat ? true : Comparer.Type != CompareTypes.Is
                    && Comparer.Type != CompareTypes.Distinct
                  && (Value == null || (Value is string strFilter && strFilter.Length == 0)
                  || string.IsNullOrEmpty(FormatValue(Value, Comparer)));
            }
            set { emptyFormat = value; }
        }

        public object Value
        {
            get { return value; }
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
            get { return typedValue; }
            set { typedValue = value; }
        }

        public CompareType Comparer
        {
            get { return comparer; }
            set
            {
                if (comparer != value)
                {
                    comparer = value;
                    OnPropertyChanged();
                }
            }
        }

        public LogicType Logic
        {
            get { return logic; }
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

        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, XmlIgnore]
        public IComparer Comparision { get; set; }

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
            if (Comparer.Type == CompareTypes.Distinct)
            {
                builder.Append($"{Comparer.Format()}({FormatName ?? Name})");
            }
            else
            {
                builder.Append($"{FormatName ?? Name} {Comparer.Format()} {FormatValue(Value, Comparer)}");
            }
            builder.Append((Group & QueryGroup.End) != 0 ? ")" : "");
        }

        private string FormatValue(object value, CompareType comparer)
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
                if (comparer.Type == CompareTypes.Like && stringValue.IndexOf('%') < 0)
                    result = $"'%{stringValue}%'";
                else if (comparer.Type == CompareTypes.In && Invoker?.DataType != typeof(string))
                    result = stringValue;
                else
                    result = $"'{stringValue}'";
            }
            else if (value is IEnumerable enumerable)
            {
                var casted = enumerable.Cast<object>();
                result = casted.Count() == 0
                    ? string.Empty
                    : string.Join(", ", casted.Select(p => FormatValue(p, CompareType.Equal)));
            }
            else
            {
                result = value.ToString();
            }
            if (comparer.Type == CompareTypes.In)
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
            return $"{(IsEnabled ? "On" : "Off")} {Logic} {Name} {Comparer} {Value}";
        }
    }

    [Flags]
    public enum QueryGroup
    {
        None = 0,
        Begin = 1,
        End = 2
    }
}

