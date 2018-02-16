using System;
using Xwt;

namespace DataWF.Gui
{
    public class ToolSearchEntry : ToolItem
    {
        public ToolSearchEntry() : base(new TextEntry())
        {
            DisplayStyle = ToolItemDisplayStyle.None;
            FillWidth = true;
        }

        public TextEntry Entry
        {
            get { return content as TextEntry; }
        }

        public event EventHandler EntryTextChanged
        {
            add { Entry.Changed += value; }
            remove { Entry.Changed -= value; }
        }
    }
}
