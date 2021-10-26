using System.Xml;

namespace Doc.Odf
{
    public class ParagraphProperties : BaseProperties
    {
        public ParagraphProperties(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
        public ParagraphProperties(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.ParagraphProperties, Service.nsStyle))
        {
        }
        public TabStops TabStops
        {
            get { return (TabStops)this[Service.TabStops]; }
        }
        public string LineHeight
        {
            get { return GetAttributeByParent("fo:line-height"); }
            set { Service.SetAttribute(Element, "fo:line-height", Service.nsFO, value); }
        }
        //public string LineHeight
        //{
        //    get { return e.GetAttribute("fo:line-height"); }
        //    set { e.SetAttribute("fo:line-height", value); }
        //}
        public string LineHeightAtLeast
        {
            get { return GetAttributeByParent("style:line-height-at-least"); }
            set { Service.SetAttribute(Element, "style:line-height-at-least", Service.nsStyle, value); }
        }
        public string LineSpacing
        {
            get { return GetAttributeByParent("style:line-spacing"); }
            set { Service.SetAttribute(Element, "style:line-spacing", Service.nsStyle, value); }
        }
        public string TextAlignLast
        {
            get { return GetAttributeByParent("fo:text-align-last"); }
            set { Service.SetAttribute(Element, "fo:text-align-last", Service.nsFO, value); }
        }
        public string TextAlign
        {
            get { return GetAttributeByParent("fo:text-align"); }
            set { Service.SetAttribute(Element, "fo:text-align", Service.nsFO, value); }
        }
        public string MarginLeft
        {
            get { return GetAttributeByParent("fo:margin-left"); }
            set { Service.SetAttribute(Element, "fo:margin-left", Service.nsFO, value); }
        }
        public string MarginRight
        {
            get { return GetAttributeByParent("fo:margin-right"); }
            set { Service.SetAttribute(Element, "fo:margin-right", Service.nsFO, value); }
        }
        public string MarginTop
        {
            get { return GetAttributeByParent("fo:margin-top"); }
            set { Service.SetAttribute(Element, "fo:margin-top", Service.nsFO, value); }
        }
        public string MarginBottom
        {
            get { return GetAttributeByParent("fo:margin-bottom"); }
            set { Service.SetAttribute(Element, "fo:margin-bottom", Service.nsFO, value); }
        }
        public string TextIndent
        {
            get { return GetAttributeByParent("fo:text-indent"); }
            set { Service.SetAttribute(Element, "fo:text-indent", Service.nsFO, value); }
        }
        public string BackgroundColor
        {
            get { return GetAttributeByParent("fo:background-color"); }
            set { Service.SetAttribute(Element, "fo:background-color", Service.nsFO, value); }
        }
        public string Border
        {
            get { return GetAttributeByParent("fo:border"); }
            set { Service.SetAttribute(Element, "fo:border", Service.nsFO, value); }
        }
        public string BorderLeft
        {
            get { return GetAttributeByParent("fo:border-left"); }
            set { Service.SetAttribute(Element, "fo:border-left", Service.nsFO, value); }
        }
        public string BorderRight
        {
            get { return GetAttributeByParent("fo:border-right"); }
            set { Service.SetAttribute(Element, "fo:border-right", Service.nsFO, value); }
        }
        public string BorderTop
        {
            get { return GetAttributeByParent("fo:border-top"); }
            set { Service.SetAttribute(Element, "fo:border-top", Service.nsFO, value); }
        }
        public string BorderBottom
        {
            get { return GetAttributeByParent("fo:border-bottom"); }
            set { Service.SetAttribute(Element, "fo:border-bottom", Service.nsFO, value); }
        }
        public string BorderLineWidth
        {
            get { return GetAttributeByParent("style:border-line-width"); }
            set { Service.SetAttribute(Element, "style:border-line-width", Service.nsStyle, value); }
        }

        //public Color Background
        //{
        //    get { string value = e.GetAttribute("fo:background-color"); 
        //        Color.FromArgb(

        //    }

        //    set { e.SetAttribute("fo:background-color", value); }
        //}
    }

}