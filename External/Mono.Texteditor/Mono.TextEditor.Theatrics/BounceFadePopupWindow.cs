// 
// BounceFadePopupWindow.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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

namespace Mono.TextEditor.Theatrics
{
    /// <summary>
    /// Tooltip that "bounces", then fades away.
    /// </summary>
    public abstract class BounceFadePopupWindow : Xwt.PopupWindow
    {
        Stage<BounceFadePopupWindow> stage = new Stage<BounceFadePopupWindow>();
        Image textImage = null;
        TextEditor editor;

        protected double scale = 0.0;
        protected double opacity = 1.0;

        public BounceFadePopupWindow(TextEditor editor) : base(PopupType.Tooltip)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));
            Decorated = false;
            this.editor = editor;
            Duration = 500;
            ExpandWidth = 12;
            ExpandHeight = 2;
            BounceEasing = Easing.Sine;

            stage.ActorStep += OnAnimationActorStep;
            stage.Iteration += OnAnimationIteration;
            stage.UpdateFrequency = 10;
        }

        protected TextEditor Editor { get { return editor; } }

        /// <summary>Duration of the animation, in milliseconds.</summary>
        public uint Duration { get; set; }

        /// <summary>The number of pixels by which the window's width will expand</summary>
        public uint ExpandWidth { get; set; }

        /// <summary>The number of pixels by which the window's height will expand</summary>
        public uint ExpandHeight { get; set; }

        /// <summary>The easing used for the bounce part of the animation.</summary>
        public Easing BounceEasing { get; set; }

        Rectangle rect;
        double vValue, hValue;
        protected Rectangle bounds;
        ScrollAdjustment vAdjustment;
        ScrollAdjustment hAdjustment;

        public virtual void Popup()
        {
            rect = editor.ParentWindow.ScreenBounds;
            bounds = CalculateInitialBounds();
            rect.X = rect.X + bounds.X - (int)(ExpandWidth / 2);
            rect.Y = rect.Y + bounds.Y - (int)(ExpandHeight / 2);
            Location = rect.Location;

            rect.Width = System.Math.Max(1, bounds.Width + (int)ExpandWidth);
            rect.Height = System.Math.Max(1, bounds.Height + (int)ExpandHeight);
            Size = rect.Size;

            stage.AddOrReset(this, Duration);
            stage.Play();
            ListenToEvents();
            Show();
        }

        protected void ListenToEvents()
        {
            if (vAdjustment == null)
            {
                vAdjustment = editor.VAdjustment;
                hAdjustment = editor.HAdjustment;

                vAdjustment.ValueChanged += HandleEditorVAdjustmentValueChanged;
                hAdjustment.ValueChanged += HandleEditorHAdjustmentValueChanged;
            }
            vValue = vAdjustment.Value;
            hValue = hAdjustment.Value;
        }

        protected override void OnShown()
        {
            base.OnShown();
        }

        protected void DetachEvents()
        {
            if (vAdjustment == null)
                return;
            vAdjustment.ValueChanged -= HandleEditorVAdjustmentValueChanged;
            hAdjustment.ValueChanged -= HandleEditorHAdjustmentValueChanged;
            vAdjustment = null;
            hAdjustment = null;
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            DetachEvents();
        }

        void HandleEditorVAdjustmentValueChanged(object sender, EventArgs e)
        {
            rect.Y += (int)(vValue - editor.VAdjustment.Value);
            Location = rect.Location;
            vValue = editor.VAdjustment.Value;
        }

        void HandleEditorHAdjustmentValueChanged(object sender, EventArgs e)
        {
            rect.X += (int)(hValue - editor.HAdjustment.Value);
            Location = rect.Location;
            hValue = editor.HAdjustment.Value;
        }

        void OnAnimationIteration(object sender, EventArgs args)
        {
            ((Canvas)Content).QueueDraw();
        }

        protected virtual bool OnAnimationActorStep(Actor<BounceFadePopupWindow> actor)
        {
            if (actor.Expired)
            {
                OnAnimationCompleted();
                return false;
            }

            // for the first half, use an easing
            if (actor.Percent < 0.5)
            {
                scale = Choreographer.Compose(actor.Percent * 2, BounceEasing);
                opacity = 1.0;
            }
            //for the second half, vary opacity linearly from 1 to 0.
            else
            {
                scale = Choreographer.Compose(1.0, BounceEasing);
                opacity = 2.0 - actor.Percent * 2;
            }
            return true;
        }

        protected virtual void OnAnimationCompleted()
        {
            StopPlaying();
        }

        protected override void Dispose(bool disposing)
        {
            editor.VAdjustment.ValueChanged -= HandleEditorVAdjustmentValueChanged;
            base.Dispose(disposing);
            StopPlaying();
        }

        internal virtual void StopPlaying()
        {
            stage.Playing = false;

            if (textImage != null)
            {
                textImage.Dispose();
                textImage = null;
            }
        }

        protected abstract Rectangle CalculateInitialBounds();

    }

    /// <summary>
    /// Tooltip that "bounces", then fades away.
    /// </summary>
    public abstract class BounceFadePopupWidget : Canvas
    {
        Stage<BounceFadePopupWidget> stage = new Stage<BounceFadePopupWidget>();
        Image textImage = null;
        TextEditor editor;

        protected double scale = 0.0;
        protected double opacity = 1.0;

        public BounceFadePopupWidget(TextEditor editor)
        {
            if (editor == null)
                throw new ArgumentNullException("Editor");
            this.editor = editor;
            Duration = 500;
            ExpandWidth = 12;
            ExpandHeight = 2;
            BounceEasing = Easing.Sine;

            stage.ActorStep += OnAnimationActorStep;
            stage.Iteration += OnAnimationIteration;
            stage.UpdateFrequency = 10;
        }

        protected TextEditor Editor { get { return editor; } }

        /// <summary>Duration of the animation, in milliseconds.</summary>
        public uint Duration { get; set; }

        /// <summary>The number of pixels by which the window's width will expand</summary>
        public uint ExpandWidth { get; set; }

        /// <summary>The number of pixels by which the window's height will expand</summary>
        public uint ExpandHeight { get; set; }

        /// <summary>The easing used for the bounce part of the animation.</summary>
        public Easing BounceEasing { get; set; }

        int xExpandedOffset, yExpandedOffset;
        Rectangle userspaceArea;
        Rectangle bounds;

        public virtual void Popup()
        {
            bounds = CalculateInitialBounds();

            //GTK uses integer position coords, so round fractions down, we'll add them back later as draw offsets
            int x = (int)System.Math.Floor(bounds.X);
            int y = (int)System.Math.Floor(bounds.Y);

            //capture any lost fractions to pass as an offset to Draw
            userspaceArea = new Rectangle(bounds.X - x, bounds.Y - y, bounds.Width, bounds.Height);

            //lose half-pixels on the expansion, it's not a big deal
            xExpandedOffset = (int)(System.Math.Floor(ExpandWidth / 2d));
            yExpandedOffset = (int)(System.Math.Floor(ExpandHeight / 2d));

            //round the width/height up to make sure we have room for restored fractions
            WidthRequest = System.Math.Max(1, (int)(System.Math.Ceiling(bounds.Width) + ExpandWidth));
            HeightRequest = System.Math.Max(1, (int)(System.Math.Ceiling(bounds.Height) + ExpandHeight));
            editor.TextArea.AddTopLevelWidget(this, x - xExpandedOffset, y - yExpandedOffset);

            stage.AddOrReset(this, Duration);
            stage.Play();
            Show();
        }

        void OnAnimationIteration(object sender, EventArgs args)
        {
            QueueDraw();
        }

        protected virtual bool OnAnimationActorStep(Actor<BounceFadePopupWidget> actor)
        {
            if (actor.Expired)
            {
                OnAnimationCompleted();
                return false;
            }

            // for the first half, use an easing
            if (actor.Percent < 0.5)
            {
                scale = Choreographer.Compose(actor.Percent * 2, BounceEasing);
                opacity = 1.0;
            }
            //for the second half, vary opacity linearly from 1 to 0.
            else
            {
                scale = Choreographer.Compose(1.0, BounceEasing);
                opacity = 1 - 2 * (actor.Percent - 0.5);
            }
            return true;
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            try
            {
                var alloc = dirtyRect;
                ctx.Translate(alloc.X, alloc.Y);
                ctx.Translate(xExpandedOffset * (1 - scale), yExpandedOffset * (1 - scale));
                var scaleX = (alloc.Width / userspaceArea.Width - 1) * scale + 1;
                var scaleY = (alloc.Height / userspaceArea.Height - 1) * scale + 1;
                ctx.Scale(scaleX, scaleY);
                Draw(ctx, userspaceArea);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in animation:" + e);
            }
        }

        protected abstract void Draw(Xwt.Drawing.Context context, Rectangle area);

        protected virtual void OnAnimationCompleted()
        {
            StopPlaying();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            StopPlaying();
        }

        internal virtual void StopPlaying()
        {
            stage.Playing = false;

            if (textImage != null)
            {
                textImage.Dispose();
                textImage = null;
            }
        }

        protected abstract Rectangle CalculateInitialBounds();
    }
}
