using DataWF.Common;
using DataWF.Data;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    public class ModuleInitialize : IModuleInitialize
    {
        public Task Initialize()
        {
            Book.DBTable.Load();

            Department.DBTable.Load();
            Position.DBTable.Load();

            UserGroup.DBTable.Load();
            UserGroup.SetCurrent();

            User.DBTable.DefaultComparer = new DBComparer(User.DBTable.CodeKey) { Hash = true };
            User.DBTable.Load();

            UserLog.DBTable.DefaultComparer = new DBComparer(UserLog.DBTable.PrimaryKey) { Hash = true };
            DBLogTable.UserLogTable = UserLog.DBTable;
            DBService.RowLoging += UserLog.OnDBItemLoging;

            GroupPermission.DBTable.Load();
            return GroupPermission.CachePermission();
        }
    }
}
