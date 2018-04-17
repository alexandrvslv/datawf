using DataWF.Common;

namespace DataWF.Gui
{
    public class CategoryList : SelectableList<Category>
    {
        static readonly Invoker<Category, string> nameInvoker = new Invoker<Category, string>(nameof(Category.Name), (item) => item.Name);

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

        public override void Add(Category item)
        {
            item.Order = items.Count;
            base.Add(item);
        }
    }
}

