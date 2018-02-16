// 
// ListWidget.cs
//  
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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

using Xwt;
using Xwt.Drawing;

using System;
using System.Text;

namespace Mono.TextEditor.PopupWindow
{
    class ListWidget<T> : Canvas
    {
        int margin = 0;
        int padding = 4;
        double listWidth = 300;

        Xwt.Drawing.TextLayout layout;
        ListWindow<T> win;
        int selection = 0;
        int visibleRows = -1;
        int rowHeight;
        bool buttonPressed;
        bool disableSelection;

        protected virtual void OnSelectionChanged(EventArgs e)
        {
            EventHandler handler = this.SelectionChanged;
            if (handler != null)
                handler(this, e);
        }
        public event EventHandler SelectionChanged;

        public ListWidget(ListWindow<T> win)
        {
            this.win = win;
        }

        public void Reset()
        {
            if (win.DataProvider == null)
            {
                selection = -1;
                return;
            }

            if (win.DataProvider.Count == 0)
                selection = -1;
            else
                selection = 0;

            disableSelection = false;

            UpdateStyle();
            QueueDraw();

            OnSelectionChanged(EventArgs.Empty);
        }

        public int Selection
        {
            get { return selection; }
            set
            {
                if (value < 0)
                    value = 0;
                if (value >= win.DataProvider.Count)
                    value = win.DataProvider.Count - 1;

                if (value != selection)
                {
                    selection = value;
                    UpdatePage();

                    OnSelectionChanged(EventArgs.Empty);
                }

                if (disableSelection)
                    disableSelection = false;

                this.QueueDraw();
            }
        }

        void UpdatePage()
        {
            var area = GetRowArea(selection);
            if (area.Y < vadj.Value)
            {
                vadj.Value = area.Y;
                return;
            }
            if (vadj.Value + Bounds.Height < area.Bottom)
            {
                vadj.Value = Math.Max(0, area.Bottom - vadj.PageSize + 1);
            }
        }

        public bool SelectionDisabled
        {
            get { return disableSelection; }
            set
            {
                disableSelection = value;
                this.QueueDraw();
            }
        }

        protected override void OnButtonPressed(ButtonEventArgs e)
        {
            Selection = GetRowByPosition((int)(vadj.Value + e.Y));
            buttonPressed = true;
            base.OnButtonPressed(e);
        }

        protected override void OnButtonReleased(ButtonEventArgs e)
        {
            buttonPressed = false;
            base.OnButtonReleased(e);
        }

        protected override void OnMouseMoved(MouseMovedEventArgs e)
        {
            if (!buttonPressed)
            {
                OnMouseMoved(e);
                return;
            }
            /*		int ypos = (int) e.Y;
                    if (ypos < 0) {
                    }
                    else if (ypos >= winHeight) {
                    }
                    else
            */
            Selection = GetRowByPosition((int)(vadj.Value + e.Y));
        }

        ScrollAdjustment hadj;
        ScrollAdjustment vadj;

        protected override void SetScrollAdjustments(ScrollAdjustment horizontal, ScrollAdjustment vertical)
        {
            this.hadj = horizontal;
            this.vadj = vertical;
            if (this.vadj != null)
                this.vadj.ValueChanged += (sender, e) => QueueDraw();
            base.SetScrollAdjustments(horizontal, vertical);
        }

        //void SetAdjustments(Rectangle allocation)
        //{
        //    hadj.SetBounds(0, allocation.Width, 0, 0, allocation.Width);
        //    var height = System.Math.Max(allocation.Height, rowHeight * this.win.DataProvider.Count);
        //    vadj.SetBounds(0, height, rowHeight, allocation.Height, allocation.Height);
        //}

        //FIXME: we could use the expose event's clipbox to make the drawing more efficient
        protected override void OnDraw(Context cr, Rectangle dirtyRect)
        {
            var ypos = margin;
            var lineWidth = Size.Width - margin * 2;
            int xpos = margin + padding;

            //avoid recreating the GC objects that we use multiple times
            var textColor = Colors.Black;

            int n = 0;
            n = (int)(vadj.Value / rowHeight);

            while (ypos < Size.Height - margin && n < win.DataProvider.Count)
            {
                bool hasMarkup = false;
                IMarkupListDataProvider<T> markupListDataProvider = win.DataProvider as IMarkupListDataProvider<T>;
                if (markupListDataProvider != null)
                {
                    if (markupListDataProvider.HasMarkup(n))
                    {
                        layout.Markup = (markupListDataProvider.GetMarkup(n) ?? "&lt;null&gt;");
                        hasMarkup = true;
                    }
                }

                if (!hasMarkup)
                    layout.Text = (win.DataProvider.GetText(n) ?? "<null>");

                var icon = win.DataProvider.GetIcon(n);
                int iconHeight, iconWidth;

                if (icon != null)
                {
                    iconWidth = (int)icon.Width;
                    iconHeight = (int)icon.Height;
                }
                iconHeight = iconWidth = 24;

                var s = layout.GetSize();
                int typos, iypos;
                int he = (int)s.Height;

                typos = he < rowHeight ? ypos + (rowHeight - he) / 2 : ypos;
                iypos = iconHeight < rowHeight ? ypos + (rowHeight - iconHeight) / 2 : ypos;

                if (n == selection)
                {
                    if (!disableSelection)
                    {
                        cr.Rectangle(margin, ypos, lineWidth, he + padding);
                        cr.SetColor(Colors.DarkOrange);
                        cr.Fill();

                        cr.SetColor(Colors.White);
                        cr.DrawTextLayout(layout, xpos + iconWidth + 2, typos);
                    }
                    else
                    {
                        cr.Rectangle(margin, ypos, lineWidth, he + padding);
                        cr.SetColor(Colors.OrangeRed);
                        cr.Stroke();

                        cr.SetColor(textColor);
                        cr.DrawTextLayout(layout, xpos + iconWidth + 2, typos);
                    }
                }
                else
                {
                    cr.SetColor(textColor);
                    cr.DrawTextLayout(layout, xpos + iconWidth + 2, typos);
                }

                if (icon != null)
                    cr.DrawImage(icon, xpos, iypos);

                ypos += rowHeight;
                n++;

                //reset the markup or it carries over to the next SetText
                if (hasMarkup)
                    layout.Markup = string.Empty;
            }
        }

        public int TextOffset
        {
            get
            {
                int iconWidth = 24;
                return iconWidth + margin + padding + 2;
            }
        }

        int GetRowByPosition(int ypos)
        {
            return ypos / rowHeight;
        }

        public Rectangle GetRowArea(int row)
        {
            return new Xwt.Rectangle(0, row * rowHeight, Bounds.Width, rowHeight);
        }

        public int VisibleRows
        {
            get { return visibleRows; }
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            var lvsize = base.OnGetPreferredSize(widthConstraint, heightConstraint);

            var s = layout.GetSize();
            rowHeight = (int)s.Height + padding;

            visibleRows = 7;//(winHeight + padding - margin * 2) / rowHeight;

            double newHeight;

            if (this.win.DataProvider.Count > this.visibleRows)
                newHeight = (rowHeight * visibleRows) + margin * 2;
            else
                newHeight = (rowHeight * this.win.DataProvider.Count) + margin * 2;

            if (lvsize.Width != listWidth || lvsize.Height != newHeight)
                return new Size(listWidth, newHeight);
            return lvsize;
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            UpdateStyle();
            UpdatePage();
        }

        void UpdateStyle()
        {
            if (layout != null)
                layout.Dispose();
            layout = new TextLayout(this);
            layout.Trimming = TextTrimming.Word;
        }
    }
}
