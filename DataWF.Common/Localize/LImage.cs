using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class LImage
    {
        public static event Func<LImage, object> ImageCache;
        public static object GetImageCache(LImage data)
        {
            if (ImageCache != null)
                return ImageCache(data);
            return null;
        }
        [DefaultValue("")]
        string key = string.Empty;
        [DefaultValue("")]
        string file = string.Empty;
        [XmlText]
        byte[] data;
        [NonSerialized]
        object cache;

        [DefaultValue("")]
        public string Key
        {
            get { return key; }
            set { key = value; }
        }

        [DefaultValue("")]
        public string FileName
        {
            get { return file; }
            set
            {
                file = System.IO.Path.GetFileName(value);
                key = System.IO.Path.GetFileNameWithoutExtension(file);
            }
        }

        [XmlText]
        public byte[] Data
        {
            get { return data; }
            set
            {
                data = value;
                cache = null;
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
    }
}
