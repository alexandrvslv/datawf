using System;
using DataWF;
using Avalonia;
using Avalonia.Controls;

namespace DataWF.Gui
{
    public class CellEditorFields : CellEditorText
    {
        public CellEditorFields()
            : base()
        {
            HandleText = false;
        }

        public ListEditor ListEditor
        {
            get { return DropDown?.Target as ListEditor; }
        }

        public override IControl InitDropDownContent()
        {
            return Editor.GetCached<ListEditor>();
        }

        protected override object GetDropDownValue()
        {
            return ListEditor.DataSource;
        }

        public override object Value
        {
            get => base.Value;
            set
            {
                base.Value = value;
                ListEditor.DataSource = Value;
            }
        }

        public override void FreeEditor()
        {
            ListEditor.DataSource = null;
            base.FreeEditor();
        }
    }
}

