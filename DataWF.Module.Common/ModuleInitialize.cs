using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataWF.Module.Common
{
    public class ModuleInitialize : IModuleInitialize
    {
        public void Initialize()
        {
            BookType.DBTable.Load();
            Book.DBTable.Load();

            Department.DBTable.Load();
            Position.DBTable.Load();

            UserGroup.DBTable.Load();
            UserGroup.SetCurrent();

            User.DBTable.DefaultComparer = new DBComparer(User.DBTable.CodeKey) { Hash = true };
            User.DBTable.Load();
            User.SetCurrent();

            UserLog.DBTable.DefaultComparer = new DBComparer(UserLog.DBTable.PrimaryKey) { Hash = true };

            GroupPermission.DBTable.Load();
            GroupPermission.CachePermission();
        }
    }
}
