//
// WindowTransparencyDecorator.cs
//
// Author:
//   Michael Hutchinson <mhutch@xamarin.com>
//
// Based on code derived from Banshee.Widgets.EllipsizeLabel
// by Aaron Bockover (aaron@aaronbock.net)
//
// Copyright (C) 2005-2008 Novell, Inc (http://www.novell.com)
// Copyright (C) 2012 Xamarin Inc (http://www.xamarin.com)
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
using System.Reflection;
using System.Runtime.InteropServices;

using Xwt;
using Xwt.Drawing;

namespace Mono.TextEditor.PopupWindow
{

    public class WindowTransparencyDecorator
    {
        Xwt.Window window;
        bool semiTransparent;
        const double opacity = 0.2;

        WindowTransparencyDecorator(Xwt.Window window)
        {
            this.window = window;

            window.Shown += ShownHandler;
            window.Hidden += HiddenHandler;
            window.Disposed += DestroyedHandler;
        }

        public static WindowTransparencyDecorator Attach(Xwt.Window window)
        {
            return new WindowTransparencyDecorator(window);
        }

        public void Detach()
        {
            if (window == null)
                return;

            //remove the snooper
            HiddenHandler(null, null);

            //annul allreferences between this and the window
            window.Content.KeyPressed -= KeyPressed;
            window.Content.KeyReleased -= KeyReleased;
            window.Shown -= ShownHandler;
            window.Hidden -= HiddenHandler;
            window.Disposed -= DestroyedHandler;
            window = null;
        }

        void ShownHandler(object sender, EventArgs args)
        {
            window.Content.KeyPressed += KeyPressed;
            window.Content.KeyReleased += KeyReleased;
            SemiTransparent = false;
        }

        void HiddenHandler(object sender, EventArgs args)
        {
            window.Content.KeyPressed -= KeyPressed;
            window.Content.KeyReleased -= KeyReleased;
        }

        void DestroyedHandler(object sender, EventArgs args)
        {
            Detach();
        }

        void KeyPressed(object widget, KeyEventArgs evnt)
        {
            if (evnt.Key == Key.ControlLeft || evnt.Key == Key.ControlRight)
                SemiTransparent = true;
        }
        void KeyReleased(object widget, KeyEventArgs evnt)
        {
            if (evnt.Key == Key.ControlLeft || evnt.Key == Key.ControlRight)
                SemiTransparent = false;
        }

        bool SemiTransparent
        {
            set
            {
                if (semiTransparent != value)
                {
                    semiTransparent = value;
                    window.Opacity = semiTransparent ? opacity : 1.0;
                }
            }
        }
    }
}
