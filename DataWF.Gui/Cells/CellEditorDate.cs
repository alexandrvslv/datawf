using System;
using Xwt;
using DataWF.Common;

namespace DataWF.Gui
{
    public class CellEditorDate : CellEditorText
    {
        protected bool twoDate = false;

        public CellEditorDate()
            : base()
        {
            //format = Localize.Data.Culture.DateTimeFormat.ShortDatePattern;
            Masked = true;
            string temp = Format;
            if (temp == null || temp.Length == 0)
                temp = Locale.Instance.Culture.DateTimeFormat.ShortDatePattern;
            if (temp.IndexOf("MM", StringComparison.OrdinalIgnoreCase) < 0)
                temp = temp.Replace("M", "MM");
            if (temp.IndexOf("dd", StringComparison.OrdinalIgnoreCase) < 0)
                temp = temp.Replace("d", "dd");
            temp = temp.Replace("MMM", "LLL").Replace('y', '0').Replace('M', '0').Replace('d', '0').Replace('.', '/').Replace("|", @"\|");
            temp = twoDate ? string.Format("{0}  {0}", temp) : temp;
        }

        public bool TwoDate
        {
            get { return twoDate; }
            set { twoDate = value; }
        }

        public IntervalSelector Selector
        {
            get { return DropDown?.Target as IntervalSelector; }
        }

        public override object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value == null || value.ToString().Length == 0)
                return null;
            if (twoDate && value is string)
                return DateInterval.Parse(value.ToString());
            if (!twoDate && value is DateInterval)
                return ((DateInterval)value).Min;
            return base.ParseValue(value, dataSource, valueType);
        }

        public override Widget InitDropDownContent()
        {
            var interval = Editor.GetCached<IntervalSelector>();
            interval.TwoDate = twoDate;
            interval.ValueChanged += PickerDateChanged;
            return interval;
        }

        protected override void OnTextKeyPressed(object sender, KeyEventArgs e)
        {
            base.OnTextKeyPressed(sender, e);
            //var box = sender as MaskedTextBox;
            //if (e.Key == Key.BackSpace || e.Key == Key.Delete)
            //{
            //    e.Handled = true;
            //}
            //var box = sender as TextEntry;
            //if (Keyboard.CurrentModifiers == ModifierKeys.None && !box.ReadOnly && box.SelectionLength > 1)
            //{
            //    int start = box.SelectionStart;
            //    string temp = string.Empty;
            //    for (int i = 0; i < box.SelectionLength; i++)
            //        temp += box.PromptChar;
            //    box.Text = string.Format("{0}{1}{2}",
            //        box.Text.Substring(0, box.SelectionStart),
            //        temp,
            //        box.Text.Substring(box.SelectionStart + box.SelectionLength));
            //    box.SelectionStart = start;
            //    box.SelectionLength = 0;
            //    //e.Handled
            //}
        }

        private void PickerKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Editor.DropDown.Hide();
            }
        }

        private void PickerDateChanged(object sender, EventArgs e)
        {
            if (HandleText)
            {
                HandleText = false;
                Value = twoDate ? Selector.Value : (object)Selector.Value.Min;
                EntryText = FormatValue(Value) as string;
                HandleText = true;
            }
        }

        public override object Value
        {
            get { return base.Value; }
            set
            {
                bool flag = HandleText;
                HandleText = false;
                if (twoDate)
                {
                    if (value is DateTime)
                        value = new DateInterval((DateTime)value);
                    //else if (value == null)
                    //    value = new DateInterval();
                    if (Selector != null && value is DateInterval)
                        Selector.Value = (DateInterval)value;
                }
                else
                {
                    DateTime val = DateTime.Now;
                    if (value is DateTime)
                        val = (DateTime)value;
                    if (Selector != null)
                        Selector.Value = new DateInterval(val);
                }
                HandleText = flag;
                base.Value = value;
            }
        }

        protected override object GetDropDownValue()
        {
            return twoDate ? Selector.Value : (object)Selector.Value.Min;
        }

        public override void FreeEditor()
        {
            Selector.ValueChanged -= PickerDateChanged;
            base.FreeEditor();
        }
    }
}

