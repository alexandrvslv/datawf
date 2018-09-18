using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public abstract class SynchronizedItem : IContainerNotifyPropertyChanged, ISynchronized
    {
        private bool? isSynchronized = true;

        public event PropertyChangedEventHandler PropertyChanged;

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
                    if (isSynchronized ?? true)
                    {
                        Changes.Clear();
                    }
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (IsSynchronized != null)
            {
                IsSynchronized = false;
                Changes.Add(propertyName);
            }
            var arg = new PropertyChangedEventArgs(propertyName);
            Container?.OnItemPropertyChanged(this, arg);
            PropertyChanged?.Invoke(this, arg);
        }
    }
}