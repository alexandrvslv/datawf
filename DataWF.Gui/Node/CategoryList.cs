using DataWF.Common;

namespace DataWF.Gui
{
    public class CategoryList : SelectableList<Category>
    {
        static readonly Invoker<Category, string> nameInvoker = new ActionInvoker<Category, string>(nameof(Category.Name), (item) => item.Name);

        public CategoryList()
            : base()
        {
            Indexes.Add(nameInvoker);
        }

        public Category this[string name]
        {
            get { return Find(name); }
        }

        public Category Find(string name)
        {
            return SelectOne(nameof(Category.Name), CompareType.Equal, name);
        }

        public override int Add(Category item)
        {
            item.Order = items.Count;
            return base.Add(item);
        }
    }
}

