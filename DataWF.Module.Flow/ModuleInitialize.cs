using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataWF.Module.Flow
{
    public class ModuleInitialize : IModuleInitialize
    {
        public void Initialize()
        {
            Work.DBTable.DefaultComparer = new DBComparer(Work.DBTable.CodeKey) { Hash = true };
            Work.DBTable.Load();

            Stage.DBTable.Load();

            StageParam.DBTable.DefaultComparer = new DBComparer(StageParam.DBTable.PrimaryKey) { Hash = true };
            StageParam.DBTable.Load();

            Template.DBTable.DefaultComparer = new DBComparer(Template.DBTable.CodeKey) { Hash = true };
            Template.DBTable.Load();

            TemplateData.DBTable.DefaultComparer = new DBComparer(TemplateData.DBTable.PrimaryKey) { Hash = true };
            TemplateData.DBTable.Load();
        }
    }
}
