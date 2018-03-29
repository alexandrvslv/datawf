using DataWF.Common;
using System;
using System.ComponentModel;
using Xwt.Drawing;
using Xwt;

namespace DataWF.Gui
{
	public class GroupBoxItem : LayoutItem, IComparable, IDisposable, IText
	{
		private Widget control;
		private CellStyle styleHeader = null;
		private CellStyle style = null;
		private Rectangle rectExpand = new Rectangle();
		private Rectangle rectHeader = new Rectangle();
		private Rectangle rectGlyph = new Rectangle();
		private Rectangle rectText = new Rectangle();
		private bool expand = true;
		private bool autos = true;
		public int HeaderHeight = 21;
		private int dHeight = 100;
		public GlyphType Glyph = GlyphType.GearAlias;
		private string text;

		public event EventHandler TextChanged;

		public GroupBoxItem()
		{
            Width = 200;
            Height = 200;
			styleHeader = GuiEnvironment.StylesInfo["GroupBoxHeader"];
			style = GuiEnvironment.StylesInfo["GroupBox"];
		}

		public void CheckBounds()
		{
			var top = TopMap as GroupBoxMap;
			if (top == null)
				return;
			top.GetBound(this);
			var bound = base.bound;

			if (!expand)
				bound.Height = HeaderHeight + 5;

			if (control != null && GroupBoxMap != null)
			{
				if (control.Visible)
				{
					var rect = new Rectangle(bound.X + 5, bound.Y + HeaderHeight + 3,
						bound.Width - 10, bound.Height - (HeaderHeight + 6));
					if (rect.Width < 1)
						rect.Width = 1;
					if (rect.Height < 1)
						rect.Height = 1;

					GroupBox.SetChildBounds(control, rect);
				}
			}
		}

		public GroupBoxMap GroupBoxMap
		{
			get { return Map as GroupBoxMap; }
		}

		public GroupBox GroupBox
		{
			get { return GroupBoxMap?.GroupBox; }
		}

		public bool Autosize
		{
			get { return autos; }
			set
			{
				if (autos != value)
				{
					autos = value;
				}
			}
		}

		public Widget Widget
		{
			get { return control; }
			set
			{
				if (control == value)
					return;

				control = value;
				control.Visible = true;
				if (GroupBoxMap != null)
					GroupBoxMap.GroupBox.AddChild(control);
			}
		}

		public int DefaultHeight
		{
			get { return dHeight; }
			set { dHeight = value; }
		}

		[DefaultValue(true)]
		public bool Expand
		{
			get { return expand; }
			set
			{
				if (expand != value)
				{
					expand = value;
					if (control != null)
						control.Visible = visible && expand;
					OnPropertyChanged(nameof(Expand));
					//if (map != null)
					//    map.ResizeLayout();
				}
			}
		}

		public void Paint(GraphContext context)
		{
			context.DrawCell(style, null, bound, bound, CellDisplayState.Default);

			GetExpandBound(Bound);

			rectHeader = new Rectangle(Bound.X + 6, Bound.Y + 0, Bound.Width - 12, this.HeaderHeight);
			rectGlyph = new Rectangle(Bound.X + 10, Bound.Y + 3, 15, 15);
			rectText = new Rectangle(Bound.X + 30, Bound.Y + 3, Bound.Width - 40, rectHeader.Height - 5);

			context.DrawCell(styleHeader, Text, rectHeader, rectText, CellDisplayState.Default);
			context.DrawGlyph(styleHeader, rectGlyph, Glyph);
			context.DrawGlyph(styleHeader, rectExpand, Expand ? GlyphType.ChevronDown : GlyphType.ChevronRight);
		}

		public Rectangle GetExpandBound(Rectangle bound)
		{
			rectExpand = new Rectangle(bound.Right - 30, bound.Y + 4, 16, 16);
			return rectExpand;
		}

		protected override void OnPropertyChanged(string property)
		{
			switch (property)
			{
				case nameof(Visible):
					if (control != null)
						control.Visible = visible && expand;
					break;
				case nameof(Row):
				case nameof(Col):
					if (Map != null)
						Map.Sort();
					break;

			}
			base.OnPropertyChanged(property);
		}

		public override bool FillHeight
		{
			get { return base.FillHeight && expand; }
			set { base.FillHeight = value; }
		}

		public string Text
		{
			get => text;
			set
			{
				text = value;
				TextChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public void Dispose()
		{
			if (control != null)
				control.Dispose();
		}

		public void Localize()
		{
			if (GroupBox != null)
				GuiService.Localize(this, GroupBox.Name, name);
			if (control is ILocalizable)
				((ILocalizable)control).Localize();
		}
	}

}

