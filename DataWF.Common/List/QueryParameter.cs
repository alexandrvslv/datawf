using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
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
        private ListSortDirection? sortDirection;
        private bool isEnabled = true;

        public QueryParameter()
        { }

        public QueryParameter(string property)
        {
            Property = property;
            if (Invoker?.DataType == typeof(string))
            {
                Comparer = CompareType.Like;
            }
        }

        [JsonIgnore, XmlIgnore]
        public object Tag { get; set; }

        public string Property
        {
            get { return property; }
            set
            {
                if (property != value)
                {
                    if (invoker != null && invoker.Name != value)
                    {
                        invoker = null;
                    }

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

        public bool IsEmptyFormat
        {
            get
            {
                return Comparer.Type != CompareTypes.Is
                  && (Value == null || (Value is string strFilter && strFilter.Length == 0) || string.IsNullOrEmpty(FormatValue()));
            }
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

        public ListSortDirection? SortDirection
        {
            get { return sortDirection; }
            set
            {
                if (sortDirection != value)
                {
                    sortDirection = value;
                    OnPropertyChanged();
                }
            }
        }

        public IComparer<T> GetComparer()
        {
            return SortDirection == null ? null :
                new InvokerComparer<T>(Invoker, SortDirection.Value);
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

