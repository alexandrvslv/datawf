using System.Xml;


namespace Doc.Odf
{
    //office:spreadsheet
    public class DocumentSpreadSheet : DocumentElementCollection
    {
        public DocumentSpreadSheet(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
