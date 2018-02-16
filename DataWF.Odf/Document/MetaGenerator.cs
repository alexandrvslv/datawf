using System.Xml;


namespace Doc.Odf
{
    public class MetaGenerator : DocumentElementCollection
    {
        //meta:generator
        public MetaGenerator(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
