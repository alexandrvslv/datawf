using DataWF.Common;
using System;
using System.ComponentModel;
using Xwt.Drawing;
using Xwt;

namespace DataWF.Gui
{
    public class GroupBoxItem : LayoutItem<GroupBoxItem>, IComparable, IDisposable, IText
    {
        private Widget widget;
        private CellStyle styleHeader = null;
        private CellStyle style = null;
        private Rectangle rectExpand = new Rectangle();
        private Rectangle rectHeader = new Rectangle();
        private Rectangle rectGlyph = new Rectangle();
        private Rectangle rectText = new Rectangle();
        private bool expand = true;
        private bool autos = true;
        public int HeaderHeight = 23;
        private int dHeight = 100;
        public GlyphType Glyph = GlyphType.GearAlias;
        private string text;
        private GroupBox groupBox;

        public event EventHandler TextChanged;

        public GroupBoxItem()
        {
            Width = 200;
            Height = 200;
            styleHeader = GuiEnvironment.Theme["GroupBoxHeader"];
            style = GuiEnvironment.Theme["GroupBox"];
        }

        public GroupBoxItem(GroupBox groupBox)
        {
            GroupBox = groupBox;
        }

        public GroupBoxItem(params GroupBoxItem[] items)
        {
            AddRange(items);
        }

        public GroupBox GroupBox
        {
            get { return groupBox ?? Map?.GroupBox; }
            set { groupBox = value; }
        }

        public override void OnListChanged(ListChangedType type, int newIndex = -1, int oldIndex = -1, string property = null)
        {
            base.OnListChanged(type, newIndex, oldIndex, property);
            GroupBox?.QueueForReallocate();
        }

        public void CheckBounds()
        {
            var top = TopMap;
            if (top == null)
                return;

            var bound = top.GetBound(this);

            //if (!expand)
            //    bound.Height = HeaderHeight + 5;

            if (widget != null && Map != null)
            {
                if (widget.Visible)
                {
                    var rect = new Rectangle(bound.X + 5, bound.Y + HeaderHeight,
                        bound.Width - 10, bound.Height - (HeaderHeight + 5));
                    if (rect.Width < 1)
                        rect.Width = 1;
                    if (rect.Height < 1)
                        rect.Height = 1;

                    GroupBox.SetChildBounds(widget, rect);
                }
            }
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

        public override bool Visible
        {
            get => base.Visible;
            set
            {
                if (Visible != value)
                {
                    base.Visible = value;

                }
            }
        }

        public Widget Widget
        {
            get { return widget; }
            set
            {
                if (widget == value)
                    return;
                if (widget != null && GroupBox != null)
                {
                    GroupBox.RemoveChild(widget);
                }
                widget = value;
                if (widget != null)
                {
                    widget.Visible = Visible;
                    if (GroupBox != null)
                        GroupBox.AddChild(widget);
                }
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
                    if (widget != null)
                        widget.Visible = visible && expand;
                    if (Map != null && value && RadioGroup > -1)
                    {
                        foreach (var item in Map)
                        {
                            if (item != this && item.RadioGroup == RadioGroup)
                            {
                                item.Expand = false;
                            }
                        }
                    }
                    OnPropertyChanged(nameof(Expand));
                    GroupBox?.QueueDraw();
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
            rectExpand = new Rectangle(bound.Right - 30, bound.Y + 2, 16, 16);
            return rectExpand;
        }

        protected override void OnPropertyChanged(string property)
        {
            switch (property)
            {
                case nameof(Visible):
                    if (widget != null)
                        widget.Visible = visible && expand;
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

        [DefaultValue(-1)]
        public int RadioGroup { get; set; } = -1;

        public override void Dispose()
        {
            if (widget != null)
                widget.Dispose();
            base.Dispose();
        }

        public void Localize()
        {
            if (GroupBox != null)
                GuiService.Localize(this, GroupBox.Name, name);
            if (widget is ILocalizable)
                ((ILocalizable)widget).Localize();
        }
    }

}

