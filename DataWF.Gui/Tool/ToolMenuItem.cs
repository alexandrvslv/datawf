using DataWF.Common;
using System;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{

	public class ToolMenuItem : ToolDropDown
	{

		public ToolMenuItem() : base()
		{
			DisplayStyle = ToolItemDisplayStyle.ImageAndText;
			indent = 0;
		}

		public ToolMenuItem(EventHandler click) : base(click)
		{
			DisplayStyle = ToolItemDisplayStyle.ImageAndText;
			indent = 0;
		}

		public ToolMenuItem Owner { get; set; }

		public Font Font { get; set; }

		public override void OnDraw(GraphContext context)
		{
			base.OnDraw(context);
		}

	}
}
