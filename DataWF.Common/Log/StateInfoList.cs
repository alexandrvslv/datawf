using System.IO;

namespace DataWF.Common
{
    public class StateInfoList : SelectableList<StateInfo>, IFileSerialize
    {
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

