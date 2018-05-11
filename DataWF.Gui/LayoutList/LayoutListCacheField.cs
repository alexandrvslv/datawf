using System;
using System.Collections.Generic;

namespace DataWF.Gui
{
    public class LayoutListCacheField : Dictionary<string, LayoutFieldInfo>
    {
        public LayoutListCacheField() : base(StringComparer.OrdinalIgnoreCase)
        { }

        public void Remove(LayoutFieldInfo info)
        {
            foreach (var item in this)
            {
                if (item.Value == info)
                {
                    Remove(item.Key);
                    return;
                }
            }
        }

        public new LayoutFieldInfo this[string key]
        {
            get { return TryGetValue(key, out var temp) ? temp : null; }
            set { base[key] = value; }
        }
    }
}

