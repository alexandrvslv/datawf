using System.Xml;


namespace Doc.Odf
{
    public class BaseConfig : DocumentElementCollection
    {
        public BaseConfig(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public string Name
        {
            get { return Element.GetAttribute("config:name"); }
            set { Element.SetAttribute("config:name", value); }
        }
    }

}
