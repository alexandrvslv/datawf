using System;
using Avalonia;
using Avalonia.Controls;

namespace DataWF.Gui
{
    public class ToolTextEntry : ToolContentItem
    {
        public ToolTextEntry() : base(new TextBox())
        {
            FillWidth = true;
        }

        public TextBox Entry
        {
            get { return content as TextBox; }
        }

        public event EventHandler<TextChangedEventArgs> EntryTextChanged
        {
            add => Entry.TextChanged += value;
            remove => Entry.TextChanged -= value;
        }
    }
}
