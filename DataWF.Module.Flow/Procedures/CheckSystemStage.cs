using DataWF.Common;
using DataWF.Data;
using System.Threading.Tasks;

namespace DataWF.Module.Flow
{
    public class CheckSystemStage : IExecutable
    {
        public async Task<object> Execute(ExecuteArgs parameters)
        {
            var workTable = (DocumentWorkTable)parameters.Schema.GetTable<DocumentWork>();
            var filter = workTable.Query(DBLoadParam.Load)
                .Where(workTable.IsCompleteKey, CompareType.Equal, false)
                .And(workTable.IsSystemKey, CompareType.Equal, true);
            //string filter = string.Format("{0}!='{1}' and {2} in (select {3} from {4} where {5} = '{6}')",
            foreach (DocumentWork work in workTable.Load(filter, parameters.Transaction))
            {
                if (work.Stage == null || work.Document == null)
                    continue;
                work.Document.ExecuteProceduresByWork(work, StageParamProcudureType.Manual, parameters.Transaction);
                await work.Document.Save(parameters.Transaction);
            }
            return null;
        }

    }
}
