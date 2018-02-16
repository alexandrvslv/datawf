using System.Xml;

namespace Doc.Odf
{
    public class TextSpan : DocumentElementCollection, ITextual, IStyledElement
    {
        public TextSpan(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }

        public TextSpan(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.TextSpan, Service.nsText))
        { }

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

        //public Paragraph Paragraph
        //{
        //    get { return (Paragraph)Owner; }
        //}
        //public TextElement Text
        //{
        //    get { return this.FirstElement; }
        //    set { }
        //}
        #region ITextual Members

        public string Value
        {
            get { return Service.GetText(this); }
            set
            {
                //thi;
            }
        }

        #endregion

    }
}