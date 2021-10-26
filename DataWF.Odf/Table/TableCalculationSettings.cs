using System.Globalization;
using System.Xml;


namespace Doc.Odf
{
    public class TableCalculationSettings : DocumentElementCollection
    {
        public TableCalculationSettings(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }

        public string TableUseRegularExpressions
        {
            get { return Element.GetAttribute("table:use-regular-expressions"); }
            set { Service.SetAttribute(Element, "table:use-regular-expressions", Service.nsText, value); }
        }
        public bool UseRegularExpressions
        {
            get { return TableUseRegularExpressions == "" ? false : bool.Parse(TableUseRegularExpressions); }
            set { TableUseRegularExpressions = value.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture); }
        }
    }

}
