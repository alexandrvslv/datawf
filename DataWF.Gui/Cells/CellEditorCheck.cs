using DataWF;
using System;
using Avalonia;
using Avalonia.Controls;

namespace DataWF.Gui
{
    public class CellEditorCheck : CellEditorText
    {
        public CellEditorCheck()
            : base()
        {
            DropDownVisible = false;
        }

        public bool ThreeState { get; set; }

        public object ValueTrue { get; set; }

        public object ValueFalse { get; set; }

        public object ValueNull { get; set; }

        protected override void OnTextChanged(object sender, EventArgs e)
        {
            if (HandleText)
                Value = ((CheckBox)sender).IsChecked;
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value == null)
            {
                return ValueNull;
            }
            else if (value is CheckedState checkedState)
            {
                if (checkedState == CheckedState.Checked)
                    return ValueTrue;
                else if (checkedState == CheckedState.Unchecked)
                    return ValueFalse;
                else
                    return ValueNull;
            }
            else if (value is bool boolValue)
            {
                return boolValue ? ValueTrue : ValueFalse;
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

        public override Control InitEditorContent()
        {
            var box = Editor.GetCached<CheckBox>();
            box.IsThreeState = ThreeState;
            if (!ReadOnly)
            {
                box.IsEnabled = true;
                box.IsCheckedChanged += OnTextChanged;
            }
            else
            {
                box.IsEnabled = false;
            }

            return box;
        }

        public override object Value
        {
            get => base.Value;
            set
            {
                bool flag = HandleText;
                HandleText = false;
                ((CheckBox)Editor.Control).IsChecked = (CheckedState)FormatValue(value) == CheckedState.Checked;
                HandleText = flag;
            }
        }

        public override void FreeEditor()
        {
            if (Editor?.Control is CheckBox checkBox)
                checkBox.IsCheckedChanged -= OnTextChanged;
            base.FreeEditor();
        }
    }

}

