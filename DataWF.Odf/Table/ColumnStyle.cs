using System.Xml;

namespace Doc.Odf
{
    public class ColumnStyle : DefaultStyle
    {
        public ColumnStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public ColumnStyle(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Style, Service.nsStyle))
        {
            Family = StyleFamily.Column;
            Name = "co" + (document.GetColumnStyles().Count + 1);
            document.Content.AutomaticStyles.Add(this);
        }

        public ColumnProperties ColumnProperty
        {
            get
            {
                if (!(this[Service.ColumnProperties] is ColumnProperties cp))
                {
                    cp = new ColumnProperties(document);
                    this.Add(cp);
                }
                return cp;
            }
        }
    }

}