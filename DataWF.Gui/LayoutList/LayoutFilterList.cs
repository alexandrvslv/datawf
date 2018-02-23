using System.Collections.Generic;
using DataWF.Common;

namespace DataWF.Gui
{
    public class LayoutFilterList : SelectableList<LayoutFilter>
    {
        public LayoutFilterList()
        {
            Indexes.Add(new Invoker<LayoutFilter, string>(nameof(LayoutFilter.Name), (item) => item.Name));
        }

        public LayoutFilterList(LayoutList list) : this()
        {
            List = list;
        }

        public LayoutList List { get; set; }

        public LayoutFilter GetByName(string name)
        {
            return SelectOne(nameof(LayoutFilter.Name), name);
        }

        internal IEnumerable<QueryParameter> GetParameters()
        {
            foreach (var filter in this)
            {
                var param = filter.GetParameter();
                if (param != null)
                    yield return param;
            }
        }
    }

}
