using System.Xml;

namespace Doc.Odf
{
    public class RowStyle : DefaultStyle
    {
        public RowStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public RowStyle(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Style, Service.nsStyle))
        {
            Family = StyleFamily.Row;
        }
        public RowProperties RowProperty
        {
            get
            {
                RowProperties rp = this[Service.RowProperties] as RowProperties;
                if (rp == null)
                {
                    rp = new RowProperties(document);
                    this.Add(rp);
                }
                return rp;
            }
        }
    }

}