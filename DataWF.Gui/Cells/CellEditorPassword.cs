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

        public override string EditorText
        {
            get { return (Editor?.Widget as PasswordEntry)?.Password; }
            set
            {
                bool flag = handleText;
                handleText = false;
                ((PasswordEntry)Editor.Widget).Password = value;
                handleText = flag;
            }
        }

        public override Widget InitEditorContent()
        {
            var box = editor.GetCached<PasswordEntry>();
            box.KeyPressed += OnTextKeyPressed;
            box.KeyReleased += OnTextKeyReleased;
            //box.Readonly = readOnly;
            if (!ReadOnly && handleText)
                box.Changed += OnTextChanged;
            return box;
        }

        public override void FreeEditor()
        {
            base.FreeEditor();
            var password = editor.Widget as PasswordEntry;
            password.KeyPressed -= OnTextKeyPressed;
            password.KeyReleased -= OnTextKeyReleased;
            password.Changed -= OnTextChanged;
        }
    }
}
