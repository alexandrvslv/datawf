using System;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
	public class ToolItemWidget : Toolsbar
	{
		ToolItem tool;

		public ToolItemWidget(ToolItem tool)
		{
			Tool = tool;
		}

		public ToolItem Tool
		{
			get { return tool; }
			set
			{
				Items.Clear();
				tool = value;
				Items.Add(tool);
			}
		}
	}
}
