using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace DataWF.Common
{
    public class Locale : SelectableList<LocaleCategory>
    {
        private CultureInfo culture = CultureInfo.GetCultureInfo("en-US");

        public Locale()
        {
            Indexes.Add(new Invoker<LocaleCategory, string>(nameof(LocaleCategory.Name), item => item.Name));
            Cultures.Add(CultureInfo.GetCultureInfo("ru-RU"));
            Cultures.Add(CultureInfo.GetCultureInfo("en-US"));
            try { Cultures.Add(CultureInfo.GetCultureInfo("kk-KZ")); }
            catch (Exception ex) { Helper.OnException(ex); }
        }

        public CultureInfo Culture
        {
            get { return culture; }
            set { culture = value; }
        }

        [Browsable(false)]
        public List<CultureInfo> Cultures { get; set; } = new List<CultureInfo>();

        public LocaleImageList Images { get; set; } = new LocaleImageList();

        public string Version { get; set; } = "1.0";

        public bool Contains(string name)
        {
            return SelectOne(nameof(LocaleCategory.Name), name) != null;
        }

        public LocaleCategory GetByName(string name)
        {
            var item = SelectOne(nameof(LocaleCategory.Name), name);
            if (item == null)
            {
                item = new LocaleCategory { Name = name };
                Add(item);
            }
            return item;
        }

        public static void Load()
        {
            Serialization.Deserialize(Path.Combine(Helper.GetDirectory(), "localize.xml"), Instance);
        }

        public static void Save()
        {
            Serialization.Serialize(Instance, Path.Combine(Helper.GetDirectory(), "localize.xml"));
        }

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

        public static string Get(string category, string name)
        {
            return GetItem(category, name).Value;
        }

        public static string Get(object obj)
        {
            return Get(obj.GetType().FullName, obj.ToString());
        }

        public static LocaleItem GetItem(string category, string name)
        {
            return Instance.GetByName(category).GetByName(name);
        }

        public static GlyphType GetGlyph(string category, string name, GlyphType def = GlyphType.None)
        {
            var item = GetItem(category, name);
            if (item.Glyph == GlyphType.None && def != GlyphType.None)
                item.Glyph = def;
            return item.Glyph;
        }

        public static string GetImageKey(string category, string name)
        {
            return GetItem(category, name).ImageKey;
        }

        public static object GetImage(string key)
        {
            return Instance.Images.GetByKey(key)?.Cache;
        }

        public static object GetImage(string category, string name)
        {
            return GetImage(GetImageKey(category, name));
        }

        public static Locale Instance = new Locale();
    }

}

