
using DataWF.Data;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Module.Flow
{
    public class DocumentCheckStageCompleate : IExecutable
    {
        public Task<object> Execute(ExecuteArgs arg)
        {
            var darg = (DocumentExecuteArgs)arg;
            var document = (Document)arg.Document;
            var builder = new StringBuilder();

            foreach (var work in document.Works)
            {
                if (work == darg.Work)
                    continue;
                if (!work.Completed && work.Stage == darg.Stage)
                {
                    if (builder.Length == 0)
                    {
                        builder.AppendLine($"Stage {darg.Stage} is uncompleate! Current holders: ");
                    }
                    builder.AppendLine($"    {work.Department} - {work.Position} {work.User};");
                }
            }
            return Task.FromResult<object>(builder.Length == 0 ? null : builder.ToString());
        }
    }
}
