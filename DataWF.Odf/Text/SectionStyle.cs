using System.Xml;

namespace Doc.Odf
{
    public class SectionStyle : DefaultStyle
    {
        public SectionStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public SectionStyle(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Style, Service.nsStyle))
        {
            Family = StyleFamily.Section;
            Name = "SEC" + (document.GetSectionStyles().Count + 1);
            document.Content.AutomaticStyles.Add(this);
        }
        public SectionProperties SectionProperty
        {
            get { return (SectionProperties)this[Service.SectionProperties]; }
        }
    }

}