using Newtonsoft.Json;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

        [XmlIgnore]
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

        public IInvoker Invoker
        {
            get { return invoker ?? (invoker = EmitInvoker.Initialize<T>(Property)); }
            set
            {
                invoker = value;
                Property = invoker?.Name;
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
                    OnPropertyChanged();
                }
            }
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

        [JsonIgnore, XmlIgnore]
        public IComparer Comparision { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

