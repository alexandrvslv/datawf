using System.Xml;


namespace Doc.Odf
{
    public class DocumentBody : DocumentElementCollection
    {
        //office:body
        public DocumentBody(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public BodyText Text
        {
            get { return (BodyText)this[Service.DocumentText]; }
        }

        public DocumentSpreadSheet SpreadSheet
        {
            get { return (DocumentSpreadSheet)this[Service.DocumentSpreadSheet]; }
        }
    }

}
