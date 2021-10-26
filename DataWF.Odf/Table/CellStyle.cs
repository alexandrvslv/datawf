using System.Xml;

namespace Doc.Odf
{
    public class CellStyle : DefaultStyle
    {
        public CellStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public CellStyle(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Style, Service.nsStyle))
        {
            Family = StyleFamily.Cell;
            Name = "ce" + (document.GetCellStyles().Count + 1);
            document.Content.AutomaticStyles.Add(this);
        }
        public CellProperties ColumnProperty
        {
            get
            {
                if (!(this[Service.CellProperties] is CellProperties cp))
                {
                    cp = new CellProperties(document);
                    this.Add(cp);
                }
                return cp;
            }
        }
    }

}