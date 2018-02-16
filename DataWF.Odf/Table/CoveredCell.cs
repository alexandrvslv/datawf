using System.Xml;


namespace Doc.Odf
{
    public class CoveredCell : DocumentElementCollection
    {
        public CoveredCell(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }

        public CoveredCell(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.CoveredCell, Service.nsTable))
        {
        }
        //table:number-columns-repeated
        public string ColumnsRepeatedCount
        {
            get { return Element.GetAttribute("table:number-columns-repeated"); }
            set { Service.SetAttribute(Element, "table:number-columns-repeated", Service.nsTable, value); }
        }

    }

}
