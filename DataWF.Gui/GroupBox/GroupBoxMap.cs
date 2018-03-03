using DataWF.Common;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Gui
{
    public class GroupBoxMap : LayoutMap
    {
        public GroupBoxMap(GroupBox groupBox)
        {
            GroupBox = groupBox;
        }

        public GroupBox GroupBox { get; set; }

        protected override void OnItemsListChanged(object sender, ListChangedEventArgs e)
        {
            base.OnItemsListChanged(sender, e);
            if (GroupBox != null)
                GroupBox.ResizeLayout();
        }
    }
}

