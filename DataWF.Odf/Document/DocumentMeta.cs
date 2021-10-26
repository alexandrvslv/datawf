using System.Xml;


namespace Doc.Odf
{
    public class DocumentMeta : DocumentElementCollection
    {
        //office:document-meta
        public DocumentMeta(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
