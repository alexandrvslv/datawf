using System.Xml;


namespace Doc.Odf
{
    public class MetaCreator : DocumentElementCollection
    {
        //dc:creator
        public MetaCreator(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
