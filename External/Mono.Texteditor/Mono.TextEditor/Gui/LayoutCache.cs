//
// LayoutCache.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

namespace Mono.TextEditor
{
    /// <summary>
    /// Caches native pango layout objects.
    /// </summary>
    public class LayoutCache : IDisposable
    {
        readonly Canvas widget;
        readonly Queue<LayoutProxy> layoutQueue = new Queue<LayoutProxy>();

        public LayoutCache(Canvas widget)
        {
            if (widget == null)
                throw new ArgumentNullException(nameof(widget));
            this.widget = widget;
        }

        public LayoutProxy RequestLayout()
        {
            if (layoutQueue.Count == 0)
            {
                layoutQueue.Enqueue(new LayoutProxy(this, PangoUtil.CreateLayout(widget)));
            }
            return layoutQueue.Dequeue();
        }

        #region IDisposable implementation
        public void Dispose()
        {
            foreach (var proxy in layoutQueue)
            {
                proxy.DisposeNativeObject();
            }
            layoutQueue.Clear();
        }
        #endregion

        public class LayoutProxy : IDisposable
        {
            readonly LayoutCache layoutCache;
            readonly TextLayout layout;
            private TabArray tabs;
            private WrapMode wrap;

            public LayoutProxy(LayoutCache layoutCache, TextLayout layout)
            {
                if (layoutCache == null)
                    throw new ArgumentNullException(nameof(layoutCache));
                if (layout == null)
                    throw new ArgumentNullException(nameof(layout));
                this.layoutCache = layoutCache;
                this.layout = layout;
            }

            internal void DisposeNativeObject()
            {
                layout.Dispose();
            }

            #region IDisposable implementation

            public void Dispose()
            {
                layout.ClearAttributes();
                layout.Width = -1;
                layout.TextAlignment = Xwt.Alignment.Start;
                layoutCache.layoutQueue.Enqueue(this);
            }

            #endregion

            public static implicit operator TextLayout(LayoutProxy proxy)
            {
                return proxy.layout;
            }

            public string Text
            {
                get { return layout.Text; }
            }

            public Alignment Alignment
            {
                get { return layout.TextAlignment; }
                set { layout.TextAlignment = value; }
            }

            public Font Font
            {
                get { return layout.Font; }
                set
                {
                    layout.Font = value;

                }
            }

            public TabArray Tabs
            {
                get { return tabs; }
                set { tabs = value; }
            }

            public WrapMode Wrap
            {
                get { return wrap; }
                set { wrap = value; }
            }

            public double Width
            {
                get { return layout.Width; }
                set { layout.Width = value; }
            }

            public int LineCount
            {
                get { return (int)(layout.Height / layout.Font.Size); }
            }

            public void SetText(string text)
            {
                layout.Text = text;
            }

            public Rectangle IndexToPos(int index_)
            {
                return new Rectangle(layout.GetCoordinateFromIndex(index_),
                                     new Size(layout.Font.Size, layout.Font.Size));
            }

            public void IndexToLineX(int index_, out int line, out double x_pos)
            {
                var point = layout.GetCoordinateFromIndex(index_);
                x_pos = point.X;
                line = (int)(point.Y / layout.Font.Size);
            }

            //public LayoutLine GetLine(int line)
            //{
            //    return layout.GetLine(line);
            //}

            public Size GetSize()
            {
                return layout.GetSize();
            }

            public bool XyToIndex(int x, int y, out int index, out int trailing)
            {
                trailing = 0;
                index = -1;
                if (!string.IsNullOrEmpty(layout.Text))
                {
                    index = layout.GetIndexFromCoordinates(x, y);
                }
                return index >= 0;
            }

            public void GetCursorPos(int index, out Rectangle strong_pos, out Rectangle weak_pos)
            {
                var pos = IndexToPos(index);
                strong_pos = pos; weak_pos = pos;
            }

            public Rectangle GetExtents()
            {
                return new Rectangle(Point.Zero, layout.GetSize());
            }
        }
    }
}
