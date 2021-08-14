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

        public bool WriteDebug { get; set; }

        public int Limit { get; set; }

        public void Add(string module, string message, string descriprion = null, StatusType type = StatusType.Information, object tag = null)
        {
            Add(new StateInfo(module, message, descriprion, type, tag));
        }

        public void Add(Exception ex)
        {
            Add(new StateInfo(ex));
        }

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

        public override int AddInternal(StateInfo item)
        {
            if(WriteDebug)
            System.Diagnostics.Debug.WriteLine(string.Concat(item.Module, " ", item.Message, " ", item.Description));

            return base.AddInternal(item);
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

