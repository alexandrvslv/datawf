using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[assembly: ModuleInitialize(typeof(DataWF.Module.Common.ModuleInitialize))]
namespace DataWF.Module.Common
{
    public class ModuleInitialize : IModuleInitialize
    {
        public Task Initialize()
        {
            if (AccessValue.Provider is AccessProviderStub)
            {
                AccessValue.Provider = new CommonAccessProvider(Book.DBTable.Schema);
            }
            Book.DBTable.Load();

            Department.DBTable.Load();
            Position.DBTable.Load();

            UserGroup.DBTable.Load();

            User.DBTable.DefaultComparer = new DBComparer<User, string>(User.DBTable.CodeKey) { Hash = true };
            User.DBTable.Load();

            UserReg.DBTable.DefaultComparer = new DBComparer<UserReg, long?>(UserReg.DBTable.PrimaryKey) { Hash = true };
            DBLogItem.UserLogTable = UserReg.DBTable;
            DBService.RowLoging += UserReg.OnDBItemLoging;

            GroupPermission.DBTable.Load();
            return GroupPermission.CachePermission();
        }
    }
}
