using System;
using Xwt;

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
                Value = picker.FileName;
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

            picker.InitialFileName = value?.ToString();
        }

        public override void FreeEditor()
        {
            Editor.DropDownClick -= OnDropDownClick;
            base.FreeEditor();
        }
    }
}

