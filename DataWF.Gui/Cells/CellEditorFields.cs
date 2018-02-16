using System;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class CellEditorFields : CellEditorText
    {
        public CellEditorFields()
            : base()
        {
            HandleTextChanged = false;
        }

        public ListEditor ListEditor
        {
            get { return DropDown?.Target as ListEditor; }
        }

        public override Widget InitDropDownContent()
        {
            return editor.GetCacheControl<ListEditor>("ListEditor");
        }

        protected override object GetDropDownValue()
        {
            return ListEditor.DataSource;
        }

        public override object Value
        {
            get { return base.Value; }
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

