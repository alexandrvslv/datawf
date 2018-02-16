using System.Xml;

namespace Doc.Odf
{
    public class TextBibliographyConfiguration : BaseStyle
    {
        public TextBibliographyConfiguration(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public string Prefix
        {
            get { return Element.GetAttribute("text:prefix"); }
            set { Service.SetAttribute(Element, "text:prefix", Service.nsText, value); }
        }
        public string Suffix
        {
            get { return Element.GetAttribute("text:suffix"); }
            set { Service.SetAttribute(Element, "text:suffix", Service.nsText, value); }
        }
        public string SortByPosition
        {
            get { return Element.GetAttribute("text:sort-by-position"); }
            set { Service.SetAttribute(Element, "text:sort-by-position", Service.nsText, value); }
        }
        public string SortAlgorithm
        {
            get { return Element.GetAttribute("text:sort-algorithm"); }
            set { Service.SetAttribute(Element, "text:sort-algorithm", Service.nsText, value); }
        }
        public string Language
        {
            get { return Element.GetAttribute("fo:language"); }
            set { Service.SetAttribute(Element, "fo:language", Service.nsFO, value); }
        }
        public string Country
        {
            get { return Element.GetAttribute("fo:country"); }
            set { Service.SetAttribute(Element, "fo:country", Service.nsFO, value); }
        }
    }

}