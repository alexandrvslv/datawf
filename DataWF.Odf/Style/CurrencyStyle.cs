using System.Globalization;
using System.Xml;

namespace Doc.Odf
{
    public class CurrencyStyle : BaseStyle
    {
        //number:currency-style
        public CurrencyStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        //style:volatile="true"
        public string StyleVolatile
        {
            get { return Element.GetAttribute("style:volatile"); }
            set { Service.SetAttribute(Element, "style:volatile", Service.nsStyle, value); }
        }
        public bool Volatile
        {
            get { return StyleVolatile == "" ? false : bool.Parse(StyleVolatile); }
            set { StyleVolatile = value.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture); }
        }
    }

}