using System;
using System.Collections.Generic;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class Menubar : PopupWindow, ILocalizable, ISerializableElement
    {
        public Menubar() : base(PopupType.Menu)
        {
            Bar = new Toolsbar() { Indent = 0 };
            Bar.ItemClick += OnItemClick;
            Bar.Items.GrowMode = Orientation.Vertical;

            BackgroundColor = GuiEnvironment.Theme["Window"].BaseColor;
            Content = Bar;
            Decorated = false;
            Padding = new WidgetSpacing(8, 8, 8, 8);
            InitialLocation = WindowLocation.Manual;
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

        public Menubar Owner { get; set; }

        public ToolDropDown OwnerItem
        {
            get { return Bar.Owner as ToolDropDown; }
            set { Bar.Owner = value; }
        }

        public override string Name
        {
            get { return base.Name; }
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

        public void Popup(Widget owner, Point point)
        {
            if (owner?.ParentWindow != null)
            {
                Owner = owner.ParentWindow as Menubar;
                if (TransientFor != null)
                {
                    ((Window)TransientFor).Hidden -= BaseGetFocus;
                    ((Window)TransientFor).Content.GotFocus -= BaseGetFocus;
                }
                TransientFor = owner.ParentWindow;
                if (TransientFor != null)
                {
                    ((Window)TransientFor).Hidden += BaseGetFocus;
                    ((Window)TransientFor).Content.GotFocus += BaseGetFocus;
                }
            }
            var location = owner?.ConvertToScreenCoordinates(point) ?? point;
            var screen = TransientFor?.Screen ?? Desktop.PrimaryScreen;
            if ((ScreenBounds.Width + location.X) > screen.Bounds.Right)
                location.X = screen.Bounds.Right - ScreenBounds.Width;
            Location = location;
            Show();
            Location = location;
        }

        private void BaseGetFocus(object sender, EventArgs e)
        {
            if (Visible)
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

        protected override void OnClosed()
        {
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

        public void Serialize(ISerializeWriter writer)
        {
            writer.Write(Bar.Items);
        }

        public void Deserialize(ISerializeReader reader)
        {
            reader.Read(Bar.Items);
        }
    }
}
