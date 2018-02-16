using System;
using System.Globalization;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Common
{
    /// <summary>
    /// Culture string.
    /// </summary>
    public class LString : ICloneable, INotifyPropertyChanged
    {
        internal string culture = "ru-RU";
        protected string value;
        protected string description;
        //cache Culture Info object
        [NonSerialized]
        protected CultureInfo info;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.LString"/> class.
        /// </summary>
        public LString()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.LString"/> class.
        /// </summary>
        /// <param name='value'>
        /// Value of the CString (culture take default ru_RU)
        /// </param>
        public LString(string value)
        {
            this.value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.LString"/> class.
        /// </summary>
        /// <param name='value'>
        /// Value.
        /// </param>
        /// <param name='culture'>
        /// Culture name.
        /// </param>
        public LString(string value, string culture)
            : this(value)
        {
            this.culture = culture;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.LString"/> class.
        /// </summary>
        /// <param name='value'>
        /// Value.
        /// </param>
        /// <param name='culture'>
        /// Culture.
        /// </param>
        public LString(string value, CultureInfo culture)
            : this(value, culture.Name)
        {
        }

        /// <summary>
        /// Gets or sets the name of the culture.
        /// </summary>
        /// <value>
        /// The name of the culture.
        /// </value>
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

        /// <summary>
        /// Gets or sets the culture.
        /// </summary>
        /// <value>
        /// The culture.
        /// </value>
        /// 
        [XmlIgnore]
        public CultureInfo Culture
        {
            get
            {
                if (info == null)
                    info = CultureInfo.GetCultureInfo(culture);
                return info;
            }
            set
            {
                CultureName = value == null ? null : value.Name;
                info = value;
            }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
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

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
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
        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Dwf.Tool.LString"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents the current <see cref="Dwf.Tool.LString"/>.
        /// </returns>
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
            LString s = new LString();
            s.culture = culture;
            s.info = info;
            s.value = value;
            s.description = description;
            return s;
        }

        #endregion

        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Occurs when ever property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        /// <param name='property'>
        /// Property.
        /// </param>
        protected void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        #endregion
    }


}

