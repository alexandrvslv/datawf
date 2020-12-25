using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using System.Linq;
using System.Threading.Tasks;

[assembly: ModuleInitialize(typeof(ModuleInitialize))]
namespace DataWF.Module.Common
{
    public class ModuleInitialize : IModuleInitialize
    {
        public Task Initialize(object[] args)
        {
            var schema = args.FirstOrDefault() as DBSchema;

            schema.GetTable<Book>().Load();

            schema.GetTable<Department>().Load();
            schema.GetTable<Position>().Load();

            var userGoup = (UserGroupTable)schema.GetTable<UserGroup>();
            userGoup.Load();
            userGoup.SetCurrent();

            User.DBTable.DefaultComparer = new DBComparer<User, string>(User.DBTable.CodeKey) { Hash = true };
            User.DBTable.Load();

            UserReg.DBTable.DefaultComparer = new DBComparer<UserReg, long?>(UserReg.DBTable.PrimaryKey) { Hash = true };
            DBLogItem.UserLogTable = UserReg.DBTable;
            DBService.AddItemLoging(UserReg.OnDBItemLoging);

            GroupPermission.DBTable.Load();
            return GroupPermission.CachePermission();
        }
    }
}
