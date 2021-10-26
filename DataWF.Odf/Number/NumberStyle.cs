using System.Xml;

namespace Doc.Odf
{
    public class NumberStyle : BaseStyle
    {
        //number:number-style
        public NumberStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
    }

}