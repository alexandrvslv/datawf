using System.Xml;

namespace Doc.Odf
{
    public class NumberTextStyle : BaseStyle
    {
        //number:number-style
        public NumberTextStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
    }

}