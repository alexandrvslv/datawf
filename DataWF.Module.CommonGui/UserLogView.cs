using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using System.Linq;
using DataWF.Module.Common;
using Xwt;
using System.Collections;
using System.Threading.Tasks;

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

            toolTypeAuthorization = new ToolMenuItem() { Checked = true, Name = "Authorization", Tag = UserLogType.Authorization };
            toolTypePassword = new ToolMenuItem { Checked = true, Name = "Password", Tag = UserLogType.Password };
            toolTypeStart = new ToolMenuItem { Checked = true, Name = "Start", Tag = UserLogType.Start };
            toolTypeStop = new ToolMenuItem { Checked = true, Name = "Stop", Tag = UserLogType.Stop };
            toolTypeProcedure = new ToolMenuItem { Checked = true, Name = "Procedure", Tag = UserLogType.Execute };
            toolTypeTransaction = new ToolMenuItem { Checked = true, Name = "Transaction", Tag = UserLogType.Transaction };

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
            var log = list.SelectedItem as UserLog;
            if (log != null)
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
            if (filter is User || filter is UserGroup || filter is UserLog)
            {
                var view = list.ListSource as DBTableView<UserLog>;
                if (view == null)
                {
                    if (list.ListSource is IDBTableView)
                    {
                        ((IDisposable)list.ListSource).Dispose();
                    }
                    list.ListSource = view = new DBTableView<UserLog>((string)null, DBViewKeys.Empty);
                }
                var query = view.Query;

                var f = new List<object>();
                foreach (var toolItem in toolType.DropDownItems)
                {
                    if (toolItem.Checked)
                    {
                        f.Add((int)(UserLogType)toolItem.Tag);
                    }
                }
                query.BuildPropertyParam(nameof(UserLog.LogType), CompareType.In, f);

                if (Date != null)
                {
                    query.BuildPropertyParam(nameof(UserLog.DateCreate), CompareType.GreaterOrEqual, Date.Min);
                    query.BuildPropertyParam(nameof(UserLog.DateCreate), CompareType.LessOrEqual, Date.Max.AddDays(1));
                }
                if (filter is User && mode == DataLogMode.User)
                {
                    query.BuildPropertyParam(nameof(UserLog.UserId), CompareType.Equal, filter.PrimaryId);
                }
                else if (filter is UserGroup && mode == DataLogMode.Group)
                {
                    query.BuildPropertyParam(nameof(UserLog.UserId), CompareType.In, ((UserGroup)filter).GetUsers().ToList());
                }
                else if (filter is UserLog)
                {
                    query.BuildPropertyParam(nameof(UserLog.ParentId), CompareType.Equal, filter.PrimaryId);
                }
            }
            base.UpdateFilter();
        }

    }
}
