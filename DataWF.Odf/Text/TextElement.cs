using System.Xml;


namespace Doc.Odf
{
    public class TextElement : BaseItem, ITextual, ITextContainer
    {
        public TextElement(ODFDocument document, XmlNode node)
            : base(document, node)
        {
        }
        public TextElement(ODFDocument document, string text)
            : base(document, document.xmlContent.CreateTextNode(text))
        {
        }
        public string Value
        {
            get { return node.InnerText; }
            set { node.InnerText = value; }
        }
    }

}
