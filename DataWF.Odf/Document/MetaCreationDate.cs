using System.Xml;


namespace Doc.Odf
{
    public class MetaCreationDate : DocumentElementCollection
    {
        //meta:creation-date
        public MetaCreationDate(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
