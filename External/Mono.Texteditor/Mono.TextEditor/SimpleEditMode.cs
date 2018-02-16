//
// SimpleEditMode.cs
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
using System.Collections.Generic;
using Xwt;
using Xwt.Drawing;

namespace Mono.TextEditor
{
    public class SimpleEditMode : EditMode
    {
        Dictionary<int, Action<TextEditorData>> keyBindings = new Dictionary<int, Action<TextEditorData>>();
        public Dictionary<int, Action<TextEditorData>> KeyBindings { get { return keyBindings; } }

        public SimpleEditMode()
        {
            if (Platform.IsMac)
                InitMacBindings();
            else
                InitDefaultBindings();
        }

        void InitCommonBindings()
        {
            Action<TextEditorData> action;

            Xwt.ModifierKeys wordModifier = Platform.IsMac ? Xwt.ModifierKeys.Alt : Xwt.ModifierKeys.Control;
            Xwt.ModifierKeys subwordModifier = Platform.IsMac ? Xwt.ModifierKeys.Control : Xwt.ModifierKeys.Alt;

            // ==== Left ====

            action = CaretMoveActions.Left;
            keyBindings.Add(GetKeyCode(Key.Left), action);

            action = SelectionActions.MoveLeft;
            keyBindings.Add(GetKeyCode(Key.Left, Xwt.ModifierKeys.Shift), action);

            action = CaretMoveActions.PreviousWord;
            keyBindings.Add(GetKeyCode(Key.Left, wordModifier), action);

            action = SelectionActions.MovePreviousWord;
            keyBindings.Add(GetKeyCode(Key.Left, Xwt.ModifierKeys.Shift | wordModifier), action);

            // ==== Right ====

            action = CaretMoveActions.Right;
            keyBindings.Add(GetKeyCode(Key.Right), action);

            action = SelectionActions.MoveRight;
            keyBindings.Add(GetKeyCode(Key.Right, Xwt.ModifierKeys.Shift), action);

            action = CaretMoveActions.NextWord;
            keyBindings.Add(GetKeyCode(Key.Right, wordModifier), action);

            action = SelectionActions.MoveNextWord;
            keyBindings.Add(GetKeyCode(Key.Right, Xwt.ModifierKeys.Shift | wordModifier), action);

            // ==== Up ====

            action = CaretMoveActions.Up;
            keyBindings.Add(GetKeyCode(Key.Up), action);

            action = ScrollActions.Up;
            keyBindings.Add(GetKeyCode(Key.Up, Xwt.ModifierKeys.Control), action);

            // ==== Down ====

            action = CaretMoveActions.Down;
            keyBindings.Add(GetKeyCode(Key.Down), action);

            action = ScrollActions.Down;
            keyBindings.Add(GetKeyCode(Key.Down, Xwt.ModifierKeys.Control), action);

            // ==== Deletion, insertion ====

            action = MiscActions.SwitchCaretMode;
            keyBindings.Add(GetKeyCode(Key.Insert), action);

            keyBindings.Add(GetKeyCode(Key.Tab), MiscActions.InsertTab);
            keyBindings.Add(GetKeyCode(Key.Tab, Xwt.ModifierKeys.Shift), MiscActions.RemoveTab);

            action = MiscActions.InsertNewLine;
            keyBindings.Add(GetKeyCode(Key.Return), action);

            keyBindings.Add(GetKeyCode(Key.Return, Xwt.ModifierKeys.Control), MiscActions.InsertNewLinePreserveCaretPosition);
            keyBindings.Add(GetKeyCode(Key.Return, Xwt.ModifierKeys.Shift), MiscActions.InsertNewLineAtEnd);

            action = DeleteActions.Backspace;
            keyBindings.Add(GetKeyCode(Key.BackSpace), action);
            keyBindings.Add(GetKeyCode(Key.BackSpace, Xwt.ModifierKeys.Shift), action);

            keyBindings.Add(GetKeyCode(Key.BackSpace, wordModifier), DeleteActions.PreviousWord);

            action = DeleteActions.Delete;
            keyBindings.Add(GetKeyCode(Key.Delete), action);

            action = DeleteActions.NextWord;
            keyBindings.Add(GetKeyCode(Key.Delete, wordModifier), action);


            // == subword motions ==

            action = CaretMoveActions.PreviousSubword;
            keyBindings.Add(GetKeyCode(Key.Left, subwordModifier), action);

            action = SelectionActions.MovePreviousSubword;
            keyBindings.Add(GetKeyCode(Key.Left, Xwt.ModifierKeys.Shift | subwordModifier), action);

            action = CaretMoveActions.NextSubword;
            keyBindings.Add(GetKeyCode(Key.Right, subwordModifier), action);

            action = SelectionActions.MoveNextSubword;
            keyBindings.Add(GetKeyCode(Key.Right, Xwt.ModifierKeys.Shift | subwordModifier), action);

            keyBindings.Add(GetKeyCode(Key.BackSpace, subwordModifier), DeleteActions.PreviousSubword);

            action = DeleteActions.NextSubword;
            keyBindings.Add(GetKeyCode(Key.Delete, subwordModifier), action);
        }

        void InitDefaultBindings()
        {
            InitCommonBindings();

            Action<TextEditorData> action;

            // === Home ===

            action = CaretMoveActions.LineHome;
            keyBindings.Add(GetKeyCode(Key.Home), action);

            action = SelectionActions.MoveLineHome;
            keyBindings.Add(GetKeyCode(Key.Home, Xwt.ModifierKeys.Shift), action);

            action = CaretMoveActions.ToDocumentStart;
            keyBindings.Add(GetKeyCode(Key.Home, Xwt.ModifierKeys.Control), action);

            action = SelectionActions.MoveToDocumentStart;
            keyBindings.Add(GetKeyCode(Key.Home, Xwt.ModifierKeys.Shift | Xwt.ModifierKeys.Control), action);

            // ==== End ====

            action = CaretMoveActions.LineEnd;
            keyBindings.Add(GetKeyCode(Key.End), action);

            action = SelectionActions.MoveLineEnd;
            keyBindings.Add(GetKeyCode(Key.End, Xwt.ModifierKeys.Shift), action);

            action = CaretMoveActions.ToDocumentEnd;
            keyBindings.Add(GetKeyCode(Key.End, Xwt.ModifierKeys.Control), action);

            action = SelectionActions.MoveToDocumentEnd;
            keyBindings.Add(GetKeyCode(Key.End, Xwt.ModifierKeys.Shift | Xwt.ModifierKeys.Control), action);

            // ==== Cut, copy, paste ===

            action = ClipboardActions.Cut;
            keyBindings.Add(GetKeyCode(Key.Delete, Xwt.ModifierKeys.Shift), action);
            keyBindings.Add(GetKeyCode(Key.x, Xwt.ModifierKeys.Control), action);

            action = ClipboardActions.Copy;
            keyBindings.Add(GetKeyCode(Key.Insert, Xwt.ModifierKeys.Control), action);
            keyBindings.Add(GetKeyCode(Key.c, Xwt.ModifierKeys.Control), action);

            action = ClipboardActions.Paste;
            keyBindings.Add(GetKeyCode(Key.Insert, Xwt.ModifierKeys.Shift), action);
            keyBindings.Add(GetKeyCode(Key.v, Xwt.ModifierKeys.Control), action);

            // ==== Page up/down ====

            action = CaretMoveActions.PageUp;
            keyBindings.Add(GetKeyCode(Key.PageUp), action);

            action = SelectionActions.MovePageUp;
            keyBindings.Add(GetKeyCode(Key.PageUp, Xwt.ModifierKeys.Shift), action);

            action = CaretMoveActions.PageDown;
            keyBindings.Add(GetKeyCode(Key.PageDown), action);

            action = SelectionActions.MovePageDown;
            keyBindings.Add(GetKeyCode(Key.PageDown, Xwt.ModifierKeys.Shift), action);

            // ==== Misc ====

            keyBindings.Add(GetKeyCode(Key.a, Xwt.ModifierKeys.Control), SelectionActions.SelectAll);

            keyBindings.Add(GetKeyCode(Key.d, Xwt.ModifierKeys.Control), DeleteActions.CaretLine);
            keyBindings.Add(GetKeyCode(Key.D, Xwt.ModifierKeys.Shift | Xwt.ModifierKeys.Control), DeleteActions.CaretLineToEnd);

            keyBindings.Add(GetKeyCode(Key.z, Xwt.ModifierKeys.Control), MiscActions.Undo);
            keyBindings.Add(GetKeyCode(Key.z, Xwt.ModifierKeys.Control | Xwt.ModifierKeys.Shift), MiscActions.Redo);

            keyBindings.Add(GetKeyCode(Key.F2), BookmarkActions.GotoNext);
            keyBindings.Add(GetKeyCode(Key.F2, Xwt.ModifierKeys.Shift), BookmarkActions.GotoPrevious);

            keyBindings.Add(GetKeyCode(Key.b, Xwt.ModifierKeys.Control), MiscActions.GotoMatchingBracket);

            keyBindings.Add(GetKeyCode(Key.Escape), SelectionActions.ClearSelection);

            //Non-mac selection actions

            action = SelectionActions.MoveDown;
            keyBindings.Add(GetKeyCode(Key.Down, Xwt.ModifierKeys.Shift), action);
            keyBindings.Add(GetKeyCode(Key.Down, Xwt.ModifierKeys.Shift | Xwt.ModifierKeys.Control), action);

            action = SelectionActions.MoveUp;
            keyBindings.Add(GetKeyCode(Key.Up, Xwt.ModifierKeys.Shift), action);
            keyBindings.Add(GetKeyCode(Key.Up, Xwt.ModifierKeys.Shift | Xwt.ModifierKeys.Control), action);
        }

        void InitMacBindings()
        {
            InitCommonBindings();

            Action<TextEditorData> action;

            // Up/down

            action = CaretMoveActions.UpLineStart;
            keyBindings.Add(GetKeyCode(Key.Up, Xwt.ModifierKeys.Alt), action);

            action = CaretMoveActions.DownLineEnd;
            keyBindings.Add(GetKeyCode(Key.Down, Xwt.ModifierKeys.Alt), action);

            action = SelectionActions.MoveUpLineStart;
            keyBindings.Add(GetKeyCode(Key.Up, Xwt.ModifierKeys.Alt | Xwt.ModifierKeys.Shift), action);

            action = SelectionActions.MoveDownLineEnd;
            keyBindings.Add(GetKeyCode(Key.Down, Xwt.ModifierKeys.Alt | Xwt.ModifierKeys.Shift), action);

            // === Home ===

            action = CaretMoveActions.LineHome;
            keyBindings.Add(GetKeyCode(Key.Left, Xwt.ModifierKeys.Command), action);
            keyBindings.Add(GetKeyCode(Key.a, Xwt.ModifierKeys.Control), action); //emacs
            keyBindings.Add(GetKeyCode(Key.a, Xwt.ModifierKeys.Control | Xwt.ModifierKeys.Shift), SelectionActions.MoveLineHome);

            action = SelectionActions.MoveLineHome;
            keyBindings.Add(GetKeyCode(Key.Left, Xwt.ModifierKeys.Command | Xwt.ModifierKeys.Shift), action);

            action = CaretMoveActions.ToDocumentStart;
            keyBindings.Add(GetKeyCode(Key.Home), action);
            keyBindings.Add(GetKeyCode(Key.Up, Xwt.ModifierKeys.Command), action);

            action = SelectionActions.MoveToDocumentStart;
            keyBindings.Add(GetKeyCode(Key.Home, Xwt.ModifierKeys.Shift), action);
            keyBindings.Add(GetKeyCode(Key.Up, Xwt.ModifierKeys.Command | Xwt.ModifierKeys.Shift), action);

            // ==== End ====

            action = CaretMoveActions.LineEnd;
            keyBindings.Add(GetKeyCode(Key.Right, Xwt.ModifierKeys.Command), action);
            keyBindings.Add(GetKeyCode(Key.e, Xwt.ModifierKeys.Control), action); //emacs
            keyBindings.Add(GetKeyCode(Key.e, Xwt.ModifierKeys.Control | Xwt.ModifierKeys.Shift), SelectionActions.MoveLineEnd);


            action = SelectionActions.MoveLineEnd;
            keyBindings.Add(GetKeyCode(Key.Right, Xwt.ModifierKeys.Command | Xwt.ModifierKeys.Shift), action);

            action = CaretMoveActions.ToDocumentEnd;
            keyBindings.Add(GetKeyCode(Key.End), action);
            keyBindings.Add(GetKeyCode(Key.Down, Xwt.ModifierKeys.Command), action);

            action = SelectionActions.MoveToDocumentEnd;
            keyBindings.Add(GetKeyCode(Key.End, Xwt.ModifierKeys.Shift), action);
            keyBindings.Add(GetKeyCode(Key.Down, Xwt.ModifierKeys.Command | Xwt.ModifierKeys.Shift), action);

            // ==== Cut, copy, paste ===

            action = ClipboardActions.Cut;
            keyBindings.Add(GetKeyCode(Key.x, Xwt.ModifierKeys.Command), action);
            keyBindings.Add(GetKeyCode(Key.w, Xwt.ModifierKeys.Control), action); //emacs

            action = ClipboardActions.Copy;
            keyBindings.Add(GetKeyCode(Key.c, Xwt.ModifierKeys.Command), action);

            action = ClipboardActions.Paste;
            keyBindings.Add(GetKeyCode(Key.v, Xwt.ModifierKeys.Command), action);
            keyBindings.Add(GetKeyCode(Key.y, Xwt.ModifierKeys.Control), action); //emacs

            // ==== Page up/down ====

            action = ScrollActions.PageDown;
            keyBindings.Add(GetKeyCode(Key.PageDown), action);

            action = ScrollActions.PageUp;
            keyBindings.Add(GetKeyCode(Key.PageUp), action);

            action = CaretMoveActions.PageDown;
            keyBindings.Add(GetKeyCode(Key.PageDown, Xwt.ModifierKeys.Alt), action);

            action = CaretMoveActions.PageUp;
            keyBindings.Add(GetKeyCode(Key.PageUp, Xwt.ModifierKeys.Alt), action);

            action = SelectionActions.MovePageUp;
            keyBindings.Add(GetKeyCode(Key.PageUp, Xwt.ModifierKeys.Shift), action);

            action = SelectionActions.MovePageDown;
            keyBindings.Add(GetKeyCode(Key.PageDown, Xwt.ModifierKeys.Shift), action);

            // ==== Misc ====

            keyBindings.Add(GetKeyCode(Key.a, Xwt.ModifierKeys.Command), SelectionActions.SelectAll);

            keyBindings.Add(GetKeyCode(Key.z, Xwt.ModifierKeys.Command), MiscActions.Undo);
            keyBindings.Add(GetKeyCode(Key.z, Xwt.ModifierKeys.Command | Xwt.ModifierKeys.Shift), MiscActions.Redo);

            // selection actions

            action = SelectionActions.MoveDown;
            keyBindings.Add(GetKeyCode(Key.Down, Xwt.ModifierKeys.Shift), action);

            action = SelectionActions.MoveUp;
            keyBindings.Add(GetKeyCode(Key.Up, Xwt.ModifierKeys.Shift), action);

            // extra emacs stuff
            keyBindings.Add(GetKeyCode(Key.f, Xwt.ModifierKeys.Control), CaretMoveActions.Right);
            keyBindings.Add(GetKeyCode(Key.b, Xwt.ModifierKeys.Control), CaretMoveActions.Left);
            keyBindings.Add(GetKeyCode(Key.p, Xwt.ModifierKeys.Control), CaretMoveActions.Up);
            keyBindings.Add(GetKeyCode(Key.n, Xwt.ModifierKeys.Control), CaretMoveActions.Down);
            keyBindings.Add(GetKeyCode(Key.h, Xwt.ModifierKeys.Control), DeleteActions.Backspace);
            keyBindings.Add(GetKeyCode(Key.d, Xwt.ModifierKeys.Control), DeleteActions.Delete);
            keyBindings.Add(GetKeyCode(Key.o, Xwt.ModifierKeys.Control), MiscActions.InsertNewLinePreserveCaretPosition);
        }

        public void AddBinding(Key key, Action<TextEditorData> action)
        {
            keyBindings.Add(GetKeyCode(key), action);
        }

        public override void SelectValidShortcut(KeyboardShortcut[] accels, out Key key, out ModifierKeys mod)
        {
            foreach (var accel in accels)
            {
                int keyCode = GetKeyCode(accel.Key, accel.Modifier);
                if (keyBindings.ContainsKey(keyCode))
                {
                    key = accel.Key;
                    mod = accel.Modifier;
                    return;
                }
            }
            key = accels[0].Key;
            mod = accels[0].Modifier;
        }


        protected override void HandleKeypress(Key key, int unicodeKey, Xwt.ModifierKeys modifier)
        {
            int keyCode = GetKeyCode(key, modifier);
            if (keyBindings.ContainsKey(keyCode))
            {
                RunAction(keyBindings[keyCode]);
            }
            else if (unicodeKey != 0)// && modifier == Xwt.ModifierKeys.None)
            {
                InsertCharacter(unicodeKey);
            }
        }
    }
}
