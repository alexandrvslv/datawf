using System;
using System.IO;

namespace DataWF.Common
{
    public class StateInfoList : SelectableList<StateInfo>, IFileSerialize
    {
        public StateInfoList() : this((int)Math.Pow(2, 14))
        {
        }

        public StateInfoList(int limit) : base(128)
        {
            Limit = limit;
        }

        public int Limit { get; set; }

        public override int Add(StateInfo item)
        {
            if (Limit > 0 && Count >= Limit)
            {
                RemoveAt(0);
            }
            return base.Add(item);
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

