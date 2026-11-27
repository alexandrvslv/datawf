using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Input;

namespace DataWF.Gui
{

    public class ToolItem : LayoutItem<ToolItem>, IGlyph, IText, IDisposable, ILocalizable
    {
        protected GlyphType glyph;
        protected Color glyphColor;
        protected Bitmap image;
        protected FormattedText text;
        protected ToolItemDisplayStyle displayStyle = ToolItemDisplayStyle.Image;
        protected Rect imageBound;
        protected Rect textBound;
        protected CellDisplayState state = CellDisplayState.Default;

        private bool checkOnClick;
        private bool check;
        private bool sensitive = true;
        private Toolsbar bar;
        private CellStyle style;
        private Point pressLocation;
        private int pressCount;

        public ToolItem()
        {
        }

        public ToolItem(EventHandler click) : this()
        {
            Click += click;
        }

        public event EventHandler Click;
        public event EventHandler DoubleClick;
        public event EventHandler<PointerEventArgs> MouseMove;
        public event EventHandler TextChanged;

        [Browsable(false)]
        public string StyleName { get; set; } = "Tool";

        [XmlIgnore]
        public CellStyle Style
        {
            get => style ??= GuiEnvironment.Theme[StyleName];
            set
            {
                style = value;
                StyleName = value?.Name;
            }
        }

        [XmlIgnore]
        public virtual Toolsbar Bar
        {
            get => bar;
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
                    }
                    bar = value;
                    if (bar != null)
                    {
                        foreach (var item in this)
                        {
                            item.Bar = value;
                        }
                        CheckSize();
                    }
                }
            }
        }

        public override void InsertInternal(int index, ToolItem item)
        {
            base.InsertInternal(index, item);
            item.Bar = bar;
        }

        public override void RemoveInternal(ToolItem item, int index)
        {
            base.RemoveInternal(item, index);
            item.Bar = null;
        }

        public override void Clear()
        {
            base.Clear();
            //bar?.Clear();
        }

        protected override void OnPropertyChanged(string property)
        {
            base.OnPropertyChanged(property);
            bar?.InvalidateArrange();
            bar?.InvalidateVisual();
        }

        public ToolItemDisplayStyle DisplayStyle
        {
            get => displayStyle;
            set
            {
                if (displayStyle != value)
                {
                    displayStyle = value;
                    CheckSize();
                }
            }
        }

        public override void Dispose()
        {
            Click = null;
            TextChanged = null;
            //text?.Dispose();
            base.Dispose();
        }

        public string Text
        {
            get => text?.Text;
            set
            {
                if (Text != value)
                {
                    text ??= new FormattedText()
                    {
                        Text = value,
                        TextWrapping = TextWrapping.NoWrap,
                        Typeface = Style.Typeface,
                    };
                    OnTextChanged(EventArgs.Empty);
                }
            }
        }

        [XmlIgnore]
        public Bitmap Image
        {
            get => image;
            set
            {
                if (image != value)
                {
                    image = value;
                    CheckSize();
                }
            }
        }

        public Color GlyphColor
        {
            get => glyphColor == CellStyleBrush.ColorEmpty ? (glyphColor = Style.FontBrush.Color) : glyphColor;
            set
            {
                if (GlyphColor == value)
                    return;
                glyphColor = value.BlendWith(Style.FontBrush.Color, 0.5);
                Bar?.InvalidateVisual();
            }
        }

        [XmlIgnore]
        public Typeface Typeface
        {
            get => text?.Typeface ?? Style.Typeface;
            set
            {
                text ??= new FormattedText()
                {
                    Text = Text,
                    TextWrapping = TextWrapping.NoWrap,
                    Typeface = value,
                };
                CheckSize();
            }
        }

        public GlyphType Glyph
        {
            get => glyph;
            set
            {
                if (glyph != value)
                {
                    glyph = value;
                    CheckSize();
                }
            }
        }

        [XmlIgnore]
        public virtual CellDisplayState State
        {
            get => state;
            set
            {
                if (state != value && sensitive)
                {
                    state = value;
                    Bar?.InvalidateVisual();
                }
            }
        }

        public bool Sensitive
        {
            get => sensitive;
            set
            {
                if (sensitive != value)
                {
                    sensitive = value;
                    Bar?.InvalidateVisual();
                }
            }
        }

        public virtual bool Checked
        {
            get => check;
            set
            {
                if (check != value)
                {
                    check = value;
                    Bar?.InvalidateVisual();
                }
            }
        }

        public bool CheckOnClick
        {
            get => checkOnClick;
            set
            {
                if (checkOnClick != value)
                {
                    checkOnClick = value;
                }
            }
        }

        public ToolItem Owner
        {
            get { return Bar?.Owner; }
        }

        [XmlIgnore]
        public Rect Bound { get; set; }

        public override void ApplyBound(Rect value)
        {
            base.ApplyBound(value);
            if (Count > 0)
            {
                return;
            }
            var halfIndent = Bar.Indent / 2D;
            value = value.Inflate(new Thickness(-halfIndent, -halfIndent));
            var imaged = DisplayStyle.HasFlag(ToolItemDisplayStyle.Image) && GetFormattedImage() != null;
            if (imaged)
            {
                imageBound = imageBound.WithX(value.X + (Bar.MinItemWidth - imageBound.Width) / 2D);
                imageBound = imageBound.WithY(value.Y + (value.Height - imageBound.Height) / 2D);
            }
            if (DisplayStyle.HasFlag(ToolItemDisplayStyle.Text))
            {
                textBound = textBound.WithX(imaged ? imageBound.Right : value.X + 4D);
                textBound = textBound.WithY(value.Y + (value.Height - textBound.Height) / 2D);
            }
            Bound = value;
            //Console.WriteLine($"ToolItem {Name} Bound:{value}");
        }

        public object GetFormattedImage()
        {
            return DisplayStyle.HasFlag(ToolItemDisplayStyle.Image)
                               ? image ?? (glyph != GlyphType.None ? (object)glyph : null)
                                   : null;
        }

        public virtual void Render(GraphContext context)
        {
            Style.Angle = Bar.ItemOrientation == BoxOrientation.Horizontal ? 0 : 90;

            var dstate = !Sensitive ? CellDisplayState.Pressed : state == CellDisplayState.Default && Checked ? CellDisplayState.Selected : state;
            context.DrawCell(Style, null, Bound, Bound, dstate);

            object formatted = GetFormattedImage();
            if (DisplayStyle.HasFlag(ToolItemDisplayStyle.Image))
            {
                if (image != null)
                {
                    context.DrawImage(image, imageBound);
                }
                else if (glyph != GlyphType.None)
                {
                    context.DrawGlyph(glyph, imageBound, GlyphColor);
                }
            }
            if (DisplayStyle.HasFlag(ToolItemDisplayStyle.Text) && !string.IsNullOrEmpty(Text))
            {
                context.DrawText(text, textBound, Style, state);
            }
        }

        protected virtual internal void CheckSize(bool queue = true)
        {
            if (Bar == null)
                return;
            imageBound = new Rect(default(Point), imageBound.Size);
            if (DisplayStyle.HasFlag(ToolItemDisplayStyle.Image))
                imageBound = new Rect(imageBound.Position, Image != null || Glyph != GlyphType.None ? new Size(Bar.MinItemHeight - 2, Bar.MinItemHeight - 2) : Size.Empty);
            else
                imageBound = new Rect(imageBound.Position, Size.Empty);

            textBound = new Rect(imageBound.Right, 0, textBound.Width, textBound.Height);
            if (text != null && DisplayStyle.HasFlag(ToolItemDisplayStyle.Text))
            {
                textBound = new Rect(textBound.Position, text.Bounds.Size);
                textBound = textBound.WithWidth(textBound.Width + 6);
            }
            else
            {
                textBound = textBound.WithWidth(0);
            }
            if (Bar.ItemOrientation == BoxOrientation.Vertical)
            {
                textBound = new Rect(0, imageBound.Bottom, textBound.Height, textBound.Width);
            }

            width = Math.Max(textBound.Right, Bar.MinItemWidth) + Bar.Indent;
            height = Math.Max(Math.Max(textBound.Height, textBound.Height), Bar.MinItemHeight) + Bar.Indent;
            if (Bar.ItemOrientation == BoxOrientation.Vertical)
            {
                InvertSize();
            }
            if (queue)
            {
                Bar?.InvalidateArrange();
            }
        }

        protected void InvertSize()
        {
            var temp = height;
            height = width;
            width = temp;
        }

        protected virtual internal void OnMouseMove(PointerEventArgs args)
        {
            MouseMove?.Invoke(this, args);
            if (State == CellDisplayState.Pressed)
            {
                var position = args.GetPosition(bar);
                var dX = pressLocation.X - position.X;
                var dY = pressLocation.Y - position.Y;
                if (Math.Max(Math.Abs(dX), Math.Abs(dY)) > 5)
                    OnMove();
            }
        }

        protected virtual void OnMove()
        {
            Bar?.OnItemMove(new ToolItemEventArgs { Item = this });
        }

        protected virtual internal void OnMouseEntered(EventArgs args)
        {
            State = CellDisplayState.Hover;
        }

        protected virtual internal void OnPointerExited(PointerEventArgs args)
        {
            State = CellDisplayState.Default;
        }

        protected virtual internal void OnPointerPressed(PointerPressedEventArgs args)
        {
            pressLocation = args.GetPosition(null);
            pressCount = args.ClickCount;
            State = CellDisplayState.Pressed;
        }

        protected virtual internal void OnPointerReleased(PointerReleasedEventArgs args)
        {
            State = CellDisplayState.Hover;
            if (Sensitive && args.MouseButton == MouseButton.Left)
            {
                if (pressCount == 1)
                {
                    OnClick(args);
                }
                else if (pressCount == 2)
                {
                    OnDoubleClick(args);
                }
            }
        }

        protected virtual void OnClick(EventArgs e)
        {
            if (checkOnClick)
                check = !check;

            Bar?.OnItemClick(this);
            Click?.Invoke(this, e);
        }

        protected virtual void OnDoubleClick(EventArgs e)
        {
            Bar?.OnItemDoubleClick(this);
            DoubleClick?.Invoke(this, e);
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
