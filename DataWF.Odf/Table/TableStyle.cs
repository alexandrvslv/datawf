using System.Xml;

namespace Doc.Odf
{
    public class TableStyle : DefaultStyle
    {
        public TableStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public TableStyle(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Style, Service.nsStyle))
        {
            base.Family = StyleFamily.Table;
            base.Name = "T" + (document.GetTableStyles().Count + 1);
            document.Content.AutomaticStyles.Add(this);
        }
        public TableProperties TableProperty
        {
            get
            {
                TableProperties de = (TableProperties)this[Service.TableProperties];
                if (de == null)
                {
                    de = new TableProperties(document);
                    this.Add(de);
                }
                return de;
            }
        }

    }

}