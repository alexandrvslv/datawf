using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class LocaleImage : IContainerNotifyPropertyChanged
    {
        public static event Func<LocaleImage, object> ImageCache;

        public static object GetImageCache(LocaleImage data)
        {
            if (ImageCache != null)
                return ImageCache(data);
            return null;
        }
        string key = string.Empty;
        string file = string.Empty;
        byte[] data;
        object cache;

        [DefaultValue("")]
        public string Key
        {
            get { return key; }
            set
            {
                if (key != value)
                {
                    key = value;
                    OnPropertyChanged(nameof(Key));
                }
            }
        }

        [DefaultValue("")]
        public string FileName
        {
            get { return file; }
            set
            {
                if (Path.GetFileName(value) != file)
                {
                    file = Path.GetFileName(value);
                    Key = Path.GetFileNameWithoutExtension(file);
                    OnPropertyChanged(nameof(FileName));
                }
            }
        }

        [XmlText]
        public byte[] Data
        {
            get { return data; }
            set
            {
                if (data != value)
                {
                    data = value;
                    cache = null;
                    OnPropertyChanged(nameof(Data));
                }
            }
        }

        [XmlIgnore]
        public object Cache
        {
            get
            {
                if (cache == null && data != null)
                    cache = GetImageCache(this);
                return cache;
            }
            set { cache = value; }
        }

        public override string ToString()
        {
            return key;
        }

        public object Image
        {
            get { return Cache; }
            set { }
        }

        [XmlIgnore, Browsable(false)]
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers(PropertyChanged);

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

    }
}
