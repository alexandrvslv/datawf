using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Common
{    
    public class LocaleCategory : SelectableList<LocaleItem>, ICloneable, IContainerNotifyPropertyChanged
    {
        private string name = "";

        public LocaleCategory()
        {
            Indexes.Add(LocaleItem.NameInvoker.Instance);
        }

        [ReadOnly(true), DefaultValue("")]
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

        [Invoker(typeof(LocaleCategory), nameof(LocaleCategory.Name))]
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

