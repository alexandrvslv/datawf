using System.Collections.Generic;

namespace DataWF.Common
{
    public class ListTreeView<T> : SelectableListView<T> where T : IGroup
    {
        QueryParameter<T> groupParam;

        public ListTreeView()
        {
            groupParam = GroupHelper.CreateTreeFilter<T>();
            query.Parameters.Add(groupParam);
        }

        public ListTreeView(IList<T> baseCollection) : this()
        {
            SetCollection(baseCollection);
        }
    }
}
