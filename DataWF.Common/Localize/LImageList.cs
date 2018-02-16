using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataWF.Common
{
    /// <summary>
    /// Images for localization
    /// </summary>
    public class LImageList : SelectableList<LImage>
    {
        //indexes for fast retrieve name information
        //second inline dictionary contains name keys
        [NonSerialized]
        private Dictionary<string, LImage> index = new Dictionary<string, LImage>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.LImageList"/> class.
        /// </summary>
        public LImageList()
        {
        }

        /// <summary>
        /// Add the specified item.
        /// </summary>
        /// <returns>The add.</returns>
        /// <param name="item">Item.</param>
        public override void Add(LImage item)
        {
            if (Contains(item.Key))
                return;
            base.Add(item);
            index[item.Key] = item;
        }

        /// <summary>
        /// Remove the specified item.
        /// </summary>
        /// <param name='item'>
        /// If set to <c>true</c> item.
        /// </param>
        public override bool Remove(LImage item)
        {
            bool flag = base.Remove(item);
            if (Contains(item.Key))
                index.Remove(item.Key);
            return flag;
        }

        public bool Contains(string name)
        {
            return index.ContainsKey(name);
        }

        public LImage GetByIndex(string name)
        {
            LImage temp = null;
            if (name != null)
                index.TryGetValue(name, out temp);
            return temp;
        }

    }
}
