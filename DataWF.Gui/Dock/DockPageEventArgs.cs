using System;
using Avalonia;

namespace DataWF.Gui
{
    public class DockPageEventArgs : EventArgs
    {
        private DockPage page;
        private Point point;

        public DockPageEventArgs(DockPage page)
        {
            this.page = page;
        }

        public Point Point
        {
            get => point;
            set => point = value;
        }

        public DockPage Page
        {
            get => page;
            set => page = value;
        }
    }
}
