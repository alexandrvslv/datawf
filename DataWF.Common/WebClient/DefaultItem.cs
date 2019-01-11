using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public abstract class DefaultItem : IContainerNotifyPropertyChanged, INotifyPropertyChanging
    {
        public static Action<PropertyChangedEventHandler, object, PropertyChangedEventArgs> GlogalChangedHook;
        private PropertyChangedEventHandler propertyChanged;

        [JsonIgnore, XmlIgnore]
        public INotifyListPropertyChanged Container { get; set; }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => propertyChanged += value;
            remove => propertyChanged -= value;
        }

        public event PropertyChangingEventHandler PropertyChanging;

        protected virtual void OnPropertyChanging([CallerMemberName] string propertyName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var arg = new PropertyChangedEventArgs(propertyName);
            Container?.OnItemPropertyChanged(this, arg);
            if (propertyChanged != null)
            {
                if (GlogalChangedHook != null)
                {
                    GlogalChangedHook(propertyChanged, this, arg);
                }
                else
                {
                    propertyChanged(this, arg);
                }
            }
        }

        protected virtual void OnPropertyChanged(bool synch, [CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
    }


}