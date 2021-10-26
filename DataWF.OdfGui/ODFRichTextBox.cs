using System;
using System.Linq;
using System.ComponentModel;
using Xwt.Drawing;
using Xwt;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Doc.Odf
{
	public class TextPropertiesView
	{
		public TextProperties Text;
		protected Font _font;

		public Font Font
		{
			get
			{
				if (_font == null && !string.IsNullOrEmpty(Text.FontName))
				{
					var fs = Xwt.Drawing.FontStyle.Normal;
					if (Text.FontStyle == FontStyles.Italic)
						fs = Xwt.Drawing.FontStyle.Italic;
					else if (Text.FontStyle == FontStyles.Oblique)
						fs = Xwt.Drawing.FontStyle.Oblique;

					var fw = Xwt.Drawing.FontWeight.Normal;
					if (Text.FontWeight != FontWheights.wNormal)
						fw = Xwt.Drawing.FontWeight.Bold;

					FontFace ff = Text.FontFace;
					Length l = new Length(Text.FontSize, 0, LengthType.None);
					double size = l.Data == 0 ? 8D : (double)l.Data;
					if (l.Type == LengthType.Inch) size = size * 96;
					else if (l.Type == LengthType.Millimeter) size = (size / 25.4) * 96;
					else if (l.Type == LengthType.Percent)
					{
						var property = Text.ParentProperty as TextProperties;
						while (property != null)
						{
							if (((Length)property.FontSize).Type != LengthType.Percent)
								break;
							else
								property = property.ParentProperty as TextProperties;
						}
						if (property != null)
							size = new Length(property.FontSize, 0, LengthType.None).Data * (l.Data / 100);
					}
					_font = Font.FromName(Text.FontName == "" ? "Arial" : ff.Family)
								.WithSize(size).WithStyle(fs).WithWeight(fw);
				}
				return _font;
			}
		}
	}

	public class ODFRichTextBox : Canvas
	{
		private const double width = 500;
		public class TextLayoutCache
		{
			public TextLayoutCache()
			{
				Layout = new TextLayout();
				Text = new StringBuilder();
				Width = width;
			}

			public TextLayoutCache(TextLayoutCache layout) : this()
			{
				Previus = layout;
			}

			TextLayoutCache Previus { get; set; }

			public TextLayout Layout { get; set; }

			public Point Location { get; set; }

			public Rectangle Bound { get; set; }

			public Color Color { get; set; }

			public Color BackColor { get; set; }

			public Font Font { get => Layout.Font; set => Layout.Font = value; }

			public Alignment Align { get => Layout.TextAlignment; set => Layout.TextAlignment = value; }

			public double Width { get => Layout.Width; set => Layout.Width = value; }

			public StringBuilder Text { get; set; }

			private List<TextAttribute> attributes = new List<TextAttribute>();
			public void AppendText(string text, Font font, Color foreColor, Color backColor)
			{
				if (text.Length == 0)
					return;
				var index = Text.Length;
				Text.Append(text);
				if (backColor != BackColor)
					attributes.Add(new BackgroundTextAttribute() { Color = backColor, StartIndex = index, Count = text.Length });
				if (foreColor != Color)
					attributes.Add(new ColorTextAttribute() { Color = foreColor, StartIndex = index, Count = text.Length });
				if (!Font.Equals(font))
					attributes.Add(new FontTextAttribute() { Font = font, StartIndex = index, Count = text.Length });
			}

			public void Refresh()
			{
				Layout.Text = Text.ToString();
				foreach (var attribute in attributes)
					Layout.AddAttribute(attribute);
				var size = Layout.GetSize();
				if (Previus != null)
				{
					if (Previus.Bound == Rectangle.Zero)
						Previus.Refresh();
					Location = new Point(0, Previus.Bound.Bottom + 1);
				}
				Bound = new Rectangle(Location, size);
				Debug.WriteLine($"Bound {Bound}");
			}
		}

		protected TextDocument _document;
		protected List<TextLayoutCache> displayCache = new List<TextLayoutCache>();
		private Alignment textAlign;
		private Font textFont;
		private Color textColor;
		private Color textBackColor;
		private ScrollAdjustment horizontal;
		private ScrollAdjustment vertical;

		public ODFRichTextBox()
		{
		}

		public void Initialize(TextDocument document)
		{
			_document = document;
			displayCache.Clear();
			foreach (BaseItem item in document.BodyText)
			{
				WriteElement(item, null);
			}
		}

		public void WriteElement(BaseItem element, TextLayoutCache layout)
		{
			if (element is IStyledElement)
				ApplyStyle(((IStyledElement)element).Style);
			if (element is Paragraph || element is TextHeader)
			{
				layout = new TextLayoutCache(displayCache.Count == 0 ? null : displayCache[displayCache.Count - 1])
				{
					Color = textColor,
					BackColor = textBackColor,
					Font = textFont,
					Align = textAlign,
				};
				displayCache.Add(layout);
			}
			if (element is ITextual && layout != null)
			{
				layout.AppendText(((ITextual)element).Value, textFont, textColor, textBackColor);
			}
			else if (element is DocumentElementCollection)
			{
				foreach (BaseItem e in (DocumentElementCollection)element)
					WriteElement(e, layout);
			}

			if (element is Paragraph || element is TextHeader)
			{
				layout.Refresh();
			}
		}

		public void ApplyStyle(BaseItem item)
		{
            if (item is TextStyle ts)
            {
                textFont = new TextPropertiesView { Text = ts.TextProperties }.Font ?? textFont;
                textColor = ts.TextProperties.FontColor.Length == 0 ? Colors.Black : Color.FromName(ts.TextProperties.FontColor);
                textBackColor = ts.TextProperties.FontBackgroundColor.Length == 0 ? Colors.Transparent : Color.FromName(ts.TextProperties.FontBackgroundColor);
            }
            if (item is ParagraphStyle ps)
            {
                textAlign = Alignment.Start;
                if (ps.ParagraphProperty.TextAlign == "center")
                    textAlign = Alignment.Center;
                if (ps.ParagraphProperty.TextAlign == "right")
                    textAlign = Alignment.End;
                if (!string.IsNullOrEmpty(ps.ParagraphProperty.MarginTop))
                {

                }
            }

            if (item is ListStyle)
			{
				//var ls = (ListStyle)bs;
			}
		}

		protected override bool SupportsCustomScrolling
		{
			get { return true; }
		}

		protected override void SetScrollAdjustments(ScrollAdjustment horizontal, ScrollAdjustment vertical)
		{
			base.SetScrollAdjustments(horizontal, vertical);
			this.horizontal = horizontal;
			horizontal.ValueChanged += (s, e) => { QueueDraw(); };
			this.vertical = vertical;
			vertical.ValueChanged += (s, e) => { QueueDraw(); };
		}

		protected override void OnReallocate()
		{
			base.OnReallocate();
			if (vertical != null)
			{
				var layout = displayCache.Count == 0 ? null : displayCache[displayCache.Count - 1];
				vertical.UpperValue = layout?.Bound.Bottom ?? 0;
			}
		}

		protected override void OnDraw(Context ctx, Rectangle dirtyRect)
		{
			base.OnDraw(ctx, dirtyRect);
			var area = new Rectangle(0, vertical?.Value ?? 0, dirtyRect.Width, dirtyRect.Height);
			foreach (var item in displayCache)
			{
				if (area.IntersectsWith(item.Bound))
				{
					if (item.BackColor != Colors.Transparent)
					{
						ctx.SetColor(item.BackColor);
						ctx.Rectangle(item.Bound);
						ctx.Fill();
					}
					ctx.SetColor(item.Color);
					ctx.DrawTextLayout(item.Layout, item.Location.X, item.Location.Y - area.Y);
				}
			}
		}


	}
}

