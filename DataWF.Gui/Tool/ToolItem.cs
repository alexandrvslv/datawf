using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class ToolItem : LayoutItem<ToolItem>, IGlyph, IText, IDisposable, IToolItem, ILocalizable
    {
        protected CellStyle style;
        protected GlyphType glyph;
        protected Image image;
        protected TextLayout text;
        protected Widget content;
        protected ToolItemDisplayStyle displayStyle = ToolItemDisplayStyle.Image;
        protected Rectangle imageBound;
        protected Rectangle textBound;
        protected Rectangle contentBound;
        protected CellDisplayState state = CellDisplayState.Default;

        protected double indent = 5D;
        private bool checkOnClick;
        private bool check;
        private bool sensitive = true;
        private Toolsbar bar;

        public ToolItem()
        {
            var baseColor = Colors.Silver;
            style = GuiEnvironment.Theme["Tool"];
        }

        public ToolItem(EventHandler click) : this()
        {
            Click += click;
        }

        public ToolItem(Widget content) : this()
        {
            Content = content;
        }

        [XmlIgnore]
        public Toolsbar Bar
        {
            get { return bar; }
            set
            {
                if (bar != value)
                {
                    if (bar != null)
                    {
                        foreach (var item in this)
                        {
                            item.Bar = null;
                        }
                        if (Content != null)
                            bar.RemoveChild(Content);
                    }
                    bar = value;
                    if (bar != null)
                    {
                        foreach (var item in this)
                        {
                            item.Bar = value;
                        }
                        if (Content != null)
                            bar.AddChild(Content);
                    }
                }
            }
        }

        public override void OnListChanged(ListChangedType type, int newIndex = -1, int oldIndex = -1, string property = null)
        {
            base.OnListChanged(type, newIndex, oldIndex, property);
            if (type == ListChangedType.ItemAdded)
            {
                var toolItem = items[newIndex] as IToolItem;
                if (toolItem != null)
                {
                    toolItem.Bar = bar;
                }
            }
            else if (type == ListChangedType.ItemDeleted && newIndex >= 0)
            {
                var toolItem = items[newIndex] as IToolItem;
                if (toolItem != null)
                {
                    toolItem.Bar = null;
                }
            }
            //else if (e.ListChangedType == ListChangedType.Reset)
            //{
            //    if (items.Count == 0)
            //        bar.Clear();
            //}
            else if (type == ListChangedType.ItemChanged)
            {
                bar.QueueForReallocate();
                bar.QueueDraw();
            }
        }

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                if (content != null)
                    content.Visible = value;
                base.Visible = value;
            }
        }

        public Widget Content
        {
            get { return content; }
            set
            {
                if (content != value)
                {
                    if (Bar != null && content != null)
                        Bar.RemoveChild(content);
                    content = value;
                    if (Bar != null && content != null)
                        Bar.AddChild(content);
                    CheckSize();
                }
            }
        }

        public ToolItemDisplayStyle DisplayStyle
        {
            get { return displayStyle; }
            set
            {
                if (displayStyle != value)
                {
                    displayStyle = value;
                    CheckSize();
                }
            }
        }

        public virtual void Dispose()
        {
            text.Dispose();
            if (content != null)
                Dispose();
        }

        public string Text
        {
            get { return text?.Text; }
            set
            {
                if (Text != value)
                {
                    if (text == null)
                    {
                        text = new TextLayout()
                        {
                            Font = style.Font,
                            Trimming = TextTrimming.WordElipsis
                        };
                    }
                    text.Text = value;
                    text.Height = MinHeight;
                    text.Width = -1;
                    OnTextChanged(EventArgs.Empty);
                }
            }
        }

        public Image Image
        {
            get { return image; }
            set
            {
                if (image != value)
                {
                    image = value;
                    CheckSize();
                }
            }
        }

        public Color ForeColor
        {
            get { return style.FontBrush.Color; }
            set
            {
                style = GuiEnvironment.Theme["Tool"] == style ? style.Clone() : style;
                style.FontBrush.Color = value.BlendWith(style.FontBrush.Color, 0.6);
                Bar?.QueueDraw();
            }
        }

        public Font Font
        {
            get { return text?.Font ?? style.Font; }
            set
            {
                if (text == null)
                {
                    text = new TextLayout() { Trimming = TextTrimming.WordElipsis };
                }
                text.Font = value;
                CheckSize();
            }
        }

        public GlyphType Glyph
        {
            get { return glyph; }
            set
            {
                if (glyph != value)
                {
                    glyph = value;
                    CheckSize();
                }
            }
        }

        public CellDisplayState State
        {
            get { return state; }
            set
            {
                if (state != value && sensitive)
                {
                    state = value;
                    Bar?.QueueDraw();
                }
            }
        }

        public bool Sensitive
        {
            get { return sensitive; }
            set
            {
                if (sensitive != value)
                {
                    sensitive = value;
                    Bar?.QueueDraw();
                }
            }
        }

        public bool Checked
        {
            get { return check; }
            set
            {
                if (check != value)
                {
                    check = value;
                    Bar?.QueueDraw();
                }
            }
        }

        public bool CheckOnClick
        {
            get { return checkOnClick; }
            set
            {
                if (checkOnClick != value)
                {
                    checkOnClick = value;
                }
            }
        }

        public double MinHeight { get; set; } = 28;

        public double MinWidth { get; set; } = 28;



        public ToolItem Owner
        {
            get { return Bar?.Owner; }
        }

        public override Rectangle Bound
        {
            get { return base.Bound; }
            set
            {
                var halfIndent = indent / 2D;
                value = value.Inflate(-halfIndent, -halfIndent);
                var imaged = DisplayStyle.HasFlag(ToolItemDisplayStyle.Image) && GetFormattedImage() != null;
                if (imaged)
                {
                    imageBound.X = value.X + (MinWidth - imageBound.Width) / 2D;
                    imageBound.Y = value.Y + (value.Height - imageBound.Height) / 2D;
                }
                if (DisplayStyle.HasFlag(ToolItemDisplayStyle.Text))
                {
                    textBound.X = imaged ? imageBound.Right : value.X + 2D;
                    textBound.Y = value.Y + (value.Height - textBound.Height) / 2D;
                }
                contentBound.X = DisplayStyle.HasFlag(ToolItemDisplayStyle.Text)
                    ? textBound.Right
                    : imaged
                        ? imageBound.Right
                        : value.X + 1;
                contentBound.Y = value.Y + (value.Height - contentBound.Height) / 2D;
                contentBound.Width = value.Width - (contentBound.Left - value.Left);
                base.Bound = value;
                //Console.WriteLine($"ToolItem {Name} Bound:{value}");
            }
        }

        internal Rectangle GetContentBound()
        {
            return contentBound;
        }

        public object GetFormattedImage()
        {
            return DisplayStyle.HasFlag(ToolItemDisplayStyle.Image)
                               ? image ?? (glyph != GlyphType.None ? (object)glyph : null)
                                   : null;
        }

        public virtual void OnDraw(GraphContext context)
        {
            object formatted = GetFormattedImage();
            var dstate = !Sensitive ? CellDisplayState.Pressed : state == CellDisplayState.Default && Checked ? CellDisplayState.Selected : state;
            context.DrawCell(style, formatted, Bound, imageBound, dstate);

            if (DisplayStyle.HasFlag(ToolItemDisplayStyle.Text) && !string.IsNullOrEmpty(Text))
            {
                context.DrawText(style, text, textBound, state);
            }
        }

        protected internal void CheckSize(bool queue = true)
        {
            imageBound.Location = Point.Zero;
            if (DisplayStyle.HasFlag(ToolItemDisplayStyle.Image))
                imageBound.Size = Image != null || Glyph != GlyphType.None ? new Size(MinHeight - 2, MinHeight - 2) : Size.Zero;
            else
                imageBound.Size = Size.Zero;

            textBound.Location = new Point(imageBound.Right, 0);
            if (text != null)
            {
                textBound.Size = text.GetSize();
                textBound.Width += 6;
            }
            else
            {
                textBound.Width = 0;
            }
            contentBound.X = (DisplayStyle.HasFlag(ToolItemDisplayStyle.Text) ? textBound.Right : imageBound.Right);
            contentBound.Y = 0;
            contentBound.Height = MinHeight;
            if (content != null)
            {
                contentBound.Size = content.Surface.GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
                contentBound.Width += indent;
            }
            else
            {
                contentBound.Width = 0;
            }
            width = Math.Max(contentBound.Right, MinWidth) + indent;
            height = Math.Max(Math.Max(textBound.Height, contentBound.Height), MinHeight) + indent;
            if (queue)
            {
                Bar?.QueueForReallocate();
            }
        }

        protected virtual internal void OnMouseEntered(EventArgs args)
        {
            State = CellDisplayState.Hover;
        }

        protected virtual internal void OnMouseExited(EventArgs args)
        {
            State = CellDisplayState.Default;
        }

        protected virtual internal void OnButtonPressed(ButtonEventArgs args)
        {
            State = CellDisplayState.Pressed;
        }

        protected virtual internal void OnButtonReleased(ButtonEventArgs args)
        {
            State = CellDisplayState.Hover;
            if (args.MultiplePress <= 1 && Sensitive)
            {
                OnClick(EventArgs.Empty);
            }
        }

        public event EventHandler Click;

        public event EventHandler TextChanged;

        protected virtual void OnClick(EventArgs e)
        {
            if (checkOnClick)
                check = !check;

            Bar?.OnItemClick(this);
            Click?.Invoke(this, e);
        }

        protected virtual void OnTextChanged(EventArgs e)
        {
            CheckSize();
            TextChanged?.Invoke(this, e);
        }

        public virtual void Localize()
        {
            if (Bar != null)
            {
                GuiService.Localize(this, Bar.Name, Name);
            }
        }
    }

    [Flags]
    public enum ToolItemDisplayStyle
    {
        Content = 0,
        Text = 1,
        Image = 2,
        ImageAndText = Text | Image
    }
}
