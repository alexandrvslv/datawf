using System.Xml;


namespace Doc.Odf
{
    public class Settings : DocumentElementCollection
    {
        public Settings(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
