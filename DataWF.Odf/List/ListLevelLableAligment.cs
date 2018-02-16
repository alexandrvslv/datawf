using System.Xml;

namespace Doc.Odf
{
    public class ListLevelLableAligment : DocumentElement
    {
        public ListLevelLableAligment(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        //text:label-followed-by="listtab" text:list-tab-stop-position="1.27cm" fo:text-indent="-0.635cm" fo:margin-left="1.27cm"
        public string FollowedBy
        {
            get { return Element.GetAttribute("text:label-followed-by"); }
            set { Service.SetAttribute(Element, "text:label-followed-by", Service.nsText, value); }
        }
        public string TabStopPosition
        {
            get { return Element.GetAttribute("text:list-tab-stop-position"); }
            set { Service.SetAttribute(Element, "text:list-tab-stop-position", Service.nsText, value); }
        }
        public string TextIndent
        {
            get { return Element.GetAttribute("fo:text-indent"); }
            set { Service.SetAttribute(Element, "fo:text-indent", Service.nsFO, value); }
        }
        public string MarginLeft
        {
            get { return Element.GetAttribute("fo:margin-left"); }
            set { Service.SetAttribute(Element, "fo:margin-left", Service.nsFO, value); }
        }
    }

}