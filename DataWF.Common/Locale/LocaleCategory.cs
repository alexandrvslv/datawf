using DataWF.Common;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

[assembly: Invoker(typeof(LocaleCategory), nameof(LocaleCategory.Name), typeof(LocaleCategory.NameInvoker))]
namespace DataWF.Common
{
    public class LocaleCategory : SelectableList<LocaleItem>, ICloneable, IEntryNotifyPropertyChanged
    {
        private string name = "";

        public LocaleCategory()
        {
            Indexes.Add(LocaleItem.NameInvoker.Instance.Name,
                new ListIndex<LocaleItem, string>(LocaleItem.NameInvoker.Instance, ListIndexFactory.GetNullKey<string>(), StringComparer.Ordinal));
        }

        [DefaultValue("")]
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public bool Contains(string name)
        {
            return SelectOne(nameof(LocaleItem.Name), name) != null;
        }

        public LocaleItem GetByName(string name)
        {
            var item = SelectOne(nameof(LocaleItem.Name), name);
            if (item == null)
            {
                item = new LocaleItem(name);
                Add(item);
                Locale.IsChanged = true;
            }
            return item;
        }

        public override string ToString()
        {
            return Name;
        }

        public object Clone()
        {
            var item = new LocaleCategory() { Name = name };
            item.AddRange(items);
            return item;
        }

        public class NameInvoker : Invoker<LocaleCategory, string>
        {
            public static readonly NameInvoker Instance = new NameInvoker();
            public override string Name
            {
                get => nameof(LocaleCategory.Name);
            }

            public override bool CanWrite => true;

            public override string GetValue(LocaleCategory target) => target.Name;

            public override void SetValue(LocaleCategory target, string value) => target.Name = value;
        }
    }

}

