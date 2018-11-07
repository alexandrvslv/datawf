// 
// ViActionMaps.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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
using Xwt;

namespace Mono.TextEditor.Vi
{


    public static class ViActionMaps
    {

        public static Action<TextEditorData> GetEditObjectCharAction(char c, Motion motion)
        {
            if (motion == Motion.None) return GetEditObjectCharAction(c);

            switch (c)
            {
                case 'w':
                    return ViActions.InnerWord;
                case ')':
                case '}':
                case ']':
                case '>':
                    if (motion == Motion.Inner)
                        return ViActions.InnerSymbol(c);
                    else if (motion == Motion.Outer)
                        return ViActions.OuterSymbol(c);
                    else
                        return null;
                case '"':
                case '\'':
                case '`':
                    if (motion == Motion.Inner)
                        return ViActions.InnerQuote(c);
                    else if (motion == Motion.Outer)
                        return ViActions.OuterQuote(c);
                    else
                        return null;
                default:
                    return null;
            }
        }

        public static Action<TextEditorData> GetEditObjectCharAction(char c)
        {
            switch (c)
            {
                case 'W':
                case 'w':
                    return ViActions.WordEnd;
                case 'B':
                case 'b':
                    return ViActions.WordStart;
            }
            return GetNavCharAction(c);
        }

        public static Action<TextEditorData> GetNavCharAction(char c)
        {
            switch (c)
            {
                case 'h':
                    return ViActions.Left;
                case 'b':
                    return CaretMoveActions.PreviousSubword;
                case 'B':
                    return CaretMoveActions.PreviousWord;
                case 'l':
                    return ViActions.Right;
                case 'e':
                    return ViActions.NextSubwordEnd;
                case 'E':
                    return ViActions.NextWordEnd;
                case 'w':
                    return CaretMoveActions.NextSubword;
                case 'W':
                    return CaretMoveActions.NextWord;
                case 'k':
                    return ViActions.Up;
                case 'j':
                    return ViActions.Down;
                case '%':
                    return MiscActions.GotoMatchingBracket;
                case '0':
                    return CaretMoveActions.LineStart;
                case '^':
                case '_':
                    return CaretMoveActions.LineFirstNonWhitespace;
                case '$':
                    return ViActions.LineEnd;
                case 'G':
                    return CaretMoveActions.ToDocumentEnd;
                case '{':
                    return ViActions.MoveToPreviousEmptyLine;
                case '}':
                    return ViActions.MoveToNextEmptyLine;
            }
            return null;
        }

        public static Action<TextEditorData> GetDirectionKeyAction(Key key, Xwt.ModifierKeys modifier)
        {
            //
            // NO MODIFIERS
            //
            if ((modifier & (Xwt.ModifierKeys.Shift | Xwt.ModifierKeys.Control)) == 0)
            {
                switch (key)
                {
                    case Key.Left:
                    case Key.NumPadLeft:
                        return ViActions.Left;

                    case Key.Right:
                    case Key.NumPadRight:
                        return ViActions.Right;

                    case Key.Up:
                    case Key.NumPadUp:
                        return ViActions.Up;

                    case Key.Down:
                    case Key.NumPadDown:
                        return ViActions.Down;

                    //not strictly vi, but more useful IMO
                    case Key.NumPadHome:
                    case Key.Home:
                        return CaretMoveActions.LineHome;

                    case Key.NumPadEnd:
                    case Key.End:
                        return ViActions.LineEnd;

                    case Key.PageUp:
                        return CaretMoveActions.PageUp;

                    case Key.PageDown:
                        return CaretMoveActions.PageDown;
                }
            }
            //
            // === CONTROL ===
            //
            else if ((modifier & Xwt.ModifierKeys.Shift) == 0
                     && (modifier & Xwt.ModifierKeys.Control) != 0)
            {
                switch (key)
                {
                    case Key.Left:
                    case Key.NumPadLeft:
                        return CaretMoveActions.PreviousWord;

                    case Key.Right:
                    case Key.NumPadRight:
                        return CaretMoveActions.NextWord;

                    case Key.Up:
                    case Key.NumPadUp:
                        return ScrollActions.Up;

                    // usually bound at IDE level
                    case Key.u:
                        return CaretMoveActions.PageUp;

                    case Key.Down:
                    case Key.NumPadDown:
                        return ScrollActions.Down;

                    case Key.d:
                        return CaretMoveActions.PageDown;

                    case Key.NumPadHome:
                    case Key.Home:
                        return CaretMoveActions.ToDocumentStart;

                    case Key.NumPadEnd:
                    case Key.End:
                        return CaretMoveActions.ToDocumentEnd;
                }
            }
            return null;
        }

        public static Action<TextEditorData> GetInsertKeyAction(Key key, Xwt.ModifierKeys modifier)
        {
            //
            // NO MODIFIERS
            //
            if ((modifier & (Xwt.ModifierKeys.Shift | Xwt.ModifierKeys.Control)) == 0)
            {
                switch (key)
                {
                    case Key.Tab:
                        return MiscActions.InsertTab;

                    case Key.Return:
                    case Key.NumPadEnter:
                        return MiscActions.InsertNewLine;

                    case Key.BackSpace:
                        return DeleteActions.Backspace;

                    case Key.Delete:
                    case Key.NumPadDelete:
                        return DeleteActions.Delete;

                    case Key.Insert:
                        return MiscActions.SwitchCaretMode;
                }
            }
            //
            // CONTROL
            //
            else if ((modifier & Xwt.ModifierKeys.Control) != 0
                     && (modifier & Xwt.ModifierKeys.Shift) == 0)
            {
                switch (key)
                {
                    case Key.BackSpace:
                        return DeleteActions.PreviousWord;

                    case Key.Delete:
                    case Key.NumPadDelete:
                        return DeleteActions.NextWord;
                }
            }
            //
            // SHIFT
            //
            else if ((modifier & Xwt.ModifierKeys.Control) == 0
                     && (modifier & Xwt.ModifierKeys.Shift) != 0)
            {
                switch (key)
                {
                    case Key.Tab:
                        return MiscActions.RemoveTab;

                    case Key.BackSpace:
                        return DeleteActions.Backspace;

                    case Key.Return:
                    case Key.NumPadEnter:
                        return MiscActions.InsertNewLine;
                }
            }
            return null;
        }

        public static Action<TextEditorData> GetCommandCharAction(char c)
        {
            switch (c)
            {
                case 'u':
                    return MiscActions.Undo;
            }
            return null;
        }
    }
}
