using System.Collections.Generic;

namespace DataWF.Gui
{
    public class LayoutFieldInfoCache
    {
        Dictionary<string, LayoutFieldInfo> items = new Dictionary<string, LayoutFieldInfo>();

        public Dictionary<string, LayoutFieldInfo> Items
        {
            get { return items; }
        }

        public void Remove(string key)
        {
            items.Remove(key);
        }

        public void Remove(LayoutFieldInfo info)
        {
            foreach (var item in items)
                if (item.Value == info)
                {
                    items.Remove(item.Key);
                    return;
                }
        }

        public LayoutFieldInfo this[string key]
        {
            get { return items.TryGetValue(key, out var temp) ? temp : null; }
            set { items[key] = value; }
        }
    }
}

