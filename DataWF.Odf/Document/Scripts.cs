using System.Xml;


namespace Doc.Odf
{
    public class Scripts : DocumentElementCollection
    {
        //office:scripts
        public Scripts(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
