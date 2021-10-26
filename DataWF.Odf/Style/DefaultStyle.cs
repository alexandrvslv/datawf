using System.Xml;

namespace Doc.Odf
{
    public class DefaultStyle : BaseStyle
    {
        public DefaultStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public StyleFamily Family
        {
            get
            {
                //if (!e.HasAttribute("style:family")) return StyleFamily.None;
                string value = Element.GetAttribute("style:family");
                switch (value)
                {
                    case ("graphic"): return StyleFamily.Graphic;
                    case ("paragraph"): return StyleFamily.Paragraph;
                    case ("table"): return StyleFamily.Table;
                    case ("table-row"): return StyleFamily.Row;
                    case ("table-column"): return StyleFamily.Column;
                    case ("table-cell"): return StyleFamily.Cell;
                    case ("text"): return StyleFamily.Text;
                    case ("section"): return StyleFamily.Section;
                    default: return StyleFamily.None;
                }
            }
            set
            {
                string val = "";
                switch (value)
                {
                    case (StyleFamily.Graphic): val = "graphic"; break;
                    case (StyleFamily.Paragraph): val = "paragraph"; break;
                    case (StyleFamily.Table): val = "table"; break;
                    case (StyleFamily.Row): val = "table-row"; break;
                    case (StyleFamily.Column): val = "table-column"; break;
                    case (StyleFamily.Cell): val = "table-cell"; break;
                    case (StyleFamily.Text): val = "text"; break;
                    case (StyleFamily.Section): val = "section"; break;
                }
                Service.SetAttribute(Element, "style:family", Service.nsStyle, val);
            }
        }
        public string Class
        {
            get { return Element.GetAttribute("style:class"); }
            set { Service.SetAttribute(Element, "style:class", Service.nsStyle, value); }
        }
        public string ParentStyleName
        {
            get { return Element.GetAttribute("style:parent-style-name"); }
            set { Service.SetAttribute(Element, "style:parent-style-name", Service.nsStyle, value); }
        }
        public DefaultStyle ParentStyle
        {
            get
            {
                if (string.IsNullOrEmpty(ParentStyleName))
                {
                    return GetType() != typeof(DefaultStyle) ? (DefaultStyle)document.GetDefaultStyle(Family) : null;
                }
                else
                {
                    return (DefaultStyle)document.GetStyle(ParentStyleName);
                }
            }
            set
            {
                if (value == null) return;
                ParentStyleName = value.Name;
            }
        }
        public string DisplayName
        {
            get { return Element.GetAttribute("style:display-name"); }
            set { Service.SetAttribute(Element, "style:display-name", Service.nsStyle, value); }
        }
    }

}