using System;
using System.Security;
using Avalonia;
using Avalonia.Controls;

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

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value is string && valueType == typeof(SecureString))
                return (Editor?.Control as TextBox)?.SecurePassword;
            return base.ParseValue(value, dataSource, valueType);
        }

        public override string EntryText
        {
            get => (Editor?.Control as TextBox)?.Text;
            set
            {
                bool flag = HandleText;
                HandleText = false;
                ((TextBox)Editor.Control).Text = value;
                HandleText = flag;
            }
        }

        public override Control InitEditorContent()
        {
            var box = Editor.GetCached<TextBox>();
            box.KeyDown += OnTextKeyPressed;
            box.KeyUp += OnTextKeyReleased;
            //box.Readonly = readOnly;
            if (!ReadOnly && HandleText)
                box.TextInput += OnTextChanged;
            return box;
        }

        public override void FreeEditor()
        {
            var password = Editor.Control as TextBox;
            password.KeyDown -= OnTextKeyPressed;
            password.KeyUp -= OnTextKeyReleased;
            password.TextInput -= OnTextChanged;
            base.FreeEditor();
        }
    }
}
