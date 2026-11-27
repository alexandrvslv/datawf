using System;
using Avalonia;
using Avalonia.Controls;

namespace DataWF.Gui
{
    public class ToolSearchEntry : ToolContentItem
    {
        public ToolSearchEntry() : base(new TextBox())
        {
            DisplayStyle = ToolItemDisplayStyle.Content;
            FillWidth = true;
        }

        public ToolSearchEntry(EventHandler<TextChangedEventArgs> textChenged) : base(new TextBox())
        {
            EntryTextChanged += textChenged;
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

        public override void Localize()
        { }
    }
}
