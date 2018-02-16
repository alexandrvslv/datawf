﻿// GutterMargin.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//

using System;
using Xwt;
using Xwt.Drawing;
using System.Linq;

namespace Mono.TextEditor
{
    public class GutterMargin : Margin
    {
        TextEditor editor;
        int width;
        int oldLineCountLog10 = -1;

        double fontHeight;

        public GutterMargin(TextEditor editor)
        {
            this.editor = editor;

            this.editor.Document.LineChanged += UpdateWidth;
            this.editor.Document.TextSet += HandleEditorDocumenthandleTextSet;
            this.editor.Caret.PositionChanged += EditorCarethandlePositionChanged;
        }

        void HandleEditorDocumenthandleTextSet(object sender, EventArgs e)
        {
            UpdateWidth(null, null);
        }

        void EditorCarethandlePositionChanged(object sender, DocumentLocationEventArgs e)
        {
            if (e.Location.Line == editor.Caret.Line)
                return;
            editor.RedrawMarginLine(this, e.Location.Line);
            editor.RedrawMarginLine(this, editor.Caret.Line);
        }

        int LineCountMax
        {
            get
            {
                return System.Math.Max(1000, editor.Document.LineCount);
            }
        }

        void CalculateWidth()
        {
            using (var layout = editor.LayoutCache.RequestLayout())
            {
                layout.Font = gutterFont;
                layout.SetText(LineCountMax.ToString());
                layout.Alignment = Alignment.Start;
                layout.Width = -1;
                var size = layout.GetSize();
                this.width = (int)size.Width + 4;
                if (!editor.Options.ShowFoldMargin)
                    this.width += 2;

                fontHeight = size.Height;
            }
        }

        void UpdateWidth(object sender, LineEventArgs args)
        {
            int currentLineCountLog10 = (int)System.Math.Log10(LineCountMax);
            if (oldLineCountLog10 != currentLineCountLog10)
            {
                CalculateWidth();
                oldLineCountLog10 = currentLineCountLog10;
                editor.Document.CommitUpdateAll();
            }
        }

        public override double Width
        {
            get { return width; }
        }

        DocumentLocation anchorLocation = new DocumentLocation(DocumentLocation.MinLine, DocumentLocation.MinColumn);
        internal protected override void MousePressed(MarginMouseEventArgs args)
        {
            base.MousePressed(args);

            if (args.Button != 1 || args.LineNumber < DocumentLocation.MinLine)
                return;
            editor.LockedMargin = this;
            int lineNumber = args.LineNumber;
            bool extendSelection = (args.ModifierState & Xwt.ModifierKeys.Shift) == Xwt.ModifierKeys.Shift;
            if (lineNumber <= editor.Document.LineCount)
            {
                DocumentLocation loc = new DocumentLocation(lineNumber, DocumentLocation.MinColumn);
                DocumentLine line = args.LineSegment;
                if (args.RawEvent is ButtonEventArgs && ((ButtonEventArgs)args.RawEvent).MultiplePress == 1)
                {
                    if (line != null)
                        editor.MainSelection = new Selection(loc, GetLineEndLocation(editor.GetTextEditorData(), lineNumber));
                }
                else if (extendSelection)
                {
                    if (!editor.IsSomethingSelected)
                    {
                        editor.MainSelection = new Selection(loc, loc);
                    }
                    else
                    {
                        editor.MainSelection = editor.MainSelection.WithLead(loc);
                    }
                }
                else
                {
                    anchorLocation = loc;
                    editor.ClearSelection();
                }
                editor.Caret.PreserveSelection = true;
                editor.Caret.Location = loc;
                editor.Caret.PreserveSelection = false;
            }
        }

        internal protected override void MouseReleased(MarginMouseEventArgs args)
        {
            editor.LockedMargin = null;
            base.MouseReleased(args);
        }

        public static DocumentLocation GetLineEndLocation(TextEditorData data, int lineNumber)
        {
            DocumentLine line = data.Document.GetLine(lineNumber);

            DocumentLocation result = new DocumentLocation(lineNumber, line.Length + 1);

            FoldSegment segment = null;
            foreach (FoldSegment folding in data.Document.GetStartFoldings(line))
            {
                if (folding.IsFolded && folding.Contains(data.Document.LocationToOffset(result)))
                {
                    segment = folding;
                    break;
                }
            }
            if (segment != null)
                result = data.Document.OffsetToLocation(segment.EndLine.Offset + segment.EndColumn - 1);
            return result;
        }

        internal protected override void MouseHover(MarginMouseEventArgs args)
        {
            base.MouseHover(args);

            if (!args.TriggersContextMenu() && args.Button == 1)
            {
                //	DocumentLocation loc = editor.Document.LogicalToVisualLocation (editor.GetTextEditorData (), editor.Caret.Location);

                int lineNumber = args.LineNumber >= DocumentLocation.MinLine ? args.LineNumber : editor.Document.LineCount;
                editor.Caret.PreserveSelection = true;
                editor.Caret.Location = new DocumentLocation(lineNumber, DocumentLocation.MinColumn);
                editor.MainSelection = new Selection(anchorLocation, editor.Caret.Location);
                editor.Caret.PreserveSelection = false;
            }
        }

        public override void Dispose()
        {
            if (base.cursor == null)
                return;

            base.cursor = null;

            this.editor.Document.TextSet -= HandleEditorDocumenthandleTextSet;
            this.editor.Document.LineChanged -= UpdateWidth;
            //			layout = layout.Kill ();
            base.Dispose();
        }

        Color lineNumberBgGC, lineNumberGC/*, lineNumberHighlightGC*/;

        Font gutterFont;

        internal protected override void OptionsChanged()
        {
            lineNumberBgGC = editor.ColorStyle.LineNumbers.Background;
            lineNumberGC = editor.ColorStyle.LineNumbers.Foreground;
            gutterFont = editor.Options.GutterFont;
            //			gutterFont.Weight = (Pango.Weight)editor.ColorStyle.LineNumbers.FontWeight;
            //			gutterFont.Style = (Pango.Style)editor.ColorStyle.LineNumbers.FontStyle;

            /*			if (Platform.IsWindows) {
                            gutterFont.Size = (int)(GtkWorkarounds.PangoScale  * 8.0 * editor.Options.Zoom);
                        } else {
                            gutterFont.Size = (int)(GtkWorkarounds.PangoScale  * 11.0 * editor.Options.Zoom);
                        }*/
            CalculateWidth();
        }

        void DrawGutterBackground(Xwt.Drawing.Context cr, int line, double x, double y, double lineHeight)
        {
            if (editor.Caret.Line == line)
            {
                editor.TextViewMargin.DrawCaretLineMarker(cr, x, y, Width, lineHeight);
                return;
            }
            cr.Rectangle(x, y, Width, lineHeight);
            cr.SetSourceColor(lineNumberBgGC);
            cr.Fill();
        }

        internal protected override void Draw(Xwt.Drawing.Context cr, Rectangle area, DocumentLine lineSegment, int line, double x, double y, double lineHeight)
        {
            var gutterMarker = lineSegment != null ? (MarginMarker)lineSegment.Markers.FirstOrDefault(marker => marker is MarginMarker && ((MarginMarker)marker).CanDraw(this)) : null;
            if (gutterMarker != null && gutterMarker.CanDrawBackground(this))
            {
                bool hasDrawn = gutterMarker.DrawBackground(editor, cr, new MarginDrawMetrics(this, area, lineSegment, line, x, y, lineHeight));
                if (!hasDrawn)
                    DrawGutterBackground(cr, line, x, y, lineHeight);
            }
            else
            {
                DrawGutterBackground(cr, line, x, y, lineHeight);
            }

            if (gutterMarker != null && gutterMarker.CanDrawForeground(this))
            {
                gutterMarker.DrawForeground(editor, cr, new MarginDrawMetrics(this, area, lineSegment, line, x, y, lineHeight));
                return;
            }

            if (line <= editor.Document.LineCount)
            {
                // Due to a mac? gtk bug I need to re-create the layout here
                // otherwise I get pango exceptions.
                using (var layout = editor.LayoutCache.RequestLayout())
                {
                    layout.Font = gutterFont;
                    layout.Width = (int)Width;
                    layout.Alignment = Alignment.End;
                    layout.SetText(line.ToString());
                    cr.Save();
                    cr.Translate(x + (int)Width + (editor.Options.ShowFoldMargin ? 0 : -2), y);
                    cr.SetSourceColor(lineNumberGC);
                    cr.ShowLayout(layout);
                    cr.Restore();
                }
            }
        }
    }
}
