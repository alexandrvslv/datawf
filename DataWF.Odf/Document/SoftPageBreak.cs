using System.Xml;


namespace Doc.Odf
{
    //<text:soft-page-break xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0" />
    public class SoftPageBreak : DocumentElementCollection
    {
        public SoftPageBreak(ODFDocument document, XmlElement element)
            : base(document, element)
        {
        }
    }

}
