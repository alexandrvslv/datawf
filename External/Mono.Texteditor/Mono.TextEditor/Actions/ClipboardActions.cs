// 
// ClipboardActions.cs
// 
// Author:
//   Mike Krüger <mkrueger@novell.com>
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007-2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Xwt;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor.Utils;
using System.Linq;

namespace Mono.TextEditor
{
    public static class ClipboardActions
    {
        public static void Cut(TextEditorData data)
        {
            if (data.IsSomethingSelected && data.CanEditSelection)
            {
                Copy(data);
                DeleteActions.Delete(data);
            }
            else
            {
                Copy(data);
                DeleteActions.CaretLine(data);
            }
        }

        public static void Copy(TextEditorData data)
        {
            CopyOperation operation = new CopyOperation();
            operation.CopyData(data);
            operation.SetData(1);
            operation.SetData(2);
            operation.SetData(3);
            operation.SetData(99);
        }

        public class CopyOperation
        {
            public static readonly string MonoTextId = "monotextedit";
            public const int TextType = 1;
            public const int HTMLTextType = 2;
            public const int RichTextType = 3;
            public const int MonoTextType = 99;

            const int UTF8_FORMAT = 8;


            public CopyOperation()
            {
            }

            public string GetCopiedPlainText(string eol = "\n")
            {
                var plainText = new StringBuilder();
                bool first = true;
                foreach (var line in copiedColoredChunks)
                {
                    if (!first)
                    {
                        plainText.Append(eol);
                    }
                    else
                    {
                        first = false;
                    }

                    foreach (var chunk in line)
                    {
                        plainText.Append(chunk.Text);
                    }
                }
                return plainText.ToString();
            }

            public void SetData(uint info)
            {
                switch (info)
                {
                    case TextType:
                        Clipboard.SetText(GetCopiedPlainText());
                        break;
                    case RichTextType:
                        var rtf = RtfWriter.GenerateRtf(copiedColoredChunks, docStyle, options);
                        Clipboard.SetData(TransferDataType.Rtf, Encoding.UTF8.GetBytes(rtf));
                        break;
                    case HTMLTextType:
                        var html = HtmlWriter.GenerateHtml(copiedColoredChunks, docStyle, options);
                        Clipboard.SetData(TransferDataType.Html, Encoding.UTF8.GetBytes(html));
                        break;
                    case MonoTextType:
                        byte[] rawText = Encoding.UTF8.GetBytes(GetCopiedPlainText());
                        var copyDataLength = (byte)(copyData != null ? copyData.Length : 0);
                        var dataOffset = 1 + 1 + copyDataLength;
                        byte[] data = new byte[rawText.Length + dataOffset];
                        data[1] = copyDataLength;
                        if (copyDataLength > 0)
                            copyData.CopyTo(data, 2);
                        rawText.CopyTo(data, dataOffset);
                        data[0] = 0;
                        if (isBlockMode)
                            data[0] |= 1;
                        if (isLineSelectionMode)
                            data[0] |= 2;
                        Clipboard.SetData(data);
                        break;
                }
            }
            bool isLineSelectionMode = false;
            bool isBlockMode = false;

            internal List<List<ColoredSegment>> copiedColoredChunks;
            byte[] copyData;

            public ColorScheme docStyle;
            ITextEditorOptions options;


            void CopyData(TextEditorData data, Selection selection)
            {
                if (!selection.IsEmpty && data != null && data.Document != null)
                {
                    this.docStyle = data.ColorStyle;
                    this.options = data.Options;
                    copyData = null;


                    switch (selection.SelectionMode)
                    {
                        case SelectionMode.Normal:
                            isBlockMode = false;
                            var segment = selection.GetSelectionRange(data);
                            copiedColoredChunks = ColoredSegment.GetChunks(data, segment);
                            var pasteHandler = data.TextPasteHandler;
                            if (pasteHandler != null)
                            {
                                try
                                {
                                    copyData = pasteHandler.GetCopyData(segment);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Exception while getting copy data:" + e);
                                }
                            }
                            break;
                        case SelectionMode.Block:
                            isBlockMode = true;
                            DocumentLocation visStart = data.LogicalToVisualLocation(selection.Anchor);
                            DocumentLocation visEnd = data.LogicalToVisualLocation(selection.Lead);
                            int startCol = System.Math.Min(visStart.Column, visEnd.Column);
                            int endCol = System.Math.Max(visStart.Column, visEnd.Column);
                            copiedColoredChunks = new List<List<ColoredSegment>>();
                            for (int lineNr = selection.MinLine; lineNr <= selection.MaxLine; lineNr++)
                            {
                                DocumentLine curLine = data.Document.GetLine(lineNr);
                                int col1 = curLine.GetLogicalColumn(data, startCol) - 1;
                                int col2 = System.Math.Min(curLine.GetLogicalColumn(data, endCol) - 1, curLine.Length);
                                if (col1 < col2)
                                {
                                    copiedColoredChunks.Add(ColoredSegment.GetChunks(data, new TextSegment(curLine.Offset + col1, col2 - col1)).First());
                                }
                                else
                                {
                                    copiedColoredChunks.Add(new List<ColoredSegment>());
                                }
                            }
                            break;
                    }
                }
                else
                {
                    copiedColoredChunks = null;
                }
            }

            public void CopyData(TextEditorData data)
            {
                Selection selection;
                isLineSelectionMode = !data.IsSomethingSelected;
                if (data.IsSomethingSelected)
                {
                    selection = data.MainSelection;
                }
                else
                {
                    var start = DeleteActions.GetStartOfLineOffset(data, data.Caret.Location);
                    var end = DeleteActions.GetEndOfLineOffset(data, data.Caret.Location, false);
                    selection = new Selection(data.OffsetToLocation(start), data.OffsetToLocation(end));
                }
                CopyData(data, selection);

                if (Copy != null)
                    Copy(GetCopiedPlainText());
            }

            public delegate void CopyDelegate(string text);
            public static event CopyDelegate Copy;
        }

        public static void CopyToPrimary(TextEditorData data)
        {
            if (Platform.IsWindows) // disable middle click on windows.
                return;
            Clipboard.SetText(data.SelectedText);
        }

        public static void ClearPrimary()
        {
            Clipboard.Clear();
        }

        static int PasteFrom(TextEditorData data, bool preserveSelection, int insertionOffset)
        {
            return PasteFrom(data, preserveSelection, insertionOffset, false);
        }

        static int PasteFrom(TextEditorData data, bool preserveSelection, int insertionOffset, bool preserveState)
        {
            int result = -1;
            if (!data.CanEdit(data.Document.OffsetToLineNumber(insertionOffset)))
                return result;
            if (Clipboard.ContainsData<byte[]>())
            {
                byte[] selBytes = Clipboard.GetData<byte[]>();
                var upperBound = System.Math.Max(0, System.Math.Min(selBytes[1], selBytes.Length - 2));
                byte[] copyData = new byte[upperBound];
                Array.Copy(selBytes, 2, copyData, 0, copyData.Length);
                var rawTextOffset = 1 + 1 + copyData.Length;
                string text = Encoding.UTF8.GetString(selBytes, rawTextOffset, selBytes.Length - rawTextOffset);
                bool pasteBlock = (selBytes[0] & 1) == 1;
                bool pasteLine = (selBytes[0] & 2) == 2;
                if (pasteBlock)
                {
                    using (var undo = data.OpenUndoGroup())
                    {
                        var version = data.Document.Version;
                        if (!preserveSelection)
                            data.DeleteSelectedText(!data.IsSomethingSelected || data.MainSelection.SelectionMode != SelectionMode.Block);
                        int startLine = data.Caret.Line;
                        data.EnsureCaretIsNotVirtual();
                        insertionOffset = version.MoveOffsetTo(data.Document.Version, insertionOffset);

                        data.Caret.PreserveSelection = true;
                        var lines = new List<string>();
                        int offset = 0;
                        while (true)
                        {
                            var delimiter = LineSplitter.NextDelimiter(text, offset);
                            if (delimiter.IsInvalid)
                                break;

                            int delimiterEndOffset = delimiter.EndOffset;
                            lines.Add(text.Substring(offset, delimiter.Offset - offset));
                            offset = delimiterEndOffset;
                        }
                        if (offset < text.Length)
                            lines.Add(text.Substring(offset, text.Length - offset));

                        int lineNr = data.Document.OffsetToLineNumber(insertionOffset);
                        int col = insertionOffset - data.Document.GetLine(lineNr).Offset;
                        int visCol = data.Document.GetLine(lineNr).GetVisualColumn(data, col);
                        DocumentLine curLine;
                        int lineCol = col;
                        result = 0;
                        for (int i = 0; i < lines.Count; i++)
                        {
                            while (data.Document.LineCount <= lineNr + i)
                            {
                                data.Insert(data.Document.TextLength, Environment.NewLine);
                                result += Environment.NewLine.Length;
                            }
                            curLine = data.Document.GetLine(lineNr + i);
                            if (lines[i].Length > 0)
                            {
                                lineCol = curLine.GetLogicalColumn(data, visCol);
                                if (curLine.Length + 1 < lineCol)
                                {
                                    result += lineCol - curLine.Length;
                                    data.Insert(curLine.Offset + curLine.Length, new string(' ', lineCol - curLine.Length));
                                }
                                data.Insert(curLine.Offset + lineCol, lines[i]);
                                result += lines[i].Length;
                            }
                            if (!preserveState)
                                data.Caret.Offset = curLine.Offset + lineCol + lines[i].Length;
                        }
                        if (!preserveState)
                            data.ClearSelection();
                        data.FixVirtualIndentation(startLine);
                        data.Caret.PreserveSelection = false;
                    }
                }
                else if (pasteLine)
                {
                    using (var undo = data.OpenUndoGroup())
                    {
                        if (!preserveSelection)
                            data.DeleteSelectedText(!data.IsSomethingSelected || data.MainSelection.SelectionMode != SelectionMode.Block);
                        data.EnsureCaretIsNotVirtual();

                        data.Caret.PreserveSelection = true;
                        result = text.Length;
                        DocumentLine curLine = data.Document.GetLine(data.Caret.Line);

                        result = PastePlainText(data, curLine.Offset, text + data.EolMarker, preserveSelection, copyData);
                        if (!preserveState)
                            data.ClearSelection();
                        data.Caret.PreserveSelection = false;
                        data.FixVirtualIndentation(curLine.LineNumber);
                    }
                }
                else
                {
                    result = PastePlainText(data, insertionOffset, text, preserveSelection, copyData);
                }
            }
            else if (Clipboard.ContainsData(TransferDataType.Text))
            {
                var text = Clipboard.GetText();
                result = PastePlainText(data, insertionOffset, text, preserveState);
            }



            return result;
        }

        static int PastePlainText(TextEditorData data, int offset, string text, bool preserveSelection = false, byte[] copyData = null)
        {
            int inserted = 0;
            var undo = data.OpenUndoGroup();
            var version = data.Document.Version;
            if (!preserveSelection)
                data.DeleteSelectedText(!data.IsSomethingSelected || data.MainSelection.SelectionMode != SelectionMode.Block);
            int startLine = data.Caret.Line;
            data.EnsureCaretIsNotVirtual();
            if (data.IsSomethingSelected && data.MainSelection.SelectionMode == SelectionMode.Block)
            {
                var selection = data.MainSelection;
                var visualInsertLocation = data.LogicalToVisualLocation(selection.Anchor);
                for (int lineNumber = selection.MinLine; lineNumber <= selection.MaxLine; lineNumber++)
                {
                    var lineSegment = data.GetLine(lineNumber);
                    int insertOffset = lineSegment.GetLogicalColumn(data, visualInsertLocation.Column) - 1;
                    string textToInsert;
                    if (lineSegment.Length < insertOffset)
                    {
                        int visualLastColumn = lineSegment.GetVisualColumn(data, lineSegment.Length + 1);
                        int charsToInsert = visualInsertLocation.Column - visualLastColumn;
                        int spaceCount = charsToInsert % data.Options.TabSize;
                        textToInsert = new string('\t', (charsToInsert - spaceCount) / data.Options.TabSize) + new string(' ', spaceCount) + text;
                        insertOffset = lineSegment.Length;
                    }
                    else
                    {
                        textToInsert = text;
                    }
                    inserted = data.Insert(lineSegment.Offset + insertOffset, textToInsert);
                }
            }
            else
            {
                offset = version.MoveOffsetTo(data.Document.Version, offset);
                inserted = data.PasteText(offset, text, copyData, ref undo);
            }
            data.FixVirtualIndentation(startLine);

            undo.Dispose();
            return inserted;
        }

        public static int PasteFromPrimary(TextEditorData data, int insertionOffset)
        {
            var result = PasteFrom(data, true, insertionOffset, true);
            data.Document.CommitLineUpdate(data.GetLineByOffset(insertionOffset));
            return result;
        }

        public static void Paste(TextEditorData data)
        {
            if (!data.CanEditSelection)
                return;
            PasteFrom(data, false, data.IsSomethingSelected ? data.SelectionRange.Offset : data.Caret.Offset);
        }
    }
}
