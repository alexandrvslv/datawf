using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using Xwt.Drawing;
using Xwt;
using System.Linq;
using DataWF.Module.Common;
using DataWF.Data.Gui;

namespace DataWF.Module.CommonGui
{
    public class UserEditor : VPanel, IDockContent
    {
        private User user;
        private GroupBoxItem groupAttributes = new GroupBoxItem();
        private GroupBoxItem groupGroups = new GroupBoxItem();
        private GroupBox groupMap = new GroupBox();
        private ListEditor fields = new ListEditor();
        private AccessEditor groups = new AccessEditor();

        public UserEditor()
        {
            var info = new LayoutFieldInfo();
            info.Columns.Indent = 6;
            info.Nodes.Add(new LayoutField("UserType"));
            info.Nodes.Add(new LayoutField("Parent"));
            info.Nodes.Add(new LayoutField("Position"));
            info.Nodes.Add(new LayoutField("Login"));
            info.Nodes.Add(new LayoutField("Password"));
            info.Nodes.Add(new LayoutField("Name"));
            info.Nodes.Add(new LayoutField("EMail"));

            var f = new LayoutDBTable();
            f.AllowCellSize = true;
            f.CheckView = false;
            f.EditMode = EditModes.ByClick;
            f.EditState = EditListState.Edit;
            f.GenerateColumns = false;
            f.GenerateFields = false;
            f.GenerateToString = false;
            f.FieldInfo = info;
            f.HighLight = true;
            f.Mode = LayoutListMode.Fields;
            f.Name = "fields";
            f.Text = "Attributes";

            fields.List = f;
            fields.Saving += OnFieldsSaving;
            groupAttributes.Widget = fields;
            groupAttributes.FillWidth = true;
            groupAttributes.FillHeight = true;
            groupAttributes.Text = "Parameters";

            groupGroups = new GroupBoxItem();
            groupGroups.Col = 2;
            groupGroups.Widget = groups;
            groupGroups.Width = 252;
            groupGroups.Text = "Groups";

            groupMap.Add(groupAttributes);
            groupMap.Add(groupGroups);

            groups.Name = "groups";
            groups.Text = "Groups";

            Name = "UserEditor";
            Text = "User Editor";
            ((Box)fields.Bar.Parent).Remove(fields.Bar);
            PackStart(fields.Bar, false, false);
            PackStart(groupMap, true, true);

            Localize();
            User.DBTable.RowUpdated += OnRowUpdated;
        }

        private void OnFieldsSaving(object sender, EventArgs e)
        {
            if (User.Status == DBStatus.Actual)
                User.Status = DBStatus.Edit;
        }

        private void OnRowUpdated(object sender, DBItemEventArgs arg)
        {
            if (User != null && arg.Item.PrimaryId.Equals(User.Id))
            {
                if (arg.Columns != null && arg.Columns.Contains(User.DBTable.ParseProperty(nameof(User.Password))))
                {
                    UserLog.LogUser(User, UserLogType.Password, "Temporary Password");
                }
            }
        }

        public void FillGroup()
        {
            user.Access.Fill();
            groups.Accessable = user;
            groups.SetType(AccessType.Create);
            groups.Readonly = false;
        }

        public User User
        {
            get { return user; }
            set
            {
                if (user == value)
                    return;
                user = value;
                fields.DataSource = user;
                user.PropertyChanged += UserPropertyChanged;
                //((Field)fields.FieldInfo.Nodes["Login"]).ReadOnly = _user.Attached;
                FillGroup();
            }
        }

        private void UserPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            FillGroup();
        }

        public DockType DockType
        {
            get { return DockType.Content; }
        }

        public bool HideOnClose
        {
            get { return false; }
        }

        public void Localize()
        {
            GuiService.Localize(this, "UserEditor", "User");
        }

        protected override void Dispose(bool disposing)
        {
            groupAttributes.Dispose();
            groupGroups.Dispose();
            User.DBTable.RowUpdated -= OnRowUpdated;
            base.Dispose(disposing);
        }

        private void OnLogin(User user)
        {
            string userLogin = user.Login;
            string userPassword = user.Password;

            var list = User.DBTable.Select("where " +
                                                 User.DBTable.CodeKey.Name + " = '" + userLogin + "' and " +
                                                 User.DBTable.ParseProperty(nameof(User.Password)).Name + " = '" + userPassword + "'").ToList();

            if (list.Count != 0)
            {
                User row = list[0];
                if (row.Status != DBStatus.Actual)
                {
                    MessageDialog.ShowMessage(ParentWindow, Locale.Get("Login", "User is blocked!"), "Login");
                }
                else
                {
                    User.SetCurrentUser(row);
                    //row ["session_start"] = DateTime.Now;
                    UserLog.LogUser(row, UserLogType.Start, null);
                    if (!row.Super.GetValueOrDefault())
                    {
                        GroupPermission.CachePermission();
                    }
                    MessageDialog.ShowMessage(ParentWindow, string.Format(Locale.Get("Login", "Welcome {0}"), row.Name), "Login");
                }
            }
            else
            {
                MessageDialog.ShowMessage(ParentWindow, Locale.Get("Login", "Authorization Error: check your Login and Password."), "Login");
            }
        }
    }
}
