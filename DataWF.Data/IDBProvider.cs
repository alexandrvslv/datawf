
using DataWF.Common;

namespace DataWF.Data
{

    public interface IDBProvider
    {
        void Load();
        void Save();
    }
}