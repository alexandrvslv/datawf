using System.Xml;


namespace Doc.Odf
{
    public class DocumentContent : BaseContent
    {
        //office:document-content 
        public DocumentContent(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public DocumentBody Body
        {
            get { return (DocumentBody)this[Service.DocumentBody]; }
        }
        //public BodyText Text
        //{
        //    get { return Body.Text; }
        //}
    }

}
