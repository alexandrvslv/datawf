using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class LocaleCategory : SelectableList<LocaleItem>, ICloneable, IContainerNotifyPropertyChanged
    {
        static readonly Invoker<LocaleItem, string> nameInvoker = new Invoker<LocaleItem, string>(nameof(LocaleItem.Name), item => item.Name);
        private string name = "";

        public LocaleCategory()
        {
            Indexes.Add(nameInvoker);
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
    }

}

