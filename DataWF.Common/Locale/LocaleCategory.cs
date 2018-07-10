﻿using System;
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
            }
            return item;
        }

        [XmlIgnore, Browsable(false)]
        public INotifyListPropertyChanged Container { get; set; }

        protected override void OnPropertyChanged(string property)
        {
            base.OnPropertyChanged(property);
            Container?.OnPropertyChanged(this, new PropertyChangedEventArgs(property));
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

