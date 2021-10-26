using System.Xml;


namespace Doc.Odf
{
    public class Column : DocumentElement, IStyledElement
    {
        public Column(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {

        }
        public Column(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Column, Service.nsTable))
        {
        }
        //table:style-name="Òàáëèöà1.C" table:number-columns-repeated="2"    table:default-cell-style-name
        public string DefaultCellStyleName
        {
            get { return Element.GetAttribute("table:default-cell-style-name"); }
            set { Service.SetAttribute(Element, "table:default-cell-style-name", Service.nsTable, value); }
        }

        public string ColumnsRepeatedCount
        {
            get { return Element.GetAttribute("table:number-columns-repeated"); }
            set { Service.SetAttribute(Element, "table:number-columns-repeated", Service.nsTable, value); }
        }

        public uint RepeatedCount
        {
            get { return ColumnsRepeatedCount == "" ? 0 : uint.Parse(ColumnsRepeatedCount); }
            set { ColumnsRepeatedCount = value.ToString(); }
        }

        public string StyleName
        {
            get { return Element.GetAttribute("table:style-name"); }
            set { Service.SetAttribute(Element, "table:style-name", Service.nsTable, value); }
        }

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
    }

}
