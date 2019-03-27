using DataWF.Common;
using DataWF.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    public class ModuleInitialize : IModuleInitialize
    {
        public Task Initialize()
        {
            Book.DBTable.Load().LastOrDefault();

            Department.DBTable.Load().LastOrDefault();
            Position.DBTable.Load().LastOrDefault();

            UserGroup.DBTable.Load().LastOrDefault();
            UserGroup.SetCurrent();

            User.DBTable.DefaultComparer = new DBComparer(User.DBTable.CodeKey) { Hash = true };
            User.DBTable.Load().LastOrDefault();

            UserLog.DBTable.DefaultComparer = new DBComparer(UserLog.DBTable.PrimaryKey) { Hash = true };
            DBLogTable.UserLogTable = UserLog.DBTable;
            DBService.RowLoging += UserLog.OnDBItemLoging;

            GroupPermission.DBTable.Load().LastOrDefault();
            return GroupPermission.CachePermission();
        }
    }
}
