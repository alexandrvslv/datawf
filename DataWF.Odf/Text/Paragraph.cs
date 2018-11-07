using System.Xml;

namespace Doc.Odf
{
    public class Paragraph : DocumentElementCollection, ITextContainer, IStyledElement
    {
        public Paragraph(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public Paragraph(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Paragraph, Service.nsText))
        { }
        public Paragraph(ODFDocument document, BaseItem item)
            : this(document)
        {
            Add(item);
        }
        public Paragraph(ODFDocument document, string text)
            : this(document)
        {
            document.InsertText(text, this, false);
        }
        public void Add(string text)
        {
            document.InsertText(text, this, false);
        }
        public void Add(TextElement textElement)
        {
            base.Add(textElement);
        }
        public void Add(TextSpan text)
        {
            base.Add(text);
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
            get
            {
                return Service.GetText(this) + "\n";
            }
            set
            {
                //throw new NotImplementedException();
            }
        }

        #endregion       
    }
}