using System;
using System.Collections.Generic;
using System.Xml;


namespace Doc.Odf
{
    public class Row : DocumentElementCollection, ITextContainer
    {
        private int index;

        public Row(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }

        public Row(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Row, Service.nsTable))
        {
        }
        public string StyleName
        {
            get { return Element.GetAttribute("table:style-name"); }
            set { Service.SetAttribute(Element, "table:style-name", Service.nsTable, value); }
        }
        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        public void Add(Cell value)
        {
            base.Add(value);
        }

        public List<Cell> GetCells()
        {
            return this.GetChilds(typeof(Cell)).ConvertAll<Cell>(new Converter<BaseItem, Cell>(Service.BaseToCell));
        }

        #region ITextual Members

        public string Value
        {
            get { return Service.GetText(this); }
            set
            {
                //throw new NotImplementedException();
            }
        }

        #endregion
    }

}
