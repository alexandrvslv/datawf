using DataWF.Common;
using DataWF.Data;
using System.Linq;

namespace DataWF.Module.Flow
{
    public class ModuleInitialize : IModuleInitialize
    {
        public void Initialize()
        {
            Work.DBTable.DefaultComparer = new DBComparer(Work.DBTable.CodeKey) { Hash = true };
            Work.DBTable.Load().LastOrDefault();

            Stage.DBTable.Load().LastOrDefault();

            StageParam.DBTable.DefaultComparer = new DBComparer(StageParam.DBTable.PrimaryKey) { Hash = true };
            StageParam.DBTable.Load().LastOrDefault();

            Template.DBTable.DefaultComparer = new DBComparer(Template.DBTable.CodeKey) { Hash = true };
            Template.DBTable.Load().LastOrDefault();

            TemplateData.DBTable.DefaultComparer = new DBComparer(TemplateData.DBTable.PrimaryKey) { Hash = true };
            TemplateData.DBTable.Load().LastOrDefault();
        }
    }
}
