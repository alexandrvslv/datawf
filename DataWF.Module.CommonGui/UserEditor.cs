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
        private GroupBoxItem groupAttributes;
        private GroupBoxItem groupGroups;
        private GroupBox groupMap;
        private ListEditor fields;
        private AccessEditor groups;

        public UserEditor()
        {
            //var info = new LayoutFieldInfo();
            //info.Columns.Indent = 6;
            //info.Nodes.Add(new LayoutField("UserType"));
            //info.Nodes.Add(new LayoutField("Parent"));
            //info.Nodes.Add(new LayoutField("Position"));
            //info.Nodes.Add(new LayoutField("Login"));
            //info.Nodes.Add(new LayoutField("Password"));
            //info.Nodes.Add(new LayoutField("Name"));
            //info.Nodes.Add(new LayoutField("EMail"));

            var f = new LayoutList
            {
                AllowCellSize = true,
                CheckView = false,
                EditMode = EditModes.ByClick,
                EditState = EditListState.Edit,
                Mode = LayoutListMode.Fields,
                Name = "fields",
                Text = "Attributes"
            };

            fields = new ListEditor(f);
            fields.Saving += OnFieldsSaving;

            groups = new AccessEditor()
            {
                Name = "Groups"
            };

            groupAttributes = new GroupBoxItem
            {
                Widget = fields,
                FillWidth = true,
                FillHeight = true,
                Name = "Parameters"
            };

            groupGroups = new GroupBoxItem()
            {
                Col = 2,
                Widget = groups,
                Width = 252,
                Name = "Groups"
            };

            groupMap = new GroupBox(groupAttributes, groupGroups);

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

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, "UserEditor", "User");
        }

        protected override void Dispose(bool disposing)
        {
            groupAttributes?.Dispose();
            groupGroups?.Dispose();
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

        public bool Closing()
        {
            return true;
        }

        public void Activating()
        {
            throw new NotImplementedException();
        }
    }
}
