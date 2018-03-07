using DataWF.Gui;
using Xwt;

namespace DataWF.Data.Gui
{
	public class ToolProgressBar : ToolItem
	{
		public ToolProgressBar() : base(new ProgressBar())
		{
			Visible = false;
			DisplayStyle = ToolItemDisplayStyle.Content;
			ProgressBar.Indeterminate = true;
		}

		public ProgressBar ProgressBar
		{
			get { return (ProgressBar)base.Content; }
		}

		public int Value
		{
			get { return (int)ProgressBar.Fraction * 100; }
			set { ProgressBar.Fraction = (double)value / 100D; }
		}

		public override void Localize()
		{ }

	}
}
