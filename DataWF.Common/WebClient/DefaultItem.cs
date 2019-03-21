using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public abstract class DefaultItem : IContainerNotifyPropertyChanged
    {
        public static Action<PropertyChangedEventHandler, object, PropertyChangedEventArgs> GlogalChangedHook;
        protected PropertyChangedEventHandler propertyChanged;

        [JsonIgnore, XmlIgnore]
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers(propertyChanged);

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => propertyChanged += value;
            remove => propertyChanged -= value;
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs arg)
        {
            foreach (var handler in propertyChanged.GetInvocationList())
            {
                if (GlogalChangedHook == null || handler.Target is INotifyListPropertyChanged)
                {
                    ((PropertyChangedEventHandler)handler).Invoke(this, arg);
                }
                else
                {
                    GlogalChangedHook((PropertyChangedEventHandler)handler, this, arg);
                }
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyChanged != null)
            {
                OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            }
        }

        protected virtual void OnPropertyChanged(object oldValue, object newValue, [CallerMemberName] string propertyName = null)
        {
            if (propertyChanged != null)
            {
                OnPropertyChanged(new PropertyChangedDetailEventArgs(propertyName, oldValue, newValue));
            }
        }
    }
}