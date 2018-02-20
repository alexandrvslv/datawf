using DataWF.Data.Gui;
using System;
using DataWF.Module.Common;

namespace DataWF.Module.FlowGui
{
    public class CellEditorLogRow : CellEditorTable
    {
        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            if (dataSource is UserLog)
                value = ((UserLog)dataSource).TargetItem;
            return base.FormatValue(value, dataSource, valueType);
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            return base.ParseValue(value, dataSource, valueType);
        }
    }
}
