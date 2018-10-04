using Newtonsoft.Json;
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
        private object parameter;
        private CompareType comparer = CompareType.Equal;
        private LogicType logic = LogicType.And;
        private string name;
        private IInvoker invoker;
        private object typedValue;
        private bool isEnabled = true;
        private bool groupBegin = false;
        private bool groupEnd = false;
        private bool emptyFormat;

        public QueryParameter()
        { }

        public QueryParameter(string property)
        {
            Name = property;
            if (Invoker?.DataType == typeof(string))
            {
                Comparer = CompareType.Like;
            }
        }

        [JsonIgnore, XmlIgnore]
        public object Tag { get; set; }

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

        [JsonIgnore, XmlIgnore]
        public IInvoker Invoker
        {
            get { return invoker ?? (invoker = EmitInvoker.Initialize<T>(Name)); }
            set
            {
                invoker = value;
                Name = invoker?.Name;
            }
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

        public bool FormatEmpty
        {
            get
            {
                return emptyFormat ? true : Comparer.Type != CompareTypes.Is
                  && (Value == null || (Value is string strFilter && strFilter.Length == 0) || string.IsNullOrEmpty(FormatValue()));
            }
            set { emptyFormat = value; }
        }

        public object Value
        {
            get { return parameter; }
            set
            {
                if (parameter != value)
                {
                    parameter = value;
                    TypedValue = Comparer.Type != CompareTypes.In && Invoker != null ? Helper.Parse(parameter, Invoker.DataType) : value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore, XmlIgnore]
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

        public void Format(StringBuilder builder, bool logic = true)
        {
            if (logic)
            {
                builder.Append($" {Logic.Format()} ");
            }
            builder.Append($"{(GroupBegin ? "(" : "")}{FormatName ?? Name} {Comparer.Format()} {FormatValue()}{(GroupEnd ? ")" : "")}");
        }

        private string FormatValue()
        {
            if (Invoker == null)
            {
                return Value.ToString();
            }
            if (Value == null)
            {
                return "null";
            }
            if (Value is IQueryFormatable formatable)
            {
                return formatable.Format();
            }
            var type = TypeHelper.CheckNullable(Invoker.DataType);
            if (Value is string stringValue)
            {
                if (Comparer.Type == CompareTypes.Like && stringValue.IndexOf('%') < 0)
                    return $"'%{stringValue}%'";
                return $"'{stringValue}'";
            }
            else if (Value is IEnumerable enumerable)
            {
                return string.Concat('(', string.Join(", ", enumerable.Cast<object>().Select(p => p is string ps ? $"'ps'" : p.ToString())), ')');
            }
            else
            {
                return Value.ToString();
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

        public bool GroupBegin { get => groupBegin; set => groupBegin = value; }

        public bool GroupEnd { get => groupEnd; set => groupEnd = value; }


        [JsonIgnore, XmlIgnore]
        public IComparer Comparision { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

