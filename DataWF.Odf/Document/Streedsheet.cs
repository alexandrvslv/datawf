using System.Xml;


namespace Doc.Odf
{
    public class Streedsheet : DocumentElementCollection
    {
        //office:streedsheet
        public Streedsheet(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }

    }

}
