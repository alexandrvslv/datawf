using DataWF.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Gui
{
    public class LayoutListInfoCache
    {
        Dictionary<string, LayoutListInfo> items = new Dictionary<string, LayoutListInfo>();

        public Dictionary<string, LayoutListInfo> Items
        {
            get { return items; }
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
                LayoutListInfo info = null;
                items.TryGetValue(key, out info);
                return info;
            }
            set
            {
                items[key] = value;
            }
        }
    }
}

