
using DataWF.Common;

namespace DataWF.Data
{

    public interface IDataProvider
    {
        void Load();
        void Save();
    }
}