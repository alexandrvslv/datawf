using DataWF.Common;
using System;
using System.Text.RegularExpressions;
using Xwt.Drawing;
using Xwt;
using System.Collections.Generic;

namespace DataWF.Gui
{
    public class GraphContext
    {
        private static TextLayout measure;
        private static Font glyphFont = null;
        private static Dictionary<GlyphType, TextLayout> glyphCache = new Dictionary<GlyphType, TextLayout>();
        public static GraphContext Default = new GraphContext();

        public static TextLayout GetGlyphLayout(GlyphType type, double maxSize)
        {
            if (glyphFont == null)
            {
                glyphFont = Font.FromName("FontAwesome");
                if (glyphFont.Family != "FontAwesome")
                {
                    string name = System.IO.Path.Combine(Helper.GetDirectory(), "fontawesome-webfont.ttf");
                    if (Font.RegisterFontFromFile(name))
                    {
                        glyphFont = Font.FromName("FontAwesome");
                    }
                }
                var fontLit = new TextLayout() { TextAlignment = Alignment.Center, Trimming = TextTrimming.Word, Font = glyphFont };
                fontLit.Text = char.ConvertFromUtf32((int)GlyphType.GearAlias);
                var litSize = fontLit.GetSize();
                if (litSize.Height >= 15)
                    glyphFont = glyphFont.WithSize(glyphFont.Size * 0.9);
                else if (litSize.Height <= 12)
                    glyphFont = glyphFont.WithSize(glyphFont.Size * 1.15);
            }
            if (type == GlyphType.FolderOColumn || type == GlyphType.FolderOTable)
                type = GlyphType.FolderO;
            if (!glyphCache.TryGetValue(type, out var layout))
            {
                layout = new TextLayout()
                {
                    Font = glyphFont,
                    //TextAlignment = Alignment.Center,
                    Trimming = TextTrimming.Word,
                    Text = char.ConvertFromUtf32((int)type)
                };
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
                return Size.Zero;
            var text = GetGlyphLayout(type, maxSize);
            var factor = maxSize / text.Height;
            return new Size(text.Width * factor, text.Height * factor);
        }

        public static Size MeasureString(string text, CellStyle style, double w)
        {
            return MeasureString(text, style.Font, w);
        }

        public static Size MeasureString(string text, Font font, double w)
        {
            if (text.Length < 2000)
            {
                if (measure == null)
                {
                    measure = new TextLayout();
                }
                lock (measure)
                {
                    measure.Width = w;
                    measure.Font = font;
                    measure.Text = text;
                    return measure.GetSize();
                }
            }
            else
            {
                return MeasureStringDefault(text, font, w);
            }
        }

        public static Size MeasureStringDefault(string text, Font font, double w)
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
        public Context Context;
        private TextLayout cacheTextLayout;

        public GraphContext(Context g)
        {
            Context = g;
        }

        GraphContext()
        { }

        public void Dispose()
        {
            Context.Dispose();
        }

        public bool Print { get; set; }

        public double Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        public void DrawCheckBox(Color color, Rectangle bound, CheckedState formated)
        {
            DrawGlyph(color, bound, formated == CheckedState.Checked ? GlyphType.CheckSquareO : formated == CheckedState.Unchecked ? GlyphType.SquareO : GlyphType.Square);
        }

        public void DrawGlyph(CellStyle style, Rectangle bound, GlyphType type, CellDisplayState state = CellDisplayState.Default)
        {
            DrawGlyph(style.FontBrush.GetColorByState(state), bound, type);
        }

        public void DrawGlyph(Color color, Rectangle bound, GlyphType type)
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
            Context.DrawTextLayout(text, Point.Zero);
            Context.Restore();
            if (otext != GlyphType.None)
            {
                var sbound = new Rectangle(bound.TopLeft + new Size(bound.Width * 0.3, bound.Height * 0.3), new Size(bound.Width * 0.6, bound.Height * 0.6));
                DrawGlyph(color, sbound, otext);
            }
        }

        public void DrawGroup(CellStyle style, LayoutGroup group, Rectangle bound, CellDisplayState state, Rectangle gliph)
        {
            var textLayout = group.GetTextLayout();
            var text = new Rectangle(bound.X + 11, bound.Y + (bound.Height - group.TextSize.Height) / 2D, group.TextSize.Width, group.TextSize.Height);

            DrawCell(style, textLayout, bound, text, state);

            DrawGlyph(style, gliph, group.IsExpand ? GlyphType.ChevronDown : GlyphType.ChevronRight, state);

            var pen = style.FontBrush.GetColorByState(state);
            if (pen != CellStyleBrush.ColorEmpty)
            {
                Context.SetColor(pen);
                Context.MoveTo(bound.X + text.Width + 25, bound.Y + bound.Height / 2);
                Context.LineTo(bound.Right - 30, bound.Y + bound.Height / 2);
                Context.Stroke();
            }
        }

        public void DrawCell(CellStyle style, object formated, Rectangle bound, Rectangle textBound, CellDisplayState state)
        {
            if (bound.Width <= 0)
                return;
            //Context.Save();
            //Context.Rectangle(bound);
            //Context.Clip();
            using (DrawingPath path = GetRoundedRect(bound, style.Round))
            {
                var backColor = style.BackBrush.GetColorByState(state);
                if (backColor != CellStyleBrush.ColorEmpty)
                {
                    Context.SetColor(backColor);
                    var pattern = style.BackBrush.GetBrushByState(bound, state);
                    if (pattern != null)
                        Context.Pattern = pattern;
                    Context.AppendPath(path);
                    Context.Fill();
                    if (pattern != null)
                    {
                        pattern.Dispose();
                    }
                }

                if (formated != null)
                {
                    if (formated is string && ((string)formated).Length > 0)
                    {
                        DrawText(style, (string)formated, textBound, state);
                    }
                    else if (formated is TextLayout)
                    {
                        DrawText(style, (TextLayout)formated, textBound, state);
                    }
                    else if (formated is GlyphType)
                    {
                        DrawGlyph(style, textBound, (GlyphType)formated, state);
                    }
                    else if (formated is Image)
                    {
                        DrawImage((Image)formated, textBound);
                    }
                    else if (formated is CheckedState)
                    {
                        textBound.Width = textBound.Height;
                        DrawCheckBox(style.FontBrush.GetColorByState(state), textBound, (CheckedState)formated);
                    }
                }

                var penColor = style.BorderBrush.GetColorByState(state);
                if (penColor != CellStyleBrush.ColorEmpty && style.LineWidth > 0)
                {
                    Context.SetLineWidth(style.LineWidth);
                    Context.SetColor(penColor);
                    var pattern = style.BorderBrush.GetBrushByState(bound, state);
                    if (pattern != null)
                        Context.Pattern = pattern;
                    using (var borderPath = GetRoundedRect(bound.Inflate(-0.5, -0.5), style.Round))
                    {
                        Context.AppendPath(borderPath);
                        Context.Stroke();
                    }
                }
            }
            //Context.Restore();
        }

        public void DrawImage(Image image, Rectangle bound)
        {
            Context.DrawImage(image, bound, 1);
        }

        public void FillRectangle(Color color, Rectangle bound)
        {
            Context.SetColor(color);
            FillRectangle(bound);
        }

        public void FillRectangle(Rectangle bound)
        {
            Context.Rectangle(bound);
            Context.Fill();
        }

        public void FillRectangle(CellStyle style, Rectangle bound, CellDisplayState state = CellDisplayState.Default)
        {
            Context.SetColor(style.BackBrush.GetColorByState(state));
            var pattern = style.BackBrush.GetBrushByState(bound, state);
            if (pattern != null)
                Context.Pattern = pattern;

            FillRectangle(bound);
        }

        public void DrawRectangle(Color color, Rectangle bound)
        {
            Context.SetColor(color);
            DrawRectangle(bound);
        }

        public void DrawRectangle(Rectangle bound)
        {
            Context.Rectangle(bound);
            Context.Stroke();
        }

        public void DrawRectangle(CellStyle style, Rectangle bound, CellDisplayState state = CellDisplayState.Default)
        {
            Context.SetLineWidth(style.LineWidth);
            Context.SetColor(style.BorderBrush.GetColorByState(state));
            DrawRectangle(bound);
        }

        public void DrawText(CellStyle style, string text, Rectangle bound, CellDisplayState state = CellDisplayState.Default)
        {
            if (cacheTextLayout == null)
            {
                cacheTextLayout = new TextLayout();
                //cacheTextLayout.Trimming = TextTrimming.WordElipsis;
            }
            cacheTextLayout.Font = style.Font;
            cacheTextLayout.TextAlignment = style.Alignment;
            cacheTextLayout.Text = text;
            DrawText(style, cacheTextLayout, bound, state);
        }

        public void DrawText(CellStyle style, TextLayout textLayout, Rectangle bound, CellDisplayState state = CellDisplayState.Default)
        {
            if (textLayout.Height != bound.Height)
                textLayout.Height = bound.Height;
            if (textLayout.Width != bound.Width)
                textLayout.Width = bound.Width;
            Context.SetColor(style.FontBrush.GetColorByState(state));
            var pattern = style.FontBrush.GetBrushByState(bound, state);
            if (pattern != null)
                Context.Pattern = pattern;

            Context.DrawTextLayout(textLayout, bound.Location);
        }

        public static DrawingPath GetRoundedRect(Rectangle baseRect, float radius)
        {
            var path = new DrawingPath();
            path.RoundRectangle(baseRect, radius);
            path.ClosePath();
            return path;
        }
        public static DrawingPath GetRect(Rectangle baseRect)
        {
            var path = new DrawingPath();
            path.Rectangle(baseRect.Location, baseRect.Width, baseRect.Height);
            path.ClosePath();
            return path;
        }

    }
}

