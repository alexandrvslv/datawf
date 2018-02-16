using System;

namespace DataWF.Gui
{
    public class LayoutSelectionEventArgs : EventArgs
    {
        private object item;
        public LayoutSelectionEventArgs(object item = null, LayoutSelectionChange type = LayoutSelectionChange.Add)
            : base()
        {
            Type = type;
            Value = item;
        }

        public LayoutSelectionMode Mode
        {
            get
            {
                var mode = LayoutSelectionMode.None;
                if (item is LayoutSelectionRow)
                    mode = LayoutSelectionMode.Row;
                else if (item is LayoutGroup)
                    mode = LayoutSelectionMode.Group;
                else if (item is LayoutColumn)
                    mode = LayoutSelectionMode.Column;
                else if (item is PSelectionAggregate)
                    mode = LayoutSelectionMode.Aggregate;
                return mode;
            }
        }

        public LayoutSelectionChange Type { get; set; }

        public object Value
        {
            get { return item; }
            set
            {
                if (item != value)
                {
                    item = value;
                }
            }
        }

        public override bool Equals(object obj)
        {
            var obj2 = obj as LayoutSelectionEventArgs;
            return obj2 != null && item == obj2.item;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
