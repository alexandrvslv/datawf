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
            HandleText = false;
        }

        private void OnDropDownClick(object sender, EventArgs e)
        {
            if (picker.Run(Editor.ParentWindow))
            {
                Editor.Value = picker.FileName;
                ((TextEntry)Editor.Widget).Changed -= OnTextChanged;
                ((TextEntry)Editor.Widget).Text = picker.FileName;
                ((TextEntry)Editor.Widget).Changed += OnTextChanged;
            }
        }

        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            base.InitializeEditor(editor, value, dataSource);

            Editor.DropDownVisible = true;
            Editor.DropDownClick += OnDropDownClick;

            if (picker == null)
            {
                picker = new OpenFileDialog();
            }

            picker.InitialFileName = value == null ? null : value.ToString();
        }

        public override void FreeEditor()
        {
            Editor.DropDownClick -= OnDropDownClick;
            base.FreeEditor();
        }
    }
}

