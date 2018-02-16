using System;
using System.Xml;


namespace Doc.Odf
{
    public class TextSection : DocumentElementCollection, ITextContainer
    {
        public TextSection(ODFDocument document, XmlElement element)
            : base(document, element)
        {
        }
        //<text:section text:style-name="Sect2" text:name="Section7" 
        public SectionStyle Style
        {
            get { return (SectionStyle)document.GetStyle(StyleName); }
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
