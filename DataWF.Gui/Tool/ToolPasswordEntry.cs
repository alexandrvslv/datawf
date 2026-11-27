using System;
using Avalonia;
using Avalonia.Controls;

namespace DataWF.Gui
{
    public class ToolPasswordEntry : ToolContentItem
    {
        public ToolPasswordEntry() : base(new TextBox { RevealPassword = true })
        {
            FillHeight = true;
        }

        public TextBox Entry
        {
            get { return content as TextBox; }
        }

        public event EventHandler EntryTextChanged
        {
            add => Entry.TextInput += value;
            remove => Entry.TextInput -= value;
        }
    }
}
