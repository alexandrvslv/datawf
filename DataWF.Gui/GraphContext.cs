using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using DataWF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DataWF.Gui
{
    public class GraphContext : IDisposable
    {
        private const string DefaultFontName = "FontAwesome";
        private static FormattedText measure;
        private static FontFamily glyphFont = FontFamily.Default;
        private static Dictionary<GlyphType, TextFormatter> glyphCache = new Dictionary<GlyphType, TextFormatter>();
        //public static GraphContext Default = new GraphContext();

        public static FormattedText GetGlyphLayout(GlyphType type, double maxSize)
        {
            if (glyphFont == FontFamily.Default)
            {
                glyphFont = FontFamily.Parse(DefaultFontName);
                if (glyphFont.Name != DefaultFontName)
                {
                    string name = System.IO.Path.Combine(Helper.GetDirectory(), "fontawesome-webfont.ttf");
                    var glyphFont = new FontFamily(new Uri(name), DefaultFontName);
                    FontManager.Current.GetOrAddGlyphTypeface(new Typeface(glyphFont));
                }
                var fontLit = new FormattedText
                (
                    textToFormat: char.ConvertFromUtf32((int)GlyphType.GearAlias),
                    culture: CultureInfo.InvariantCulture,
                    flowDirection: FlowDirection.LeftToRight,
                    typeface: new Typeface(glyphFont),
                    emSize: 8,
                    foreground: null
                );
                var litSize = fontLit.BuildGeometry(new Point(0, 0));
                if (litSize.Bounds.Height >= 15)
                    fontLit.SetFontSize(8 * 0.9);
                else if (litSize.Bounds.Height <= 12)
                    fontLit.SetFontSize(8 * 1.15);
            }
            if (type == GlyphType.FolderOColumn || type == GlyphType.FolderOTable)
                type = GlyphType.FolderO;
            if (!glyphCache.TryGetValue(type, out var layout))
            {
                layout = new FormattedText
                (
                    typeface: new Typeface(glyphFont),
                    //TextAlignment = TextAlignment.Center,
                    Trimming = TextTrimming.Word,
                    Text = char.ConvertFromUtf32((int)type)
                );
                var temp = layout.GetSize();
                layout.Width = temp.Width + 3;
                layout.Height = temp.Height + 3;
                glyphCache[type] = layout;
            }
            return layout;
        }

        public static Size MeasureGlyph(GlyphType type, double maxSize)
        {
            if (type == GlyphType.None)
                return default;
            var text = GetGlyphLayout(type, maxSize);
            var factor = maxSize / text.Height;
            return new Size(text.Width * factor, text.Height * factor);
        }

        public static Size MeasureString(string text, CellStyle style, double w)
        {
            return MeasureString(text, style.FontFamily, w);
        }

        public static Size MeasureString(string text, FontFamily font, double w)
        {
            if (text == null)
                return default;
            if (text.Length < 2000)
            {
                measure ??= new FormattedText();
                lock (measure)
                {
                    measure.Width = w;
                    measure.FontFamily = font;
                    measure.Text = text;
                    return measure.GetSize();
                }
            }
            else
            {
                return MeasureStringDefault(text, font, w);
            }
        }

        public static Size MeasureStringDefault(string text, FontFamily font, double w)
        {
            //var length = s.Length * f.Size;
            var cw = w <= 0;
            var split = Regex.Split(text, "\n|\r\n");
            var countBySplit = 0;
            var mw = 0D;
            foreach (string ss in split)
            {
                countBySplit++;
                var lw = ss.Length * font.Size * (0.85F);

                if (lw > mw)
                    mw = lw;
                if (cw && mw > w)
                    w = mw;
                if (lw > w)
                    countBySplit += (int)(((lw * (0.9)) / w));
            }
            //length = countBySplit * f.Height;
            var length = countBySplit * font.Size;
            return new Size(mw < w ? mw : (float)w, length);
        }

        private double scale = 1f;
        public DrawingContext Context;
        private static FormattedText cacheFormattedText;

        public GraphContext(DrawingContext g)
        {
            Context = g;
        }

        GraphContext()
        { }

        public void Dispose()
        {
            //Context.Dispose();
        }

        public bool Print { get; set; }

        public double Scale
        {
            get => scale;
            set => scale = value;
        }

        public void DrawCheckBox(CheckedState formated, Rect bound, Color color)
        {
            DrawGlyph(formated == CheckedState.Checked ? GlyphType.CheckSquareO : formated == CheckedState.Unchecked ? GlyphType.SquareO : GlyphType.Square, bound, color);
        }

        public void DrawGlyph(GlyphType type, Rect bound, CellStyle style, CellDisplayState state = CellDisplayState.Default)
        {
            DrawGlyph(type, bound, style.FontBrush.GetColorByState(state));
        }

        public void DrawGlyph(GlyphType type, Rect bound, Color color)
        {
            var text = GetGlyphLayout(type, bound.Height);
            var otext = GlyphType.None;
            if (type == GlyphType.FolderOColumn)
            {
                otext = GlyphType.Columns;
            }
            else if (type == GlyphType.FolderOTable)
            {
                otext = GlyphType.Table;
            }
            var factor = Math.Min(bound.Width, bound.Height) / Math.Max(text.Width, text.Height);
            var diff = new Point(Math.Abs(bound.Width - ((text.Width - 3) * factor)) / 2D,
                                Math.Abs(bound.Height - ((text.Height - 3) * factor)) / 2D);
            Context.Save();
            Context.Translate(bound.Location.X + diff.X, bound.Location.Y + diff.Y);
            Context.Scale(factor, factor);
            Context.SetColor(color);
            Context.DrawText(text, Point.Zero);
            Context.Restore();
            if (otext != GlyphType.None)
            {
                var sbound = new Rect(bound.TopLeft + new Size(bound.Width * 0.3, bound.Height * 0.3), new Size(bound.Width * 0.6, bound.Height * 0.6));
                DrawGlyph(otext, sbound, color);
            }
        }

        public void DrawGroup(CellStyle style, LayoutGroup group, Rect bound, CellDisplayState state, Rect gliph)
        {
            var FormattedText = group.GetFormattedText();
            var text = new Rect(bound.X + 11, bound.Y + (bound.Height - group.TextSize.Height) / 2D, group.TextSize.Width, group.TextSize.Height);

            DrawCell(style, FormattedText, bound, text, state);

            DrawGlyph(group.IsExpand ? GlyphType.ChevronDown : GlyphType.ChevronRight, gliph, style, state);

            var pen = style.FontBrush.GetColorByState(state);
            if (pen != CellStyleBrush.ColorEmpty)
            {
                Context.DrawLine(pen,
                    new Point(bound.X + text.Width + 25, bound.Y + bound.Height / 2),
                    new Point(bound.Right - 30, bound.Y + bound.Height / 2));
            }
        }

        public void DrawCell(CellStyle style, object formated, Rect bound, Rect textBound, CellDisplayState state)
        {
            if (bound.Width <= 0)
                return;
            //Context.Save();
            //Context.Rect(bound);
            //Context.Clip();
            var backColor = style.BackBrush.GetColorByState(state);
            if (backColor != CellStyleBrush.ColorEmpty)
            {
                using (var path = GetRoundedRect(bound, style.Round))
                {
                    Context.SetColor(backColor);
                    var pattern = style.BackBrush.GetBrushByState(bound, state);
                    if (pattern != null)
                        Context.Brush = pattern;
                    Context.AppendPath(path);
                    Context.Fill();
                    if (pattern != null)
                    {
                        pattern.Dispose();
                    }
                }
            }

            if (formated != null)
            {
                if (formated is string str && str.Length > 0)
                {
                    DrawText(str, textBound, style, state);
                }
                else if (formated is FormattedText text)
                {
                    DrawText(text, textBound, style, state);
                }
                else if (formated is GlyphType glyph)
                {
                    DrawGlyph(glyph, textBound, style, state);
                }
                else if (formated is Bitmap bitmap)
                {
                    DrawImage(bitmap, textBound);
                }
                else if (formated is CheckedState checkedState)
                {
                    textBound = textBound.WithWidth(textBound.Height);
                    DrawCheckBox(checkedState, textBound, style.FontBrush.GetColorByState(state));
                }
            }

            var penColor = style.BorderBrush.GetColorByState(state);
            if (penColor != CellStyleBrush.ColorEmpty && style.LineWidth > 0)
            {
                Context.SetLineWidth(style.LineWidth);
                Context.SetColor(penColor);
                var pattern = style.BorderBrush.GetBrushByState(bound, state);
                if (pattern != null)
                    Context.Brush = pattern;
                var pen = style.BorderBrush.GetPenByState(state);
                using (var borderPath = GetRoundedRect(bound.Inflate(new Thickness(-0.5, -0.5)), style.Round))
                {
                    Context.AppendPath(borderPath);
                    Context.Stroke();
                }
            }
            //Context.Restore();
        }

        public void DrawImage(Bitmap image, Rect bound)
        {
            Context.DrawImage(image, bound, 1);
        }

        public void FillRectangle(Color color, Rect bound)
        {
            Context.SetColor(color);
            Context.FillRectangle(bound);
        }

        public void FillRectangle(CellStyle style, Rect bound, CellDisplayState state = CellDisplayState.Default)
        {
            var pattern = style.BackBrush.GetBrushByState(bound, state);
            if (pattern != null)
            {
                Context.FillRectangle(pattern, bound);
            }
        }

        public void DrawRectangle(Color color, Rect bound, double lineWidth = 2)
        {
            var brush = new SolidColorBrush(color);
            var pen = new Pen(brush, lineWidth);
            Context.DrawRectangle(pen, bound);
        }


        public void DrawRectangle(CellStyle style, Rect bound, CellDisplayState state = CellDisplayState.Default)
        {
            var pen = style.BorderBrush.GetPenByState(state, style.LineWidth);
            Context.DrawRectangle(pen, bound);
        }

        public void DrawText(string text, Rect bound, CellStyle style, CellDisplayState state = CellDisplayState.Default)
        {
            //var pattern = style.FontBrush.GetBrushByState(bound, state);
            //if (pattern != null)
            //    Context.Brush = pattern;

            DrawText(text, bound, style.Typeface, style.FontBrush.GetColorByState(state), style.TextAlignment);
        }

        public void DrawText(string text, Rect bound, Typeface font, Color color, TextAlignment alignment = TextAlignment.Left)
        {
            cacheFormattedText ??= new FormattedText(textToFormat: text, 
                culture:CultureInfo.InvariantCulture,
                flowDirection: FlowDirection.LeftToRight,
                typeface: font,
                emSize: 0,
                foreground: null);
            cacheFormattedText.TextAlignment = alignment;
            DrawText(cacheFormattedText, bound, color);
        }

        public void DrawText(FormattedText formattedText, Rect bound, CellStyle style, CellDisplayState state = CellDisplayState.Default)
        {
            if (formattedText.Ty != style.Typeface)
                formattedText.SetFontTypeface(style.Typeface);
            if (formattedText.TextAlignment != style.TextAlignment)
                formattedText.TextAlignment = style.TextAlignment;
            DrawText(formattedText, bound, style.FontBrush.GetBrushByState(bound, state));
        }

        public void DrawText(FormattedText formattedText, Rect bound, Color color)
        {
            DrawText(formattedText, bound, new SolidColorBrush(color));
        }

        public void DrawText(FormattedText formattedText, Rect bound, IBrush brush)
        {
            var textBound = formattedText.Width;
            formattedText.SetForegroundBrush(brush);
            if (textBound.Height != bound.Height)
                formattedText.Height = bound.Height;
            if (formattedText.Width != bound.Width)
                formattedText.Width = bound.Width;

            Context.DrawText(formattedText, bound.Position);
        }

        
    }
}

