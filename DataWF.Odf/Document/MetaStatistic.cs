using System.Xml;


namespace Doc.Odf
{
    public class MetaStatistic : DocumentElementCollection
    {
        //meta:document-statistic
        public MetaStatistic(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        //meta:table-count
        public string TableCount
        {
            get { return Element.GetAttribute("meta:table-count"); }
            set { Service.SetAttribute(Element, "meta:table-count", Service.nsMeta, value); }
        }
        //meta:image-count
        public string ImageCount
        {
            get { return Element.GetAttribute("meta:image-count"); }
            set { Service.SetAttribute(Element, "meta:image-count", Service.nsMeta, value); }
        }
        //meta:object-count
        public string ObjectCount
        {
            get { return Element.GetAttribute("meta:object-count"); }
            set { Service.SetAttribute(Element, "meta:object-count", Service.nsMeta, value); }
        }
        //meta:page-count
        public string PageCount
        {
            get { return Element.GetAttribute("meta:page-count"); }
            set { Service.SetAttribute(Element, "meta:page-count", Service.nsMeta, value); }
        }
        //meta:paragraph-count
        public string ParagraphCount
        {
            get { return Element.GetAttribute("meta:paragraph-count"); }
            set { Service.SetAttribute(Element, "meta:paragraph-count", Service.nsMeta, value); }
        }
        //meta:word-count
        public string WordCount
        {
            get { return Element.GetAttribute("meta:word-count"); }
            set { Service.SetAttribute(Element, "meta:word-count", Service.nsMeta, value); }
        }
        //meta:character-count
        public string CharacterCount
        {
            get { return Element.GetAttribute("meta:character-count"); }
            set { Service.SetAttribute(Element, "meta:character-count", Service.nsMeta, value); }
        }
    }

}
