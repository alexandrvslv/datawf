using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class StateInfoList : SelectableList<StateInfo>, IFileSerialize
    {
        public StateInfoList() : this((int)Math.Pow(2, 12))
        {
        }

        public StateInfoList(int limit) : base(128)
        {
            Limit = limit;
        }

        public int Limit { get; set; }

        public override NotifyCollectionChangedEventArgs OnCollectionChanged(NotifyCollectionChangedAction type, object item = null, int index = -1, int oldIndex = -1, object oldItem = null)
        {
            var args = base.OnCollectionChanged(type, item, index, oldIndex, oldItem);
            if (Limit > 0 && type == NotifyCollectionChangedAction.Add && Count == Limit)
            {
                //TODO Cross thread exception - clear when adding - index out
                _ = Task.Run(() =>
                {
                    Save($"logs_{DateTime.Now.ToString("yyMMddHHmmss")}.xml");
                    Clear();
                });
            }
            return args;
        }

        #region IFSerialize implementation
        public void Save(Stream stream)
        {
            Serialization.Serialize(this, stream);
        }

        public void Save(string file)
        {
            Serialization.Serialize(this, file);
        }

        public void Save()
        {
            Save(FileName);
        }

        public void Load(string file)
        {
            Serialization.Deserialize(file, this);
        }

        public void Load()
        {
            Load(FileName);
        }

        public string FileName
        {
            get { return "details.log"; }
            set { }
        }

        #endregion
    }
}

