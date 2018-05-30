using System;
using Xwt;

namespace DataWF.Gui
{
    public class ToolPasswordEntry : ToolContentItem
    {
        public ToolPasswordEntry() : base(new PasswordEntry())
        {
            FillHeight = true;
        }

        public PasswordEntry Entry
        {
            get { return content as PasswordEntry; }
        }

        public event EventHandler EntryTextChanged
        {
            add { Entry.Changed += value; }
            remove { Entry.Changed -= value; }
        }
    }
}
