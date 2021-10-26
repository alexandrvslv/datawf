using DataWF.Data;
using DataWF.Data.Gui;
using System;
using DataWF.Module.Common;

namespace DataWF.Module.FlowGui
{
    public class CellEditorLogTable : CellEditorDataTree
    {
        public CellEditorLogTable()
        {
            key = DataTreeKeys.TableGroup | DataTreeKeys.Table;
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            if (dataSource is DBLogItem)
                value = ((DBLogItem)dataSource).BaseTable;
            return base.FormatValue(value, dataSource, valueType);
        }

        public override object ParseValue(object value)
        {
            if (value is DBSchemaItem)
                value = ((DBSchemaItem)value).FullName;
            return base.ParseValue(value);
        }
    }
}
