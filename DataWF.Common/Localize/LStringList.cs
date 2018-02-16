using System;
using System.Globalization;
using System.Collections.Generic;

namespace DataWF.Common
{
    /// <summary>
    /// Culture string list.
    /// </summary>
    public class LStringList : SelectableList<LString>, ICloneable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.LStringList"/> class.
        /// </summary>
        public LStringList()
        {
        }

        /// <summary>
        /// Add the new CString by specified value and culture.
        /// </summary>
        /// <param name='value'>
        /// Value.
        /// </param>
        /// <param name='culture'>
        /// Culture.
        /// </param>
        public LString Add(string value, CultureInfo culture)
        {
            var item = this[culture];
            if (item == null)
            {
                item = new LString(value, culture);
                Add(item);
            }
            item.Value = value;
            return item;
        }

        /// <summary>
        /// Add the new CString by specified value and cultureName.
        /// </summary>
        /// <param name='value'>
        /// Value.
        /// </param>
        /// <param name='cultureName'>
        /// Culture name (use CultureInfo.Name).
        /// </param>
        public LString Add(string value, string cultureName)
        {
            return Add(value, CultureInfo.GetCultureInfo(cultureName));
        }

        /// <summary>
        /// Gets or sets the CString.Value with the specified culture.
        /// </summary>
        /// <param name='culture'>
        /// Culture info object
        /// </param>
        public LString this[CultureInfo culture]
        {
            get
            {
                foreach (var item in items)
                    if (culture.ThreeLetterISOLanguageName.Equals(item.Culture.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
                        return item;
                return null;
            }
            set
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (culture.ThreeLetterISOLanguageName.Equals(items[i].Culture.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
                    {
                        this.RemoveAt(i);
                        break;
                    }
                }
                if (value != null)
                    Add(value);
            }
        }

        /// <summary>
        /// Gets or sets the CString.Value with the specified culture name.
        /// </summary>
        /// <param name='cultureName'>
        /// Culture name (use CultureInfo.Name).
        /// </param>
        public LString this[string cultureName]
        {
            get { return this[CultureInfo.GetCultureInfo(cultureName)]; }
            set { this[CultureInfo.GetCultureInfo(cultureName)] = value; }
        }

        /// <summary>
        /// Gets or sets the current CString.Value with the CultureInfo.CurrentCulture.
        /// </summary>
        /// <value>
        /// The current string value
        /// </value>
        public string Value
        {
            get
            {
                var item = this[Locale.Data.Culture];
                return item == null ? (items.Count > 0 ? items[0].Value : null) : item.Value;
            }
            set { Add(value, Locale.Data.Culture); }
        }

        public string Descript
        {
            get
            {
                var item = this[Locale.Data.Culture];
                return item?.Description;
            }
            set
            {
                var item = this[Locale.Data.Culture];
                if (item == null)
                {
                    item = new LString(value, Locale.Data.Culture);
                    Add(item);
                }
                item.Description = value;
            }
        }

        /// <summary>
        /// Gets all values.
        /// </summary>
        /// <returns>
        /// The all values.
        /// </returns>
        /// <param name='separator'>
        /// Separator.
        /// </param>
        public string GetAllValues(string separator)
        {
            string rez = string.Empty;
            foreach (var item in items)
                rez += item.Value + (!IsLast(item) ? separator : string.Empty);
            return rez;

        }

        public override string ToString()
        {
            return GetAllValues("; ");
        }

        #region ICloneable implementation
        public object Clone()
        {
            var list = new LStringList();
            foreach (LString item in items)
                list.Add((LString)item.Clone());
            return list;
        }

        #endregion
    }
}

