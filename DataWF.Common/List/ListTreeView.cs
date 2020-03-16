using System.Collections.Generic;

namespace DataWF.Common
{
    public class TreeListView<T> : SelectableListView<T> where T : IGroup
    {
        public TreeListView() : base()
        {
            GroupHelper.ApplyFilter<T>(this);
        }

        public TreeListView(IEnumerable<T> baseCollection) : this()
        {
            SetCollection(baseCollection);
        }
    }
}
