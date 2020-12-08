using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public abstract class DefaultItem : IEntryNotifyPropertyChanged
    {
        public static Action<PropertyChangedEventHandler, object, PropertyChangedEventArgs> GlogalChangedHook;
        protected PropertyChangedEventHandler propertyChanged;

        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, XmlIgnore, Browsable(false)]
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers<INotifyListPropertyChanged>(propertyChanged);

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => propertyChanged += value;
            remove => propertyChanged -= value;
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs arg)
        {
            foreach (PropertyChangedEventHandler handler in propertyChanged.GetInvocationList())
            {
                if (GlogalChangedHook == null || handler.Target is INotifyListPropertyChanged)
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