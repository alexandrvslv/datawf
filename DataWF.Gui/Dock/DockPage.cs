using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class DockPage : IContainerNotifyPropertyChanged
    {
        private GlyphType glyph;
        private bool hideOnClose;
        private bool active;
        private bool visible = true;
        private string name;
        private string label;
        private string toolText;
        private bool closing = false;
        private Rectangle bound = new Rectangle();
        public object Tag;
        private Widget widget;

        public DockPage()
        {

        }

        public Rectangle Bound
        {
            get { return bound; }
            set
            {
                bound = value;
                CheckBounds();
            }
        }

        public Rectangle BoundImage { get; set; }

        public Rectangle BoundText { get; set; }

        public Rectangle BoundClose { get; set; }

        public DockPageList List
        {
            get { return (DockPageList)Container; }
        }

        public DockPageBox Box
        {
            get { return List?.Box; }
        }

        public DockPanel Panel
        {
            get { return Box != null ? Box.Panel : null; }
        }

        public Widget Widget
        {
            get { return widget; }
            set
            {
                if (widget == value)
                    return;
                if (widget != null && widget is IText)
                    ((IText)widget).TextChanged -= ControlTextChanged;
                widget = value;
                if (widget != null)
                {
                    if (widget is IText)
                        ((IText)widget).TextChanged += ControlTextChanged;
                    Label = widget is IText
                        ? ((IText)widget).Text
                                           : string.IsNullOrEmpty(widget.TooltipText)
                                           ? widget.GetType().FullName
                                           : widget.TooltipText; ;
                    if (widget is IGlyph)
                    {
                        Image = ((IGlyph)widget).Image;
                        Glyph = ((IGlyph)widget).Glyph;
                    }
                }
                OnPropertyChanged(nameof(Widget));
            }
        }

        public bool Closing
        {
            get { return closing; }
        }

        public void Close()
        {
            if (!closing)
            {
                closing = true;
                if (Box != null)
                    Box.ClosePage(this);
                closing = false;
            }
        }

        public bool Active
        {
            get { return active; }
            set
            {
                if (active != value)
                {
                    active = value;
                    if (active)
                        UncheckExcept();
                    OnPropertyChanged(nameof(Active));
                }
            }
        }

        public Image Image { get; set; }

        public GlyphType Glyph
        {
            get { return glyph; }
            set
            {
                glyph = value;
                OnPropertyChanged(nameof(Glyph));
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Label
        {
            get { return label; }
            set
            {
                if (label != value)
                {
                    label = value;
                    toolText = value;
                    OnPropertyChanged(nameof(Label));
                }
            }
        }

        public string ToolText
        {
            get { return toolText; }
            set
            {
                if (toolText != value)
                {
                    toolText = value;
                    OnPropertyChanged(nameof(ToolText));
                }
            }
        }

        public bool Visible
        {
            get { return visible; }
            set
            {
                if (visible == value)
                {
                    visible = value;
                    OnPropertyChanged(nameof(Visible));
                }
            }
        }

        public bool HideOnClose
        {
            get { return hideOnClose; }
            set { hideOnClose = value; }
        }

        [XmlIgnore]
        public INotifyListChanged Container { get; set; }

        private void DockPageFormClosed(object sender, EventArgs e)
        {
            Close();
        }

        private void ControlTextChanged(object sender, EventArgs e)
        {
            Label = ((IText)widget).Text;
        }

        private void UncheckExcept()
        {
            foreach (DockPage item in List)
                if (item != this)
                    item.Active = false;
        }

        public void CheckBounds()
        {
            BoundImage = new Rectangle(Bound.X + 2,
                Bound.Y + 2,
                Box.VisibleImage && (Image != null || Glyph != GlyphType.None) ? 19 : 3,
                Box.VisibleImage && (Image != null || Glyph != GlyphType.None) ? 19 : 3);
            BoundText = new Rectangle(Bound.X + (Box.ItemOrientation == Orientation.Horizontal ? BoundImage.Width + 3 : 3),
                Bound.Y + (Box.ItemOrientation == Orientation.Horizontal ? 3 : BoundImage.Height + 4),
                Box.ItemOrientation == Orientation.Horizontal ? Bound.Width - (3 + BoundImage.Width + (Box.VisibleClose ? 15 : 0)) : 20,
                Box.ItemOrientation == Orientation.Horizontal ? 20 : Bound.Height - (3 + BoundImage.Height + (Box.VisibleClose ? 15 : 0)));
            BoundClose = new Rectangle(
                (Box.ItemOrientation == Orientation.Horizontal ? Bound.Right - 16 : Bound.X + 3),
                (Box.ItemOrientation == Orientation.Horizontal ? Bound.Y + 3 : Bound.Bottom - 16),
                14, 14);
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            Container?.OnPropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        internal void Draw(GraphContext context)
        {
            var state = this == Box.hover ? CellDisplayState.Hover : (Active ? CellDisplayState.Selected : CellDisplayState.Default);
            context.DrawCell(Box.PageStyle, Label, Bound, BoundText, state);
            if (Box.VisibleClose)
            {
                context.DrawCell(Box.CloseStyle, GlyphType.CloseAlias, BoundClose, BoundClose, Box.closeHover == this ? CellDisplayState.Selected : CellDisplayState.Default);
            }
            //image
            if (Box.VisibleImage)
            {
                if (Image != null)
                    context.DrawImage(Image, BoundImage);
                else if (Glyph != GlyphType.None)
                    context.DrawGlyph(Box.PageStyle, BoundImage, Glyph, state);
            }
        }
        #endregion
    }
}
