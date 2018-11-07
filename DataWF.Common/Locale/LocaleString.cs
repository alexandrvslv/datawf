using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class LocaleString : ICloneable, IContainerNotifyPropertyChanged
    {
        internal string culture = "ru-RU";
        protected string value;
        protected string description;
        protected CultureInfo info;

        public LocaleString()
        {
        }

        public LocaleString(string value)
        {
            this.value = value;
        }

        public LocaleString(string value, string culture)
            : this(value)
        {
            this.culture = culture;
        }

        public LocaleString(string value, CultureInfo culture)
            : this(value, culture.Name)
        {
        }

        [Browsable(false)]
        public string CultureName
        {
            get { return culture; }
            set
            {
                if (culture == value)
                    return;
                culture = value;
                info = null;
                OnPropertyChanged(nameof(CultureName));
            }
        }

        [XmlIgnore]
        public CultureInfo Culture
        {
            get { return info ?? (info = CultureInfo.GetCultureInfo(culture)); }
            set
            {
                CultureName = value?.Name;
                info = value;
            }
        }

        public string Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public string Description
        {
            get { return description; }
            set
            {
                if (description != value)
                {
                    description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        [XmlIgnore, Browsable(false)]
        public INotifyListPropertyChanged Container { get; set; }

        public override string ToString()
        {
            return string.Format("{0}({1})", Value, CultureName);
        }

        #region IClonable
        /// <summary>
        /// Clone this instance.
        /// </summary>
        public object Clone()
        {
            return new LocaleString
            {
                culture = culture,
                info = info,
                value = value,
                description = description
            }; ;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string property)
        {
            var arg = new PropertyChangedEventArgs(property);
            Container?.OnItemPropertyChanged(this, arg);
            PropertyChanged?.Invoke(this, arg);
        }
    }


}

