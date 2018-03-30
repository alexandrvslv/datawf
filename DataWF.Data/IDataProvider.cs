
namespace DataWF.Data
{
    public interface IDataProvider
    {
        DBSchema CreateNew();
        void Generate();
        void GenerateData();
        void Load();
        void Save();
    }
}