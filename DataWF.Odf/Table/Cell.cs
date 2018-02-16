using System.Xml;


namespace Doc.Odf
{
    public class Cell : DocumentElementCollection, ITextContainer, IStyledElement
    {
        public Cell(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }

        public Cell(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Cell, Service.nsTable))
        {
        }
        //office:value-type="float" office:value="10"
        //table:formula="of:=SUM([.E1:.E9])"
        // 
        public string NumberColumnsSpanned
        {
            get { return Element.GetAttribute("table:number-columns-spanned"); }
            set { Service.SetAttribute(Element, "table:number-columns-spanned", Service.nsTable, value); }
        }

        public string NumberRowsSpanned
        {
            get { return Element.GetAttribute("table:number-rows-spanned"); }
            set { Service.SetAttribute(Element, "table:number-rows-spanned", Service.nsTable, value); }
        }

        public string TableFormula
        {
            get { return Element.GetAttribute("table:formula"); }
            set { Service.SetAttribute(Element, "table:formula", Service.nsTable, value); }
        }

        public string ValueType
        {
            get { return Element.GetAttribute("office:value-type"); }
            set { Service.SetAttribute(Element, "office:value-type", Service.nsOffice, value); }
        }

        public object Val
        {
            get { return Element.GetAttribute("office:value"); }
            set
            {
                string val = value == null ? string.Empty : value.ToString();

                if (value != null && (value.GetType() == typeof(uint) || value.GetType() == typeof(int) || value.GetType() == typeof(ulong) || value.GetType() == typeof(long) || value.GetType() == typeof(decimal) || value.GetType() == typeof(float) || value.GetType() == typeof(double)))
                {
                    Service.SetAttribute(Element, "office:value", Service.nsOffice, val);
                    ValueType = "float";
                }
                else
                    ValueType = "string";

                Value = val;
            }
        }

        public string ColumnsRepeatedCount
        {
            get { return Element.GetAttribute("table:number-columns-repeated"); }
            set { Service.SetAttribute(Element, "table:number-columns-repeated", Service.nsTable, value); }
        }

        public uint RepeatedCount
        {
            get { return ColumnsRepeatedCount == "" ? 1 : uint.Parse(ColumnsRepeatedCount); }
            set { ColumnsRepeatedCount = value.ToString(); }
        }

        public string StyleName
        {
            get { return Element.GetAttribute("table:style-name"); }
            set { Service.SetAttribute(Element, "table:style-name", Service.nsTable, value); }
        }

        public DefaultStyle Style
        {
            get { return (DefaultStyle)document.GetStyle(StyleName); }
            set
            {
                if (value == null)
                    return;
                StyleName = value.Name;
            }
        }

        public Paragraph FirstParagraph
        {
            get
            {
                Paragraph de = (Paragraph)this[Service.Paragraph];
                if (de == null)
                {
                    de = new Paragraph(document);
                    //de.StyleName = "Standard";
                    this.Add(de);
                }
                return de;
            }
        }

        #region ITextual Members

        public string Value
        {
            get { return Service.GetText(this); }
            set
            {

                if (value != null && value.Length != 0)
                {
                    FirstParagraph.Clear();
                    FirstParagraph.Add(value.Replace("\0", ""));
                }
                else if (Items.Count > 0)
                    Remove(FirstParagraph);
            }
        }

        #endregion

       
    }

}
