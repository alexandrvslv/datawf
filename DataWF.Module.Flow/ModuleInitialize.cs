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
        public Task Initialize(object[] args)
        {
            DBSchema schema = args[0] as DBSchema;
            var workTable = (WorkTable)schema.GetTable<Work>();
            workTable.DefaultComparer = new DBComparer<Work, string>(workTable.CodeKey) { Hash = true };
            workTable.Load();

            var stageTable = (StageTable<Stage>)schema.GetTable<Stage>();
            stageTable.Load();

            var stageparamTable = (StageParamTable<StageParam>)schema.GetTable<StageParam>();
            stageparamTable.DefaultComparer = new DBComparer<StageParam, int>(stageparamTable.IdKey) { Hash = true };
            stageparamTable.Load();

            var templateTable = (TemplateTable<Template>)schema.GetTable<Template>();
            templateTable.DefaultComparer = new DBComparer<Template, string>(templateTable.CodeKey) { Hash = true };
            templateTable.Load();

            var templateFile = (TemplateFileTable<TemplateFile>)schema.GetTable<TemplateFile>();
            templateFile.DefaultComparer = new DBComparer<TemplateFile, int>(templateFile.IdKey) { Hash = true };
            templateFile.Load();

            return null;
        }
    }
}
