using Newtonsoft.Json;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class QueryParameter<T> : IQueryParameter, INotifyPropertyChanged
    {
        private object parameter;
        private CompareType comparer = CompareType.Equal;
        private LogicType logic = LogicType.And;
        private string property;
        private IInvoker invoker;
        private object typedValue;

        [JsonIgnore, XmlIgnore]
        public object Tag { get; set; }

        public string Property
        {
            get { return property; }
            set
            {
                if (property != value)
                {
                    property = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public IInvoker Invoker
        {
            get { return invoker ?? (invoker = EmitInvoker.Initialize<T>(Property)); }
            set
            {
                invoker = value;
                Property = invoker?.Name;
            }
        }

        public bool IsEmpty
        {
            get { return Comparer.Type != CompareTypes.Is && (Value == null || (Value is string strFilter && strFilter.Length == 0)); }
        }

        public object Value
        {
            get { return parameter; }
            set
            {
                if (parameter != value)
                {
                    parameter = value;
                    TypedValue = Helper.Parse(parameter, Invoker.DataType);
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
            builder.Append($"{Property} {Comparer.Format()} {FormatValue()}");
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
            if (type == typeof(string))
            {
                return $"'{Value}'";
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

        [JsonIgnore, XmlIgnore]
        public IComparer Comparision { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

