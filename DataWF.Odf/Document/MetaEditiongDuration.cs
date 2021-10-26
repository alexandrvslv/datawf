using System.Xml;


namespace Doc.Odf
{
    public class MetaEditiongDuration : DocumentElementCollection
    {
        //meta:editing-duration
        public MetaEditiongDuration(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
