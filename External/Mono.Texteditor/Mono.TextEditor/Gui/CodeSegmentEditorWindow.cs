// 
// CodeSegmentEditorWindow.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor.Highlighting;
using Xwt;

namespace Mono.TextEditor
{
    public class CodeSegmentEditorWindow : Xwt.PopupWindow
    {
        TextEditor codeSegmentEditor = new TextEditor();

        public ISyntaxMode SyntaxMode
        {
            get { return codeSegmentEditor.Document.SyntaxMode; }
            set { codeSegmentEditor.Document.SyntaxMode = value; }
        }

        public string Text
        {
            get { return codeSegmentEditor.Document.Text; }
            set { codeSegmentEditor.Document.Text = value; }
        }

        public CodeSegmentEditorWindow(TextEditor editor) : base(PopupType.Menu)
        {
            var scrolledWindow = new ScrollView();
            scrolledWindow.Content = codeSegmentEditor;
            //scrolledWindow.ShadowType = Gtk.ShadowType.In;
            Content = scrolledWindow;

            ((SimpleEditMode)codeSegmentEditor.CurrentMode).AddBinding(Key.Escape, Close);
            TextEditorOptions options = new TextEditorOptions();
            options.FontName = editor.Options.FontName;
            options.ColorScheme = editor.Options.ColorScheme;
            options.ShowRuler = false;
            options.ShowLineNumberMargin = false;
            options.ShowFoldMargin = false;
            options.ShowIconMargin = false;
            options.Zoom = 0.8;
            codeSegmentEditor.Document.MimeType = editor.MimeType;
            codeSegmentEditor.Document.ReadOnly = true;
            codeSegmentEditor.Options = options;

            codeSegmentEditor.KeyPressed += delegate (object o, KeyEventArgs args)
            {
                if (args.Key == Key.Escape)
                    Dispose();
            };
            codeSegmentEditor.LostFocus += delegate (object o, EventArgs args)
            {
                Dispose();
            };
            TransientFor = editor.ParentWindow;
            ShowInTaskbar = false;
            Decorated = false;
            //Gdk.Pointer.Grab(this.GdkWindow, true, Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask, null, null, Gtk.Global.CurrentEventTime);
            //Gtk.Grab.Add(this);
            //GrabBrokenEvent += delegate
            //{
            //    Dispose();
            //};
            codeSegmentEditor.SetFocus();
        }

        public void Close(TextEditorData data)
        {
            Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            //Gtk.Grab.Remove(this);
            //Gdk.Pointer.Ungrab(Gtk.Global.CurrentEventTime);
            base.Dispose(disposing);
        }
    }
}

