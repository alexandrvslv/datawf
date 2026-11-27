using DataWF;
using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia;

namespace DataWF.Gui
{
    public class GroupBoxItem : LayoutItem<GroupBoxItem>, IComparable, IDisposable, IText
    {
        private IControl control;
        private Rect rectExpand = new Rect();
        private Rect rectHeader = new Rect();
        private Rect rectGlyph = new Rect();
        private Rect rectText = new Rect();
        private bool autos = true;
        public int HeaderHeight = 23;
        private int dHeight = 100;
        public GlyphType Glyph = GlyphType.GearAlias;
        private string text;
        private GroupBox groupBox;
        private CellStyle styleHeader;
        private CellStyle style;

        public event EventHandler TextChanged;

        public GroupBoxItem()
        {
            Width = 200;
            Height = 200;
        }

        public GroupBoxItem(GroupBox groupBox)
        {
            GroupBox = groupBox;
        }

        public GroupBoxItem(params GroupBoxItem[] items)
        {
            AddRange(items);
        }

        public string StyleHeaderName { get; set; } = "GroupBoxHeader";
        public string StyleName { get; set; } = "GroupBox";

        public CellStyle StyleHeader
        {
            get => styleHeader ??= GuiEnvironment.Theme[StyleHeaderName];
            set
            {
                styleHeader = value;
                StyleHeaderName = value?.Name;
            }
        }

        public CellStyle Style
        {
            get => style ??= GuiEnvironment.Theme[StyleName];
            set
            {
                style = value;
                StyleName = value?.Name;
            }
        }

        public GroupBox GroupBox
        {
            get => groupBox ?? Map?.GroupBox;
            set => groupBox = value;
        }

        protected override void OnPropertyChanged(string property)
        {
            base.OnPropertyChanged(property);
            GroupBox?.InvalidateArrange();
        }

        public override void ApplyBound(Rect bound)
        {
            var top = TopMap;
            if (top == null)
                return;

            Bound = bound;
            //if (!expand)
            //    bound.Height = HeaderHeight + 5;

            if (Control != null && Map != null)
            {
                if (Control.IsVisible)
                {
                    var rect = new Rect(bound.X + 5, bound.Y + HeaderHeight,
                        bound.Width - 10, bound.Height - (HeaderHeight + 5));
                    if (rect.Width < 1)
                        rect = rect.WithWidth(1);
                    if (rect.Height < 1)
                        rect = rect.WithHeight(1);

                    Control.Arrange(rect);
                }
            }
        }


        public bool Autosize
        {
            get => autos;
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
                    if (Control != null)
                        Control.IsVisible = Visible && Expand;
                }
            }
        }

        public IControl Control
        {
            get => control;
            set
            {
                if (control == value)
                    return;
                if (control != null && GroupBox != null)
                {
                    GroupBox.Children.Remove(control);
                }
                control = value;
                if (control != null)
                {
                    control.IsVisible = Visible;
                    if (GroupBox != null)
                        GroupBox.Children.Add(control);
                }
            }
        }

        public int DefaultHeight
        {
            get => dHeight;
            set => dHeight = value;
        }

        public override bool Expand
        {
            get => base.Expand;
            set
            {
                if (Expand != value)
                {
                    if (Control != null)
                    {
                        Control.IsVisible = Visible && value;
                    }

                    if (value && Map != null && value && RadioGroup > -1)
                    {
                        foreach (var item in Map)
                        {
                            if (item != this && item.RadioGroup == RadioGroup)
                            {
                                item.Expand = false;
                            }
                        }
                    }
                    base.Expand = value;
                    GroupBox?.InvalidateVisual();
                }
            }
        }

        public void Paint(GraphContext context)
        {
            context.DrawCell(Style, null, Bound, Bound, CellDisplayState.Selected);

            GetExpandBound(Bound);

            rectHeader = new Rect(Bound.X + 3, Bound.Y, Bound.Width - 3, HeaderHeight);
            rectGlyph = new Rect(Bound.X + 10, Bound.Y + 3, 15, 15);
            rectText = new Rect(Bound.X + 30, Bound.Y + 3, Bound.Width - 40, rectHeader.Height - 5);

            context.DrawCell(StyleHeader, Text, rectHeader, rectText, CellDisplayState.Default);
            context.DrawGlyph(Glyph, rectGlyph, StyleHeader);
            context.DrawGlyph(Expand ? GlyphType.ChevronDown : GlyphType.ChevronRight, rectExpand, StyleHeader);
        }

        public Rect GetExpandBound(Rect bound)
        {
            rectExpand = new Rect(bound.Right - 30, bound.Y + 2, 16, 16);
            return rectExpand;
        }

        //protected override void OnPropertyChanged(string property)
        //{
        //    switch (property)
        //    {
        //        case nameof(IsVisible):

        //            break;
        //        case nameof(Row):
        //        case nameof(Col):
        //            if (Map != null)
        //                Map.Sort();
        //            break;

        //    }
        //    base.OnPropertyChanged(property);
        //}

        public override bool FillHeight
        {
            get => base.FillHeight && Expand;
            set => base.FillHeight = value;
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
        public Rect Bound { get; private set; }

        public override void Dispose()
        {
            TextChanged = null;
            Control = null;
            base.Dispose();
        }

        public void Localize()
        {
            if (GroupBox != null)
                GuiService.Localize(this, GroupBox.Name, name);
            if (Control is ILocalizable)
                ((ILocalizable)Control).Localize();
        }
    }

}

