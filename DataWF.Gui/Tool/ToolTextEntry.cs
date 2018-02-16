using System;
using Xwt;

namespace DataWF.Gui
{
    public class ToolTextEntry : ToolItem
    {
        public ToolTextEntry()
            : base(new TextEntry())
        {
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
