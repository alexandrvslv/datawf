//
// AnimatedVboxActor.cs
//
// Authors:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
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
using Xwt.Drawing;
using Xwt;

namespace Mono.TextEditor.Theatrics
{
    internal enum AnimationState
    {
        Coming,
        Idle,
        IntendingToGo,
        Going
    }

    internal class AnimatedWidget : Canvas
    {
        public event EventHandler WidgetDestroyed;

        public Widget Widget;
        public Easing Easing;
        public Blocking Blocking;
        public AnimationState AnimationState;
        public uint Duration;
        public double Bias = 1.0;
        public double Width;
        public double Height;
        public double StartPadding;
        public double EndPadding;

        public LinkedListNode<AnimatedWidget> Node;

        private readonly bool horizontal;
        private double percent;
        private Rectangle widget_alloc;
        private ImageBuilder canvas;

        public AnimatedWidget(Widget widget, uint duration, Easing easing, Blocking blocking, bool horizontal)
        {
            this.horizontal = horizontal;
            Widget = widget;
            Duration = duration;
            Easing = easing;
            Blocking = blocking;
            AnimationState = AnimationState.Coming;

            AddChild(Widget);
            Widget.Disposed += OnWidgetDestroyed;
        }

        public double Percent
        {
            get { return percent; }
            set
            {
                percent = value * Bias;
                QueueForReallocate();
            }
        }

        private void OnWidgetDestroyed(object sender, EventArgs args)
        {
            canvas = new ImageBuilder(widget_alloc.Width, widget_alloc.Height);
            //canvas.DrawDrawable(Style.BackgroundGC(State), GdkWindow, widget_alloc.X, widget_alloc.Y, 0, 0, widget_alloc.Width, widget_alloc.Height);
            if (AnimationState != AnimationState.Going)
            {
                WidgetDestroyed(this, args);
            }
        }

        #region Overrides

        protected new void RemoveChild(Widget widget)
        {
            base.RemoveChild(widget);
            if (widget == Widget)
            {
                Widget = null;
            }
        }
        protected override Size OnGetPreferredSize(SizeConstraint w, SizeConstraint h)
        {
            if (Widget != null)
            {
                var req = Widget.Surface.GetPreferredSize();
                widget_alloc.Width = req.Width;
                widget_alloc.Height = req.Height;
            }

            if (horizontal)
            {
                Width = Choreographer.PixelCompose(percent, widget_alloc.Width + StartPadding + EndPadding, Easing);
                Height = widget_alloc.Height;
            }
            else
            {
                Width = widget_alloc.Width;
                Height = Choreographer.PixelCompose(percent, widget_alloc.Height + StartPadding + EndPadding, Easing);
            }
            return new Size(Width, Height);
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            if (Widget != null)
            {
                if (horizontal)
                {
                    widget_alloc.Height = Size.Height;
                    widget_alloc.X = StartPadding;
                    if (Blocking == Blocking.Downstage)
                    {
                        widget_alloc.X += Size.Width - widget_alloc.Width;
                    }
                }
                else
                {
                    widget_alloc.Width = Size.Width;
                    widget_alloc.Y = StartPadding;
                    if (Blocking == Blocking.Downstage)
                    {
                        widget_alloc.Y = Size.Height - widget_alloc.Height;
                    }
                }

                if (widget_alloc.Height > 0 && widget_alloc.Width > 0)
                {
                    SetChildBounds(Widget, widget_alloc);
                }
            }
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            if (canvas != null)
            {
                ctx.DrawImage(canvas.ToVectorImage(), widget_alloc.X, widget_alloc.Y, widget_alloc.Width, widget_alloc.Height);
            }
        }

        #endregion

    }
}
