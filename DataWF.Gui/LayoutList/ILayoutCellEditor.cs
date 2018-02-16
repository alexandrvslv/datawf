using System;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public interface ILayoutCellEditor
    {
        LayoutEditor Editor { get; }

        Type DataType { get; set; }

        string Format { get; set; }

        object EditItem { get; set; }

        object Value { get; set; }

        bool ReadOnly { get; set; }

        object FormatValue(object value);

        object FormatValue(object value, object item, Type valueType);

        object ParseValue(object value);

        object ParseValue(object value, object item, Type valueType);

        void InitializeEditor(LayoutEditor editor, object value, object dataSource);

        void FreeEditor();

        void DrawCell(LayoutListDrawArgs e);
    }
}
