// HelperMethods.cs
// 
// Cut & paste from PangoCairoHelper.
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Xwt.Drawing;

namespace Mono.TextEditor
{
    public static class HelperMethods
    {
        public static TextSegment AdjustSegment(this TextSegment segment, DocumentChangeEventArgs args)
        {
            if (args.Offset < segment.Offset)
                return new TextSegment(segment.Offset + args.ChangeDelta, segment.Length);
            if (args.Offset <= segment.EndOffset)
                return new TextSegment(segment.Offset, segment.Length);
            return segment;
        }
        public static IEnumerable<TextSegment> AdjustSegments(this IEnumerable<TextSegment> segments, DocumentChangeEventArgs args)
        {
            foreach (var segment in segments)
            {
                yield return segment.AdjustSegment(args);
            }
        }

        public static T Kill<T>(this T gc) where T : IDisposable
        {
            if (gc != null)
                gc.Dispose();
            return default(T);
        }

        public static T Kill<T>(this T gc, Action<T> action) where T : IDisposable
        {
            if (gc != null)
            {
                action(gc);
                gc.Dispose();
            }

            return default(T);
        }

        public static string GetColorString(Color color)
        {
            return string.Format("#{0:X02}{1:X02}{2:X02}", color.Red / 256, color.Green / 256, color.Blue / 256);
        }

        public static void ShowLayout(this Context cr, TextLayout layout)
        {
            cr.DrawTextLayout(layout, 0, 0);
        }

        public static void DrawLine(this Xwt.Drawing.Context cr, Color color, double x1, double y1, double x2, double y2)
        {
            cr.SetSourceColor(color);
            cr.MoveTo(x1, y1);
            cr.LineTo(x2, y2);
            cr.Stroke();
        }

        public static void Line(this Xwt.Drawing.Context cr, double x1, double y1, double x2, double y2)
        {
            cr.MoveTo(x1, y1);
            cr.LineTo(x2, y2);
        }

        public static void SharpLineX(this Xwt.Drawing.Context cr, double x1, double y1, double x2, double y2)
        {
            cr.MoveTo(x1 + 0.5, y1);
            cr.LineTo(x2 + 0.5, y2);
        }

        public static void SharpLineY(this Xwt.Drawing.Context cr, double x1, double y1, double x2, double y2)
        {
            cr.MoveTo(x1, y1 + 0.5);
            cr.LineTo(x2, y2 + 0.5);
        }

        public static void SetSourceColor(this Xwt.Drawing.Context cr, Color color)
        {
            cr.SetColor(color);
        }
    }
}
