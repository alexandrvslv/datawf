using System.Xml;


namespace Doc.Odf
{
    public class MetaEditiongCycles : DocumentElementCollection
    {
        //meta:editing-cycles
        public MetaEditiongCycles(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
