using System.Xml;


namespace Doc.Odf
{
    public class DocumentManifest : DocumentElementCollection
    {
        //manifest:manifest
        public DocumentManifest(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
