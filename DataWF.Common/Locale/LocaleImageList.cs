using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataWF.Common
{
    /// <summary>
    /// Images for localization
    /// </summary>
    public class LocaleImageList : SelectableList<LocaleImage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.LocaleImageList"/> class.
        /// </summary>
        public LocaleImageList()
        {
            Indexes.Add(LocaleImage.KeyInvoker.Instance.Name,
                new ListIndex<LocaleImage, string>(LocaleImage.KeyInvoker.Instance, ListIndexFabric.GetNullKey<string>(), StringComparer.Ordinal));
        }

        public bool Contains(string name)
        {
            return GetByKey(name) != null;
        }

        public LocaleImage GetByKey(string key)
        {
            if (key == null)
                return null;
            return SelectOne(nameof(LocaleImage.Key), key);
        }
    }
}
