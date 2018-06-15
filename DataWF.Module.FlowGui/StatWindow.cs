using DataWF.Module.Common;
using DataWF.Gui;
using DataWF.Common;

namespace DataWF.Module.FlowGui
{
    public class StatWindow : ToolWindow
    {
        LayoutList users = new LayoutList();
        LayoutList stats = new LayoutList();
        GroupBox map = new GroupBox();

        public StatWindow()
        {
            Mode = ToolShowMode.Dialog;
            ButtonClose.Visible = false;

            users.ListSource = Instance.DBTable.DefaultView;
            stats.ListSource = NetStat.Items;

            map.Add(new GroupBoxItem() { Widget = users, Name = "Users", FillHeight = true, FillWidth = true });
            map.Add(new GroupBoxItem() { Widget = stats, Name = "Statistic", Row = 1, FillHeight = true, FillWidth = true });
            Target = map;
            Title = "Instance Network Status";
        }

    }
}

