namespace DataWF.Common
{
    public interface IFileSerialize
    {
        void Save(string file);

        void Save();

        void Load(string file);

        void Load();

        string FileName
        { get; set; }
    }
}

