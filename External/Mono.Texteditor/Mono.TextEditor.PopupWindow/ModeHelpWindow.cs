// 
// ModeHelpWindow.cs
//  
// Author:
//       Mike Kr端ger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using Xwt;
using Xwt.Drawing;

namespace Mono.TextEditor.PopupWindow
{
    public abstract class ModeHelpWindow : Canvas
    {
        public string TitleText
        {
            get;
            set;
        }

        public List<KeyValuePair<string, string>> Items
        {
            get;
            set;
        }

        public bool SupportsAlpha
        {
            get;
            private set;
        }

        public ModeHelpWindow()
        {

            Items = new List<KeyValuePair<string, string>>();
        }
    }

    public class TableLayoutModeHelpWindow : ModeHelpWindow
    {
        TextLayout layout;
        double xSpacer = 0;
        const int xBorder = 4;
        const int yBorder = 2;

        public TableLayoutModeHelpWindow()
        {
            layout = new TextLayout(this);
        }

        protected override Size OnGetPreferredSize(SizeConstraint w, SizeConstraint h)
        {
            var size = base.OnGetPreferredSize(w, h);
            double descriptionWidth = 1;
            double totalHeight = yBorder * 2 + 1;

            layout.Text = TitleText;
            var lsize = layout.GetSize();
            totalHeight += lsize.Height;
            xSpacer = 0;
            foreach (var pair in Items)
            {
                layout.Markup = pair.Key;
                var w1 = layout.GetSize();

                layout.Markup = pair.Value;
                var w2 = layout.GetSize();
                descriptionWidth = System.Math.Max(descriptionWidth, w2.Width);
                xSpacer = System.Math.Max(xSpacer, w1.Width);

                totalHeight += w2.Height;
            }
            xSpacer += xBorder * 2 + 1;

            return new Size(descriptionWidth + xSpacer + xBorder * 2 + 1,
                            totalHeight);
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (layout != null)
            {
                layout.Dispose();
                layout = null;
            }
        }


        protected override void OnDraw(Context g, Rectangle bound)
        {
            Color bgColor = new Color(1, 1, 1);
            Color titleBgColor = new Color(0.88, 0.88, 0.98);
            Color categoryBgColor = new Color(0.58, 0.58, 0.98);
            Color borderColor = new Color(0.4, 0.4, 0.6);
            Color textColor = new Color(0.3, 0.3, 1);
            Color gridColor = new Color(0.8, 0.8, 0.8);


            g.Translate(Bounds.X, Bounds.Y);
            g.SetLineWidth(1);

            layout.Markup = TitleText;
            var size = layout.GetSize();
            size.Width += xBorder * 2;
            FoldingScreenbackgroundRenderer.DrawRoundRectangle(g, true, false, 0.5, 0.5, size.Height + yBorder * 2 + 1.5, size.Width, size.Height + yBorder * 2);
            g.SetSourceColor(titleBgColor);
            g.FillPreserve();
            g.SetSourceColor(borderColor);
            g.Stroke();

            g.Save();
            g.SetSourceColor(textColor);
            g.Translate(xBorder, yBorder);
            g.ShowLayout(layout);
            g.Restore();

            FoldingScreenbackgroundRenderer.DrawRoundRectangle(g, false, true, 0.5, size.Height * 2 + yBorder * 2 + 0.5, size.Height, Bounds.Width - 1, Bounds.Height - size.Height * 2 - yBorder * 2 - 1);
            g.SetSourceColor(bgColor);
            g.FillPreserve();
            g.SetSourceColor(borderColor);
            g.Stroke();

            g.MoveTo(xSpacer + 0.5, size.Height * 2 + yBorder * 2);
            g.LineTo(xSpacer + 0.5, Bounds.Height - 1);
            g.SetSourceColor(gridColor);
            g.Stroke();

            var y = size.Height + yBorder * 2;

            for (int i = 0; i < Items.Count; i++)
            {
                KeyValuePair<string, string> pair = Items[i];

                layout.Markup = pair.Key;
                size = layout.GetSize();

                if (i == 0)
                {
                    FoldingScreenbackgroundRenderer.DrawRoundRectangle(g, false, true, false, false, 0, y + 0.5, size.Height + 1.5, Bounds.Width, size.Height);
                    g.SetSourceColor(categoryBgColor);
                    g.FillPreserve();
                    g.SetSourceColor(borderColor);
                    g.Stroke();

                    g.MoveTo(xSpacer + 0.5, size.Height + yBorder * 2 + 1);
                    g.LineTo(xSpacer + 0.5, size.Height * 2 + yBorder * 2 + 1);
                    g.SetSourceColor(gridColor);
                    g.Stroke();
                }

                //gc.RgbFgColor = (HslColor)(i == 0 ? bgColor : textColor);
                g.Save();
                g.SetSourceColor(textColor);
                g.Translate(xBorder, y);
                g.ShowLayout(layout);
                g.Restore();

                g.Save();
                g.SetSourceColor(textColor);
                g.Translate(xSpacer + xBorder, y);
                layout.Markup = pair.Value;
                g.ShowLayout(layout);
                g.Restore();

                // draw top line
                if (i > 0)
                {
                    g.MoveTo(1, y + 0.5);
                    g.LineTo(Bounds.Width - 1, y + 0.5);
                    g.SetSourceColor(gridColor);
                    g.Stroke();
                }
                y += size.Height;
            }

            base.OnDraw(g, bound);
        }
    }

    public class InsertionCursorLayoutModeHelpWindow : ModeHelpWindow
    {
		static readonly Color bgColor;
		static readonly Color titleBgColor;
		static readonly Color titleTextColor;
		static readonly Color borderColor;
		static readonly Color textColor;

		static InsertionCursorLayoutModeHelpWindow()
		{
			Color.TryParse("#ffe97f", out bgColor);
			Color.TryParse("#cfb94f", out titleBgColor);
			Color.TryParse("#000000", out titleTextColor);
			Color.TryParse("#7f6a00", out borderColor);
			Color.TryParse("#555753", out textColor);
		}

		TextLayout titleLayout;
        TextLayout descriptionLayout;

        public InsertionCursorLayoutModeHelpWindow()
        {
            titleLayout = new TextLayout(this);
            descriptionLayout = new TextLayout(this);
            descriptionLayout.Markup = "<small>Use Up/Down to move to another location.\nPress Enter to select the location\nPress Esc to cancel this operation</small>";
        }

        protected override Size OnGetPreferredSize(SizeConstraint w, SizeConstraint h)
        {
            base.OnGetPreferredSize(w, h);
            double descriptionWidth = 1;
            double totalHeight = yTitleBorder * 2 + yDescriptionBorder * 2 + 1;

            titleLayout.Text = TitleText;
            var size1 = titleLayout.GetSize();
            totalHeight += size1.Height;
            xSpacer = 0;

            var size2 = descriptionLayout.GetSize();
            totalHeight += size2.Height;
            xSpacer = System.Math.Max(size1.Width, size2.Width);

            xSpacer += xDescriptionBorder * 2 + 1;

            return new Size(triangleWidth + descriptionWidth + xSpacer, totalHeight);
        }

        double xSpacer = 0;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (titleLayout != null)
            {
                titleLayout.Dispose();
                titleLayout = null;
            }

            if (descriptionLayout != null)
            {
                descriptionLayout.Dispose();
                descriptionLayout = null;
            }
        }

        const int triangleHeight = 16;
        const int triangleWidth = 8;

        const int xDescriptionBorder = 6;
        const int yDescriptionBorder = 6;
        const int yTitleBorder = 2;

        protected override void OnDraw(Context g, Rectangle bound)
        {
            g.Translate(Bounds.X, Bounds.Y);
            g.SetLineWidth(1.5);
            titleLayout.Markup = TitleText;
            var size = titleLayout.GetSize();
            var tw = SupportsAlpha ? triangleWidth : 0;
            var th = SupportsAlpha ? triangleHeight : 0;
            size.Width += xDescriptionBorder * 2;
            if (SupportsAlpha)
            {
                FoldingScreenbackgroundRenderer.DrawRoundRectangle(g, true, false, tw + 0.5, 0.5, size.Height + yTitleBorder * 2 + 1.5, Bounds.Width - 1 - tw, size.Height + yTitleBorder * 2);
            }
            else
            {
                g.Rectangle(0, 0, Bounds.Width, size.Height + yTitleBorder * 2);
            }
            g.SetSourceColor(titleBgColor);
            g.FillPreserve();
            g.SetSourceColor(borderColor);
            g.Stroke();


            g.MoveTo(tw + xDescriptionBorder, yTitleBorder);
            g.SetSourceColor(titleTextColor);
            g.ShowLayout(titleLayout);

            if (SupportsAlpha)
            {
                FoldingScreenbackgroundRenderer.DrawRoundRectangle(g, false, true, tw + 0.5, size.Height + yTitleBorder * 2 + 0.5, size.Height, Bounds.Width - 1 - tw, Bounds.Height - size.Height - yTitleBorder * 2 - 1);
            }
            else
            {
                g.Rectangle(0, size.Height + yTitleBorder * 2, Bounds.Width, Bounds.Height - size.Height - yTitleBorder * 2);
            }
            g.SetSourceColor(bgColor);
            g.FillPreserve();
            g.SetSourceColor(borderColor);
            g.Stroke();

            if (SupportsAlpha)
            {

                g.MoveTo(tw, Bounds.Height / 2 - th / 2);
                g.LineTo(0, Bounds.Height / 2);
                g.LineTo(tw, Bounds.Height / 2 + th / 2);
                g.LineTo(tw + 5, Bounds.Height / 2 + th / 2);
                g.LineTo(tw + 5, Bounds.Height / 2 - th / 2);
                g.ClosePath();
                g.SetSourceColor(bgColor);
                g.Fill();

                g.MoveTo(tw, Bounds.Height / 2 - th / 2);
                g.LineTo(0, Bounds.Height / 2);
                g.LineTo(tw, Bounds.Height / 2 + th / 2);
                g.SetSourceColor(borderColor);
                g.Stroke();
            }

            var y = size.Height + yTitleBorder * 2 + yDescriptionBorder;
            g.MoveTo(tw + xDescriptionBorder, y);
            g.SetSourceColor(textColor);
            g.ShowLayout(descriptionLayout);
            base.OnDraw(g, bound);
        }
    }
}
