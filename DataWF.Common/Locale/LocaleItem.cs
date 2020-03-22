using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class LocaleItem : SelectableList<LocaleString>, ICloneable, IEntryNotifyPropertyChanged
    {
        private GlyphType glyph = GlyphType.None;
        private string name = String.Empty;
        private string image;

        public LocaleItem()
        {
        }

        public LocaleItem(string name)
        {
            this.name = name;
        }

        public string ImageKey
        {
            get { return image; }
            set
            {
                if (image != value)
                {
                    image = value;
                    OnPropertyChanged(nameof(ImageKey));
                }
            }
        }

        [DefaultValue("")]
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [DefaultValue(GlyphType.None)]
        public GlyphType Glyph
        {
            get { return glyph; }
            set
            {
                if (glyph != value)
                {
                    glyph = value;
                    OnPropertyChanged(nameof(Glyph));
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public object Picture => Locale.GetImage(image);

        [JsonIgnore, Browsable(false)]
        public LocaleCategory Category => Containers.FirstOrDefault() as LocaleCategory;

        public LocaleString this[CultureInfo culture]
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

        public LocaleString this[string cultureName]
        {
            get { return this[CultureInfo.GetCultureInfo(cultureName)]; }
            set { this[CultureInfo.GetCultureInfo(cultureName)] = value; }
        }

        [XmlIgnore, JsonIgnore]
        public string Value
        {
            get
            {
                var item = this[Locale.Instance.Culture];
                if (item == null)
                {
                    item = new LocaleString(GetFirst()?.Value ?? Name, Locale.Instance.Culture);
                    Add(item);
                    Locale.IsChanged = true;
                }
                return item.Value;
            }
            set
            {
                Add(value, Locale.Instance.Culture);
                OnPropertyChanged(nameof(Value));
            }
        }

        [XmlIgnore, JsonIgnore]
        public string Description
        {
            get { return this[Locale.Instance.Culture]?.Description ?? name; }
            set
            {
                if (Description == value)
                    return;
                var item = this[Locale.Instance.Culture];
                if (item == null)
                {
                    item = new LocaleString(value, Locale.Instance.Culture);
                    Add(item);
                }
                item.Description = value;
                OnPropertyChanged(nameof(Description));
            }
        }
        public void Merge(LocaleItem item)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return name;
        }

        public LocaleString Add(string value, CultureInfo culture)
        {
            var item = this[culture];
            if (item == null)
            {
                item = new LocaleString(value, culture);
                Add(item);
            }
            item.Value = value;
            return item;
        }

        public LocaleString Add(string value, string cultureName)
        {
            return Add(value, CultureInfo.GetCultureInfo(cultureName));
        }

        public string GetAllValues(string separator)
        {
            string rez = string.Empty;
            foreach (var item in this)
                rez += item.Value + (!IsLast(item) ? separator : string.Empty);
            return rez;

        }

        public object Clone()
        {
            var litem = new LocaleItem(name);
            foreach (var item in this)
                litem.Add((LocaleString)item.Clone());
            return litem;
        }

        [Invoker(typeof(LocaleItem), nameof(LocaleItem.Name))]
        public class NameInvoker : Invoker<LocaleItem, string>
        {
            public static readonly NameInvoker Instance = new NameInvoker();
            public override string Name
            {
                get => nameof(LocaleItem.Name);
            }

            public override bool CanWrite => true;

            public override string GetValue(LocaleItem target) => target.Name;

            public override void SetValue(LocaleItem target, string value) => target.Name = value;
        }

        [Invoker(typeof(LocaleItem), nameof(LocaleItem.Glyph))]
        public class GlyphInvoker : Invoker<LocaleItem, GlyphType>
        {
            public static readonly GlyphInvoker Instance = new GlyphInvoker();
            public override string Name
            {
                get => nameof(LocaleItem.Glyph);
            }

            public override bool CanWrite => true;

            public override GlyphType GetValue(LocaleItem target) => target.Glyph;

            public override void SetValue(LocaleItem target, GlyphType value) => target.Glyph = value;
        }

        [Invoker(typeof(LocaleItem), nameof(LocaleItem.ImageKey))]
        public class ImageKeyInvoker : Invoker<LocaleItem, string>
        {
            public static readonly ImageKeyInvoker Instance = new ImageKeyInvoker();
            public override string Name
            {
                get => nameof(LocaleItem.ImageKey);
            }

            public override bool CanWrite => true;

            public override string GetValue(LocaleItem target) => target.ImageKey;

            public override void SetValue(LocaleItem target, string value) => target.ImageKey = value;
        }

        [Invoker(typeof(LocaleItem), nameof(LocaleItem.Value))]
        public class ValueInvoker : Invoker<LocaleItem, string>
        {
            public static readonly ValueInvoker Instance = new ValueInvoker();
            public override string Name
            {
                get => nameof(LocaleItem.Value);
            }

            public override bool CanWrite => true;

            public override string GetValue(LocaleItem target) => target.Value;

            public override void SetValue(LocaleItem target, string value) => target.Value = value;
        }
    }

}

