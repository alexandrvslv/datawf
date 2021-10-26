﻿//
// UrlMarker.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using Xwt.Drawing;
using Mono.TextEditor.Highlighting;
using Xwt;

namespace Mono.TextEditor
{

    public enum UrlType
    {
        Unknown,
        Url,
        Email
    }


    public class UrlMarker : TextLineMarker, IDisposable
    {
        string url;
        string style;
        int startColumn;
        int endColumn;
        DocumentLine line;
        UrlType urlType;
        TextDocument doc;

        public string Url
        {
            get
            {
                return url;
            }
        }

        public int StartColumn
        {
            get
            {
                return startColumn;
            }
        }

        public int EndColumn
        {
            get
            {
                return endColumn;
            }
        }

        public UrlType UrlType
        {
            get
            {
                return urlType;
            }
        }

        public UrlMarker(TextDocument doc, DocumentLine line, string url, UrlType urlType, string style, int startColumn, int endColumn)
        {
            this.doc = doc;
            this.line = line;
            this.url = url;
            this.urlType = urlType;
            this.style = style;
            this.startColumn = startColumn;
            this.endColumn = endColumn;
            doc.LineChanged += HandleDocLineChanged;
        }

        void HandleDocLineChanged(object sender, LineEventArgs e)
        {
            if (line == e.Line)
                doc.RemoveMarker(this);
        }

        public void Dispose()
        {
            if (doc != null)
            {
                doc.LineChanged -= HandleDocLineChanged;
                doc = null;
            }
            line = null;
        }

        public override void Draw(TextEditor editor, Xwt.Drawing.Context cr, double y, LineMetrics metrics)
        {
            var startOffset = metrics.TextStartOffset;
            int endOffset = metrics.TextEndOffset;
            double startXPos = metrics.TextRenderStartPosition;
            double endXPos = metrics.TextRenderEndPosition;
            var layout = metrics.Layout.Layout;
            int markerStart = line.Offset + startColumn;
            int markerEnd = line.Offset + endColumn;

            if (markerEnd < startOffset || markerStart > endOffset)
                return;

            double @from;
            double to;

            if (markerStart < startOffset && endOffset < markerEnd)
            {
                @from = startXPos;
                to = endXPos;
            }
            else
            {
                int start = startOffset < markerStart ? markerStart : startOffset;
                int end = endOffset < markerEnd ? endOffset : markerEnd;

                int curIndex = 0, byteIndex = 0;
                var x_pos = layout.IndexToPos((int)metrics.Layout.TranslateToUTF8Index(start - startOffset, ref curIndex, ref byteIndex)).X;

                @from = startXPos + (int)(x_pos  );

                x_pos = layout.IndexToPos((int)metrics.Layout.TranslateToUTF8Index(end - startOffset, ref curIndex, ref byteIndex)).X;

                to = startXPos + (int)(x_pos  );
            }

            @from = System.Math.Max(@from, editor.TextViewMargin.XOffset);
            to = System.Math.Max(to, editor.TextViewMargin.XOffset);
            if (@from < to)
            {
                cr.DrawLine(editor.ColorStyle.GetForeground(editor.ColorStyle.GetChunkStyle(style)), @from + 0.5, y + editor.LineHeight - 1.5, to + 0.5, y + editor.LineHeight - 1.5);
            }
        }
    }

}
