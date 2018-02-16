using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    /// <summary>
    /// Localize item list.
    /// </summary>
    public class LocaleItemList : SelectableList<LocaleItem>
    {
        //indexes for fast retrieve name information
        //first dictionary contains category keys
        //second inline dictionary contains name keys
        [NonSerialized]
        private Dictionary<string, Dictionary<string, LocaleItem>> index = new Dictionary<string, Dictionary<string, LocaleItem>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.LocalizeItemList"/> class.
        /// </summary>
        public LocaleItemList()
        {
        }

        /// <summary>
        /// Add the specified item. And create index.
        /// </summary>
        /// <param name='item'>
        /// Item.
        /// </param>
        public override void Add(LocaleItem item)
        {
            if (Contains(item.Category, item.Name))
            {
                var oitem = GetByIndex(item.Category, item.Name);
                if (oitem != null && oitem != item)
                {
                    oitem.Merge(item);
                }
            }
            else
            {
                base.Add(item);
                AddIndex(item);
            }
        }

        /// <summary>
        /// Remove the specified item.
        /// </summary>
        /// <param name='item'>
        /// If set to <c>true</c> item.
        /// </param>
        public override bool Remove(LocaleItem item)
        {
            bool flag = base.Remove(item);
            RemoveIndex(item);
            return flag;
        }


        /// <summary>
        /// Adds the index.
        /// </summary>
        /// <param name='item'>
        /// Value.
        /// </param>
        public void AddIndex(LocaleItem item)
        {
            AddIndex(item.Category, item.Name, item);
        }

        /// <summary>
        /// Adds the index.
        /// </summary>
        /// <param name='category'>
        /// Category.
        /// </param>
        /// <param name='name'>
        /// Name.
        /// </param>
        /// <param name='value'>
        /// Value.
        /// </param>
        public void AddIndex(string category, string name, LocaleItem value)
        {
            Dictionary<string, LocaleItem> cat;

            if (!index.TryGetValue(category, out cat))
            {
                cat = new Dictionary<string, LocaleItem>();
                index.Add(category, cat);
            }
            cat[name] = value;
        }

        /// <summary>
        /// Removes the index.
        /// </summary>
        /// <param name='item'>
        /// Item.
        /// </param>
        public void RemoveIndex(LocaleItem item)
        {
            Dictionary<string, LocaleItem> cat;

            if (index.TryGetValue(item.Category, out cat))
            {
                if (cat.Remove(item.Name) && cat.Count == 0)
                    index.Remove(item.Category);
            }
        }

        /// <summary>
        /// Check Contains
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Contains(string category, string name)
        {
            Dictionary<string, LocaleItem> cat;

            if (!index.TryGetValue(category, out cat))
                return false;

            return cat.ContainsKey(name);
        }

        /// <summary>
        /// Retreive item by category and index
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public LocaleItem GetByIndex(string category, string name)
        {
            Dictionary<string, LocaleItem> cat;
            if (!index.TryGetValue(category, out cat))
            {
                cat = new Dictionary<string, LocaleItem>(StringComparer.InvariantCultureIgnoreCase);
                index.Add(category, cat);
            }

            LocaleItem item;
            if (!cat.TryGetValue(name, out item))
            {
                item = new LocaleItem(category, name);
                Add(item);
            }
            return item;
        }
    }
}

