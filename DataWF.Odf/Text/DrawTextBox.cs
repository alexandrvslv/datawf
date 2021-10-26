using System;
using System.Xml;


namespace Doc.Odf
{
    //draw:text-box fo:min-height="0.37cm" 
    public class DrawTextBox : DocumentElementCollection, ITextContainer
    {
        public DrawTextBox(ODFDocument document, XmlElement element)
            : base(document, element)
        {
        }
        public DrawTextBox(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.DrawTextBox, Service.nsDraw))
        {
        }
        public string MinHeight
        {
            get { return Element.GetAttribute("fo:min-height"); }
            set { Service.SetAttribute(Element, "fo:min-height", Service.nsFO, value); }
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
