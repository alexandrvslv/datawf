//
// TextArea.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

//#define DEBUG_EXPOSE

using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor.PopupWindow;
using Mono.TextEditor.Theatrics;

using Xwt.Drawing;
using Xwt;

namespace Mono.TextEditor
{
    public class TextArea : Canvas, ITextEditorDataProvider
    {
        private TextEditorData textEditorData;
        private TextEditor editor;
        protected IconMargin iconMargin;
        protected ActionMargin actionMargin;
        protected GutterMargin gutterMargin;
        protected FoldMarkerMargin foldMarkerMargin;
        protected TextViewMargin textViewMargin;

        private DocumentLine longestLine = null;
        private double longestLineWidth = -1;

        private List<Margin> margins = new List<Margin>();
        private int oldRequest = -1;

        private bool isDisposed = false;
        //IMMulticontext imContext;
        //bool imContextNeedsReset;
        //KeyEventArgs lastIMEvent;
        //Key lastIMEventMappedKey;
        //uint lastIMEventMappedChar;
        //Xwt.ModifierKeys lastIMEventMappedModifier;
        private bool sizeHasBeenAllocated;
        private string currentStyleName;

        private double mx, my;
        private IDisposable scrollWindowTimer = null;
        private double scrollWindowTimer_x;
        private double scrollWindowTimer_y;
        private Xwt.ModifierKeys scrollWindowTimer_mod;

        public TextDocument Document
        {
            get { return textEditorData.Document; }
        }

        public bool IsDisposed
        {
            get { return textEditorData.IsDisposed; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Mono.TextEditor.TextEditor"/> converts tabs to spaces.
        /// It is possible to overwrite the default options value for certain languages (like F#).
        /// </summary>
        /// <value>
        /// <c>true</c> if tabs to spaces should be converted; otherwise, <c>false</c>.
        /// </value>
        public bool TabsToSpaces
        {
            get { return textEditorData.TabsToSpaces; }
            set { textEditorData.TabsToSpaces = value; }
        }

        public Mono.TextEditor.Caret Caret
        {
            get { return textEditorData.Caret; }
        }

        //protected internal IMMulticontext IMContext
        //{
        //    get { return imContext; }
        //}

        public MenuItem CreateInputMethodMenuItem(string label)
        {
            MenuItem imContextMenuItem = new MenuItem(label);
            Menu imContextMenu = new Menu();
            imContextMenuItem.SubMenu = imContextMenu;
            //IMContext.AppendMenuitems(imContextMenu);
            return imContextMenuItem;
        }

        //[DllImport(PangoUtil.LIBGTK, CallingConvention = CallingConvention.Cdecl)]
        //static extern void gtk_im_multicontext_set_context_id(IntPtr context, string context_id);

        //[DllImport(PangoUtil.LIBGTK, CallingConvention = CallingConvention.Cdecl)]
        //static extern string gtk_im_multicontext_get_context_id(IntPtr context);

        //[GLib.Property("im-module")]
        //public string IMModule
        //{
        //    get
        //    {
        //        if (GtkWorkarounds.GtkMinorVersion < 16 || imContext == null)
        //            return null;
        //        return gtk_im_multicontext_get_context_id(imContext.Handle);
        //    }
        //    set
        //    {
        //        if (GtkWorkarounds.GtkMinorVersion < 16 || imContext == null)
        //            return;
        //        gtk_im_multicontext_set_context_id(imContext.Handle, value);
        //    }
        //}

        public ITextEditorOptions Options
        {
            get { return textEditorData.Options; }
            set
            {
                if (textEditorData.Options != null)
                    textEditorData.Options.Changed -= OptionsChanged;
                textEditorData.Options = value;
                if (textEditorData.Options != null)
                {
                    textEditorData.Options.Changed += OptionsChanged;
                    OptionsChanged(null, null);
                }
            }
        }


        public string FileName
        {
            get { return Document.FileName; }
        }

        public string MimeType
        {
            get { return Document.MimeType; }
        }

        void HandleTextEditorDataDocumentMarkerChange(object sender, TextMarkerEvent e)
        {
            if (e.TextMarker is IExtendingTextLineMarker)
            {
                int lineNumber = e.Line.LineNumber;
                if (lineNumber <= LineCount)
                {
                    try
                    {
                        textEditorData.HeightTree.SetLineHeight(lineNumber, GetLineHeight(e.Line));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }

        void HAdjustmentValueChanged(object sender, EventArgs args)
        {
            var alloc = this.Bounds;
            alloc.X = alloc.Y = 0;

            HAdjustmentValueChanged();
        }

        protected virtual void HAdjustmentValueChanged()
        {
            HideTooltip(false);
            double value = textEditorData.HAdjustment.Value;
            if (value != Math.Round(value))
            {
                value = System.Math.Round(value);
                this.textEditorData.HAdjustment.Value = value;
            }
            textViewMargin.HideCodeSegmentPreviewWindow();
            QueueDraw((int)textViewMargin.XOffset, 0, this.Bounds.Width - (int)this.textViewMargin.XOffset, this.Bounds.Height);
            OnHScroll(EventArgs.Empty);
            SetChildrenPositions(Bounds);
        }

        void VAdjustmentValueChanged(object sender, EventArgs args)
        {
            var alloc = this.Bounds;
            alloc.X = alloc.Y = 0;

            VAdjustmentValueChanged();
            SetChildrenPositions(alloc);
        }

        protected virtual void VAdjustmentValueChanged()
        {
            HideTooltip(false);
            textViewMargin.HideCodeSegmentPreviewWindow();
            double value = this.textEditorData.VAdjustment.Value;
            if (value != System.Math.Round(value))
            {
                value = System.Math.Round(value);
                this.textEditorData.VAdjustment.Value = value;
            }
            if (isMouseTrapped)
                FireMotionEvent(mx + textViewMargin.XOffset, my, lastState);

            double delta = value - this.oldVadjustment;
            oldVadjustment = value;
            TextViewMargin.caretY -= delta;

            this.QueueDraw();
            if (System.Math.Abs(delta) >= Bounds.Height - this.LineHeight * 2 || this.TextViewMargin.InSelectionDrag)
            {

                OnVScroll(EventArgs.Empty);
                return;
            }

            OnVScroll(EventArgs.Empty);
        }

        protected virtual void OnVScroll(EventArgs e)
        {
            if (VScroll != null)
                VScroll(this, e);
        }

        protected virtual void OnHScroll(EventArgs e)
        {
            if (HScroll != null)
                HScroll(this, e);
        }

        public event EventHandler VScroll;
        public event EventHandler HScroll;

        void UnregisterAdjustments()
        {
            if (textEditorData.HAdjustment != null)
                textEditorData.HAdjustment.ValueChanged -= HAdjustmentValueChanged;
            if (textEditorData.VAdjustment != null)
                textEditorData.VAdjustment.ValueChanged -= VAdjustmentValueChanged;
        }

        protected override bool SupportsCustomScrolling
        {
            get { return true; }
        }

        protected override void SetScrollAdjustments(ScrollAdjustment horizontal, ScrollAdjustment vertical)
        {
            if (textEditorData == null)
                return;
            UnregisterAdjustments();
            base.SetScrollAdjustments(horizontal, vertical);

            if (horizontal == null || vertical == null)
                return;

            this.textEditorData.HAdjustment = horizontal;
            this.textEditorData.VAdjustment = vertical;

            this.textEditorData.HAdjustment.ValueChanged += HAdjustmentValueChanged;
            this.textEditorData.VAdjustment.ValueChanged += VAdjustmentValueChanged;
        }

        internal void SetScrollAdjustmentsInternal(ScrollAdjustment horizontal, ScrollAdjustment vertical)
        {
            SetScrollAdjustments(horizontal, vertical);
        }

        internal TextArea(TextDocument doc, ITextEditorOptions options, EditMode initialMode)
        {
            base.CanGetFocus = true;

            // This is required to properly handle resizing and rendering of children
            //ResizeMode = ResizeMode.Queue;
        }


        internal void Initialize(TextEditor editor, TextDocument doc, ITextEditorOptions options, EditMode initialMode)
        {
            if (doc == null)
                throw new ArgumentNullException("doc");
            this.editor = editor;
            textEditorData = new TextEditorData(doc);
            textEditorData.RecenterEditor += delegate
            {
                CenterToCaret();
                StartCaretPulseAnimation();
            };
            textEditorData.Document.TextReplaced += OnDocumentStateChanged;
            textEditorData.Document.TextSet += OnTextSet;
            textEditorData.Document.LineChanged += UpdateLinesOnTextMarkerHeightChange;
            textEditorData.Document.MarkerAdded += HandleTextEditorDataDocumentMarkerChange;
            textEditorData.Document.MarkerRemoved += HandleTextEditorDataDocumentMarkerChange;

            textEditorData.CurrentMode = initialMode;

            this.textEditorData.Options = options ?? TextEditorOptions.DefaultOptions;


            textEditorData.Parent = editor;

            iconMargin = new IconMargin(editor);
            gutterMargin = new GutterMargin(editor);
            actionMargin = new ActionMargin(editor);
            foldMarkerMargin = new FoldMarkerMargin(editor);
            textViewMargin = new TextViewMargin(editor);

            margins.Add(iconMargin);
            margins.Add(gutterMargin);
            margins.Add(actionMargin);
            margins.Add(foldMarkerMargin);

            margins.Add(textViewMargin);
            this.textEditorData.SelectionChanged += TextEditorDataSelectionChanged;
            this.textEditorData.UpdateAdjustmentsRequested += TextEditorDatahandleUpdateAdjustmentsRequested;
            Document.DocumentUpdated += DocumentUpdatedHandler;

            this.textEditorData.Options.Changed += OptionsChanged;

            SetDragDropTarget(DragDropAction.Move | DragDropAction.Copy, TransferDataType.Text);

            //imContext = new IMMulticontext();
            //imContext.Commit += IMCommit;
            //imContext.UsePreedit = true;
            //imContext.PreeditChanged += PreeditStringChanged;
            //imContext.RetrieveSurrounding += delegate (object o, RetrieveSurroundingArgs args)
            //{
            //    //use a single line of context, whole document would be very expensive
            //    //FIXME: UTF16 surrogates handling for caret offset? only matters for astral plane
            //    imContext.SetSurrounding(Document.GetLineText(Caret.Line, false), Caret.Column);
            //    args.RetVal = true;
            //};
            //imContext.SurroundingDeleted += delegate (object o, SurroundingDeletedArgs args)
            //{
            //    //FIXME: UTF16 surrogates handling for offset and NChars? only matters for astral plane
            //    var line = Document.GetLine(Caret.Line);
            //    Document.Remove(line.Offset + args.Offset, args.NChars);
            //    args.RetVal = true;
            //};

            //using (Pixmap inv = new Pixmap(null, 1, 1, 1))
            //{
            //    invisibleCursor = new Cursor(inv, inv, Gdk.Color.Zero, Gdk.Color.Zero, 0, 0);
            //}

            InitAnimations();
            this.Document.EndUndo += HandleDocumenthandleEndUndo;
            this.textEditorData.HeightTree.LineUpdateFrom += delegate (object sender, HeightTree.HeightChangedEventArgs e)
            {
                //Console.WriteLine ("redraw from :" + e.Line);
                RedrawFromLine(e.Line);
            };

            OptionsChanged(this, EventArgs.Empty);

            Caret.PositionChanged += CaretPositionChanged;

            SetWidgetBgFromStyle();
        }


        public void RunAction(Action<TextEditorData> action)
        {
            try
            {
                action(GetTextEditorData());
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while executing " + action + " :" + e);
            }
        }

        void HandleDocumenthandleEndUndo(object sender, TextDocument.UndoOperationEventArgs e)
        {
            if (this.Document.HeightChanged)
            {
                this.Document.HeightChanged = false;
                SetAdjustments();
            }
        }

        void TextEditorDatahandleUpdateAdjustmentsRequested(object sender, EventArgs e)
        {
            SetAdjustments();
        }


        public void ShowListWindow<T>(ListWindow<T> window, DocumentLocation loc)
        {
            var p = LocationToPoint(loc);
            p.X -= window.TextOffset;
            p.Y += LineHeight;

            window.Location = ConvertToScreenCoordinates(p);
            window.Show();
        }

        internal int preeditOffset = -1, preeditLine, preeditCursorCharIndex;
        internal string preeditString;
        internal FastPangoAttrList preeditAttrs;
        internal bool preeditHeightChange;

        internal bool ContainsPreedit(int offset, int length)
        {
            if (string.IsNullOrEmpty(preeditString))
                return false;

            return offset <= preeditOffset && preeditOffset <= offset + length;
        }

        void PreeditStringChanged(object sender, EventArgs e)
        {
            //imContext.GetPreeditString(out preeditString, out preeditAttrs, out preeditCursorCharIndex);
            if (!string.IsNullOrEmpty(preeditString))
            {
                if (preeditOffset < 0)
                {
                    preeditOffset = Caret.Offset;
                    preeditLine = Caret.Line;
                }
                if (UpdatePreeditLineHeight())
                    QueueDraw();
            }
            else
            {
                preeditOffset = -1;
                preeditString = null;
                preeditAttrs = null;
                preeditCursorCharIndex = 0;
                if (UpdatePreeditLineHeight())
                    QueueDraw();
            }
            this.textViewMargin.ForceInvalidateLine(preeditLine);
            this.textEditorData.Document.CommitLineUpdate(preeditLine);
        }

        internal bool UpdatePreeditLineHeight()
        {
            if (!string.IsNullOrEmpty(preeditString))
            {
                using (var preeditLayout = new TextLayout(this))
                {
                    preeditLayout.Text = preeditString;
                    preeditAttrs.AssignTo(preeditLayout);
                    var size = preeditLayout.GetSize();
                    var calcHeight = System.Math.Ceiling(size.Height);
                    if (LineHeight < calcHeight)
                    {
                        textEditorData.HeightTree.SetLineHeight(preeditLine, calcHeight);
                        preeditHeightChange = true;
                        return true;
                    }
                }
            }
            else if (preeditHeightChange)
            {
                preeditHeightChange = false;
                textEditorData.HeightTree.Rebuild();
                return true;
            }
            return false;
        }

        void CaretPositionChanged(object sender, DocumentLocationEventArgs args)
        {
            HideTooltip();
            //ResetIMContext();

            if (Caret.AutoScrollToCaret && HasFocus)
                ScrollToCaret();

            //			Rectangle rectangle = textViewMargin.GetCaretRectangle (Caret.Mode);
            RequestResetCaretBlink();

            textEditorData.CurrentMode.InternalCaretPositionChanged(textEditorData.Parent, textEditorData);

            if (!IsSomethingSelected)
            {
                if (/*Options.HighlightCaretLine && */args.Location.Line != Caret.Line)
                    RedrawMarginLine(TextViewMargin, args.Location.Line);
                RedrawMarginLine(TextViewMargin, Caret.Line);
            }
        }

        Selection oldSelection = Selection.Empty;
        void TextEditorDataSelectionChanged(object sender, EventArgs args)
        {
            if (IsSomethingSelected)
            {
                var selectionRange = MainSelection.GetSelectionRange(textEditorData);
                if (selectionRange.Offset >= 0 && selectionRange.EndOffset < Document.TextLength)
                {
                    ClipboardActions.CopyToPrimary(this.textEditorData);
                }
                else
                {
                    ClipboardActions.ClearPrimary();
                }
            }
            else
            {
                ClipboardActions.ClearPrimary();
            }
            // Handle redraw
            Selection selection = MainSelection;
            int startLine = !selection.IsEmpty ? selection.Anchor.Line : -1;
            int endLine = !selection.IsEmpty ? selection.Lead.Line : -1;
            int oldStartLine = !oldSelection.IsEmpty ? oldSelection.Anchor.Line : -1;
            int oldEndLine = !oldSelection.IsEmpty ? oldSelection.Lead.Line : -1;
            if (SelectionMode == SelectionMode.Block)
            {
                this.RedrawMarginLines(this.textViewMargin,
                                        System.Math.Min(System.Math.Min(oldStartLine, oldEndLine), System.Math.Min(startLine, endLine)),
                                        System.Math.Max(System.Math.Max(oldStartLine, oldEndLine), System.Math.Max(startLine, endLine)));
            }
            else
            {
                if (endLine < 0 && startLine >= 0)
                    endLine = Document.LineCount;
                if (oldEndLine < 0 && oldStartLine >= 0)
                    oldEndLine = Document.LineCount;
                int from = oldEndLine, to = endLine;
                if (!selection.IsEmpty && !oldSelection.IsEmpty)
                {
                    if (startLine != oldStartLine && endLine != oldEndLine)
                    {
                        from = System.Math.Min(startLine, oldStartLine);
                        to = System.Math.Max(endLine, oldEndLine);
                    }
                    else if (startLine != oldStartLine)
                    {
                        from = startLine;
                        to = oldStartLine;
                    }
                    else if (endLine != oldEndLine)
                    {
                        from = endLine;
                        to = oldEndLine;
                    }
                    else if (startLine == oldStartLine && endLine == oldEndLine)
                    {
                        if (selection.Anchor == oldSelection.Anchor)
                        {
                            this.RedrawMarginLine(this.textViewMargin, endLine);
                        }
                        else if (selection.Lead == oldSelection.Lead)
                        {
                            this.RedrawMarginLine(this.textViewMargin, startLine);
                        }
                        else
                        { // 3rd case - may happen when changed programmatically
                            this.RedrawMarginLine(this.textViewMargin, endLine);
                            this.RedrawMarginLine(this.textViewMargin, startLine);
                        }
                        from = to = -1;
                    }
                }
                else
                {
                    if (selection.IsEmpty)
                    {
                        from = oldStartLine;
                        to = oldEndLine;
                    }
                    else if (oldSelection.IsEmpty)
                    {
                        from = startLine;
                        to = endLine;
                    }
                }

                if (from >= 0 && to >= 0)
                {
                    this.RedrawMarginLines(this.textViewMargin,
                                            System.Math.Max(0, System.Math.Min(from, to) - 1),
                                            System.Math.Max(from, to));
                }
            }
            oldSelection = selection;
            OnSelectionChanged(EventArgs.Empty);
        }

        //internal void ResetIMContext()
        //{
        //    if (imContextNeedsReset)
        //    {
        //        imContext.Reset();
        //        imContextNeedsReset = false;
        //    }
        //}

        //void IMCommit(object sender, Gtk.CommitArgs ca)
        //{
        //    if (!IsRealized || !IsFocus)
        //        return;

        //    //this, if anywhere, is where we should handle UCS4 conversions
        //    for (int i = 0; i < ca.Str.Length; i++)
        //    {
        //        int utf32Char;
        //        if (char.IsHighSurrogate(ca.Str, i))
        //        {
        //            utf32Char = char.ConvertToUtf32(ca.Str, i);
        //            i++;
        //        }
        //        else
        //        {
        //            utf32Char = (int)ca.Str[i];
        //        }

        //        //include the other pre-IM state *if* the post-IM char matches the pre-IM (key-mapped) one
        //        if (lastIMEventMappedChar == utf32Char && lastIMEventMappedChar == (uint)lastIMEventMappedKey)
        //        {
        //            editor.OnIMProcessedKeyPressEvent(lastIMEventMappedKey, lastIMEventMappedChar, lastIMEventMappedModifier);
        //        }
        //        else
        //        {
        //            editor.OnIMProcessedKeyPressEvent((Key)0, (uint)utf32Char, Xwt.ModifierKeys.None);
        //        }
        //    }

        //    //the IME can commit while there's still a pre-edit string
        //    //since we cached the pre-edit offset when it started, need to update it
        //    if (preeditOffset > -1)
        //    {
        //        preeditOffset = Caret.Offset;
        //    }
        //}

        protected override void OnGotFocus(EventArgs args)
        {
            base.OnGotFocus(args);
            //imContextNeedsReset = true;
            //IMContext.FocusIn();
            RequestResetCaretBlink();
            Document.CommitLineUpdate(Caret.Line);
        }

        IDisposable focusOutTimerId = null;
        void RemoveFocusOutTimerId()
        {
            if (focusOutTimerId == null)
                return;
            focusOutTimerId.Dispose();
            focusOutTimerId = null;
        }

        protected override void OnLostFocus(EventArgs args)
        {
            base.OnLostFocus(args);
            //imContextNeedsReset = true;
            //imContext.FocusOut();
            RemoveFocusOutTimerId();

            if (tipWindow != null && currentTooltipProvider != null)
            {
                if (!currentTooltipProvider.IsInteractive(textEditorData.Parent, tipWindow))
                    DelayedHideTooltip();
            }
            else
            {
                HideTooltip();
            }

            TextViewMargin.StopCaretThread();
            Document.CommitLineUpdate(Caret.Line);
        }

        void DocumentUpdatedHandler(object sender, EventArgs args)
        {
            foreach (DocumentUpdateRequest request in Document.UpdateRequests)
            {
                request.Update(textEditorData.Parent);
            }
        }

        public event EventHandler EditorOptionsChanged;

        protected virtual void OptionsChanged(object sender, EventArgs args)
        {
            if (Options == null)
                return;
            if (currentStyleName != Options.ColorScheme)
            {
                currentStyleName = Options.ColorScheme;
                this.textEditorData.ColorStyle = Options.GetColorStyle();
                SetWidgetBgFromStyle();
            }

            iconMargin.IsVisible = Options.ShowIconMargin;
            gutterMargin.IsVisible = Options.ShowLineNumberMargin;
            foldMarkerMargin.IsVisible = Options.ShowFoldMargin || Options.EnableQuickDiff;
            //			dashedLineMargin.IsVisible = foldMarkerMargin.IsVisible || gutterMargin.IsVisible;

            if (EditorOptionsChanged != null)
                EditorOptionsChanged(this, args);

            textViewMargin.OptionsChanged();
            foreach (Margin margin in this.margins)
            {
                if (margin == textViewMargin)
                    continue;
                margin.OptionsChanged();
            }
            SetAdjustments(Bounds);
            textEditorData.HeightTree.Rebuild();
            this.QueueForReallocate();
        }

        void SetWidgetBgFromStyle()
        {
            // This is a hack around a problem with repainting the drag widget.
            // When this is not set a white square is drawn when the drag widget is moved
            // when the bg color is differs from the color style bg color (e.g. oblivion style)
            if (this.textEditorData.ColorStyle != null)
            {
                settingWidgetBg = true; //prevent infinite recusion

                Widget parent = this;
                while (parent.Parent != null && !(parent is ScrollView))
                {
                    parent = parent.Parent;
                }

                if (parent != null)
                {
                    parent.BackgroundColor = textEditorData.ColorStyle.PlainText.Background;
                }

                BackgroundColor = textEditorData.ColorStyle.PlainText.Background;
                settingWidgetBg = false;
            }
        }

        bool settingWidgetBg = false;


        protected override void Dispose(bool disposing)
        {
            CancelScheduledHide();
            if (popupWindow != null)
                popupWindow.Dispose();

            HideTooltip();
            Document.EndUndo -= HandleDocumenthandleEndUndo;
            Document.TextReplaced -= OnDocumentStateChanged;
            Document.TextSet -= OnTextSet;
            Document.LineChanged -= UpdateLinesOnTextMarkerHeightChange;
            Document.MarkerAdded -= HandleTextEditorDataDocumentMarkerChange;
            Document.MarkerRemoved -= HandleTextEditorDataDocumentMarkerChange;

            DisposeAnimations();

            RemoveFocusOutTimerId();
            RemoveScrollWindowTimer();

            Caret.PositionChanged -= CaretPositionChanged;

            Document.DocumentUpdated -= DocumentUpdatedHandler;
            if (textEditorData.Options != null)
                textEditorData.Options.Changed -= OptionsChanged;

            //if (imContext != null)
            //{
            //    ResetIMContext();
            //    imContext = imContext.Kill(x => x.Commit -= IMCommit);
            //}

            UnregisterAdjustments();

            foreach (Margin margin in this.margins)
            {
                if (margin is IDisposable)
                    ((IDisposable)margin).Dispose();
            }
            textEditorData.ClearTooltipProviders();

            this.textEditorData.SelectionChanged -= TextEditorDataSelectionChanged;
            this.textEditorData.Dispose();
            longestLine = null;

            base.Dispose(disposing);
        }

        public void RedrawMargin(Margin margin)
        {
            if (isDisposed)
                return;
            QueueDraw((int)margin.XOffset, 0, GetMarginWidth(margin), this.Bounds.Height);
        }

        public void RedrawMarginLine(Margin margin, int logicalLine)
        {
            if (isDisposed)
                return;

            double y = LineToY(logicalLine) - this.textEditorData.VAdjustment.Value;
            double h = GetLineHeight(logicalLine);

            if (y + h > 0)
                QueueDraw((int)margin.XOffset, (int)y, (int)GetMarginWidth(margin), (int)h);
        }

        double GetMarginWidth(Margin margin)
        {
            if (margin.Width < 0)
                return Bounds.Width - margin.XOffset;
            return (int)margin.Width;
        }

        internal void RedrawLine(int logicalLine)
        {
            if (isDisposed || logicalLine > LineCount || logicalLine < DocumentLocation.MinLine)
                return;
            double y = LineToY(logicalLine) - this.textEditorData.VAdjustment.Value;
            double h = GetLineHeight(logicalLine);

            if (y + h > 0)
                QueueDraw(0, y, this.Bounds.Width, h);
        }

        public void QueueDraw(double x, double y, double w, double h)
        {
            base.QueueDraw(new Rectangle(x, y, w, h));
#if DEBUG_EXPOSE
				Console.WriteLine ("invalidated {0},{1} {2}x{3}", x, y, w, h);
#endif
        }

        public new void QueueDraw()
        {
            base.QueueDraw();
#if DEBUG_EXPOSE
				Console.WriteLine ("invalidated entire widget");
#endif
        }

        internal void RedrawPosition(int logicalLine, int logicalColumn)
        {
            if (isDisposed)
                return;
            //				Console.WriteLine ("Redraw position: logicalLine={0}, logicalColumn={1}", logicalLine, logicalColumn);
            RedrawLine(logicalLine);
        }

        public void RedrawMarginLines(Margin margin, int start, int end)
        {
            if (isDisposed)
                return;
            if (start < 0)
                start = 0;
            double visualStart = -this.textEditorData.VAdjustment.Value + LineToY(start);
            if (end < 0)
                end = Document.LineCount;
            double visualEnd = -this.textEditorData.VAdjustment.Value + LineToY(end) + GetLineHeight(end);
            QueueDraw((int)margin.XOffset, (int)visualStart, GetMarginWidth(margin), (int)(visualEnd - visualStart));
        }

        internal void RedrawLines(int start, int end)
        {
            //			Console.WriteLine ("redraw lines: start={0}, end={1}", start, end);
            if (isDisposed)
                return;
            if (start < 0)
                start = 0;
            double visualStart = -this.textEditorData.VAdjustment.Value + LineToY(start);
            if (end < 0)
                end = Document.LineCount;
            double visualEnd = -this.textEditorData.VAdjustment.Value + LineToY(end) + GetLineHeight(end);
            QueueDraw(0, (int)visualStart, this.Bounds.Width, (int)(visualEnd - visualStart));
        }

        public void RedrawFromLine(int logicalLine)
        {
            //			Console.WriteLine ("Redraw from line: logicalLine={0}", logicalLine);
            if (isDisposed)
                return;
            int y = System.Math.Max(0, (int)(-this.textEditorData.VAdjustment.Value + LineToY(logicalLine)));
            QueueDraw(0, y,
                           this.Bounds.Width, this.Bounds.Height - y);
        }

        internal bool IsInKeypress
        {
            get;
            set;
        }

        /// <summary>Handles key input after key mapping and input methods.</summary>
        /// <param name="key">The mapped keycode.</param>
        /// <param name="unicodeChar">A UCS4 character. If this is nonzero, it overrides the keycode.</param>
        /// <param name="modifier">Keyboard modifier, excluding any consumed by key mapping or IM.</param>
        public void SimulateKeyPress(Key key, int unicodeChar, ModifierKeys modifier)
        {
            IsInKeypress = true;
            try
            {
                var filteredModifiers = modifier & (ModifierKeys.Shift | ModifierKeys.Alt
                                                    | ModifierKeys.Control);//| ModifierKeys.Command
                CurrentMode.InternalHandleKeypress(textEditorData.Parent, textEditorData, key, unicodeChar, filteredModifiers);
            }
            finally
            {
                IsInKeypress = false;
            }
            RequestResetCaretBlink();

        }

        //bool IMFilterKeyPress(KeyEventArgs evt, Key mappedKey, uint mappedChar, Xwt.ModifierKeys mappedModifiers)
        //{
        //    if (lastIMEvent == evt)
        //        return false;

        //    if (evt.Type == EventType.KeyPress)
        //    {
        //        lastIMEvent = evt;
        //        lastIMEventMappedChar = mappedChar;
        //        lastIMEventMappedKey = mappedKey;
        //        lastIMEventMappedModifier = mappedModifiers;
        //    }

        //    if (imContext.FilterKeypress(evt))
        //    {
        //        imContextNeedsReset = true;
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        internal void HideMouseCursor()
        {
            SetCursor(CursorType.Invisible);
        }

        CursorType currentCursor;

        /// <summary>
        /// Sets the mouse cursor of the gdk window and avoids unnecessary native calls.
        /// </summary>
        void SetCursor(CursorType cursor)
        {
            Cursor = currentCursor = cursor ?? CursorType.Arrow;
        }

        protected override void OnKeyPressed(KeyEventArgs evt)
        {
            var key = evt.Key;
            var keyVal = evt.NativeKeyCode;
            var mod = Keyboard.CurrentModifiers;
            var shortcut = new KeyboardShortcut(key, mod);
            Console.WriteLine($"{mod} {key} {key:x} {keyVal}");
            //HACK: we never call base.OnKeyPressEvent, so implement the popup key manually
            if (key == Key.Menu || (key == Key.F10 && mod.HasFlag(ModifierKeys.Shift)))
            {
                OnPopupMenu();
                return;
            }
            if ((int)key > 0xff00)
            {
                keyVal = 0;
            }

            //CurrentMode.SelectValidShortcut(accels, out key, out mod);
            if (key == Key.F1 && (mod & (ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.Control)
            {
                var p = LocationToPoint(Caret.Location);
                ShowTooltip(Xwt.ModifierKeys.None, Caret.Offset, p.X, p.Y);
                return;
            }
            if (key == Key.F2 && textViewMargin.IsCodeSegmentPreviewWindowShown)
            {
                textViewMargin.OpenCodeSegmentEditor();
                return;
            }

            if (CurrentMode.WantsToPreemptIM || CurrentMode.PreemptIM(key, keyVal, mod))
            {
                //ResetIMContext();
                //FIXME: should call base.OnKeyPressEvent when SimulateKeyPress didn't handle the event
                SimulateKeyPress(key, keyVal, mod);
                return;
            }

            //FIXME: OnIMProcessedKeyPressEvent should return false when it didn't handle the event
            if (editor.OnIMProcessedKeyPressEvent(key, keyVal, mod))
                return;

            base.OnKeyPressed(evt);
        }


        protected override void OnKeyReleased(KeyEventArgs evnt)
        {
            //if (IMFilterKeyPress(evnt, 0, 0, ModifierType.None))
            //{
            //    imContextNeedsReset = true;
            //}
        }

        int mouseButtonPressed = 0;
        int lastTime;
        double pressPositionX, pressPositionY;
        protected override void OnButtonPressed(ButtonEventArgs e)
        {
            if (overChildWidget)
                return;

            pressPositionX = e.X;
            pressPositionY = e.Y;
            base.SetFocus();

            mouseButtonPressed = (int)e.Button;
            double startPos;
            Margin margin = GetMarginAtX(e.X, out startPos);
            if (margin == textViewMargin)
            {
                //main context menu
                if (DoPopupMenu != null && e.Button == PointerButton.Right)
                {
                    DoClickedPopupMenu(e);
                    return;
                }
            }
            if (margin != null)
                margin.MousePressed(new MarginMouseEventArgs(textEditorData.Parent, e, (int)e.Button, e.X - startPos, e.Y, Keyboard.CurrentModifiers));

            base.OnButtonPressed(e);
        }

        bool DoClickedPopupMenu(ButtonEventArgs e)
        {
            double tmOffset = e.X - textViewMargin.XOffset;
            if (tmOffset >= 0)
            {
                DocumentLocation loc;
                if (textViewMargin.CalculateClickLocation(tmOffset, e.Y, out loc))
                {
                    if (!this.IsSomethingSelected || !this.SelectionRange.Contains(Document.LocationToOffset(loc)))
                    {
                        Caret.Location = loc;
                    }
                }
                DoPopupMenu(e);
                this.ResetMouseState();
                return true;
            }
            return false;
        }

        public Action<ButtonEventArgs> DoPopupMenu { get; set; }

        protected bool OnPopupMenu()
        {
            if (DoPopupMenu != null)
            {
                DoPopupMenu(null);
                return true;
            }
            return false;
        }

        public Margin LockedMargin
        {
            get;
            set;
        }

        Margin GetMarginAtX(double x, out double startingPos)
        {
            double curX = 0;
            foreach (Margin margin in this.margins)
            {
                if (!margin.IsVisible)
                    continue;
                if (LockedMargin != null)
                {
                    if (LockedMargin == margin)
                    {
                        startingPos = curX;
                        return margin;
                    }
                }
                else
                {
                    if (curX <= x && (x <= curX + margin.Width || margin.Width < 0))
                    {
                        startingPos = curX;
                        return margin;
                    }
                }
                curX += margin.Width;
            }
            startingPos = -1;
            return null;
        }

        protected override void OnButtonReleased(ButtonEventArgs e)
        {
            RemoveScrollWindowTimer();

            //main context menu
            if (DoPopupMenu != null && e.Button == PointerButton.Right)
            {
                return;
            }
            double startPos;
            Margin margin = GetMarginAtX(e.X, out startPos);
            if (margin != null)
                margin.MouseReleased(new MarginMouseEventArgs(textEditorData.Parent, e, (int)e.Button, e.X - startPos, e.Y, Keyboard.CurrentModifiers));
            ResetMouseState();
            base.OnButtonReleased(e);
        }

        /// <summary>
        /// Use this method with care.
        /// </summary>
        public void ResetMouseState()
        {
            mouseButtonPressed = 0;
            textViewMargin.inDrag = false;
            textViewMargin.InSelectionDrag = false;
        }

        bool dragOver = false;
        //ClipboardActions.CopyOperation dragContents = null;
        DocumentLocation defaultCaretPos, dragCaretPos;
        Selection selection = Selection.Empty;

        public bool IsInDrag
        {
            get { return dragOver; }
        }

        public void CaretToDragCaretPosition()
        {
            Caret.Location = defaultCaretPos = dragCaretPos;
        }

        protected override void OnDragLeave(EventArgs args)
        {
            base.OnDragLeave(args);
            if (dragOver)
            {
                Caret.PreserveSelection = true;
                Caret.Location = defaultCaretPos;
                Caret.PreserveSelection = false;
                ResetMouseState();
                dragOver = false;
            }
        }

        protected override void OnDragStarted(DragStartedEventArgs args)
        {
            var dragContents = new ClipboardActions.CopyOperation();
            dragContents.CopyData(textEditorData);
            args.DragOperation.Data.AddValue(dragContents.GetCopiedPlainText());
            base.OnDragStarted(args);
        }

        protected override void OnDragOver(DragOverEventArgs args)
        {
            var undo = OpenUndoGroup();
            int dragOffset = Document.LocationToOffset(dragCaretPos);
            if (args.Action == DragDropAction.Move)
            {
                if (CanEdit(Caret.Line) && !selection.IsEmpty)
                {
                    var selectionRange = selection.GetSelectionRange(textEditorData);
                    if (selectionRange.Offset < dragOffset)
                        dragOffset -= selectionRange.Length;
                    Caret.PreserveSelection = true;
                    textEditorData.DeleteSelection(selection);
                    Caret.PreserveSelection = false;

                    selection = Selection.Empty;
                }
            }
            if (args.Data != null && args.Data.HasType(TransferDataType.Text))
            {
                Caret.Offset = dragOffset;
                if (CanEdit(dragCaretPos.Line))
                {
                    int offset = Caret.Offset;
                    if (!selection.IsEmpty && selection.GetSelectionRange(textEditorData).Offset >= offset)
                    {
                        var start = Document.OffsetToLocation(selection.GetSelectionRange(textEditorData).Offset + args.Data.Text.Length);
                        var end = Document.OffsetToLocation(selection.GetSelectionRange(textEditorData).Offset + args.Data.Text.Length + selection.GetSelectionRange(textEditorData).Length);
                        selection = new Selection(start, end);
                    }
                    textEditorData.PasteText(offset, args.Data.Text, null, ref undo);
                    Caret.Offset = offset + args.Data.Text.Length;
                    MainSelection = new Selection(Document.OffsetToLocation(offset), Caret.Location);
                }
                dragOver = false;
                //context = null;
            }
            mouseButtonPressed = 0;
            undo.Dispose();
            base.OnDragOver(args);
            //base.OnDragDataReceived(context, x, y, selection_data, info, time_);
        }
        protected override void OnDragOverCheck(DragOverCheckEventArgs args)
        {
            if (!this.HasFocus)
                this.SetFocus();
            if (!dragOver)
            {
                defaultCaretPos = Caret.Location;
            }

            DocumentLocation oldLocation = Caret.Location;
            dragOver = true;
            Caret.PreserveSelection = true;
            dragCaretPos = PointToLocation(args.Position.X - textViewMargin.XOffset, args.Position.Y);
            int offset = Document.LocationToOffset(dragCaretPos);
            if (!selection.IsEmpty && offset >= this.selection.GetSelectionRange(textEditorData).Offset && offset < this.selection.GetSelectionRange(textEditorData).EndOffset)
            {
                args.AllowedAction = DragDropAction.Default;
                Caret.Location = defaultCaretPos;
            }
            else
            {
                args.AllowedAction = args.Action.HasFlag(DragDropAction.Move) ? DragDropAction.Move : DragDropAction.Copy;
                Caret.Location = dragCaretPos;
            }
            this.RedrawLine(oldLocation.Line);
            if (oldLocation.Line != Caret.Line)
                this.RedrawLine(Caret.Line);
            Caret.PreserveSelection = false;
            base.OnDragOverCheck(args);
        }

        Margin oldMargin = null;
        bool overChildWidget;

        public event EventHandler BeginHover;

        protected virtual void OnBeginHover(EventArgs e)
        {
            var handler = BeginHover;
            if (handler != null)
                handler(this, e);
        }

        protected override void OnMouseMoved(MouseMovedEventArgs e)
        {
            OnBeginHover(EventArgs.Empty);
            try
            {
                // The coordinates have to be properly adjusted to the origin since
                // the event may come from a child widget
                double x = e.X;
                double y = e.Y;
                overChildWidget = containerChildren.Any(w => w.Child.ParentBounds.Contains(x, y));

                RemoveScrollWindowTimer();
                Xwt.ModifierKeys mod = Keyboard.CurrentModifiers;
                double startPos;
                Margin margin = GetMarginAtX(x, out startPos);
                var dragCheck = new DragCheckEventArgs(e.Position,
                                                       new TransferDataType[] { TransferDataType.Text },
                                                       DragDropAction.Move | DragDropAction.Copy);
                OnDragDropCheck(dragCheck);

                if (textViewMargin.inDrag && margin == textViewMargin && dragCheck.Result == DragDropResult.Success)
                {
                    var dragContents = new ClipboardActions.CopyOperation();
                    dragContents.CopyData(textEditorData);
                    var context = base.CreateDragOperation();
                    context.AllowedActions = DragDropAction.Copy | DragDropAction.Move;
                    context.Data.AddValue(dragContents.GetCopiedPlainText());
                    context.Start();

                    var window = new CodeSegmentPreviewWindow(textEditorData.Parent, true, textEditorData.SelectionRange, 300, 300);
                    var img = Toolkit.CurrentEngine.RenderWidget(window.canvas);
                    context.SetDragImage(window.Icon, 300, 300);
                    context.Start();
                    selection = MainSelection;
                    textViewMargin.inDrag = false;
                }
                else
                {
                    FireMotionEvent(x, y, mod);
                    if (mouseButtonPressed != 0)
                    {
                        UpdateScrollWindowTimer(x, y, mod);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            base.OnMouseMoved(e);
        }



        void UpdateScrollWindowTimer(double x, double y, Xwt.ModifierKeys mod)
        {
            scrollWindowTimer_x = x;
            scrollWindowTimer_y = y;
            scrollWindowTimer_mod = mod;
            if (scrollWindowTimer == null)
            {
                scrollWindowTimer = Xwt.Timeout.Add(50, delegate
                {
                    //'If' below shouldn't be needed, but after reproducing bug with FireMotionEvent being called
                    //when it shouldn't and attaching with debugger it turned out that it's called from here
                    //even when scrollWindowTimer was 0, looks like GLib bug
                    if (scrollWindowTimer == null)
                    {
                        return false;
                    }
                    FireMotionEvent(scrollWindowTimer_x, scrollWindowTimer_y, scrollWindowTimer_mod);
                    return true;
                });
            }
        }

        void RemoveScrollWindowTimer()
        {
            if (scrollWindowTimer != null)
            {
                scrollWindowTimer.Dispose();
                scrollWindowTimer = null;
            }
        }

        Xwt.ModifierKeys lastState = ModifierKeys.None;

        void FireMotionEvent(double x, double y, Xwt.ModifierKeys state)
        {
            lastState = state;
            mx = x - textViewMargin.XOffset;
            my = y;

            ShowTooltip(state);

            double startPos;
            Margin margin;
            if (textViewMargin.InSelectionDrag)
            {
                margin = textViewMargin;
                startPos = textViewMargin.XOffset;
            }
            else
            {
                margin = GetMarginAtX(x, out startPos);
                if (margin != null)
                {
                    if (!overChildWidget)
                    {
                        if (!editor.IsInKeypress)
                            SetCursor(margin.MarginCursor);
                    }
                    else
                    {
                        // Set the default cursor when the mouse is over an embedded widget
                        SetCursor(null);
                    }
                }
            }

            if (oldMargin != margin && oldMargin != null)
                oldMargin.MouseLeft();

            if (margin != null)
                margin.MouseHover(new MarginMouseEventArgs(textEditorData.Parent,
                    mouseButtonPressed, x - startPos, y, state));
            oldMargin = margin;
        }

        #region CustomDrag (for getting dnd data from toolbox items for example)
        string customText;
        Widget customSource;
        public void BeginDrag(string text, Widget source, DragOperation context)
        {
            customText = text;
            customSource = source;
            source.DragStarted += CustomDragDataGet;
            source.DragDrop += CustomDragEnd;
        }
        void CustomDragDataGet(object sender, DragStartedEventArgs args)
        {
            args.DragOperation.Data.AddValue(customText);
        }
        void CustomDragEnd(object sender, DragEventArgs args)
        {
            customSource.DragStarted -= CustomDragDataGet;
            customSource.DragDrop -= CustomDragEnd;
            customSource = null;
            customText = null;
        }
        #endregion
        bool isMouseTrapped = false;

        protected override void OnMouseEntered(EventArgs evnt)
        {
            isMouseTrapped = true;
            base.OnMouseEntered(evnt);
        }

        protected override void OnMouseExited(EventArgs e)
        {
            isMouseTrapped = false;
            if (tipWindow != null && currentTooltipProvider != null)
            {
                if (!currentTooltipProvider.IsInteractive(textEditorData.Parent, tipWindow))
                    DelayedHideTooltip();
            }
            else
            {
                HideTooltip();
            }
            textViewMargin.HideCodeSegmentPreviewWindow();

            SetCursor(null);
            if (oldMargin != null)
                oldMargin.MouseLeft();

            base.OnMouseExited(e);
        }

        public double LineHeight
        {
            get { return this.textEditorData.LineHeight; }
            internal set
            {
                this.textEditorData.LineHeight = value;
            }
        }

        public TextViewMargin TextViewMargin
        {
            get { return textViewMargin; }
        }

        public Margin IconMargin
        {
            get { return iconMargin; }
        }

        public ActionMargin ActionMargin
        {
            get { return actionMargin; }
        }

        public DocumentLocation LogicalToVisualLocation(DocumentLocation location)
        {
            return textEditorData.LogicalToVisualLocation(location);
        }

        public DocumentLocation LogicalToVisualLocation(int line, int column)
        {
            return textEditorData.LogicalToVisualLocation(line, column);
        }

        public void CenterToCaret()
        {
            CenterTo(Caret.Location);
        }

        public void CenterTo(int offset)
        {
            CenterTo(Document.OffsetToLocation(offset));
        }

        public void CenterTo(int line, int column)
        {
            CenterTo(new DocumentLocation(line, column));
        }

        public void CenterTo(DocumentLocation p)
        {
            if (isDisposed || p.Line < 0 || p.Line > Document.LineCount)
                return;
            SetAdjustments(this.Bounds);
            //			Adjustment adj;
            //adj.Upper
            if (this.textEditorData.VAdjustment.UpperValue < Bounds.Height)
            {
                this.textEditorData.VAdjustment.Value = 0;
                return;
            }

            //	int yMargin = 1 * this.LineHeight;
            double caretPosition = LineToY(p.Line);
            caretPosition -= this.textEditorData.VAdjustment.PageSize / 2;

            // Make sure the caret position is inside the bounds. This avoids an unnecessary bump of the scrollview.
            // The adjustment does this check, but does it after assigning the value, so the value may be out of bounds for a while.
            if (caretPosition + this.textEditorData.VAdjustment.PageSize > this.textEditorData.VAdjustment.UpperValue)
                caretPosition = this.textEditorData.VAdjustment.UpperValue - this.textEditorData.VAdjustment.PageSize;

            this.textEditorData.VAdjustment.Value = caretPosition;

            if (this.textEditorData.HAdjustment.UpperValue < Bounds.Width)
            {
                this.textEditorData.HAdjustment.Value = 0;
            }
            else
            {
                double caretX = ColumnToX(Document.GetLine(p.Line), p.Column);
                double textWith = Bounds.Width - textViewMargin.XOffset;
                if (this.textEditorData.HAdjustment.Value > caretX)
                {
                    this.textEditorData.HAdjustment.Value = caretX;
                }
                else if (this.textEditorData.HAdjustment.Value + textWith < caretX + TextViewMargin.CharWidth)
                {
                    double adjustment = System.Math.Max(0, caretX - textWith + TextViewMargin.CharWidth);
                    this.textEditorData.HAdjustment.Value = adjustment;
                }
            }
        }

        public void ScrollTo(int offset)
        {
            ScrollTo(Document.OffsetToLocation(offset));
        }

        public void ScrollTo(int line, int column)
        {
            ScrollTo(new DocumentLocation(line, column));
        }

        //		class ScrollingActor
        //		{
        //			readonly TextEditor editor;
        //			readonly double targetValue;
        //			readonly double initValue;
        //			
        //			public ScrollingActor (Mono.TextEditor.TextEditor editor, double targetValue)
        //			{
        //				this.editor = editor;
        //				this.targetValue = targetValue;
        //				this.initValue = editor.VAdjustment.Value;
        //			}
        //
        //			public bool Step (Actor<ScrollingActor> actor)
        //			{
        //				if (actor.Expired) {
        //					editor.VAdjustment.Value = targetValue;
        //					return false;
        //				}
        //				var newValue = initValue + (targetValue - initValue) / 100   * actor.Percent;
        //				editor.VAdjustment.Value = newValue;
        //				return true;
        //			}
        //		}

        internal void SmoothScrollTo(double value)
        {
            this.textEditorData.VAdjustment.Value = value;
            /*			Stage<ScrollingActor> scroll = new Stage<ScrollingActor> (50);
                        scroll.UpdateFrequency = 10;
                        var scrollingActor = new ScrollingActor (this, value);
                        scroll.Add (scrollingActor, 50);

                        scroll.ActorStep += scrollingActor.Step;
                        scroll.Play ();*/
        }

        public void ScrollTo(DocumentLocation p)
        {
            if (isDisposed || p.Line < 0 || p.Line > Document.LineCount || inCaretScroll)
                return;
            inCaretScroll = true;
            try
            {
                if (this.textEditorData.VAdjustment.UpperValue < Bounds.Height)
                {
                    this.textEditorData.VAdjustment.Value = 0;
                }
                else
                {
                    double caretPosition = LineToY(p.Line);
                    if (this.textEditorData.VAdjustment.Value > caretPosition)
                    {
                        this.textEditorData.VAdjustment.Value = caretPosition;
                    }
                    else if (this.textEditorData.VAdjustment.Value + this.textEditorData.VAdjustment.PageSize - this.LineHeight < caretPosition)
                    {
                        this.textEditorData.VAdjustment.Value = caretPosition - this.textEditorData.VAdjustment.PageSize + this.LineHeight;
                    }
                }

                if (this.textEditorData.HAdjustment.UpperValue < Bounds.Width)
                {
                    this.textEditorData.HAdjustment.Value = 0;
                }
                else
                {
                    double caretX = ColumnToX(Document.GetLine(p.Line), p.Column);
                    double textWith = Bounds.Width - textViewMargin.XOffset;
                    if (this.textEditorData.HAdjustment.Value > caretX)
                    {
                        this.textEditorData.HAdjustment.Value = caretX;
                    }
                    else if (this.textEditorData.HAdjustment.Value + textWith < caretX + TextViewMargin.CharWidth)
                    {
                        double adjustment = System.Math.Max(0, caretX - textWith + TextViewMargin.CharWidth);
                        this.textEditorData.HAdjustment.Value = adjustment;
                    }
                }
            }
            finally
            {
                inCaretScroll = false;
            }
        }

        /// <summary>
        /// Scrolls the editor as required for making the specified area visible 
        /// </summary>
        public void ScrollTo(Rectangle rect)
        {
            inCaretScroll = true;
            try
            {
                var vad = this.textEditorData.VAdjustment;
                if (vad.UpperValue < Bounds.Height)
                {
                    vad.Value = 0;
                }
                else
                {
                    if (vad.Value >= rect.Top)
                    {
                        vad.Value = rect.Top;
                    }
                    else if (vad.Value + vad.PageSize - rect.Height < rect.Top)
                    {
                        vad.Value = rect.Top - vad.PageSize + rect.Height;
                    }
                }

                var had = this.textEditorData.HAdjustment;
                if (had.UpperValue < Bounds.Width)
                {
                    had.Value = 0;
                }
                else
                {
                    if (had.Value >= rect.Left)
                    {
                        had.Value = rect.Left;
                    }
                    else if (had.Value + had.PageSize - rect.Width < rect.Left)
                    {
                        had.Value = rect.Left - had.PageSize + rect.Width;
                    }
                }
            }
            finally
            {
                inCaretScroll = false;
            }
        }

        bool inCaretScroll = false;
        public void ScrollToCaret()
        {
            ScrollTo(Caret.Location);
        }

        public void TryToResetHorizontalScrollPosition()
        {
            int caretX = (int)ColumnToX(Document.GetLine(Caret.Line), Caret.Column);
            var textWith = Bounds.Width - textViewMargin.XOffset;
            if (caretX < textWith - TextViewMargin.CharWidth)
                this.textEditorData.HAdjustment.Value = 0;
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            SetAdjustments(Bounds);
            sizeHasBeenAllocated = true;
            if (Options.WrapLines)
                textViewMargin.PurgeLayoutCache();
            SetChildrenPositions(Bounds);
        }

        long lastScrollTime;
        protected override void OnMouseScrolled(MouseScrolledEventArgs args)
        {
            var modifier = !Platform.IsMac ? Xwt.ModifierKeys.Control
                //Mac window manager already uses control-scroll, so use command
                //Command might be either meta or mod1, depending on GTK version
                : Xwt.ModifierKeys.Alt;

            var hasZoomModifier = (Keyboard.CurrentModifiers & modifier) != 0;
            if (hasZoomModifier && lastScrollTime != 0 && (args.Timestamp - lastScrollTime) < 100)
                hasZoomModifier = false;

            if (hasZoomModifier)
            {
                if (args.Direction == ScrollDirection.Up)
                    Options.ZoomIn();
                else if (args.Direction == ScrollDirection.Down)
                    Options.ZoomOut();
                this.QueueDraw();
                if (isMouseTrapped)
                    FireMotionEvent(mx + textViewMargin.XOffset, my, lastState);
            }

            if (!Platform.IsMac)
            {
                if ((Keyboard.CurrentModifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    if (args.Direction == ScrollDirection.Down)
                        HAdjustment.Value = System.Math.Min(HAdjustment.UpperValue - HAdjustment.PageSize, HAdjustment.Value + HAdjustment.StepIncrement * 3);
                    else if (args.Direction == ScrollDirection.Up)
                        HAdjustment.Value -= HAdjustment.StepIncrement * 3;
                    return;
                }
            }
            lastScrollTime = args.Timestamp;
            base.OnMouseScrolled(args);
        }

        void SetHAdjustment()
        {
            textEditorData.HeightTree.Rebuild();

            if (textEditorData.HAdjustment == null || Options == null)
                return;
            textEditorData.HAdjustment.ValueChanged -= HAdjustmentValueChanged;
            if (Options.WrapLines)
            {
                textEditorData.HAdjustment.LowerValue = 0;
                textEditorData.HAdjustment.UpperValue = 0;
                textEditorData.HAdjustment.StepIncrement = 0;
                textEditorData.HAdjustment.PageSize = 0;
                textEditorData.HAdjustment.PageIncrement = 0;
            }
            else
            {
                if (longestLine != null && this.textEditorData.HAdjustment != null)
                {
                    double maxX = longestLineWidth;
                    if (maxX > Bounds.Width)
                        maxX += 2 * this.textViewMargin.CharWidth;
                    double width = Math.Abs(Bounds.Width - this.TextViewMargin.XOffset);
                    var realMaxX = Math.Max(maxX, this.textEditorData.HAdjustment.Value + width);

                    foreach (var containerChild in editor.containerChildren.Concat(containerChildren))
                    {
                        if (containerChild.Child == this)
                            continue;
                        realMaxX = Math.Max(realMaxX, containerChild.X + containerChild.Child.Size.Width);
                    }

                    textEditorData.HAdjustment.LowerValue = 0;
                    textEditorData.HAdjustment.UpperValue = realMaxX;
                    textEditorData.HAdjustment.StepIncrement = textViewMargin.CharWidth;
                    textEditorData.HAdjustment.PageSize = width;
                    textEditorData.HAdjustment.PageIncrement = width;
                    if (realMaxX < width)
                        this.textEditorData.HAdjustment.Value = 0;
                }
            }
            textEditorData.HAdjustment.ValueChanged += HAdjustmentValueChanged;
        }

        internal void SetAdjustments()
        {
            SetAdjustments(Bounds);
        }

        internal void SetAdjustments(Rectangle allocation)
        {
            SetHAdjustment();

            if (this.textEditorData.VAdjustment != null)
            {
                double maxY = textEditorData.HeightTree.TotalHeight;
                //if (maxY > allocation.Height)
                maxY += allocation.Height / 2 - LineHeight;

                foreach (var containerChild in editor.containerChildren.Concat(containerChildren))
                {
                    maxY = System.Math.Max(maxY, containerChild.Y + containerChild.Child.Surface.GetPreferredSize().Height);
                }

                if (VAdjustment.Value > maxY - allocation.Height)
                {
                    VAdjustment.Value = System.Math.Max(0, maxY - allocation.Height);
                    QueueDraw();
                }
                textEditorData.VAdjustment.LowerValue = 0;
                textEditorData.VAdjustment.UpperValue = Math.Max(allocation.Height, maxY);
                textEditorData.VAdjustment.StepIncrement = LineHeight;
                textEditorData.VAdjustment.PageSize = allocation.Height;
                textEditorData.VAdjustment.PageIncrement = allocation.Height;
                if (maxY < allocation.Height)
                    this.textEditorData.VAdjustment.Value = 0;
            }
        }

        public double GetWidth(string text)
        {
            return this.textViewMargin.GetWidth(text);
        }

        void UpdateMarginXOffsets()
        {
            double curX = 0;
            foreach (Margin margin in this.margins)
            {
                if (!margin.IsVisible)
                    continue;
                margin.XOffset = curX;
                curX += margin.Width;
            }
        }

        void RenderMargins(Xwt.Drawing.Context cr, Xwt.Drawing.Context textViewCr, Rectangle cairoRectangle)
        {
            this.TextViewMargin.rulerX = Options.RulerColumn * this.TextViewMargin.CharWidth - this.textEditorData.HAdjustment.Value;
            int startLine = YToLine(cairoRectangle.Y + this.textEditorData.VAdjustment.Value);
            double startY = LineToY(startLine);
            double curY = startY - this.textEditorData.VAdjustment.Value;
            bool setLongestLine = false;
            foreach (var margin in this.margins)
            {
                if (margin.BackgroundRenderer != null)
                {
                    var area = new Rectangle(0, 0, Bounds.Width, Bounds.Height);
                    margin.BackgroundRenderer.Draw(cr, area);
                }
            }


            for (int visualLineNumber = textEditorData.LogicalToVisualLine(startLine); ; visualLineNumber++)
            {
                int logicalLineNumber = textEditorData.VisualToLogicalLine(visualLineNumber);
                var line = Document.GetLine(logicalLineNumber);

                // Ensure that the correct line height is set.
                if (line != null)
                {
                    var wrapper = textViewMargin.GetLayout(line);
                    if (wrapper.IsUncached)
                        wrapper.Dispose();
                }

                double lineHeight = GetLineHeight(line);
                foreach (var margin in this.margins)
                {
                    if (!margin.IsVisible)
                        continue;
                    try
                    {
                        margin.Draw(margin == textViewMargin ? textViewCr : cr, cairoRectangle, line, logicalLineNumber, margin.XOffset, curY, lineHeight);
                    }
                    catch (Exception e)
                    {
                        System.Console.WriteLine(e);
                    }
                }
                // take the line real render width from the text view margin rendering (a line can consist of more than 
                // one line and be longer (foldings!) ex. : someLine1[...]someLine2[...]someLine3)
                double lineWidth = textViewMargin.lastLineRenderWidth + HAdjustment.Value;
                if (longestLine == null || lineWidth > longestLineWidth)
                {
                    longestLine = line;
                    longestLineWidth = lineWidth;
                    setLongestLine = true;
                }
                curY += lineHeight;
                if (curY > cairoRectangle.Y + cairoRectangle.Height)
                    break;
            }

            foreach (var margin in this.margins)
            {
                if (!margin.IsVisible)
                    continue;
                foreach (var drawer in margin.MarginDrawer)
                    drawer.Draw(cr, cairoRectangle);
            }

            if (setLongestLine)
                SetHAdjustment();
        }

        /*
		protected override bool OnWidgetEvent (Event evnt)
		{
			System.Console.WriteLine(evnt);
			return base.OnWidgetEvent (evnt);
		}*/

        double oldVadjustment = 0;

        void UpdateAdjustments()
        {
            int lastVisibleLine = textEditorData.LogicalToVisualLine(Document.LineCount);
            if (oldRequest != lastVisibleLine)
            {
                SetAdjustments(this.Bounds);
                oldRequest = lastVisibleLine;
            }
        }

#if DEBUG_EXPOSE
		DateTime started = DateTime.Now;
#endif
        protected override void OnDraw(Context cr, Rectangle area)
        {
            if (this.isDisposed)
                return;
            UpdateAdjustments();

            cr.SetColor(Colors.White);
            var cairoArea = new Rectangle(area.X, area.Y, area.Width, area.Height);
            var textViewCr = cr;
            {
                UpdateMarginXOffsets();

                cr.SetLineWidth(Options.Zoom);
                textViewCr.SetLineWidth(Options.Zoom);
                textViewCr.Rectangle(textViewMargin.XOffset, 0, Bounds.Width - textViewMargin.XOffset, Bounds.Height);
                textViewCr.Clip();

                RenderMargins(cr, textViewCr, cairoArea);

#if DEBUG_EXPOSE
				Console.WriteLine ("{0} expose {1},{2} {3}x{4}", (long)(DateTime.Now - started).TotalMilliseconds,
					e.Area.X, e.Area.Y, e.Area.Width, e.Area.Height);
#endif
                if (requestResetCaretBlink && HasFocus)
                {
                    textViewMargin.ResetCaretBlink(200);
                    requestResetCaretBlink = false;
                }

                foreach (Animation animation in actors)
                {
                    animation.Drawer.Draw(cr);
                }

                OnPainted(new PaintEventArgs(cr, cairoArea));
            }

            if (Caret.IsVisible)
                textViewMargin.DrawCaret(cr, Bounds);
        }

        protected virtual void OnPainted(PaintEventArgs e)
        {
            if (Painted != null)
                Painted(this, e);
        }

        public event EventHandler<PaintEventArgs> Painted;

        #region TextEditorData delegation
        public string EolMarker
        {
            get { return textEditorData.EolMarker; }
        }

        public Mono.TextEditor.Highlighting.ColorScheme ColorStyle
        {
            get { return this.textEditorData.ColorStyle; }
        }

        public EditMode CurrentMode
        {
            get { return this.textEditorData.CurrentMode; }
            set { this.textEditorData.CurrentMode = value; }
        }

        public bool IsSomethingSelected
        {
            get { return this.textEditorData.IsSomethingSelected; }
        }

        public Selection MainSelection
        {
            get { return textEditorData.MainSelection; }
            set { textEditorData.MainSelection = value; }
        }

        public SelectionMode SelectionMode
        {
            get { return textEditorData.SelectionMode; }
            set { textEditorData.SelectionMode = value; }
        }

        public TextSegment SelectionRange
        {
            get { return this.textEditorData.SelectionRange; }
            set { this.textEditorData.SelectionRange = value; }
        }

        public string SelectedText
        {
            get { return this.textEditorData.SelectedText; }
            set { this.textEditorData.SelectedText = value; }
        }

        public int SelectionAnchor
        {
            get { return this.textEditorData.SelectionAnchor; }
            set { this.textEditorData.SelectionAnchor = value; }
        }

        public IEnumerable<DocumentLine> SelectedLines
        {
            get { return this.textEditorData.SelectedLines; }
        }

        public ScrollAdjustment HAdjustment
        {
            get { return this.textEditorData.HAdjustment; }
        }

        public ScrollAdjustment VAdjustment
        {
            get { return this.textEditorData.VAdjustment; }
        }

        public int Insert(int offset, string value)
        {
            return textEditorData.Insert(offset, value);
        }

        public void Remove(DocumentRegion region)
        {
            textEditorData.Remove(region);
        }

        public void Remove(TextSegment removeSegment)
        {
            textEditorData.Remove(removeSegment);
        }

        public void Remove(int offset, int count)
        {
            textEditorData.Remove(offset, count);
        }

        public int Replace(int offset, int count, string value)
        {
            return textEditorData.Replace(offset, count, value);
        }

        public void ClearSelection()
        {
            this.textEditorData.ClearSelection();
        }

        public void DeleteSelectedText()
        {
            this.textEditorData.DeleteSelectedText();
        }

        public void DeleteSelectedText(bool clearSelection)
        {
            this.textEditorData.DeleteSelectedText(clearSelection);
        }

        public void RunEditAction(Action<TextEditorData> action)
        {
            action(this.textEditorData);
        }

        public void SetSelection(int anchorOffset, int leadOffset)
        {
            this.textEditorData.SetSelection(anchorOffset, leadOffset);
        }

        public void SetSelection(DocumentLocation anchor, DocumentLocation lead)
        {
            this.textEditorData.SetSelection(anchor, lead);
        }

        public void SetSelection(int anchorLine, int anchorColumn, int leadLine, int leadColumn)
        {
            this.textEditorData.SetSelection(anchorLine, anchorColumn, leadLine, leadColumn);
        }

        public void ExtendSelectionTo(DocumentLocation location)
        {
            this.textEditorData.ExtendSelectionTo(location);
        }
        public void ExtendSelectionTo(int offset)
        {
            this.textEditorData.ExtendSelectionTo(offset);
        }
        public void SetSelectLines(int from, int to)
        {
            this.textEditorData.SetSelectLines(from, to);
        }

        public void InsertAtCaret(string text)
        {
            textEditorData.InsertAtCaret(text);
        }

        public bool CanEdit(int line)
        {
            return textEditorData.CanEdit(line);
        }

        public string GetLineText(int line)
        {
            return textEditorData.GetLineText(line);
        }

        public string GetLineText(int line, bool includeDelimiter)
        {
            return textEditorData.GetLineText(line, includeDelimiter);
        }

        /// <summary>
        /// Use with care.
        /// </summary>
        /// <returns>
        /// A <see cref="TextEditorData"/>
        /// </returns>
        public TextEditorData GetTextEditorData()
        {
            return this.textEditorData;
        }

        public event EventHandler SelectionChanged;
        protected virtual void OnSelectionChanged(EventArgs args)
        {
            CurrentMode.InternalSelectionChanged(editor, textEditorData);
            if (SelectionChanged != null)
                SelectionChanged(this, args);
        }
        #endregion

        #region Document delegation
        public int Length
        {
            get
            {
                return Document.TextLength;
            }
        }

        public string Text
        {
            get
            {
                return Document.Text;
            }
            set
            {
                Document.Text = value;
            }
        }

        public string GetTextBetween(int startOffset, int endOffset)
        {
            return Document.GetTextBetween(startOffset, endOffset);
        }

        public string GetTextBetween(DocumentLocation start, DocumentLocation end)
        {
            return Document.GetTextBetween(start, end);
        }

        public string GetTextBetween(int startLine, int startColumn, int endLine, int endColumn)
        {
            return Document.GetTextBetween(startLine, startColumn, endLine, endColumn);
        }

        public string GetTextAt(int offset, int count)
        {
            return Document.GetTextAt(offset, count);
        }


        public string GetTextAt(TextSegment segment)
        {
            return Document.GetTextAt(segment);
        }

        public string GetTextAt(DocumentRegion region)
        {
            return Document.GetTextAt(region);
        }

        public char GetCharAt(int offset)
        {
            return Document.GetCharAt(offset);
        }

        public IEnumerable<DocumentLine> Lines
        {
            get { return Document.Lines; }
        }

        public int LineCount
        {
            get { return Document.LineCount; }
        }

        public int LocationToOffset(int line, int column)
        {
            return Document.LocationToOffset(line, column);
        }

        public int LocationToOffset(DocumentLocation location)
        {
            return Document.LocationToOffset(location);
        }

        public DocumentLocation OffsetToLocation(int offset)
        {
            return Document.OffsetToLocation(offset);
        }

        public string GetLineIndent(int lineNumber)
        {
            return Document.GetLineIndent(lineNumber);
        }

        public string GetLineIndent(DocumentLine segment)
        {
            return Document.GetLineIndent(segment);
        }

        public DocumentLine GetLine(int lineNumber)
        {
            return Document.GetLine(lineNumber);
        }

        public DocumentLine GetLineByOffset(int offset)
        {
            return Document.GetLineByOffset(offset);
        }

        public int OffsetToLineNumber(int offset)
        {
            return Document.OffsetToLineNumber(offset);
        }

        public IDisposable OpenUndoGroup()
        {
            return Document.OpenUndoGroup();
        }
        #endregion

        #region Search & Replace

        bool highlightSearchPattern = false;

        public string SearchPattern
        {
            get { return this.textEditorData.SearchRequest.SearchPattern; }
            set
            {
                if (this.textEditorData.SearchRequest.SearchPattern != value)
                {
                    this.textEditorData.SearchRequest.SearchPattern = value;
                }
            }
        }

        public ISearchEngine SearchEngine
        {
            get { return this.textEditorData.SearchEngine; }
            set
            {
                Debug.Assert(value != null);
                this.textEditorData.SearchEngine = value;
            }
        }

        public event EventHandler HighlightSearchPatternChanged;
        public bool HighlightSearchPattern
        {
            get { return highlightSearchPattern; }
            set
            {
                if (highlightSearchPattern != value)
                {
                    this.highlightSearchPattern = value;
                    if (HighlightSearchPatternChanged != null)
                        HighlightSearchPatternChanged(this, EventArgs.Empty);
                    textViewMargin.DisposeLayoutDict();
                    this.QueueDraw();
                }
            }
        }

        public bool IsCaseSensitive
        {
            get { return this.textEditorData.SearchRequest.CaseSensitive; }
            set { this.textEditorData.SearchRequest.CaseSensitive = value; }
        }

        public bool IsWholeWordOnly
        {
            get { return this.textEditorData.SearchRequest.WholeWordOnly; }
            set { this.textEditorData.SearchRequest.WholeWordOnly = value; }
        }

        public TextSegment SearchRegion
        {
            get { return this.textEditorData.SearchRequest.SearchRegion; }
            set { this.textEditorData.SearchRequest.SearchRegion = value; }
        }

        public SearchResult SearchForward(int fromOffset)
        {
            return textEditorData.SearchForward(fromOffset);
        }

        public SearchResult SearchBackward(int fromOffset)
        {
            return textEditorData.SearchBackward(fromOffset);
        }

        class CaretPulseAnimation : IAnimationDrawer
        {
            TextEditor editor;

            public double Percent { get; set; }

            public Rectangle AnimationBounds
            {
                get
                {
                    double x = editor.TextViewMargin.caretX;
                    double y = editor.TextViewMargin.caretY;
                    double extend = 100 * 5;
                    int width = (int)(editor.TextViewMargin.charWidth + 2 * extend * editor.Options.Zoom / 2);
                    return new Xwt.Rectangle((int)(x - extend * editor.Options.Zoom / 2),
                                              (int)(y - extend * editor.Options.Zoom),
                                              width,
                                              (int)(editor.LineHeight + 2 * extend * editor.Options.Zoom));
                }
            }

            public CaretPulseAnimation(TextEditor editor)
            {
                this.editor = editor;
            }

            public void Draw(Xwt.Drawing.Context cr)
            {
                double x = editor.TextViewMargin.caretX;
                double y = editor.TextViewMargin.caretY;
                if (editor.Caret.Mode != CaretMode.Block)
                    x -= editor.TextViewMargin.charWidth / 2;
                cr.Save();
                cr.Rectangle(editor.TextViewMargin.XOffset, 0, editor.Bounds.Width - editor.TextViewMargin.XOffset, editor.Bounds.Height);
                cr.Clip();

                double extend = Percent * 5;
                double width = editor.TextViewMargin.charWidth + 2 * extend * editor.Options.Zoom / 2;
                FoldingScreenbackgroundRenderer.DrawRoundRectangle(cr, true, true,
                                                                    x - extend * editor.Options.Zoom / 2,
                                                                    y - extend * editor.Options.Zoom,
                                                                    System.Math.Min(editor.TextViewMargin.charWidth / 2, width),
                                                                    width,
                                                                    editor.LineHeight + 2 * extend * editor.Options.Zoom);
                Color color = editor.ColorStyle.PlainText.Foreground;
                color.Alpha = 0.8;
                cr.SetLineWidth(editor.Options.Zoom);
                cr.SetSourceColor(color);
                cr.Stroke();
                cr.Restore();
            }
        }

        public enum PulseKind
        {
            In, Out, Bounce
        }

        public class RegionPulseAnimation : IAnimationDrawer
        {
            TextEditor editor;

            public PulseKind Kind { get; set; }
            public double Percent { get; set; }

            Rectangle region;

            public Rectangle AnimationBounds
            {
                get
                {
                    var x = region.X;
                    var y = region.Y;
                    int animationPosition = (int)(100 * 100);
                    int width = (int)(region.Width + 2 * animationPosition * editor.Options.Zoom / 2);

                    return new Xwt.Rectangle((int)(x - animationPosition * editor.Options.Zoom / 2),
                                              (int)(y - animationPosition * editor.Options.Zoom),
                                              width,
                                              (int)(region.Height + 2 * animationPosition * editor.Options.Zoom));
                }
            }

            public RegionPulseAnimation(TextEditor editor, Point position, Size size)
                : this(editor, new Xwt.Rectangle(position, size)) { }

            public RegionPulseAnimation(TextEditor editor, Rectangle region)
            {
                if (region.X < 0 || region.Y < 0 || region.Width < 0 || region.Height < 0)
                    throw new ArgumentException("region is invalid");

                this.editor = editor;
                this.region = region;
            }

            public void Draw(Xwt.Drawing.Context cr)
            {
                var x = region.X;
                var y = region.Y;
                int animationPosition = (int)(Percent * 100);
                cr.Save();
                cr.Rectangle(editor.TextViewMargin.XOffset, 0, editor.Bounds.Width - editor.TextViewMargin.XOffset, editor.Bounds.Height);
                cr.Clip();

                int width = (int)(region.Width + 2 * animationPosition * editor.Options.Zoom / 2);
                FoldingScreenbackgroundRenderer.DrawRoundRectangle(cr, true, true,
                                                                    (int)(x - animationPosition * editor.Options.Zoom / 2),
                                                                    (int)(y - animationPosition * editor.Options.Zoom),
                                                                    System.Math.Min(editor.TextViewMargin.charWidth / 2, width),
                                                                    width,
                                                                    (int)(region.Height + 2 * animationPosition * editor.Options.Zoom));
                Color color = editor.ColorStyle.PlainText.Foreground;
                color.Alpha = 0.8;
                cr.SetLineWidth(editor.Options.Zoom);
                cr.SetSourceColor(color);
                cr.Stroke();
                cr.Restore();
            }
        }

        Rectangle RangeToRectangle(DocumentLocation start, DocumentLocation end)
        {
            if (start.Column < 0 || start.Line < 0 || end.Column < 0 || end.Line < 0)
                return Rectangle.Zero;

            var startPt = this.LocationToPoint(start);
            var endPt = this.LocationToPoint(end);
            var width = endPt.X - startPt.X;

            if (startPt.Y != endPt.Y || startPt.X < 0 || startPt.Y < 0 || width < 0)
                return Rectangle.Zero;

            return new Xwt.Rectangle(startPt.X, startPt.Y, width, (int)this.LineHeight);
        }

        /// <summary>
        /// Initiate a pulse at the specified document location
        /// </summary>
        /// <param name="pulseStart">
        /// A <see cref="DocumentLocation"/>
        /// </param>
        public void PulseCharacter(DocumentLocation pulseStart)
        {
            if (pulseStart.Column < 0 || pulseStart.Line < 0)
                return;
            var rect = RangeToRectangle(pulseStart, new DocumentLocation(pulseStart.Line, pulseStart.Column + 1));
            if (rect.X < 0 || rect.Y < 0 || System.Math.Max(rect.Width, rect.Height) <= 0)
                return;
            StartAnimation(new RegionPulseAnimation(editor, rect)
            {
                Kind = PulseKind.Bounce
            });
        }


        public SearchResult FindNext(bool setSelection)
        {
            var result = textEditorData.FindNext(setSelection);
            if (result == null)
                return result;
            TryToResetHorizontalScrollPosition();
            AnimateSearchResult(result);
            return result;
        }

        public void StartCaretPulseAnimation()
        {
            StartAnimation(new CaretPulseAnimation(editor));
        }

        SearchHighlightPopupWindow popupWindow = null;

        public void StopSearchResultAnimation()
        {
            if (popupWindow == null)
                return;
            popupWindow.StopPlaying();
        }

        public void AnimateSearchResult(SearchResult result)
        {
            if (!Options.EnableAnimations || result == null)
                return;

            // Don't animate multi line search results
            if (OffsetToLineNumber(result.Segment.Offset) != OffsetToLineNumber(result.Segment.EndOffset))
                return;

            TextViewMargin.MainSearchResult = result.Segment;
            if (!TextViewMargin.MainSearchResult.IsInvalid)
            {
                if (popupWindow != null)
                {
                    popupWindow.StopPlaying();
                    popupWindow.Dispose();
                }
                popupWindow = new SearchHighlightPopupWindow(editor);
                popupWindow.Result = result;
                popupWindow.Popup();
                popupWindow.Disposed += delegate
                {
                    popupWindow = null;
                };
            }
        }

        class SearchHighlightPopupWindow : BounceFadePopupWidget
        {
            public SearchResult Result
            {
                get;
                set;
            }

            public SearchHighlightPopupWindow(TextEditor editor) : base(editor)
            {
            }

            public override void Popup()
            {
                ExpandWidth = (uint)Editor.LineHeight;
                ExpandHeight = (uint)Editor.LineHeight / 2;
                BounceEasing = Easing.Sine;
                Duration = 150;
                base.Popup();
            }

            protected override void OnAnimationCompleted()
            {
                base.OnAnimationCompleted();
                Dispose();
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (layout != null)
                    layout.Dispose();
            }

            protected override Rectangle CalculateInitialBounds()
            {
                DocumentLine line = Editor.Document.GetLineByOffset(Result.Offset);
                int lineNr = Editor.Document.OffsetToLineNumber(Result.Offset);
                ISyntaxMode mode = Editor.Document.SyntaxMode != null && Editor.Options.EnableSyntaxHighlighting ? Editor.Document.SyntaxMode : new SyntaxMode(Editor.Document);
                int logicalRulerColumn = line.GetLogicalColumn(Editor.GetTextEditorData(), Editor.Options.RulerColumn);
                var lineLayout = Editor.TextViewMargin.CreateLinePartLayout(mode, line, logicalRulerColumn, line.Offset, line.Length, -1, -1);
                if (lineLayout == null)
                    return new Rectangle();

                int l; double x1, x2;
                int index = Result.Offset - line.Offset - 1;
                if (index >= 0)
                {
                    lineLayout.Layout.IndexToLineX(index, out l, out x1);
                }
                else
                {
                    x1 = l = 0;
                }

                index = Result.Offset - line.Offset - 1 + Result.Length;
                if (index >= 0)
                {
                    lineLayout.Layout.IndexToLineX(index, out l, out x2);
                }
                else
                {
                    x2 = 0;
                    Console.WriteLine("Invalid end index :" + index);
                }

                double y = Editor.LineToY(lineNr);
                double w = (x2 - x1);
                double x = (x1 + Editor.TextViewMargin.XOffset + Editor.TextViewMargin.TextStartPosition);
                var h = Editor.LineHeight;

                //adjust the width to match TextViewMargin
                w = System.Math.Ceiling(w + 1);

                //add space for the shadow
                w += shadowOffset;
                h += shadowOffset;

                return new Rectangle(x, y, w, h);
            }

            const int shadowOffset = 1;

            TextLayout layout = null;

            protected override void Draw(Xwt.Drawing.Context cr, Rectangle area)
            {
                cr.SetLineWidth(Editor.Options.Zoom);

                if (layout == null)
                {
                    layout = new TextLayout();
                    layout.Font = Editor.Options.Font;
                    string markup = Editor.GetTextEditorData().GetMarkup(Result.Offset, Result.Length, true);
                    layout.Markup = markup;
                }

                // subtract off the shadow again
                var width = area.Width - shadowOffset;
                var height = area.Height - shadowOffset;

                //from TextViewMargin's actual highlighting
                double corner = System.Math.Min(4, width) * Editor.Options.Zoom;

                //fill in the highlight rect with solid white to prevent alpha blending artifacts on the corners
                FoldingScreenbackgroundRenderer.DrawRoundRectangle(cr, true, true, 0, 0, corner, width, height);
                cr.SetColor(new Color(1, 1, 1));
                cr.Fill();

                //draw the shadow
                FoldingScreenbackgroundRenderer.DrawRoundRectangle(cr, true, true,
                    shadowOffset, shadowOffset, corner, width, height);
                var color = TextViewMargin.DimColor(Editor.ColorStyle.SearchResultMain.Color, 0.3);
                color.Alpha = 0.5 * opacity * opacity;
                cr.SetSourceColor(color);
                cr.Fill();

                //draw the highlight rectangle
                FoldingScreenbackgroundRenderer.DrawRoundRectangle(cr, true, true, 0, 0, corner, width, height);
                using (var gradient = new LinearGradient(0, 0, 0, height))
                {
                    color = ColorLerp(
                        TextViewMargin.DimColor(Editor.ColorStyle.SearchResultMain.Color, 1.1),
                        Editor.ColorStyle.SearchResultMain.Color,
                        1 - opacity);
                    gradient.AddColorStop(0, color);
                    color = ColorLerp(
                        TextViewMargin.DimColor(Editor.ColorStyle.SearchResultMain.Color, 0.9),
                        Editor.ColorStyle.SearchResultMain.Color,
                        1 - opacity);
                    gradient.AddColorStop(1, color);
                    cr.Pattern = gradient;
                    cr.Fill();
                }

                //and finally the text
                cr.Translate(area.X, area.Y);
                cr.SetColor(new Color(0, 0, 0));
                cr.ShowLayout(layout);
            }

            static Color ColorLerp(Color from, Color to, double scale)
            {
                return new Color(
                    Lerp(from.Red, to.Red, scale),
                    Lerp(from.Green, to.Green, scale),
                    Lerp(from.Blue, to.Blue, scale),
                    Lerp(from.Alpha, to.Alpha, scale)
                );
            }

            static double Lerp(double from, double to, double scale)
            {
                return from + scale * (to - from);
            }
        }

        public SearchResult FindPrevious(bool setSelection)
        {
            var result = textEditorData.FindPrevious(setSelection);
            if (result == null)
                return result;
            TryToResetHorizontalScrollPosition();
            AnimateSearchResult(result);
            return result;
        }

        public bool Replace(string withPattern)
        {
            return textEditorData.SearchReplace(withPattern, true);
        }

        public int ReplaceAll(string withPattern)
        {
            return textEditorData.SearchReplaceAll(withPattern);
        }
        #endregion

        #region Tooltips
        // Tooltip fields
        const int TooltipTimeout = 650;
        TooltipItem tipItem;

        double tipX, tipY;
        IDisposable tipHideTimeoutId = null;
        IDisposable tipShowTimeoutId = null;
        static Xwt.Window tipWindow;
        static TooltipProvider currentTooltipProvider;

        // Data for the next tooltip to be shown
        double nextTipOffset = 0;
        double nextTipX = 0; double nextTipY = 0;
        Xwt.ModifierKeys nextTipModifierState = ModifierKeys.None;
        DateTime nextTipScheduledTime; // Time at which we want the tooltip to show

        void ShowTooltip(Xwt.ModifierKeys modifierState)
        {
            if (mx < TextViewMargin.TextStartPosition)
            {
                HideTooltip();
                return;
            }

            var loc = PointToLocation(mx, my, true);
            if (loc.IsEmpty)
            {
                HideTooltip();
                return;
            }

            // Hide editor tooltips for text marker extended regions (message bubbles)
            double y = LineToY(loc.Line);
            if (y + LineHeight < my)
            {
                HideTooltip();
                return;
            }

            ShowTooltip(modifierState,
                         Document.LocationToOffset(loc),
                         (int)mx,
                         (int)my);
        }

        void ShowTooltip(Xwt.ModifierKeys modifierState, int offset, double xloc, double yloc)
        {
            CancelScheduledShow();
            if (textEditorData.SuppressTooltips)
                return;
            if (tipWindow != null && currentTooltipProvider != null && currentTooltipProvider.IsInteractive(editor, tipWindow))
            {
                var wx = tipX - tipWindow.Size.Width / 2;
                if (xloc >= wx && xloc < tipX + tipWindow.Size.Width && yloc >= tipY && yloc < tipY + 20 + tipWindow.Size.Height)
                    return;
            }
            if (tipItem != null && !tipItem.ItemSegment.IsInvalid && !tipItem.ItemSegment.Contains(offset))
                HideTooltip();
            nextTipX = xloc;
            nextTipY = yloc;
            nextTipOffset = offset;
            nextTipModifierState = modifierState;
            nextTipScheduledTime = DateTime.Now + TimeSpan.FromMilliseconds(TooltipTimeout);

            // If a tooltip is already scheduled, there is no need to create a new timer.
            if (tipShowTimeoutId == null)
                tipShowTimeoutId = Xwt.Timeout.Add(TooltipTimeout, TooltipTimer);
        }

        bool TooltipTimer()
        {
            // This timer can't be reused, so reset the var now
            tipShowTimeoutId = null;

            // Cancelled?
            if (nextTipOffset == -1)
                return false;

            int remainingMs = (int)(nextTipScheduledTime - DateTime.Now).TotalMilliseconds;
            if (remainingMs > 50)
            {
                // Still some significant time left. Re-schedule the timer
                tipShowTimeoutId = Xwt.Timeout.Add(remainingMs, TooltipTimer);
                return false;
            }

            // Find a provider
            TooltipProvider provider = null;
            TooltipItem item = null;

            foreach (TooltipProvider tp in textEditorData.tooltipProviders)
            {
                try
                {
                    item = tp.GetItem(editor, nextTipOffset);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception in tooltip provider " + tp + " GetItem:");
                    System.Console.WriteLine(e);
                }
                if (item != null)
                {
                    provider = tp;
                    break;
                }
            }

            if (item != null)
            {
                // Tip already being shown for this item?
                if (tipWindow != null && tipItem != null && tipItem.Equals(item))
                {
                    CancelScheduledHide();
                    return false;
                }

                tipX = nextTipX;
                tipY = nextTipY;
                tipItem = item;
                Xwt.Window tw = null;
                try
                {
                    tw = provider.ShowTooltipWindow(editor, nextTipOffset, nextTipModifierState, new Point(tipX + TextViewMargin.XOffset, tipY), item);
                }
                catch (Exception e)
                {
                    Console.WriteLine("-------- Exception while creating tooltip:");
                    Console.WriteLine(e);
                }
                if (tw == tipWindow)
                    return false;
                HideTooltip();
                if (tw == null)
                    return false;

                CancelScheduledShow();

                tipWindow = tw;
                currentTooltipProvider = provider;

                tipShowTimeoutId = null;
            }
            else
                HideTooltip();
            return false;
        }

        public void HideTooltip(bool checkMouseOver = true)
        {
            CancelScheduledHide();
            CancelScheduledShow();

            if (tipWindow != null)
            {
                if (checkMouseOver)
                {
                    // Don't hide the tooltip window if the mouse pointer is inside it.
                    //Xwt.ModifierKeys m;
                    if (tipWindow.ScreenBounds.Contains(Xwt.Desktop.MouseLocation))
                        return;
                }
                tipWindow.Dispose();
                tipWindow = null;
                tipItem = null;
            }
        }

        void DelayedHideTooltip()
        {
            CancelScheduledHide();
            tipHideTimeoutId = Xwt.Timeout.Add(300, delegate
            {
                HideTooltip();
                tipHideTimeoutId = null;
                return false;
            });
        }

        void CancelScheduledHide()
        {
            CancelScheduledShow();
            if (tipHideTimeoutId != null)
            {
                tipHideTimeoutId.Dispose();
                tipHideTimeoutId = null;
            }
        }

        void CancelScheduledShow()
        {
            // Don't remove the timeout handler since it may be reused
            nextTipOffset = -1;
        }

        void OnDocumentStateChanged(object s, EventArgs a)
        {
            HideTooltip();
        }

        void OnTextSet(object sender, EventArgs e)
        {
            DocumentLine longest = Document.longestLineAtTextSet;
            if (longest != longestLine && longest != null)
            {
                int width = (int)(longest.Length * textViewMargin.CharWidth);
                if (width > longestLineWidth)
                {
                    longestLineWidth = width;
                    longestLine = longest;
                }
            }
        }
        #endregion

        #region Coordinate transformation
        public DocumentLocation PointToLocation(double xp, double yp, bool endAtEol = false)
        {
            return TextViewMargin.PointToLocation(xp, yp, endAtEol);
        }

        public DocumentLocation PointToLocation(Point p)
        {
            return TextViewMargin.PointToLocation(p);
        }

        public Point LocationToPoint(DocumentLocation loc)
        {
            return TextViewMargin.LocationToPoint(loc);
        }

        public Point LocationToPoint(int line, int column)
        {
            return TextViewMargin.LocationToPoint(line, column);
        }

        public Point LocationToPoint(int line, int column, bool useAbsoluteCoordinates)
        {
            return TextViewMargin.LocationToPoint(line, column, useAbsoluteCoordinates);
        }

        public Point LocationToPoint(DocumentLocation loc, bool useAbsoluteCoordinates)
        {
            return TextViewMargin.LocationToPoint(loc, useAbsoluteCoordinates);
        }

        public double ColumnToX(DocumentLine line, int column)
        {
            return TextViewMargin.ColumnToX(line, column);
        }

        /// <summary>
        /// Calculates the line number at line start (in one visual line could be several logical lines be displayed).
        /// </summary>
        public int YToLine(double yPos)
        {
            return TextViewMargin.YToLine(yPos);
        }

        public double LineToY(int logicalLine)
        {
            return TextViewMargin.LineToY(logicalLine);
        }

        public double GetLineHeight(DocumentLine line)
        {
            return TextViewMargin.GetLineHeight(line);
        }

        public double GetLineHeight(int logicalLineNumber)
        {
            return TextViewMargin.GetLineHeight(logicalLineNumber);
        }
        #endregion

        #region Animation
        Stage<Animation> animationStage = new Stage<Animation>();
        List<Animation> actors = new List<Animation>();

        protected void InitAnimations()
        {
            animationStage.ActorStep += OnAnimationActorStep;
            animationStage.Iteration += OnAnimationIteration;
        }

        void DisposeAnimations()
        {
            if (animationStage != null)
            {
                animationStage.Playing = false;
                animationStage.ActorStep -= OnAnimationActorStep;
                animationStage.Iteration -= OnAnimationIteration;
                animationStage = null;
            }

            if (actors != null)
            {
                foreach (Animation actor in actors)
                {
                    if (actor is IDisposable)
                        ((IDisposable)actor).Dispose();
                }
                actors.Clear();
                actors = null;
            }
        }

        Animation StartAnimation(IAnimationDrawer drawer)
        {
            return StartAnimation(drawer, 300);
        }

        Animation StartAnimation(IAnimationDrawer drawer, uint duration)
        {
            return StartAnimation(drawer, duration, Easing.Linear);
        }

        Animation StartAnimation(IAnimationDrawer drawer, uint duration, Easing easing)
        {
            if (!Options.EnableAnimations)
                return null;
            Animation animation = new Animation(drawer, duration, easing, Blocking.Upstage);
            animationStage.Add(animation, duration);
            actors.Add(animation);
            return animation;
        }

        bool OnAnimationActorStep(Actor<Animation> actor)
        {
            switch (actor.Target.AnimationState)
            {
                case AnimationState.Coming:
                    actor.Target.Drawer.Percent = actor.Percent;
                    if (actor.Expired)
                    {
                        actor.Target.AnimationState = AnimationState.Going;
                        actor.Reset();
                        return true;
                    }
                    break;
                case AnimationState.Going:
                    if (actor.Expired)
                    {
                        RemoveAnimation(actor.Target);
                        return false;
                    }
                    actor.Target.Drawer.Percent = 1.0 - actor.Percent;
                    break;
            }
            return true;
        }

        void RemoveAnimation(Animation animation)
        {
            if (animation == null)
                return;
            Rectangle bounds = animation.Drawer.AnimationBounds;
            actors.Remove(animation);
            if (animation is IDisposable)
                ((IDisposable)animation).Dispose();
            QueueDraw(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        void OnAnimationIteration(object sender, EventArgs args)
        {
            foreach (Animation actor in actors)
            {
                Rectangle bounds = actor.Drawer.AnimationBounds;
                QueueDraw(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }
        #endregion

        internal void FireLinkEvent(string link, int button, ModifierKeys modifierState)
        {
            if (LinkRequest != null)
                LinkRequest(this, new LinkEventArgs(link, button, modifierState));
        }

        public event EventHandler<LinkEventArgs> LinkRequest;

        /// <summary>
        /// Inserts a margin at the specified list position
        /// </summary>
        public void InsertMargin(int index, Margin margin)
        {
            margins.Insert(index, margin);
            RedrawFromLine(0);
        }

        /// <summary>
        /// Checks whether the editor has a margin of a given type
        /// </summary>
        public bool HasMargin(Type marginType)
        {
            return margins.Exists((margin) => { return marginType.IsAssignableFrom(margin.GetType()); });
        }

        /// <summary>
        /// Gets the first margin of a given type
        /// </summary>
        public Margin GetMargin(Type marginType)
        {
            return margins.Find((margin) => { return marginType.IsAssignableFrom(margin.GetType()); });
        }
        bool requestResetCaretBlink = false;
        public void RequestResetCaretBlink()
        {
            if (this.HasFocus)
                requestResetCaretBlink = true;
        }

        void UpdateLinesOnTextMarkerHeightChange(object sender, LineEventArgs e)
        {
            if (Document.CurrentAtomicUndoOperationType == OperationType.Format)
                return;
            if (!e.Line.Markers.Any(m => m is IExtendingTextLineMarker))
                return;
            var line = e.Line.LineNumber;
            textEditorData.HeightTree.SetLineHeight(line, GetLineHeight(e.Line));
            RedrawLine(line);
        }

        class SetCaret
        {
            TextEditor view;
            int line, column;
            bool highlightCaretLine;
            bool centerCaret;

            public SetCaret(TextEditor view, int line, int column, bool highlightCaretLine, bool centerCaret)
            {
                this.view = view;
                this.line = line;
                this.column = column;
                this.highlightCaretLine = highlightCaretLine;
                this.centerCaret = centerCaret;
            }

            public void Run(object sender, EventArgs e)
            {
                if (view.IsDisposed)
                    return;
                line = System.Math.Min(line, view.Document.LineCount);
                view.Caret.AutoScrollToCaret = false;
                try
                {
                    view.Caret.Location = new DocumentLocation(line, column);
                    view.SetFocus();
                    if (centerCaret)
                        view.CenterToCaret();
                    if (view.TextViewMargin.XOffset == 0)
                        view.HAdjustment.Value = 0;
                    view.TextArea.BoundsChanged -= Run;
                }
                finally
                {
                    view.Caret.AutoScrollToCaret = true;
                    if (highlightCaretLine)
                    {
                        view.TextViewMargin.HighlightCaretLine = true;
                        view.StartCaretPulseAnimation();
                    }
                }
            }
        }

        public void SetCaretTo(int line, int column)
        {
            SetCaretTo(line, column, true);
        }

        public void SetCaretTo(int line, int column, bool highlight)
        {
            SetCaretTo(line, column, highlight, true);
        }

        public void SetCaretTo(int line, int column, bool highlight, bool centerCaret)
        {
            if (line < DocumentLocation.MinLine)
                throw new ArgumentException("line < MinLine");
            if (column < DocumentLocation.MinColumn)
                throw new ArgumentException("column < MinColumn");

            if (!sizeHasBeenAllocated)
            {
                SetCaret setCaret = new SetCaret(editor, line, column, highlight, centerCaret);
                BoundsChanged += setCaret.Run;
            }
            else
            {
                new SetCaret(editor, line, column, highlight, centerCaret).Run(null, null);
            }
        }

        #region Container


        internal List<TextEditor.EditorContainerChild> containerChildren = new List<TextEditor.EditorContainerChild>();

        public void AddTopLevelWidget(Widget widget, int x, int y)
        {
            base.AddChild(widget);
            var info = new TextEditor.EditorContainerChild(this, widget);
            info.X = x;
            info.Y = y;
            var newContainerChildren = new List<TextEditor.EditorContainerChild>(containerChildren);
            newContainerChildren.Add(info);
            containerChildren = newContainerChildren;
            ResizeChild(Bounds, info);
            SetAdjustments();
        }

        public void MoveTopLevelWidget(Widget widget, double x, double y)
        {
            foreach (var info in containerChildren.ToArray())
            {
                if (info.Child == widget || (info.Child is AnimatedWidget && ((AnimatedWidget)info.Child).Widget == widget))
                {
                    if (info.X == x && info.Y == y)
                        break;
                    info.X = x;
                    info.Y = y;
                    if (widget.Visible)
                        ResizeChild(Bounds, info);
                    break;
                }
            }
            SetAdjustments();
        }

        /// <summary>
        /// Returns the position of an embedded widget
        /// </summary>
        public void GetTopLevelWidgetPosition(Widget widget, out double x, out double y)
        {
            foreach (var info in containerChildren.ToArray())
            {
                if (info.Child == widget || (info.Child is AnimatedWidget && ((AnimatedWidget)info.Child).Widget == widget))
                {
                    x = info.X;
                    y = info.Y;
                    return;
                }
            }
            x = y = 0;
        }

        public void MoveToTop(Widget widget)
        {
            var editorContainerChild = containerChildren.FirstOrDefault(c => c.Child == widget);
            if (editorContainerChild == null)
                throw new Exception("child " + widget + " not found.");
            var newChilds = containerChildren.Where(child => child != editorContainerChild).ToList();
            newChilds.Add(editorContainerChild);
            this.containerChildren = newChilds;
            QueueForReallocate();
        }

        protected new void AddChild(Widget widget)
        {
            AddTopLevelWidget(widget, 0, 0);
        }

        protected new void RemoveChild(Widget widget)
        {
            var newContainerChildren = new List<TextEditor.EditorContainerChild>(containerChildren);
            foreach (var info in newContainerChildren.ToArray())
            {
                if (info.Child == widget)
                {
                    base.RemoveChild(widget);
                    newContainerChildren.Remove(info);
                    SetAdjustments();
                    break;
                }
            }
            containerChildren = newContainerChildren;
        }

        void ResizeChild(Rectangle allocation, TextEditor.EditorContainerChild child)
        {
            var req = child.Child.Surface.GetPreferredSize();
            var childRectangle = new Xwt.Rectangle(child.X, child.Y, req.Width, req.Height);
            if (!child.FixedPosition)
            {
                //				double zoom = Options.Zoom;
                childRectangle.X = (int)(child.X /* * zoom */- HAdjustment.Value);
                childRectangle.Y = (int)(child.Y /* * zoom */- VAdjustment.Value);
            }
            //			childRectangle.X += allocation.X;
            //			childRectangle.Y += allocation.Y;
            SetChildBounds(child.Child, childRectangle);
        }

        void SetChildrenPositions(Rectangle allocation)
        {
            foreach (var child in containerChildren.ToArray())
            {
                ResizeChild(allocation, child);
            }
        }
        #endregion

    }

    public interface ITextEditorDataProvider
    {
        TextEditorData GetTextEditorData();
    }

    [Serializable]
    public sealed class PaintEventArgs : EventArgs
    {
        public Xwt.Drawing.Context Context
        {
            get;
            set;
        }

        public Rectangle Area
        {
            get;
            set;
        }

        public PaintEventArgs(Xwt.Drawing.Context context, Rectangle area)
        {
            this.Context = context;
            this.Area = area;
        }
    }
}


