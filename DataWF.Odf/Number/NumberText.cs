using System.Xml;

namespace Doc.Odf
{
    public class NumberText : DocumentElementCollection
    {
        //number:text
        public NumberText(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
    }

}