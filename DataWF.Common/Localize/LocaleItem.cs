using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Common
{
    /// <summary>
    /// Localize item.
    /// </summary>
    public class LocaleItem : ICloneable, INotifyPropertyChanged
    {
        private GlyphType glyph = GlyphType.None;
        //category of name (group for logical separator and performance tag)
        [DefaultValue("")]
        private string category = "";
        [DefaultValue("")]
        private string name = "";
        //store culture specific strings
        private LStringList names = new LStringList();
        //image		
        private string image;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.LocalizeItem"/> class.
        /// </summary>
        public LocaleItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.LocalizeItem"/> class.
        /// </summary>
        /// <param name='category'>
        /// Category.
        /// </param>
        /// <param name='name'>
        /// Name.
        /// </param>
        public LocaleItem(string category, string name)
        {
            this.category = category;
            this.name = name;
        }

        /// <summary>
        /// Gets or sets the image key.
        /// </summary>
        /// <value>
        /// The image key.
        /// </value>
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

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>
        /// The category.
        /// </value>
        [ReadOnly(true), DefaultValue("")]
        public string Category
        {
            get { return category; }
            set
            {
                if (category != value)
                {
                    category = value;
                    OnPropertyChanged(nameof(Category));
                }
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [ReadOnly(true), DefaultValue("")]
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

        /// <summary>
        /// Gets or sets the names.
        /// </summary>
        /// <value>
        /// The names.
        /// </value>
        public LStringList Names
        {
            get { return this.names; }
        }

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        /// <value>
        /// The current value by the CStringList.Current
        /// </value>
        [XmlIgnore]
        public string CurrentName
        {
            get { return names.Value ?? name; }
            set
            {
                if (names.Value != value)
                {
                    names.Value = value;
                    OnPropertyChanged(nameof(CurrentName));
                }
            }
        }

        /// <summary>
        /// Gets or sets the current description.
        /// </summary>
        /// <value>The current description.</value>
        [XmlIgnore]
        public string CurrentDescription
        {
            get { return names.Descript ?? name; }
            set
            {
                if (names.Descript != value)
                {
                    names.Descript = value;
                    OnPropertyChanged(nameof(CurrentDescription));
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

        public object Picture
        {
            get { return Locale.GetImage(image); }
        }

        public void Merge(LocaleItem item)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", category, name);
        }

        #region ICloneable implementation
        public object Clone()
        {
            return new LocaleItem(category, name)
            {
                names = (LStringList)names.Clone()
            };
        }
        #endregion

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        #endregion


    }

}

