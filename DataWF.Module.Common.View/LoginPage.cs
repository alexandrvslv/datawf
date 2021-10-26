//using Xwt.Drawing.Drawing2D;
using System;
using System.ComponentModel;
using System.Security;
using System.Text;
using DataWF.Common;
using DataWF.Gui;
using Xwt;

namespace DataWF.Data.CommonGui
{
    public class LoginPage : VPanel
    {
        private LayoutList listUser;
        private Button bLogin;
        private Button bCancel;

        public LoginPage()
        {
            listUser = new LayoutList()
            {
                EditMode = EditModes.ByClick,
                //GenerateColumns = false,
                //GenerateFields = false,
                //Mode = LayoutListMode.Fields,
                //FieldInfo = new LayoutFieldInfo(
                //    new LayoutField(nameof(User.Login)),
                //    new LayoutField(nameof(User.Password)))
            };

            bLogin = new Button { Name = "buttonLogin", Label = "Login" };
            bLogin.Clicked += BLoginClicked;

            bCancel = new Button { Name = "buttonCancel", Label = "Cancel" };
            bCancel.Clicked += BCancelClicked;

            PackStart(listUser, true, true);
            //PackStart(bLogin, false, false);

            User = new UserCredential();
            Name = nameof(LoginPage);
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, nameof(LoginPage), "Login");
        }

        private void BCancelClicked(object sender, EventArgs e)
        {
            if (ParentWindow is Dialog dialog)
            {
                dialog.Respond(Command.Cancel);
            }
        }

        private void BLoginClicked(object sender, System.EventArgs e)
        {
            if (ParentWindow is Dialog dialog)
            {
                dialog.Respond(Command.Ok);
            }
        }

        public UserCredential User
        {
            get { return listUser.FieldSource as UserCredential; }
            set { listUser.FieldSource = value; }
        }
    }

    public class UserCredential
    {

        public string Login { get; set; }

        [PasswordPropertyText(true)]
        public SecureString Password { get; set; }
    }
}

