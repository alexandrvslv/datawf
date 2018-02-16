using DataWF.Common;
using System.Collections.Generic;

namespace DataWF.Gui
{
    public class GroupBoxMap : LayoutMap
    {
        private GroupBox groupBox;

        public GroupBoxMap(GroupBox groupBox)
        {
            this.groupBox = groupBox;
        }

        public GroupBox GroupBox { get => groupBox; set => groupBox = value; }
    }
}

