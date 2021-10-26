using System;
using Xwt;

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
            get { return point; }
            set { point = value; }
        }

        public DockPage Page
        {
            get { return page; }
            set { page = value; }
        }
    }
}
