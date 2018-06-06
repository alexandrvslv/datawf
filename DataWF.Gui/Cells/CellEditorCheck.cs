using System;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class CellEditorCheck : CellEditorText
    {
        private bool _treeState = false;

        public CellEditorCheck()
            : base()
        {
            DropDownVisible = false;
        }

        public bool TreeState { get; set; }

        public object ValueTrue { get; set; }

        public object ValueFalse { get; set; }

        public object ValueNull { get; set; }

        protected override void OnTextChanged(object sender, EventArgs e)
        {
            if (HandleText)
                Editor.Value = ParseValue(((CheckBox)sender).State, EditItem, DataType);
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value is CheckBoxState)
            {
                if (((CheckBoxState)value) == CheckBoxState.On)
                    return ValueTrue;
                else if (((CheckBoxState)value) == CheckBoxState.Off)
                    return ValueFalse;
                else
                    return ValueNull;
            }
            else if (value is CheckedState)
            {
                if (((CheckedState)value) == CheckedState.Checked)
                    return ValueTrue;
                else if (((CheckedState)value) == CheckedState.Unchecked)
                    return ValueFalse;
                else
                    return ValueNull;
            }
            else if (value is bool)
            {
                if (((bool)value))
                    return ValueTrue;
                else
                    return ValueFalse;
            }
            return value;
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            if (value == null)
            {
                return CheckedState.Indeterminate;
            }
            if (value is CheckedState)
            {
                return value;
            }
            if (ValueTrue == null || ValueFalse == null)
            {
                return CheckedState.Indeterminate;
            }
            else if (value.ToString() == ValueTrue.ToString())
            {
                return CheckedState.Checked;
            }
            else if (value.ToString() == ValueFalse.ToString())
            {
                return CheckedState.Unchecked;
            }
            else if (value is bool)
            {
                return (bool)value ? CheckedState.Checked : CheckedState.Unchecked;
            }
            else
            {
                return CheckedState.Indeterminate;
            }
        }

        public override Widget InitEditorContent()
        {
            var box = Editor.GetCached<CheckBox>();
            box.AllowMixed = _treeState;
            if (!ReadOnly)
            {
                box.Sensitive = true;
                box.Toggled += OnTextChanged;
            }
            else
            {
                box.Sensitive = false;
            }

            return box;
        }

        public override object Value
        {
            get { return base.Value; }
            set
            {
                bool flag = HandleText;
                HandleText = false;
                ((CheckBox)Editor.Widget).State = (CheckBoxState)(CheckedState)FormatValue(value);
                HandleText = flag;
            }
        }

        public override void FreeEditor()
        {
            if (Editor?.Widget is CheckBox)
                ((CheckBox)Editor.Widget).Toggled -= OnTextChanged;
            base.FreeEditor();
        }
    }

}

