using System.Collections.Generic;

namespace DataWF.Common
{
    public class TreeListView<T> : SelectableListView<T> where T : IGroup
    {
        QueryParameter<T> groupParam;

        public TreeListView() : base()
        {
            groupParam = GroupHelper.CreateTreeFilter<T>();
            FilterQuery.Parameters.Add(groupParam);
            ApplySort((IComparer<T>)new TreeComparer<T>());
        }

        public TreeListView(IEnumerable<T> baseCollection) : this()
        {
            SetCollection(baseCollection);
        }
    }
}
