using DataWF.Common;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Gui
{
	public class GroupBoxMap : LayoutMap
	{
		public GroupBoxMap()
		{ }

		public GroupBoxMap(GroupBox groupBox)
		{
			GroupBox = groupBox;
		}

		public GroupBoxMap(params ILayoutItem[] items)
		{
			AddRange(items);
		}

		public GroupBox GroupBox { get; set; }

		protected override void OnItemsListChanged(object sender, ListChangedEventArgs e)
		{
			base.OnItemsListChanged(sender, e);
			GroupBox?.ResizeLayout();
		}
	}
}

