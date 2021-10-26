//
// ViStatusArea.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using Xwt.Drawing;

namespace Mono.TextEditor.Vi
{
    class ViStatusArea : Canvas
    {
        TextEditor editor;
        bool showCaret;
        string statusText;

        public ViStatusArea(TextEditor editor)
        {
            this.editor = editor;
            editor.TextViewMargin.CaretBlink += HandleCaretBlink;
            editor.Caret.PositionChanged += HandlePositionChanged;

            editor.AddTopLevelWidget(this, 0, 0);
            editor[this].FixedPosition = true;
            Show();
        }

        void HandlePositionChanged(object sender, DocumentLocationEventArgs e)
        {
            QueueDraw();
        }

        void HandleCaretBlink(object sender, EventArgs e)
        {
            QueueDraw();
        }

        public void RemoveFromParentAndDestroy()
        {
            editor.RemoveChild(this);
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            editor.Caret.PositionChanged -= HandlePositionChanged;
            editor.TextViewMargin.CaretBlink -= HandleCaretBlink;
            base.Dispose(disposing);
        }

        Rectangle lastAllocation;
        public void AllocateArea(TextArea textArea, Rectangle allocation)
        {
            if (!Visible)
                Show();
            allocation.Height -= (int)textArea.LineHeight;
            if (lastAllocation.Width == allocation.Width &&
                lastAllocation.Height == allocation.Height || allocation.Height <= 1)
                return;
            lastAllocation = allocation;

            if (textArea.Bounds != allocation)
            {
                ((Canvas)textArea.Parent).SetChildBounds(textArea, allocation);
                //SizeRequest(allocation.Width, (int)editor.LineHeight);
                var pos = ((TextEditor.EditorContainerChild)editor[this]);
                if (pos.X != 0 || pos.Y != allocation.Height)
                    editor.MoveTopLevelWidget(this, 0, allocation.Height);
            }
        }

        public bool ShowCaret
        {
            get { return showCaret; }
            set
            {
                if (showCaret != value)
                {
                    showCaret = value;
                    editor.Caret.IsVisible = !showCaret;
                    editor.RequestResetCaretBlink();
                    QueueDraw();
                }
            }
        }

        public string Message
        {
            get { return statusText; }
            set
            {
                if (statusText == value)
                    return;
                statusText = value;
                if (showCaret)
                {
                    editor.RequestResetCaretBlink();
                }
                QueueDraw();
            }
        }

        protected override void OnDraw(Xwt.Drawing.Context cr, Rectangle bound)
        {
            {
                cr.Rectangle(bound.X, bound.Y, bound.Width, bound.Height);
                cr.SetSourceColor(editor.ColorStyle.PlainText.Background);
                cr.Fill();
                using (var layout = PangoUtil.CreateLayout(editor))
                {
                    layout.Font = editor.Options.Font;

                    layout.Text = "000,00-00";
                    var mins = layout.GetSize();

                    var line = editor.GetLine(editor.Caret.Line);
                    var visColumn = line.GetVisualColumn(editor.GetTextEditorData(), editor.Caret.Column);

                    if (visColumn != editor.Caret.Column)
                    {
                        layout.Text = editor.Caret.Line + "," + editor.Caret.Column + "-" + visColumn;
                    }
                    else
                    {
                        layout.Text = editor.Caret.Line + "," + editor.Caret.Column;
                    }

                    var statuss = layout.GetSize();

                    statuss.Width = System.Math.Max(statuss.Width, mins.Width);

                    statuss.Width += 8;
                    cr.MoveTo(Size.Width - statuss.Width, 0);
                    statuss.Width += 8;
                    cr.SetSourceColor(editor.ColorStyle.PlainText.Foreground);
                    cr.ShowLayout(layout);

                    layout.Text = statusText ?? "";
                    var size = layout.GetSize();
                    var x = System.Math.Min(0, -size.Width + Size.Width - editor.TextViewMargin.CharWidth - statuss.Width);
                    cr.MoveTo(x, 0);
                    cr.SetSourceColor(editor.ColorStyle.PlainText.Foreground);
                    cr.ShowLayout(layout);
                    if (ShowCaret)
                    {
                        if (editor.TextViewMargin.caretBlink)
                        {
                            cr.Rectangle(size.Width + x, 0, (int)editor.TextViewMargin.CharWidth, (int)editor.LineHeight);
                            cr.Fill();
                        }
                    }
                }
            }
        }
    }
}
