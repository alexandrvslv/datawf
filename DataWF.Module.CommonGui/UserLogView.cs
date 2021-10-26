using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Module.Common;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Module.CommonGui
{

    public class UserLogView : DataLogView
    {
        private ToolMenuItem toolModeUser;
        private ToolMenuItem toolModeGroup;

        private ToolDropDown toolType;
        private ToolMenuItem toolTypeAuthorization;
        private ToolMenuItem toolTypePassword;
        private ToolMenuItem toolTypeStart;
        private ToolMenuItem toolTypeStop;
        private ToolMenuItem toolTypeProcedure;
        private ToolMenuItem toolTypeTransaction;

        public UserLogView()
        {
            toolModeGroup = new ToolMenuItem { Name = "Group", Tag = DataLogMode.Group };
            toolModeUser = new ToolMenuItem { Name = "User", Tag = DataLogMode.User };

            toolMode.DropDownItems.AddRange(new[] { toolModeGroup, toolModeUser });

            toolTypeAuthorization = new ToolMenuItem() { Checked = true, Name = "Authorization", Tag = UserRegType.Authorization };
            toolTypePassword = new ToolMenuItem { Checked = true, Name = "Password", Tag = UserRegType.Password };
            toolTypeStart = new ToolMenuItem { Checked = true, Name = "Start", Tag = UserRegType.Start };
            toolTypeStop = new ToolMenuItem { Checked = true, Name = "Stop", Tag = UserRegType.Stop };
            toolTypeProcedure = new ToolMenuItem { Checked = true, Name = "Procedure", Tag = UserRegType.Execute };
            toolTypeTransaction = new ToolMenuItem { Checked = true, Name = "Transaction", Tag = UserRegType.Transaction };

            toolType = new ToolDropDown(
                toolTypeAuthorization,
                toolTypePassword,
                toolTypeStart,
                toolTypeStop,
                toolTypeProcedure,
                toolTypeTransaction)
            { DisplayStyle = ToolItemDisplayStyle.Text, Name = "LogType" };
            toolType.ItemClick += ToolTypeItemClicked;
            toolMode.InsertAfter(toolType);

            Name = "UserLog";
        }

        private void ToolTypeItemClicked(object sender, ToolItemEventArgs e)
        {
            e.Item.Checked = !e.Item.Checked;
            UpdateFilter();
        }

        protected override void SelectData()
        {
            if (list.SelectedItem is UserReg log)
            {
                detailList.ListSource = log.Items;
                detailRow.FieldSource = log;
            }
            else
            {
                base.SelectData();
            }
        }

        public override void UpdateFilter()
        {
            if (filter is User || filter is UserGroup || filter is UserReg)
            {
                if (!(list.ListSource is DBTableView<UserReg> view))
                {
                    if (list.ListSource is IDBTableView tableView)
                    {
                        tableView.Dispose();
                    }
                    list.ListSource = view = new DBTableView<UserReg>((string)null, DBViewKeys.Empty);
                }
                var query = view.Query;

                var f = new List<object>();
                foreach (var toolItem in toolType.DropDownItems)
                {
                    if (toolItem.Checked)
                    {
                        f.Add((int)(UserRegType)toolItem.Tag);
                    }
                }
                query.BuildPropertyParam(nameof(UserReg.RegType), CompareType.In, f);

                if (Date != null)
                {
                    query.BuildPropertyParam(nameof(UserReg.DateCreate), CompareType.GreaterOrEqual, Date.Min);
                    query.BuildPropertyParam(nameof(UserReg.DateCreate), CompareType.LessOrEqual, Date.Max.AddDays(1));
                }
                if (filter is User && mode == DataLogMode.User)
                {
                    query.BuildPropertyParam(nameof(UserReg.UserId), CompareType.Equal, filter.PrimaryId);
                }
                else if (filter is UserGroup && mode == DataLogMode.Group)
                {
                    query.BuildPropertyParam(nameof(UserReg.UserId), CompareType.In, ((UserGroup)filter).GetUsers().ToList());
                }
                else if (filter is UserReg)
                {
                    query.BuildPropertyParam(nameof(UserReg.ParentId), CompareType.Equal, filter.PrimaryId);
                }
            }
            base.UpdateFilter();
        }

    }
}
