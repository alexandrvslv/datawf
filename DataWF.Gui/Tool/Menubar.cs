using System;
using System.Collections.Generic;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
	public class Menubar : Popover, ILocalizable
	{
		public Menubar() : base()
		{
			Content = Bar = new Toolsbar();
			Bar.ItemClick += OnItemClick;
			Bar.Items.GrowMode = LayoutGrowMode.Vertical;
		}

		public Menubar(params ToolItem[] items) : this()
		{
			Items.AddRange(items);
		}

		public Toolsbar Bar { get; set; }

		public ToolLayoutMap Items { get { return Bar.Items; } }

		public ToolItem this[string name]
		{
			get { return (ToolItem)Items[name]; }
		}

		public Menubar Owner { get; set; }

		public override string Name
		{
			get { return base.Name; }
			set
			{
				base.Name = value;
				Bar.Name = Name;
			}
		}

		public bool Visible { get; internal set; }

		public void Localize()
		{
			Bar.Localize();
		}

		public void Popup(Widget owner, Rectangle point)
		{
			if (owner?.ParentWindow != null)
			{
				Owner = owner.Container as Menubar;
			}
			//Location = owner?.ConvertToScreenCoordinates(point) ?? point;
			Show(Position.Bottom, owner, point);
			Visible = true;
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			Visible = false;
		}

		private void OnItemClick(object sender, ToolItemEventArgs e)
		{
			if (e.Item is ToolDropDown && ((ToolDropDown)e.Item).HasDropDown)
				return;
			Hide();
		}
	}
}
