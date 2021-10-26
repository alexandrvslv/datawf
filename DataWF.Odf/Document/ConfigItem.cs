using System.Xml;


namespace Doc.Odf
{
    public class ConfigItem : BaseConfig
    {
        public ConfigItem(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public string Type
        {
            get { return Element.GetAttribute("config:type"); }
            set { Element.SetAttribute("config:type", value); }
        }
        public TextElement TextElement
        {
            get { return Count == 0 ? null : (TextElement)Items[0]; }
        }
        public string Value
        {
            get { return TextElement == null ? "" : TextElement.Value; }
        }
    }

}
