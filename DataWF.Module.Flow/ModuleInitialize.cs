using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataWF.Module.Flow
{
    public class ModuleInitialize : IModuleInitialize
    {
        public void Initialize()
        {
            Work.DBTable.Load();
            Stage.DBTable.Load();
            StageParam.DBTable.Load();
            Template.DBTable.Load();
            TemplateParam.DBTable.Load();
        }
    }
}
