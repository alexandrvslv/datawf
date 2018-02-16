using System;
using System.Xml;


namespace Doc.Odf
{
    public class TableOfContent : DocumentElementCollection, ITextContainer, IStyledElement
    {
        public TableOfContent(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public DefaultStyle Style
        {
            get { return (DefaultStyle)document.GetStyle(StyleName); }
            set
            {
                if (value == null)
                    return;
                StyleName = value.Name;
            }
        }

        public string StyleName
        {
            get { return Element.GetAttribute("text:style-name"); }
            set { Service.SetAttribute(Element, "text:style-name", Service.nsText, value); }
        }
        public string TextProtected
        {
            get { return Element.GetAttribute("text:protected"); }
            set { Service.SetAttribute(Element, "text:protected", Service.nsText, value); }
        }
        public string Name
        {
            get { return Element.GetAttribute("text:name"); }
            set { Service.SetAttribute(Element, "text:name", Service.nsText, value); }
        }



        #region ITextual Members

        public string Value
        {
            get { return Service.GetText(this); }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }

}
