using System;
using System.Xml;


namespace Doc.Odf
{
    public class List : DocumentElementCollection, ITextContainer
    {
        public List(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        //xml:id="list34545435" text:style-name="L1"
        public string StyleName
        {
            get { return Element.GetAttribute("text:style-name"); }
            set { Service.SetAttribute(Element, "text:style-name", Service.nsText, value); }
        }
        public ListStyle Style
        {
            get { return (ListStyle)document.GetStyle(StyleName); }
            set
            {
                if (value == null)
                    return;
                StyleName = value.Name;
            }
        }
        public string Id
        {
            get { return Element.GetAttribute("xml:id"); }
            set { Service.SetAttribute(Element, "xml:id", "", value); }
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
