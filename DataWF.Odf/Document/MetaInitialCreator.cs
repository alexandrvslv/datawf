using System.Xml;


namespace Doc.Odf
{
    public class MetaInitialCreator : DocumentElementCollection
    {
        //meta:initial-creator
        public MetaInitialCreator(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
