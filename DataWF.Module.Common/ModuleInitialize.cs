using DataWF.Common;
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

            User.DBTable.Load();
            User.SetCurrent();

            GroupPermission.DBTable.Load();
            GroupPermission.CachePermission();
        }
    }
}
