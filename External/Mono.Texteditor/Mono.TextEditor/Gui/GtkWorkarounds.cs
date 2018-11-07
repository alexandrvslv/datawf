//
// GtkWorkarounds.cs
//
// Authors: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (C) 2011 Xamarin Inc.
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
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xwt;
using Xwt.Drawing;

namespace Mono.TextEditor
{
    public static class GtkWorkarounds
    {
        const int NSCriticalRequest = 0;
        const int NSInformationalRequest = 10;
        const int MonitorInfoFlagsPrimary = 0x01;

        static GtkWorkarounds()
        {
        }


        public static void AddValueClamped(this ScrollAdjustment adj, double value)
        {
            adj.Value = Math.Max(adj.LowerValue, Math.Min(adj.Value + value, adj.UpperValue - adj.PageSize));
        }

        public static void ShowContextMenu(Menu menu, Widget parent, ButtonEventArgs evt, Rectangle caret)
        {
            var window = parent.ParentWindow;

            var x = window.ScreenBounds.X;
            var y = window.ScreenBounds.Y;
            menu.Popup(parent, caret.X, caret.Y);
        }

        public static void ShowContextMenu(Menu menu, Widget parent, int ix, int iy, Rectangle caret)
        {
            var window = parent.ParentWindow;
            var alloc = parent.ParentBounds;

            var screen = window.ScreenBounds;
            screen.X += ix;
            screen.Y += iy;

            if (caret.X >= alloc.X && caret.Y >= alloc.Y)
            {
                screen.X += caret.X;
                screen.Y += caret.Y;
            }
            else
            {
                screen.X += alloc.X;
                screen.Y += alloc.Y;
            }
            menu.Popup(parent, screen.X, screen.Y);
        }

        public static void ShowContextMenu(Menu menu, Widget parent, ButtonEventArgs evt)
        {
            ShowContextMenu(menu, parent, evt, Rectangle.Zero);
        }

        public static void ShowContextMenu(Menu menu, Widget parent, int x, int y)
        {
            ShowContextMenu(menu, parent, x, y, Rectangle.Zero);
        }

        public static void ShowContextMenu(Menu menu, Widget parent, Rectangle caret)
        {
            ShowContextMenu(menu, parent, null, caret);
        }

        struct MappedKeys
        {
            public Key Key;
            public Xwt.ModifierKeys State;
            public KeyboardShortcut[] Shortcuts;
        }


        static void AddIfNotDuplicate<T>(List<T> list, T item) where T : IEquatable<T>
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(item))
                    return;
            }
            list.Add(item);
        }

        /// <summary>X coordinate of the pixels inside the right edge of the rectangle</summary>
        /// <remarks>Workaround for inconsistency of Right property between GTK# versions</remarks>
        public static double RightInside(this Rectangle rect)
        {
            return rect.X + rect.Width - 1;
        }

        /// <summary>Y coordinate of the pixels inside the bottom edge of the rectangle</summary>
        /// <remarks>Workaround for inconsistency of Bottom property between GTK# versions#</remarks>
        public static double BottomInside(this Rectangle rect)
        {
            return rect.Y + rect.Height - 1;
        }


        public static string MarkupLinks(string text)
        {
            return HighlightUrlSemanticRule.UrlRegex.Replace(text, MatchToUrl);
        }

        static string MatchToUrl(System.Text.RegularExpressions.Match m)
        {
            var s = m.ToString();
            return String.Format("<a href='{0}'>{1}</a>", s, s.Replace("_", "__"));
        }

        public static void Set2xVariant(Image px, Image variant2x)
        {
        }

        public static double GetPixelScale()
        {
            return 1d;
        }


    }

    public struct KeyboardShortcut : IEquatable<KeyboardShortcut>
    {
        public static readonly KeyboardShortcut Empty = new KeyboardShortcut((Key)0, (Xwt.ModifierKeys)0);

        Xwt.ModifierKeys modifier;
        Key key;

        public KeyboardShortcut(Key key, Xwt.ModifierKeys modifier)
        {
            this.modifier = modifier;
            this.key = key;
        }

        public Key Key
        {
            get { return key; }
        }

        public Xwt.ModifierKeys Modifier
        {
            get { return modifier; }
        }

        public bool IsEmpty
        {
            get { return Key == (Key)0; }
        }

        public override bool Equals(object obj)
        {
            return obj is KeyboardShortcut && this.Equals((KeyboardShortcut)obj);
        }

        public override int GetHashCode()
        {
            //FIXME: we're only using a few bits of mod and mostly the lower bits of key - distribute it better
            return (int)Key ^ (int)Modifier;
        }

        public bool Equals(KeyboardShortcut other)
        {
            return other.Key == Key && other.Modifier == Modifier;
        }
    }
}
