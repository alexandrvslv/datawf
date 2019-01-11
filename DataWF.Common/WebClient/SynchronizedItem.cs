using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public abstract class SynchronizedItem : DefaultItem, ISynchronized
    {

        private bool? isSynchronized = null;

        [JsonIgnore, XmlIgnore]
        public ISet<string> Changes { get; } = new HashSet<string>();

        [JsonIgnore, XmlIgnore]
        public virtual bool? IsSynchronized
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

        protected override void OnPropertyChanged(bool synch, [CallerMemberName] string propertyName = null)
        {
            if (synch)
            {
                if (IsSynchronized != null)
                {
                    IsSynchronized = false;
                }
                Changes.Add(propertyName);
            }

            base.OnPropertyChanged(propertyName);
        }
    }


}