using System.Xml;


namespace Doc.Odf
{
    public class ConfigItemMapEntry : DocumentElementCollection
    {
        public ConfigItemMapEntry(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
