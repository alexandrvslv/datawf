using System.Xml;

namespace Doc.Odf
{
    public class TextStyle : DefaultStyle, IStyleTextProperty
    {
        public TextStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }

        public TextStyle(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Style, Service.nsStyle))
        {
            base.Family = StyleFamily.Text;
            base.Name = "S" + (document.GetTextStyles().Count + 1);
            document.Content.AutomaticStyles.Add(this);
        }

        public TextProperties TextProperties
        {
            get
            {
                TextProperties property = (TextProperties)this[Service.TextProperties];
                if (property == null)
                {
                    property = new TextProperties(document);
                    this.Add(property);
                }
                return property;
            }
        }
    }

}