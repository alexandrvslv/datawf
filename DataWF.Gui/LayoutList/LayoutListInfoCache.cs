using DataWF.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Gui
{
    public class LayoutListInfoCache
    {
        Dictionary<string, LayoutListInfo> items = new Dictionary<string, LayoutListInfo>(StringComparer.Ordinal);

        public Dictionary<string, LayoutListInfo> Items
        {
            get { return items; }
            set { items = value; }
        }

        public void Remove(string key)
        {
            items.Remove(key);
        }

        public void Remove(LayoutListInfo map)
        {
            foreach (var kvp in items)
                if (kvp.Value == map)
                {
                    items.Remove(kvp.Key);
                    return;
                }
        }

        public LayoutListInfo this[string key]
        {
            get
            {
                items.TryGetValue(key, out LayoutListInfo info);
                return info;
            }
            set
            {
                items[key] = value;
            }
        }
    }
}

