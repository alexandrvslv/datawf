using System;
using System.Xml;


namespace Doc.Odf
{
    public class TextLink : DocumentElementCollection, ITextContainer, IStyledElement
    {
        public TextLink(ODFDocument document, XmlElement element)
            : base(document, element)
        {
        }
        //<text:a xlink:type="simple" xlink:href="http://docs.oasis-open.org/office/office-accessibility/v1.0/cs01/ODF_Accessibility_Guidelines-v1.0.odt" xmlns:xlink="http://www.w3.org/1999/xlink" xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0">http://docs.oasis-open.org/office/office-accessibility/v1.0/cs01/ODF_Accessibility_Guidelines-v1.0.odt</text:a>
        public string Type
        {
            get { return Element.GetAttribute("xlink:type"); }
            set { Service.SetAttribute(Element, "xlink:type", Service.nsXLink, value); }
        }
        public string Href
        {
            get { return Element.GetAttribute("xlink:href"); }
            set { Service.SetAttribute(Element, "xlink:href", Service.nsXLink, value); }
        }

        public DefaultStyle Style
        {
            get { return (DefaultStyle)document.GetStyle(StyleName); }
            set
            {
                if (value == null) return;
                if (value.Owner == null)
                    document.Content.AutomaticStyles.Add(value);
                StyleName = value.Name;
            }
        }

        public string StyleName
        {
            get { return Element.GetAttribute("text:style-name"); }
            set { Service.SetAttribute(Element, "text:style-name", Service.nsText, value); }
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
