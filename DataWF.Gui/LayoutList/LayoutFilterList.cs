using DataWF.Common;

namespace DataWF.Gui
{
    public class LayoutFilterList : SelectableList<LayoutFilter>
    {
        static readonly Invoker<LayoutFilter, string> nameInvoker = new ActionInvoker<LayoutFilter, string>(nameof(LayoutFilter.Name), (item) => item.Name);

        public LayoutFilterList()
        {
            Indexes.Add(nameInvoker);
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
    }

}
