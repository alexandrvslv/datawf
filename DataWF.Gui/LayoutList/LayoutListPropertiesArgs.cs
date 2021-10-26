using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataWF.Gui
{
    public class LayoutListPropertiesArgs : EventArgs
    {
        public LayoutListPropertiesArgs()
            : base()
        { }

        public List<string> Properties;
        public ILayoutCell Cell;
    }
}
