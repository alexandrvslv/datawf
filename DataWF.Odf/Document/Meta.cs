using System.Xml;


namespace Doc.Odf
{
    public class Meta : DocumentElementCollection
    {
        //office:meta
        public Meta(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
    }

}
