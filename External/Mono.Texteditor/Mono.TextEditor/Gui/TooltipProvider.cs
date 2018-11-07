//
// TooltipProvider.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc
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

namespace Mono.TextEditor
{
    public abstract class TooltipProvider
    {
        public abstract TooltipItem GetItem(TextEditor editor, double offset);

        public virtual bool IsInteractive(TextEditor editor, Xwt.Window tipWindow)
        {
            return false;
        }

        protected virtual void GetRequiredPosition(TextEditor editor, Xwt.Window tipWindow, out double requiredWidth, out double xalign)
        {
            requiredWidth = tipWindow.Width;
            xalign = 0.5;
        }

        protected virtual Xwt.Window CreateTooltipWindow(TextEditor editor, double offset, Xwt.ModifierKeys modifierState, TooltipItem item)
        {
            return null;
        }

        public virtual Xwt.Window ShowTooltipWindow(TextEditor editor, double offset, Xwt.ModifierKeys modifierState, Point mouse, TooltipItem item)
        {
            Xwt.Window tipWindow = CreateTooltipWindow(editor, offset, modifierState, item);
            if (tipWindow == null)
                return null;

            var point = editor.ConvertToScreenCoordinates(mouse);

            double w;
            double xalign;
            GetRequiredPosition(editor, tipWindow, out w, out xalign);
            w += 10;

            Rectangle geometry = editor.ParentWindow.Screen.VisibleBounds;

            point.X -= (int)((double)w * xalign);
            point.Y += 10;

            if (point.X + w >= geometry.X + geometry.Width)
                point.X = geometry.X + geometry.Width - w;
            if (point.X < geometry.Left)
                point.X = geometry.Left;

            var h = tipWindow.Size.Height;
            if (point.Y + h >= geometry.Y + geometry.Height)
                point.Y = geometry.Y + geometry.Height - h;
            if (point.Y < geometry.Top)
                point.Y = geometry.Top;

            tipWindow.Location = point;

            tipWindow.Show();

            return tipWindow;
        }
    }
}

