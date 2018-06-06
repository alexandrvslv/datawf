using System;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class CellEditorAccess : CellEditorText
    {
        AccessValue temp;

        public CellEditorAccess()
            : base()
        {
            DropDownVisible = true;
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            //ParseValue(obj, dataSource, ValueType);
            //if (temp != null)
            //     return temp.ToString();

            return base.FormatValue(value, dataSource, valueType);
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            temp = null;

            if (value is IAccessable)
                temp = ((IAccessable)value).Access;
            if (value is AccessValue)
                temp = (AccessValue)value;
            if (value is byte[])
            {
                temp = new AccessValue();
                temp.Read((byte[])value);
            }
            if (temp != null)
                return temp;

            return base.ParseValue(value, dataSource, valueType);
        }

        public override Widget InitDropDownContent()
        {
            var target = Editor.GetCached<AccessEditor>();
            target.Access = ParseValue(Value, null, DataType) as AccessValue;
            return target;
        }
    }
}

