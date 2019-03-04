using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public enum SynchronizedStatus
    {
        New,
        Edit,
        Load,
        Actual
    }

    public abstract class SynchronizedItem : DefaultItem, ISynchronized
    {
        private SynchronizedStatus syncStatus = SynchronizedStatus.New;

        [JsonIgnore, XmlIgnore]
        public ISet<string> Changes { get; } = new HashSet<string>();

        [JsonIgnore, XmlIgnore]
        public virtual SynchronizedStatus SyncStatus
        {
            get => syncStatus;
            set
            {
                if (syncStatus != value)
                {
                    syncStatus = value;
                    if (syncStatus == SynchronizedStatus.Actual)
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
                if (syncStatus == SynchronizedStatus.Actual)
                {
                    SyncStatus = SynchronizedStatus.Edit;
                }
                if (syncStatus != SynchronizedStatus.Load)
                {
                    Changes.Add(propertyName);
                }
            }

            base.OnPropertyChanged(propertyName);
        }
    }


}