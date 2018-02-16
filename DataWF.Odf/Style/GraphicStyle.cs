using System.Xml;

namespace Doc.Odf
{
    public class GraphicStyle : ParagraphStyle
    {
        public GraphicStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public GraphicStyle(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Style, Service.nsStyle))
        {
            base.Family = StyleFamily.Graphic;
            base.Name = "G" + (document.GetGraphicStyles().Count + 1);
            document.Content.AutomaticStyles.Add(this);
        }
        public GraphicProperties GraphicProperty
        {
            get
            {
                GraphicProperties gp = this[Service.GraphicProperties] as GraphicProperties;
                if (gp == null)
                {
                    gp = new GraphicProperties(document);
                    this.Add(gp);
                }
                return gp;
            }
        }
    }

}