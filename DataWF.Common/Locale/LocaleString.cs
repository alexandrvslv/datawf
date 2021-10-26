using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;

[assembly: Invoker(typeof(LocaleString), nameof(LocaleString.CultureName), typeof(LocaleString.CultureNameInvoker))]
[assembly: Invoker(typeof(LocaleString), nameof(LocaleString.Value), typeof(LocaleString.ValueInvoker))]
[assembly: Invoker(typeof(LocaleString), nameof(LocaleString.Description), typeof(LocaleString.DescriptionInvoker))]
namespace DataWF.Common
{
    public class LocaleString : ICloneable, IEntryNotifyPropertyChanged
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
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers<INotifyListPropertyChanged>(PropertyChanged);

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public class CultureNameInvoker : Invoker<LocaleString, string>
        {
            public static readonly CultureNameInvoker Instance = new CultureNameInvoker();
            public override string Name => nameof(LocaleString.CultureName);

            public override bool CanWrite => true;

            public override string GetValue(LocaleString target) => target.CultureName;

            public override void SetValue(LocaleString target, string value) => target.CultureName = value;
        }

        public class ValueInvoker : Invoker<LocaleString, string>
        {
            public static readonly ValueInvoker Instance = new ValueInvoker();
            public override string Name => nameof(LocaleString.Value);

            public override bool CanWrite => true;

            public override string GetValue(LocaleString target) => target.Value;

            public override void SetValue(LocaleString target, string value) => target.Value = value;
        }

        public class DescriptionInvoker : Invoker<LocaleString, string>
        {
            public static readonly DescriptionInvoker Instance = new DescriptionInvoker();
            public override string Name => nameof(LocaleString.Description);

            public override bool CanWrite => true;

            public override string GetValue(LocaleString target) => target.Description;

            public override void SetValue(LocaleString target, string value) => target.Description = value;
        }
    }


}

