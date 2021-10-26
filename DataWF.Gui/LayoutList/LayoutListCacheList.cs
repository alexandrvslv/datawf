using DataWF.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Gui
{
    public class LayoutListCacheList : Dictionary<string, LayoutListInfo>
    {
        public LayoutListCacheList() : base(StringComparer.Ordinal)
        { }

        public void Remove(LayoutListInfo map)
        {
            foreach (var kvp in this)
                if (kvp.Value == map)
                {
                    Remove(kvp.Key);
                    return;
                }
        }

        public new LayoutListInfo this[string key]
        {
            get
            {
                TryGetValue(key, out LayoutListInfo info);
                return info;
            }
            set
            {
                base[key] = value;
            }
        }
    }
}

