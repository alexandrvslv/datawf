using System;
using System.Linq;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Avalonia.Media;

namespace Doc.Odf
{
	public class TextPropertiesView
	{
		public TextProperties Text;
		protected FontFamily _font;

		public FontFamily FontFamily
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
					_font = FontFamily.FromName(Text.FontName == "" ? "Arial" : ff.Family)
								.WithSize(size).WithStyle(fs).WithWeight(fw);
				}
				return _font;
			}
		}
	}

	public class ODFRichTextBox : Canvas
	{
		private const double width = 500;
		public class FormattedTextCache
		{
			public FormattedTextCache()
			{
				Layout = new FormattedText();
				Text = new StringBuilder();
				Width = width;
			}

			public FormattedTextCache(FormattedTextCache layout) : this()
			{
				Previus = layout;
			}

			FormattedTextCache Previus { get; set; }

			public FormattedText Layout { get; set; }

			public Point Location { get; set; }

			public Rect Bound { get; set; }

			public Color Color { get; set; }

			public Color BackColor { get; set; }

			public FontFamily FontFamily { get => Layout.FontFamily; set => Layout.FontFamily = value; }

			public TextAlignment Align { get => Layout.TextAlignment; set => Layout.TextAlignment = value; }

			public double Width { get => Layout.Width; set => Layout.Width = value; }

			public StringBuilder Text { get; set; }

			private List<TextAttribute> attributes = new List<TextAttribute>();
			public void AppendText(string text, FontFamily font, Color foreColor, Color backColor)
			{
				if (text.Length == 0)
					return;
				var index = Text.Length;
				Text.Append(text);
				if (backColor != BackColor)
					attributes.Add(new BackgroundTextAttribute() { Color = backColor, StartIndex = index, Count = text.Length });
				if (foreColor != Color)
					attributes.Add(new ColorTextAttribute() { Color = foreColor, StartIndex = index, Count = text.Length });
				if (!FontFamily.Equals(font))
					attributes.Add(new FontTextAttribute() { FontFamily = font, StartIndex = index, Count = text.Length });
			}

			public void Refresh()
			{
				Layout.Text = Text.ToString();
				foreach (var attribute in attributes)
					Layout.AddAttribute(attribute);
				var size = Layout.GetSize();
				if (Previus != null)
				{
					if (Previus.Bound == Rect.Empty)
						Previus.Refresh();
					Location = new Point(0, Previus.Bound.Bottom + 1);
				}
				Bound = new Rect(Location, size);
				Debug.WriteLine($"Bound {Bound}");
			}
		}

		protected TextDocument _document;
		protected List<FormattedTextCache> displayCache = new List<FormattedTextCache>();
		private TextAlignment textAlign;
		private FontFamily textFont;
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

		public void WriteElement(BaseItem element, FormattedTextCache layout)
		{
			if (element is IStyledElement)
				ApplyStyle(((IStyledElement)element).Style);
			if (element is Paragraph || element is TextHeader)
			{
				layout = new FormattedTextCache(displayCache.Count == 0 ? null : displayCache[displayCache.Count - 1])
				{
					Color = textColor,
					BackColor = textBackColor,
					FontFamily = textFont,
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
                textFont = new TextPropertiesView { Text = ts.TextProperties }.FontFamily ?? textFont;
                textColor = ts.TextProperties.FontColor.Length == 0 ? Colors.Black : Color.FromName(ts.TextProperties.FontColor);
                textBackColor = ts.TextProperties.FontBackgroundColor.Length == 0 ? Colors.Transparent : Color.FromName(ts.TextProperties.FontBackgroundColor);
            }
            if (item is ParagraphStyle ps)
            {
                textAlign = TextAlignment.Start;
                if (ps.ParagraphProperty.TextAlign == "center")
                    textAlign = TextAlignment.Center;
                if (ps.ParagraphProperty.TextAlign == "right")
                    textAlign = TextAlignment.End;
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
			horizontal.ValueChanged += (s, e) => { InvalidateVisual(); };
			this.vertical = vertical;
			vertical.ValueChanged += (s, e) => { InvalidateVisual(); };
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

		protected override void OnDraw(Context ctx, Rect dirtyRect)
		{
			base.OnDraw(ctx, dirtyRect);
			var area = new Rect(0, vertical?.Value ?? 0, dirtyRect.Width, dirtyRect.Height);
			foreach (var item in displayCache)
			{
				if (area.IntersectsWith(item.Bound))
				{
					if (item.BackColor != Colors.Transparent)
					{
						ctx.SetColor(item.BackColor);
						ctx.Rect(item.Bound);
						ctx.Fill();
					}
					ctx.SetColor(item.Color);
					ctx.DrawFormattedText(item.Layout, item.Location.X, item.Location.Y - area.Y);
				}
			}
		}


	}
}

