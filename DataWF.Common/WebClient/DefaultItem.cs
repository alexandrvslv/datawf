using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers(propertyChanged);

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
            if (propertyChanged != null)
            {
                var arg = new PropertyChangedEventArgs(propertyName);
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
        }

        protected virtual void OnPropertyChanging(object oldValue, [CallerMemberName] string propertyName = null)
        {
            OnPropertyChanging(propertyName);
        }
    }


}