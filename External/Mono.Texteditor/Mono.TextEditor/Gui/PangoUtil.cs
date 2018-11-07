// 
// PangoUtils.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using Xwt;
using System.Runtime.InteropServices;
using Xwt.Drawing;
using System.Collections.Generic;

namespace Mono.TextEditor
{
    public static class PangoUtil
    {
        /// <summary>
        /// This doesn't leak Pango layouts, unlike some other ways to create them in GTK# &lt;= 2.12.11
        /// </summary>
        public static TextLayout CreateLayout(Canvas widget)
        {
            return new TextLayout(widget);
        }

        public static TextLayout CreateLayout(Canvas widget, string text)
        {
            return new TextLayout(widget) { Text = text };
        }
    }

    /// <summary>
    /// This creates a Pango list and applies attributes to it with *much* less overhead than the GTK# version.
    /// </summary>
    class FastPangoAttrList : IDisposable
    {
        List<TextAttribute> list;

        public FastPangoAttrList()
        {
            list = new List<TextAttribute>();
        }

        public void AddStyleAttribute(FontStyle style, int start, int end)
        {
            Add(new FontStyleTextAttribute() { Style = style }, start, end);
        }

        public void AddWeightAttribute(FontWeight weight, int start, int end)
        {
            Add(new FontWeightTextAttribute() { Weight = weight }, start, end);
        }

        public void AddForegroundAttribute(Color color, int start, int end)
        {
            Add(new ColorTextAttribute() { Color = color }, start, end);
        }

        public void AddBackgroundAttribute(Color color, int start, int end)
        {
            Add(new BackgroundTextAttribute() { Color = color }, start, end);
        }

        public void AddUnderlineAttribute(bool underline, int start, int end)
        {
            Add(new UnderlineTextAttribute() { Underline = underline }, start, end);
        }

        void Add(TextAttribute attribute, int start, int end)
        {
            attribute.StartIndex = start;
            attribute.Count = end - start;
            list.Add(attribute);
        }

        public void InsertOffsetList(FastPangoAttrList atts, int startOffset, int endOffset)
        {
            InsertOffsetList(atts.list, startOffset, endOffset);
        }
        /// <summary>
        /// Like Splice, except it only offsets/clamps the inserted items, doesn't affect items already in the list.
        /// </summary>
        public void InsertOffsetList(List<TextAttribute> atts, int startOffset, int endOffset)
        {
            foreach (var attr in atts)
            {
                AddOffsetCopy(attr, startOffset, endOffset);
            }
        }

        void AddOffsetCopy(TextAttribute attr, int startOffset, int endOffset)
        {
            var copy = attr.Clone();
            copy.StartIndex = startOffset + attr.StartIndex;
            var endIndex = Math.Min(endOffset, startOffset + (attr.StartIndex + attr.Count));
            copy.Count = endIndex - copy.StartIndex;
            list.Add(copy);
        }

        public void Splice(FastPangoAttrList attrs, int pos, int len)
        {
            throw new NotImplementedException();
            //pango_attr_list_splice(list, attrs, pos, len);
        }

        public void AssignTo(TextLayout layout)
        {
            layout.ClearAttributes();
            foreach (var item in list)
            {
                layout.AddAttribute(item);
            }
        }

        public void Dispose()
        {
            if (list.Count != 0)
            {
                GC.SuppressFinalize(this);
                Destroy();
            }
        }

        //NOTE: the list destroys all its attributes when the ref count reaches zero
        void Destroy()
        {
            list.Clear();
            list.Capacity = 0;
        }

        ~FastPangoAttrList()
        {
            Application.Invoke(delegate
            {
                Destroy();
            });
        }
    }
}