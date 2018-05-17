
namespace DataWF.Data
{
    public interface IDataProvider
    {
        DBSchema CreateNew();
        void Generate();
        void Load();
        void Save();
    }
}