using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace DataWF.Common
{
    /// <summary>
    /// Localize.
    /// </summary>/
    public class Locale
    {
        private string version = "1.0";
        private CultureInfo culture = CultureInfo.GetCultureInfo("ru-RU");
        private List<CultureInfo> cultures = new List<CultureInfo>();
        private LocaleItemList names = new LocaleItemList();
        private LImageList images = new LImageList();
        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.Localize"/> class.
        /// </summary>
        public Locale()
        {
            cultures.Add(CultureInfo.GetCultureInfo("ru-RU"));
            cultures.Add(CultureInfo.GetCultureInfo("en-US"));
            try { cultures.Add(CultureInfo.GetCultureInfo("kk-KZ")); }
            catch (Exception ex) { Helper.OnException(ex); }
        }

        /// <summary>
        /// Gets or sets the current culture.
        /// </summary>
        /// <value>
        /// The culture.
        /// </value>
        public CultureInfo Culture
        {
            get { return this.culture; }
            set { culture = value; }
        }

        /// <summary>
        /// Gets or sets the list of avilible cultures
        /// </summary>
        /// <value>
        /// The cultures.
        /// </value>
        [Browsable(false)]
        public List<CultureInfo> Cultures
        {
            get { return this.cultures; }
            set { cultures = value; }
        }

        /// <summary>
        /// Gets or sets the LImageList list.
        /// </summary>
        /// <value>
        /// The images.
        /// </value>
        public LImageList Images
        {
            get { return this.images; }
            set { images = value; }
        }

        /// <summary>
        /// Gets or sets the LocalizeItem list.
        /// </summary>
        /// <value>
        /// The names.
        /// </value>
        public LocaleItemList Names
        {
            get { return this.names; }
            set { names = value; }
        }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public string Version
        {
            get { return this.version; }
            set { version = value; }
        }

        /// <summary>
        /// Load this instance.
        /// </summary>
        public static void Load()
        {
            Serialization.Deserialize(Path.Combine(Helper.GetDirectory(), "localize.xml"), Data);
        }

        /// <summary>
        /// Save this instance.
        /// </summary>
        public static void Save()
        {
            Serialization.Serialize(Data, Path.Combine(Helper.GetDirectory(), "localize.xml"));
        }

        /// <summary>
        /// Get Localized string by the specified type, name and separator.
        /// </summary>
        /// <returns>The get.</returns>
        /// <param name="type">Type.</param>
        /// <param name="name">Name.</param>
        /// <param name="separator">Separator.</param>
        public static string Get(Type type, string name, string separator = " ")
        {
            var builder = new StringBuilder();
            foreach (var info in TypeHelper.GetMemberInfoList(type, name))
            {
                builder.Append(Get(info.DeclaringType.FullName, info.Name));
                builder.Append(separator);
            }
            builder.Length -= separator.Length;
            return builder.ToString();
        }

        /// <summary>
        /// Gets Localized string by specific category and name.
        /// </summary>
        /// <returns>
        /// The name.
        /// </returns>
        /// <param name='category'>
        /// Category.
        /// </param>
        /// <param name='name'>
        /// Name.
        /// </param>
        public static string Get(string category, string name)
        {
            return Data.names.GetByIndex(category, name).CurrentName;
        }

        public static string Get(object obj)
        {
            return Get(obj.GetType().FullName, obj.ToString());
        }

        public static GlyphType GetGlyph(string category, string name, GlyphType def = GlyphType.None)
        {
            var item = Data.names.GetByIndex(category, name);
            if (item.Glyph == GlyphType.None && def != GlyphType.None)
                item.Glyph = def;
            return item.Glyph;
        }

        public static string GetImageKey(string category, string name)
        {
            return Data.names.GetByIndex(category, name).ImageKey;
        }

        public static object GetImage(string key)
        {
            if (key == null)
                return null;
            LImage image = Data.images.GetByIndex(key);
            return image == null ? null : image.Cache;
        }

        public static Object GetImage(string category, string name)
        {
            return GetImage(GetImageKey(category, name));
        }

        /// <summary>
        /// The data of localize.
        /// </summary>
        public static Locale Data = new Locale();
    }

}

