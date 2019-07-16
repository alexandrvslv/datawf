using System;
using System.ComponentModel;

namespace DataWF.Common
{
    public class WebLink
    {
        private string url;
        [DefaultValue("default")]
        private string category = "default";
        [NonSerialized]
        LocaleItem litem;

        public WebLink()
        {
        }

        public string Category
        {
            get { return category; }
            set { category = value; }
        }

        public string Url
        {
            get { return url; }
            set
            {
                if (url == value)
                    return;
                url = value;
                if (litem != null)
                    litem.Name = url;
            }
        }

        public LocaleItem Name
        {
            get
            {
                if (litem == null && url != null)
                    litem = Locale.Instance.GetItem("WebLink", url);
                return litem;
            }
        }
    }

    public class WebLinkList : SelectableList<WebLink>
    {
        public WebLinkList()
        {
        }
    }
}

