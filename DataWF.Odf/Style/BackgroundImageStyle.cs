using System.Xml;

namespace Doc.Odf
{
    public class BackgroundImageStyle : DocumentElementCollection
    {
        public BackgroundImageStyle(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
    }

}