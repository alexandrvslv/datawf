using System;
using Xwt;
using DataWF.Common;

namespace DataWF.Gui
{
    public class CellEditorPath : CellEditorText
    {
        private static FileDialog picker;

        public CellEditorPath() : base()
        {
            DropDownWindow = false;
            handleText = false;
        }

        private void OnDropDownClick(object sender, EventArgs e)
        {
            if (picker.Run(editor.ParentWindow))
            {
                editor.Value = picker.FileName;
                ((TextEntry)editor.Widget).Changed -= OnTextChanged;
                ((TextEntry)editor.Widget).Text = picker.FileName;
                ((TextEntry)editor.Widget).Changed += OnTextChanged;
            }
        }

        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            base.InitializeEditor(editor, value, dataSource);

            base.editor.DropDownVisible = true;
            base.editor.DropDownClick += OnDropDownClick;

            if (picker == null)
            {
                picker = new OpenFileDialog();
            }

            picker.InitialFileName = value == null ? null : value.ToString();
        }

        public override void FreeEditor()
        {
            editor.DropDownClick -= OnDropDownClick;
            base.FreeEditor();
        }
    }
}

