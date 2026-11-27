using DataWF.Collections;
using DataWF.Collections.Query;
using DataWF.Invokers;

namespace DataWF
{
    public class ProjectHandleList : SelectableList<ProjectHandler>
    {
        public ProjectHandleList()
        {
            Indexes.AddIndex(new ActionInvoker<ProjectHandler, string>(nameof(ProjectHandler.FileName),
                                                                  (item) => item.FileName));
        }

        public ProjectHandler GetByFileName(string fileName)
        {
            return SelectOne(ProjectHandler.FileNameInvoker.Instance, CompareOperator.Equal, fileName);
        }
    }
}

