using System;
using System.Collections.Generic;
using DataWF;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Avalonia.Media.Immutable;
using Avalonia.Platform;

namespace DataWF.Gui
{
    public class Menubar : Window, ILocalizable, IXMLSerializable
    {
        public Menubar() : base()
        {
            Bar = new Toolsbar() { Indent = 0 };
            Bar.ItemClick += OnItemClick;
            Bar.Items.GrowMode = BoxOrientation.Vertical;
            Bar.Margin = new Thickness(8, 8, 8, 8);
            Bar.Background = new ImmutableSolidColorBrush(GuiEnvironment.Theme["Window"].BaseColor);

            Content = Bar;
            CanResize = true;
            WindowStartupLocation = WindowStartupLocation.Manual;
            ShowInTaskbar = false;
            Name = nameof(Menubar);
        }

        public Menubar(params ToolItem[] items) : this()
        {
            Items.AddRange(items);
        }

        public Toolsbar Bar { get; set; }

        public ToolItem Items { get { return Bar.Items; } }

        public ToolItem this[string name]
        {
            get { return (ToolItem)Items[name]; }
        }

        public Menubar OwnerBar { get; set; }

        public ToolDropDown OwnerItem
        {
            get => Bar.Owner as ToolDropDown;
            set => Bar.Owner = value;
        }

        public override string Name
        {
            get => base.Name;
            set
            {
                base.Name = value;
                Bar.Name = Name;
            }
        }

        public void Localize()
        {
            Bar.Localize();
        }

        public void Popup(Control owner, Point point)
        {
            if (owner?.GetParent<Window>() is Window window)
            {
                OwnerBar = window as Menubar;
                if (Owner is WindowBase baseWindow)
                {
                    baseWindow.Closed -= BaseGetFocus;
                    if (baseWindow.Content is Control content)
                        content.GotFocus -= BaseGetFocus;
                }
                Owner = window;
                if (Owner != null)
                {
                    window.Closed += BaseGetFocus;
                    if (window.Content is Control content)
                        content.GotFocus += BaseGetFocus;
                }
            }
            var size = Bar.Items.GetBound(0, 0);
            Width = size.Width;
            Height = size.Height;
            var location = owner?.PointToScreen(point) ?? new PixelPoint((int)point.X, (int)point.Y);
            var screen = Owner?.Screens.ScreenFromWindow((IWindowBaseImpl)Owner) ?? Owner?.Screens.Primary;
            if ((Owner.Bounds.Width + location.X) > screen.Bounds.Right)
                location = location.WithX((int)(screen.Bounds.Right - Owner.Bounds.Width));
            Position = location;
            Show();
            Position = location;
        }

        private void BaseGetFocus(object sender, EventArgs e)
        {
            if (IsVisible)
            {
                if (OwnerItem != null)
                {
                    OwnerItem.Bar.CurrentMenubar = null;
                }
                else
                {
                    Hide();
                }
            }
        }

        public new void Hide()
        {
            Owner = null;
            base.Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            BaseGetFocus(null, null);
        }

        private void OnItemClick(object sender, ToolItemEventArgs e)
        {
            if (e.Item.CheckOnClick
                || (e.Item is ToolDropDown && ((ToolDropDown)e.Item).HasDropDown))
            {
                return;
            }
            OwnerItem?.OnItemClick(e);
            BaseGetFocus(null, null);
        }

        public void Serialize(XmlInvokerWriter writer)
        {
            writer.Write(Bar.Items);
        }

        public void Deserialize(XmlInvokerReader reader)
        {
            reader.Read(Bar.Items);
        }
    }
}
