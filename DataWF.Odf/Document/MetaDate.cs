using System.Xml;


namespace Doc.Odf
{
    public class MetaDate : DocumentElementCollection
    {
        //dc:date
        public MetaDate(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
