using System.Xml;

namespace Doc.Odf
{
    public class ParagraphStyle : TextStyle
    {
        public ParagraphStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public ParagraphStyle(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Style, Service.nsStyle))
        {
            base.Family = StyleFamily.Paragraph;
            base.Name = "P" + (document.GetParagraphStyles().Count + 1);
            document.Content.AutomaticStyles.Add(this);
        }
        public ParagraphProperties ParagraphProperty
        {
            get
            {
                ParagraphProperties pp = this[Service.ParagraphProperties] as ParagraphProperties;
                if (pp == null)
                {
                    pp = new ParagraphProperties(document);
                    this.Add(pp);
                }
                return pp;
            }
        }
    }

}