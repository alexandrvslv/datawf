using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using System.Threading.Tasks;

[assembly: ModuleInitialize(typeof(ModuleInitialize))]
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

            User.DBTable.DefaultComparer = new DBComparer<User, string>(User.DBTable.CodeKey) { Hash = true };
            User.DBTable.Load();

            UserReg.DBTable.DefaultComparer = new DBComparer<UserReg, long?>(UserReg.DBTable.PrimaryKey) { Hash = true };
            DBLogItem.UserLogTable = UserReg.DBTable;
            DBService.AddRowLoging(UserReg.OnDBItemLoging);

            GroupPermission.DBTable.Load();
            return GroupPermission.CachePermission();
        }
    }
}
