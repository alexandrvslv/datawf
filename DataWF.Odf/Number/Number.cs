using System.Globalization;
using System.Xml;

namespace Doc.Odf
{
    public class Number : DocumentElementCollection
    {
        //number:number
        public Number(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        //number:min-integer-digits="1" //number:decimal-places="2" number:grouping="true" 
        public string MinIntegerDigits
        {
            get { return Element.GetAttribute("number:min-integer-digits"); }
            set { Service.SetAttribute(Element, "number:min-integer-digits", Service.nsDataStyle, value); }
        }
        public string DecimalPlaces
        {
            get { return Element.GetAttribute("number:decimal-places"); }
            set { Service.SetAttribute(Element, "number:decimal-places", Service.nsDataStyle, value); }
        }
        public string NumberGrouping
        {
            get { return Element.GetAttribute("number:grouping"); }
            set { Service.SetAttribute(Element, "number:grouping", Service.nsDataStyle, value); }
        }
        public bool Grouping
        {
            get { return NumberGrouping == "" ? false : bool.Parse(NumberGrouping); }
            set { NumberGrouping = value.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture); }
        }
    }

}