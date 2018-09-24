using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public abstract class SynchronizedItem : IContainerNotifyPropertyChanged, INotifyPropertyChanging, ISynchronized
    {
        private bool? isSynchronized = null;

        public event PropertyChangedEventHandler PropertyChanged;

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
            PropertyChanged?.Invoke(this, arg);
        }
    }
}