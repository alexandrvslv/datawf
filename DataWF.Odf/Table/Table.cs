using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;


namespace Doc.Odf
{
    public class Table : DocumentElementCollection, ITextContainer, IStyledElement
    {
        //private int index = -1;
        public Table(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public Table(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.Table, Service.nsTable))
        {
            Name = "Table" + document.GetTables().Count + 1;
        }
        //table:name= table:style-name= table:print
        public string TablePrint
        {
            get { return Element.GetAttribute("table:print"); }
            set { Service.SetAttribute(Element, "table:print", Service.nsTable, value); }
        }
        public bool Print
        {
            get { return TablePrint == "" ? false : bool.Parse(TablePrint); }
            set { TablePrint = value.ToString(CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture); }
        }
        public string Name
        {
            get { return Element.GetAttribute("table:name"); }
            set { Service.SetAttribute(Element, "table:name", Service.nsTable, value); }
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
        public void Add(Column value)
        {
            List<Column> columns = GetColumns();
            if (columns.Count > 0)
                base.InsertAfter(columns[columns.Count - 1], value);
            else
                base.Insert(0, value);
        }

        public void Add(Row value)
        {
            //if (index == -1)
            //    index = GetRows().Count;
            //else
            //    index++;
            //value.Index = index;
            base.Add(value);
        }
        public List<Column> GetColumns()
        {
            return GetChilds(typeof(Column)).ConvertAll<Column>(new Converter<BaseItem, Column>(Service.BaseToColumn));
        }
        public List<Row> GetRows()
        {
            return GetChilds(typeof(Row)).ConvertAll<Row>(new Converter<BaseItem, Row>(Service.BaseToRow));
        }

        #region ITextual Members

        public string Value
        {
            get { return Service.GetText(this); }
            // throw new NotImplementedException();
            set { }
        }

        #endregion
       
    }

}
