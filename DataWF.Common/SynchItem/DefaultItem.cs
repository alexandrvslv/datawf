using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public abstract class DefaultItem : IEntryNotifyPropertyChanged
    {
        public static Action<PropertyChangedEventHandler, object, PropertyChangedEventArgs> GlogalChangedHook;
        protected ThreadSafeList<PropertyChangedEventHandler> propertyChanged;
        
        [Newtonsoft.Json.JsonIgnore, JsonIgnore, XmlIgnore, Browsable(false)]
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers<INotifyListPropertyChanged, PropertyChangedEventHandler>(propertyChanged);

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                if (propertyChanged == null)
                    propertyChanged = new ThreadSafeList<PropertyChangedEventHandler>();
                propertyChanged.Add(value);
            }
            remove => propertyChanged?.Remove(value);
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs arg)
        {
            foreach (PropertyChangedEventHandler handler in propertyChanged)
            {
                if (GlogalChangedHook == null
                    || handler.Target is INotifyListPropertyChanged)
                {
                    handler.Invoke(this, arg);
                }
                else
                {
                    GlogalChangedHook(handler, this, arg);
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

        protected virtual void OnPropertyChanged<T>(T oldValue, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (propertyChanged != null)
            {
                OnPropertyChanged(new PropertyChangedDetailEventArgs<T>(propertyName, oldValue, newValue));
            }
        }
    }
}