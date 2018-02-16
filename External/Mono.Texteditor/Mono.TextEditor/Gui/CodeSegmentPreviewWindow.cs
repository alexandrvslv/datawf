//
// CodeSegmentPreviewWindow.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Xwt.Drawing;
using Xwt;
using System.Collections.Generic;

namespace Mono.TextEditor
{
    public class CodeSegmentPreviewWindow : Xwt.PopupWindow
    {
        internal const int DefaultPreviewWindowWidth = 320;
        internal const int DefaultPreviewWindowHeight = 200;
        public SegmentCanvas canvas;

        public static string CodeSegmentPreviewInformString
        {
            get;
            set;
        }

        public bool HideCodeSegmentPreviewInformString
        {
            get;
            private set;
        }

        public TextSegment Segment
        {
            get;
            private set;
        }

        public bool IsEmptyText
        {
            get { return string.IsNullOrEmpty((canvas.layout.Text ?? "").Trim()); }
        }

        public CodeSegmentPreviewWindow(TextEditor editor, bool hideCodeSegmentPreviewInformString, TextSegment segment, bool removeIndent = true) : this(editor, hideCodeSegmentPreviewInformString, segment, DefaultPreviewWindowWidth, DefaultPreviewWindowHeight, removeIndent)
        {
        }

        public CodeSegmentPreviewWindow(TextEditor editor, bool hideCodeSegmentPreviewInformString, TextSegment segment, int width, int height, bool removeIndent = true) : base(PopupType.Tooltip)
        {
            this.HideCodeSegmentPreviewInformString = hideCodeSegmentPreviewInformString;
            this.Segment = segment;
            this.ShowInTaskbar = false;
            //this.
            canvas = new SegmentCanvas(editor, segment, removeIndent);
            canvas.window = this;
            Size = new Size(width, height);
        }

        public double PreviewInformStringHeight
        {
            get; internal set;
        }
    }

    public class SegmentCanvas : Canvas
    {
        private const int maxLines = 40;
        public CodeSegmentPreviewWindow window;

        public TextEditor editor;
        public Font fontDescription;
        public TextLayout layout;
        public TextLayout informLayout;

        public SegmentCanvas(TextEditor editor, TextSegment segment, bool removeIndent = true)
        {
            this.editor = editor;
            layout = PangoUtil.CreateLayout(this);
            informLayout = PangoUtil.CreateLayout(this);
            informLayout.Text = CodeSegmentPreviewWindow.CodeSegmentPreviewInformString;

            fontDescription = Font.FromName(editor.Options.FontName).WithSize(Font.Size * 0.8f);
            layout.Font = fontDescription;
            layout.Trimming = TextTrimming.WordElipsis;
            // setting a max size for the segment (40 lines should be enough), 
            // no need to markup thousands of lines for a preview window
            SetSegment(segment, removeIndent);
        }

        public void SetSegment(TextSegment segment, bool removeIndent)
        {
            int startLine = editor.Document.OffsetToLineNumber(segment.Offset);
            int endLine = editor.Document.OffsetToLineNumber(segment.EndOffset);

            bool pushedLineLimit = endLine - startLine > maxLines;
            if (pushedLineLimit)
                segment = new TextSegment(segment.Offset, editor.Document.GetLine(startLine + maxLines).Offset - segment.Offset);

            layout.Markup = editor.GetTextEditorData().GetMarkup(
                segment.Offset, segment.Length,
                removeIndent) + (pushedLineLimit ? Environment.NewLine + "..." : "");
            QueueDraw();
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            var size = layout.GetSize();

            if (!window.HideCodeSegmentPreviewInformString)
            {
                var size2 = informLayout.GetSize();
                window.PreviewInformStringHeight = size2.Height;
                size.Width = System.Math.Max(size.Width, size2.Width);
                size.Height += size2.Height;
            }
            Rectangle geometry = ScreenBounds;
            return new Size(System.Math.Max(1, System.Math.Min(size.Width + 3, geometry.Width * 2 / 5)),
                                System.Math.Max(1, System.Math.Min(size.Height + 3, geometry.Height * 2 / 5)));
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            ctx.SetColor(editor.ColorStyle.PlainText.Background);
            ctx.Rectangle(dirtyRect);
            ctx.Fill();
            ctx.SetColor(editor.ColorStyle.PlainText.Foreground);
            ctx.DrawTextLayout(layout, 1, 1);

            ctx.SetColor(editor.ColorStyle.PlainText.Background);
            ctx.Rectangle(1, 1, this.Size.Width - 3, this.Size.Height - 3);
            ctx.Stroke();

            ctx.SetColor(editor.ColorStyle.CollapsedText.Foreground);
            ctx.Rectangle(0, 0, this.Size.Width - 1, this.Size.Height - 1);
            ctx.Stroke();


            if (!window.HideCodeSegmentPreviewInformString)
            {
                informLayout.Text = CodeSegmentPreviewWindow.CodeSegmentPreviewInformString;
                var size = informLayout.GetSize();
                window.PreviewInformStringHeight = size.Height;

                ctx.SetColor(editor.ColorStyle.CollapsedText.Background);
                ctx.Rectangle(Size.Width - size.Width - 3,
                                        Size.Height - size.Height,
                                        size.Width + 2, size.Height - 1);
                ctx.Fill();
                ctx.SetColor(editor.ColorStyle.CollapsedText.Foreground);
                ctx.DrawTextLayout(informLayout, Size.Width - size.Width - 3,
                                     Size.Height - size.Height);
            }
        }

        protected override void Dispose(bool disposing)
        {
            layout.Dispose();
            informLayout.Dispose();

            base.Dispose(disposing);
        }
    }
}
