using System.Xml;


namespace Doc.Odf
{
    public class TextHeader : DocumentElementCollection, ITextContainer, IStyledElement
    {
        public TextHeader(ODFDocument document, XmlElement element)
            : base(document, element)
        {
        }
        public TextHeader(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.TextHeader, Service.nsText))
        {

        }
        public TextHeader(ODFDocument document, string text)
            : this(document)
        {
            document.InsertText(text, this, false);
        }
        //<text:h text:style-name="P29" text:outline-level="1" 
        //xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0">Background and Overview</text:h>        
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
        public string OutlineLevel
        {
            get { return Element.GetAttribute("text:outline-level"); }
            set { Service.SetAttribute(Element, "text:outline-level", Service.nsText, value); }
        }


        #region ITextual Members

        public string Value
        {
            get { return Service.GetText(this) + "\n"; }
            //throw new NotImplementedException();
            set { }
        }

        #endregion
    }

}
