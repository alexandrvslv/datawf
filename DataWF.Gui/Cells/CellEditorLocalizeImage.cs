using DataWF.Common;
using System;


namespace DataWF.Gui
{
    public class CellEditorLocalizeImage : CellEditorList
    {
        public CellEditorLocalizeImage()
        {
            listSource = Locale.Data.Images;
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            if (value is string)
                return Locale.GetImage((string)value);
            return base.FormatValue(value, dataSource, valueType);
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value is LImage)
                value = ((LImage)value).Key;
            return base.ParseValue(value, dataSource, valueType);
        }
    }

}
