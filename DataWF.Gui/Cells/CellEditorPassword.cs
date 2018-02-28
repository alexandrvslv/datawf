using System;
using Xwt;

namespace DataWF.Gui
{
    public class CellEditorPassword : CellEditorText
    {
        public CellEditorPassword()
        {
            DropDownWindow = false;
            DropDownVisible = false;
        }


        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            return "*******";
        }

        public override Widget InitEditorContent()
        {
            var box = editor.GetCacheControl<PasswordEntry>();
            box.KeyPressed += TextBoxKeyPress;
            box.KeyReleased += TextBoxKeyUp;
            //box.Readonly = readOnly;
            if (!ReadOnly && handleText)
                box.Changed += OnControlValueChanged;
            return box;
        }
    }
}
