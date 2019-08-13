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

        [Invoker(typeof(LocaleImage), nameof(LocaleImage.Key))]
        public class KeyInvoker : Invoker<LocaleImage, string>
        {
            public static readonly KeyInvoker Instance = new KeyInvoker();
            public override string Name
            {
                get => nameof(LocaleImage.Key);
            }

            public override bool CanWrite => true;

            public override string GetValue(LocaleImage target) => target.Key;

            public override void SetValue(LocaleImage target, string value) => target.Key = value;
        }


        [Invoker(typeof(LocaleImage), nameof(LocaleImage.FileName))]
        public class FileNameInvoker : Invoker<LocaleImage, string>
        {
            public static readonly FileNameInvoker Instance = new FileNameInvoker();
            public override string Name
            {
                get => nameof(LocaleImage.FileName);
            }

            public override bool CanWrite => true;

            public override string GetValue(LocaleImage target) => target.FileName;

            public override void SetValue(LocaleImage target, string value) => target.FileName = value;
        }

        [Invoker(typeof(LocaleImage), nameof(LocaleImage.Data))]
        public class DataInvoker : Invoker<LocaleImage, byte[]>
        {
            public static readonly DataInvoker Instance = new DataInvoker();
            public override string Name
            {
                get => nameof(LocaleImage.Data);
            }

            public override bool CanWrite => true;

            public override byte[] GetValue(LocaleImage target) => target.Data;

            public override void SetValue(LocaleImage target, byte[] value) => target.Data = value;
        }

    }
}
