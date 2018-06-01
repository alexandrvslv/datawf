using DataWF.Common;
using System.Xml.Serialization;

namespace DataWF.Gui
{
    public class ToolFieldEditor : ToolContentItem
    {

        public ToolFieldEditor() : base(new FieldEditor())
        {
            DisplayStyle = ToolItemDisplayStyle.Content;
        }

        public FieldEditor Field
        {
            get { return content as FieldEditor; }
        }

        [XmlIgnore]
        public ILayoutCellEditor Editor
        {
            get { return Field.CellEditor; }
            set
            {
                Field.CellEditor = value;
                CheckSize();
            }
        }

        [XmlIgnore]
        public object DataValue
        {
            get { return Field.DataValue; }
            set { Field.DataValue = value; }
        }
    }
}
