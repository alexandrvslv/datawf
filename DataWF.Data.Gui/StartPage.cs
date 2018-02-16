using System;
using Xwt.Drawing;
//using Xwt.Drawing.Drawing2D;
using System.Text;
using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using Xwt;

namespace DataWF.Data.Gui
{
    public class StartPage : VPanel, IDockContent
    {
        private Label label = new Label();
        private VPanel panel = new VPanel();
        private GroupBox box1 = new GroupBox();
        private GroupBox box2 = new GroupBox();
        private GroupBoxItem boxLogin = new GroupBoxItem();
        private GroupBoxItem boxRecent = new GroupBoxItem();
        private GroupBoxItem boxLinks = new GroupBoxItem();
        private LayoutList listRecent = new LayoutList();
        private LayoutList listLinks = new LayoutList();
        private LayoutList listUser = new LayoutList();
        private Button buttonLogin = new Button();

        public StartPage()
        {
            listUser.Mode = LayoutListMode.Fields;
            listUser.EditMode = EditModes.ByClick;
            listUser.GenerateFields = false;
            listUser.FieldInfo = new LayoutFieldInfo();
            listUser.FieldInfo.Nodes.Add(new LayoutField("Login"));
            listUser.FieldInfo.Nodes.Add(new LayoutField("Password"));
            // 
            this.buttonLogin.Name = "buttonLogin";
            // 
            // listRecent
            // 
            listRecent.EditState = EditListState.Edit;
            listRecent.Name = "listRecent";
            listRecent.CellMouseClick += ListRecentCellMouseClick;
            // 
            // listLinks
            // 
            listLinks.Mode = LayoutListMode.List;
            listLinks.Name = "listLinks";
            // 
            // box1
            // 
            this.box1.Col = 0;
            this.box1.Row = 0;
            this.box1.Add(boxLinks);
            this.box1.Name = "box1";
            // 
            // box2
            // 
            this.box2.Col = 0;
            this.box2.Row = 0;
            this.box2.Add(boxLogin);
            this.box2.Add(boxRecent);
            this.box2.Name = "box2";
            // 
            // boxLogin
            // 
            this.boxLogin.Col = 0;
            this.boxLogin.Row = 0;
            this.boxLogin.Widget = panel;
            this.boxLogin.DefaultHeight = 210;
            this.boxLogin.Name = "boxLogin";
            this.boxLogin.Width = 370;
            // 
            // boxRecent
            // 
            this.boxRecent.Col = 0;
            this.boxRecent.Widget = this.listRecent;
            this.boxRecent.DefaultHeight = 421;
            this.boxRecent.FillWidth = false;
            this.boxRecent.Name = "boxRecent";
            this.boxRecent.Row = 1;
            this.boxRecent.Width = 367;
            this.boxRecent.Height = 40;
            // 
            // boxLinks
            // 
            this.boxLinks.Col = 1;
            this.boxLinks.Widget = this.listLinks;
            this.boxLinks.DefaultHeight = 571;
            this.boxLinks.FillWidth = true;
            this.boxLinks.Name = "boxLinks";
            this.boxLinks.Row = 0;
            this.boxLinks.Width = 692;
            this.boxLinks.Height = 205;
            // 
            // label
            // 
            this.label.Name = "label";
            this.label.Text = "Document work office && database solution";

            this.PackStart(label, false, false);
            this.PackStart(box1, true, true);

            buttonLogin.Label = "Login";


            //panel.Controls.Add(this.buttonLogin);

            this.Name = "StartPage";

            listRecent.ListSource = GuiEnvironment.ProjectsInfo;
            listRecent.ListInfo.HotSelection = true;
            listLinks.ListSource = GuiEnvironment.WebLinks;
            listLinks.ListInfo.HotSelection = true;

            Localize();
        }

        #region IDockModule implementation
        public DockType DockType
        {
            get { return DockType.Content; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        #endregion

        public void Localize()
        {
            GuiService.Localize(label, "StartPage", "Welcome to databese and document managment");
            GuiService.Localize(boxLogin, "StartPage", "User identify");
            GuiService.Localize(boxRecent, "StartPage", "Recent Projects");
            GuiService.Localize(boxLinks, "StartPage", "Links");
            GuiService.Localize(this, "StartPage", "Welcome");
        }

        private void ListRecentCellMouseClick(object sender, LayoutHitTestEventArgs e)
        {
            GuiService.Main.CurrentProject = listRecent.SelectedItem as ProjectHandler;
        }

        private static string GetString(byte[] data)
        {
            StringBuilder s = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
                s.Append(data[i].ToString("x2"));

            return s.ToString();
        }

        private static string GetSha(string input)
        {
            if (input == null)
                return null;

            return GetString(System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.Default.GetBytes(input)));
        }

        private static string GetMd5(string input)
        {
            if (input == null)
                return null;

            return GetString(System.Security.Cryptography.MD5.Create().ComputeHash(Encoding.Default.GetBytes(input)));
        }


    }
}

