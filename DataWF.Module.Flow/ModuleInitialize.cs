using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Flow;
using System.Linq;
using System.Threading.Tasks;

[assembly: ModuleInitialize(typeof(ModuleInitialize))]
namespace DataWF.Module.Flow
{
    public class ModuleInitialize : IModuleInitialize
    {
        public Task Initialize()
        {
            Work.DBTable.DefaultComparer = new DBComparer<Work, string>(Work.DBTable.CodeKey) { Hash = true };
            Work.DBTable.Load();

            Stage.DBTable.Load();

            StageParam.DBTable.DefaultComparer = new DBComparer<StageParam, int?>(StageParam.DBTable.PrimaryKey) { Hash = true };
            StageParam.DBTable.Load();

            Template.DBTable.DefaultComparer = new DBComparer<Template, string>(Template.DBTable.CodeKey) { Hash = true };
            Template.DBTable.Load();

            TemplateData.DBTable.DefaultComparer = new DBComparer<TemplateData, int?>(TemplateData.DBTable.PrimaryKey) { Hash = true };
            TemplateData.DBTable.Load();

            return null;
        }
    }
}
