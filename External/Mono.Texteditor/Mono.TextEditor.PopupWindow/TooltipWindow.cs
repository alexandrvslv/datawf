// 
// TooltipWindow.cs
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
using System.Reflection;
using System.Runtime.InteropServices;

using Xwt;
using Xwt.Drawing;

namespace Mono.TextEditor.PopupWindow
{
    public class TooltipWindow : Xwt.PopupWindow
    {
        WindowTransparencyDecorator decorator;
        readonly Label label;
        string markup;

        public string Markup
        {
            get { return markup; }
            set { label.Markup = markup = value; }
        }

        public TooltipWindow() : base(PopupType.Tooltip)
        {
            label = new Label();
            label.Ellipsize = EllipsizeMode.End;

            this.ShowInTaskbar = false;
            this.Decorated = false;
            //fake widget name for stupid theme engines
            this.Name = "gtk-tooltip";
            this.Title = "tooltip";
            this.Content = label;
            EnableTransparencyControl = true;
        }

        public double SetMaxWidth(double maxWidth)
        {
            var l = (Label)Content;
            l.WidthRequest = maxWidth;
            return maxWidth;
        }

        public bool NudgeVertical
        {
            get; set;
        }

        public bool NudgeHorizontal
        {
            get; set;
        }

        public bool EnableTransparencyControl
        {
            get { return decorator != null; }
            set
            {
                if (value && decorator == null)
                    decorator = WindowTransparencyDecorator.Attach(this);
                else if (!value && decorator != null)
                    decorator.Detach();
            }
        }

        //protected override bool OnDraw(Gdk.EventExpose evnt)
        //{
        //    int winWidth, winHeight;
        //    this.GetSize(out winWidth, out winHeight);
        //    Gtk.Style.PaintFlatBox(Style, this.GdkWindow, StateType.Normal, ShadowType.Out, evnt.Area, this, "tooltip", 0, 0, winWidth, winHeight);
        //    foreach (var child in this.Children)
        //        this.PropagateExpose(child, evnt);
        //    return false;
        //}

        protected override void OnBoundsChanged(BoundsChangedEventArgs a)
        {
            base.OnBoundsChanged(a);
            if (NudgeHorizontal || NudgeVertical)
            {
                var loc = Location;
                var old = loc;
                const int edgeGap = 2;

                //				int w = Bounds.Width;
                //				
                //				if (fitWidthToScreen && (x + w >= screenW - edgeGap)) {
                //					int fittedWidth = screenW - x - edgeGap;
                //					if (fittedWidth < minFittedWidth) {
                //						x -= (minFittedWidth - fittedWidth);
                //						fittedWidth = minFittedWidth;
                //					}
                //					LimitWidth (fittedWidth);
                //				}

                Rectangle geometry = Screen.VisibleBounds;
                if (NudgeHorizontal)
                {
                    if (ScreenBounds.Width <= geometry.Width && loc.X + ScreenBounds.Width >= geometry.Left + geometry.Width - edgeGap)
                        loc.X = geometry.Left + (geometry.Width - ScreenBounds.Width - edgeGap);
                    if (loc.X <= geometry.Left + edgeGap)
                        loc.X = geometry.Left + edgeGap;
                }

                if (NudgeVertical)
                {
                    if (ScreenBounds.Height <= geometry.Height && loc.Y + ScreenBounds.Height >= geometry.Top + geometry.Height - edgeGap)
                        loc.Y = geometry.Top + (geometry.Height - ScreenBounds.Height - edgeGap);
                    if (loc.Y <= geometry.Top + edgeGap)
                        loc.Y = geometry.Top + edgeGap;
                }

                if (loc != old)
                    Location = loc;
            }
        }

        //		void LimitWidth (int width)
        //		{
        //			if (Child is MonoDevelop.Components.FixedWidthWrapLabel) 
        //				((MonoDevelop.Components.FixedWidthWrapLabel)Child).MaxWidth = width - 2 * (int)this.BorderWidth;
        //			
        //			int childWidth = Child.SizeRequest ().Width;
        //			if (childWidth < width)
        //				WidthRequest = childWidth;
        //			else
        //				WidthRequest = width;
        //		}


        //static int tooltipTypeHint = -1;
        [System.ComponentModel.Category("MonoDevelop.Components")]
        [System.ComponentModel.ToolboxItem(true)]
        public class FixedWidthWrapLabel : Canvas
        {
            string text;
            bool use_markup = false;
            TextLayout layout;
            int indent;
            double width = int.MaxValue;

            bool breakOnPunctuation;
            bool breakOnCamelCasing;
            string brokentext;

            TextTrimming wrapMode = TextTrimming.Word;

            public FixedWidthWrapLabel()
            {
                CreateLayout();
            }

            public FixedWidthWrapLabel(string text)
                : this()
            {
                this.text = text;
            }

            public FixedWidthWrapLabel(string text, int width)
                : this(text)
            {
                this.width = width;
            }

            private void CreateLayout()
            {
                if (layout != null)
                {
                    layout.Dispose();
                }

                layout = PangoUtil.CreateLayout(this, null);
                if (use_markup)
                {
                    layout.Markup = brokentext != null ? brokentext : (text ?? string.Empty);
                }
                else
                {
                    layout.Text = brokentext != null ? brokentext : (text ?? string.Empty);
                }
                layout.Trimming = wrapMode;
                if (width >= 0)
                {
                    layout.Width = width;
                }
                else
                {
                    layout.Width = -1;
                }
                QueueForReallocate();
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (layout != null)
                {
                    layout.Dispose();
                    layout = null;
                }
            }

            private void UpdateLayout()
            {
                if (layout == null)
                {
                    CreateLayout();
                }
            }

            public double MaxWidth
            {
                get { return width; }
                set
                {
                    width = value;
                    if (layout != null)
                    {
                        if (width >= 0)
                            layout.Width = (int)(width);
                        else
                            layout.Width = -1;
                        QueueForReallocate();
                    }
                }
            }

            public double RealWidth
            {
                get
                {
                    UpdateLayout();
                    return layout.GetSize().Width;
                }
            }

            protected override Size OnGetPreferredSize(SizeConstraint w, SizeConstraint h)
            {
                base.OnGetPreferredSize(w, h);
                UpdateLayout();
                var size = layout.GetSize();
                size.Width += indent;
                return size;
            }

            //		protected override void OnSizeAllocated (Rectangle Bounds)
            //		{
            //			//wrap to Bounds and set automatic height if MaxWidth is -1
            //			if (width < 0) {
            //				int lw, lh;
            //				layout.Width = (int)(Bounds.Width  );
            //				layout.GetPixelSize (out lw, out lh);
            //				HeightRequest = lh;
            //			}
            //			base.OnSizeAllocated (Bounds);
            //		}


            protected override void OnDraw(Context ctx, Rectangle r)
            {
                UpdateLayout();
                ctx.DrawTextLayout(layout, Bounds.X + indent, Bounds.Y);
            }

            public string Markup
            {
                get { return text; }
                set
                {
                    use_markup = true;
                    text = value;
                    breakText();
                }
            }

            public string Text
            {
                get { return text; }
                set
                {
                    use_markup = false;
                    text = value;
                    breakText();
                }
            }

            public int Indent
            {
                get { return indent; }
                set
                {
                    indent = value;
                    if (layout != null)
                    {
                        QueueForReallocate();
                    }
                }
            }

            public bool BreakOnPunctuation
            {
                get { return breakOnPunctuation; }
                set
                {
                    breakOnPunctuation = value;
                    breakText();
                }
            }

            public bool BreakOnCamelCasing
            {
                get { return breakOnCamelCasing; }
                set
                {
                    breakOnCamelCasing = value;
                    breakText();
                }
            }

            public TextTrimming Wrap
            {
                get { return wrapMode; }
                set
                {
                    wrapMode = value;
                    if (layout != null)
                    {
                        layout.Trimming = wrapMode;
                        QueueForReallocate();
                    }
                }
            }

            void breakText()
            {
                brokentext = null;
                if ((!breakOnCamelCasing && !breakOnPunctuation) || string.IsNullOrEmpty(text))
                {
                    QueueForReallocate();
                    return;
                }

                var sb = new System.Text.StringBuilder(text.Length);

                bool prevIsLower = false;
                bool inMarkup = false;
                bool inEntity = false;

                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];

                    //ignore markup
                    if (use_markup)
                    {
                        switch (c)
                        {
                            case '<':
                                inMarkup = true;
                                sb.Append(c);
                                continue;
                            case '>':
                                inMarkup = false;
                                sb.Append(c);
                                continue;
                            case '&':
                                inEntity = true;
                                sb.Append(c);
                                continue;
                            case ';':
                                if (inEntity)
                                {
                                    inEntity = false;
                                    sb.Append(c);
                                    continue;
                                }
                                break;
                        }
                    }
                    if (inMarkup || inEntity)
                    {
                        sb.Append(c);
                        continue;
                    }

                    //insert breaks using zero-width space unicode char
                    if ((breakOnPunctuation && char.IsPunctuation(c))
                        || (breakOnCamelCasing && prevIsLower && char.IsUpper(c)))
                        sb.Append('\u200b');

                    sb.Append(c);

                    if (breakOnCamelCasing)
                        prevIsLower = char.IsLower(c);
                }
                brokentext = sb.ToString();

                if (layout != null)
                {
                    if (use_markup)
                    {
                        layout.Markup = brokentext != null ? brokentext : (text ?? string.Empty);
                    }
                    else
                    {
                        layout.Text = brokentext != null ? brokentext : (text ?? string.Empty);
                    }
                }
                QueueForReallocate();
            }
        }
    }
}
