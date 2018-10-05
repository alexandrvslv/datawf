using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public abstract class SynchronizedItem : IContainerNotifyPropertyChanged, INotifyPropertyChanging, ISynchronized
    {
        public static Action<PropertyChangedEventHandler, object, PropertyChangedEventArgs> GlogalChangedHook;

        private bool? isSynchronized = null;
        private PropertyChangedEventHandler propertyChanged;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => propertyChanged += value;
            remove => propertyChanged -= value;
        }

        public event PropertyChangingEventHandler PropertyChanging;

        [JsonIgnore, XmlIgnore]
        public INotifyListPropertyChanged Container { get; set; }

        [JsonIgnore, XmlIgnore]
        public ISet<string> Changes { get; } = new HashSet<string>();

        [JsonIgnore, XmlIgnore]
        public bool? IsSynchronized
        {
            get => isSynchronized;
            set
            {
                if (isSynchronized != value)
                {
                    isSynchronized = value;
                    if (isSynchronized ?? false)
                    {
                        Changes.Clear();
                    }
                }
            }
        }

        protected virtual void OnPropertyChanging([CallerMemberName] string propertyName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (IsSynchronized != null)
            {
                IsSynchronized = false;
            }

            Changes.Add(propertyName);

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
    }


}