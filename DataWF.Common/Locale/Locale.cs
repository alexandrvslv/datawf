using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;

[assembly: Invoker(typeof(Locale), nameof(Locale.Version), typeof(Locale.VersionInvoker))]
[assembly: Invoker(typeof(Locale), nameof(Locale.Culture), typeof(Locale.CultureInvoker))]
[assembly: Invoker(typeof(Locale), nameof(Locale.Cultures), typeof(Locale.CulturesInvoker))]
[assembly: Invoker(typeof(Locale), nameof(Locale.Images), typeof(Locale.ImagesInvoker))]
namespace DataWF.Common
{
    public class Locale : SelectableList<LocaleCategory>
    {
        public static Locale Instance = new Locale();

        private CultureInfo culture = CultureInfo.GetCultureInfo("en-US");

        public Locale()
        {
            Indexes.Add(LocaleCategory.NameInvoker.Instance.Name,
                new ListIndex<LocaleCategory, string>(LocaleCategory.NameInvoker.Instance, ListIndexFabric.GetNullKey<string>(), StringComparer.Ordinal));
            Cultures.Add(CultureInfo.GetCultureInfo("ru-RU"));
            Cultures.Add(CultureInfo.GetCultureInfo("en-US"));
            //try { Cultures.Add(CultureInfo.GetCultureInfo("kk-KZ")); }
            //catch (Exception ex) { Helper.OnException(ex); }
        }

        public CultureInfo Culture
        {
            get { return culture; }
            set
            {
                culture = value;
                if (culture != null && !Cultures.Contains(culture))
                {
                    Cultures.Add(culture);
                }
            }
        }

        public List<CultureInfo> Cultures { get; set; } = new List<CultureInfo>();

        public LocaleImageList Images { get; set; } = new LocaleImageList();

        public string Version { get; set; } = "1.0";

        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, XmlIgnore]
        public static bool IsChanged { get; internal set; }

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
                IsChanged = true;
            }
            return item;
        }

        public LocaleItem GetItem(string category, string name)
        {
            var categoryItem = GetByName(category);
            var nameItem = categoryItem.GetByName(name);
            _ = nameItem.Value;
            return nameItem;
        }

        public static string FilePath = Path.Combine(Helper.GetDirectory(), "localize.xml");

        public static void Load()
        {
            Load(FilePath);
        }

        public static void Load(string filePath)
        {
            if (!File.Exists(filePath))
                return;
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Load(fileStream);
            }
        }

        public static void Load(Stream stream)
        {
            Serialization.Deserialize(stream, Instance);
        }

        public static void Save()
        {
            if (IsChanged || !File.Exists(FilePath))
            {
                Save(FilePath);
            }
        }

        public static void Save(string filePath)
        {
            Serialization.Serialize(Instance, filePath);
            IsChanged = false;
        }

        public static string GetTypeCategory(Type type)
        {
            return $"{type.Namespace}.{type.Name}";
        }

        public static string Get(Type type)
        {
            return Get(GetTypeCategory(type), type.Name);
        }

        public static string Get(Type type, string name, string separator)
        {
            var builder = new StringBuilder();
            foreach (var info in TypeHelper.GetMemberInfoList(type, name))
            {
                builder.Append(Get(GetTypeCategory(type), info.Info.Name));
                builder.Append(separator);
            }
            builder.Length -= separator.Length;
            return builder.ToString();
        }

        public static string Get(Type type, string name)
        {
            return Get(GetTypeCategory(type), name);
        }

        public static string Get(string category, string name)
        {
            return Instance.GetItem(category, name).Value;
        }

        public static string Get(object obj)
        {
            return Get(GetTypeCategory(obj.GetType()), obj.ToString());
        }

        public static LocaleItem GetItem(Type category, string name)
        {
            return Instance.GetByName(GetTypeCategory(category)).GetByName(name);
        }
        public static GlyphType GetGlyph(Type category, string name, GlyphType def = GlyphType.None)
        {
            return GetGlyph(GetTypeCategory(category), name, def);
        }

        public static GlyphType GetGlyph(string category, string name, GlyphType def = GlyphType.None)
        {
            var item = Instance.GetItem(category, name);
            if (item.Glyph == GlyphType.None && def != GlyphType.None)
                item.Glyph = def;
            return item.Glyph;
        }

        public static string GetImageKey(string category, string name)
        {
            return Instance.GetItem(category, name).ImageKey;
        }

        public static object GetImage(string key)
        {
            return Instance.Images.GetByKey(key)?.Cache;
        }

        public static object GetImage(string category, string name)
        {
            return GetImage(GetImageKey(category, name));
        }

        public class VersionInvoker : Invoker<Locale, string>
        {
            public override string Name => nameof(Locale.Version);

            public override bool CanWrite => true;

            public override string GetValue(Locale target) => target.Version;

            public override void SetValue(Locale target, string value) => target.Version = value;
        }

        public class CultureInvoker : Invoker<Locale, CultureInfo>
        {
            public override string Name => nameof(Locale.Culture);

            public override bool CanWrite => true;

            public override CultureInfo GetValue(Locale target) => target.Culture;

            public override void SetValue(Locale target, CultureInfo value) => target.Culture = value;
        }

        public class CulturesInvoker : Invoker<Locale, List<CultureInfo>>
        {
            public override string Name => nameof(Locale.Cultures);

            public override bool CanWrite => true;

            public override List<CultureInfo> GetValue(Locale target) => target.Cultures;

            public override void SetValue(Locale target, List<CultureInfo> value) => target.Cultures = value;
        }

        public class ImagesInvoker : Invoker<Locale, LocaleImageList>
        {
            public override string Name => nameof(Locale.Images);

            public override bool CanWrite => true;

            public override LocaleImageList GetValue(Locale target) => target.Images;

            public override void SetValue(Locale target, LocaleImageList value) => target.Images = value;
        }
    }
}

