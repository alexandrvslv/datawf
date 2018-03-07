using System;
using Xwt;

namespace DataWF.Gui
{
	public class ToolSearchEntry : ToolItem
	{
		public ToolSearchEntry() : base(new TextEntry())
		{
			DisplayStyle = ToolItemDisplayStyle.Content;
			FillWidth = true;
		}

		public ToolSearchEntry(EventHandler textChenged) : base(new TextEntry())
		{
			EntryTextChanged += textChenged;
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

		public override void Localize()
		{ }
	}
}
