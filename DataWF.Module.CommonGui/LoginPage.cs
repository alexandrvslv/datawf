//using Xwt.Drawing.Drawing2D;
using System.ComponentModel;
using System.Security;
using System.Text;
using DataWF.Common;
using DataWF.Gui;
using DataWF.Module.Common;
using Xwt;

namespace DataWF.Data.CommonGui
{
	public class LoginPage : VPanel
	{
		private LayoutList listUser;
		private Button bLogin;

		public LoginPage()
		{
			listUser = new LayoutList()
			{
				EditMode = EditModes.ByClick,
				GenerateFields = false,
				Mode = LayoutListMode.Fields,
				FieldInfo = new LayoutFieldInfo(
					new LayoutField(nameof(User.Login)),
					new LayoutField(nameof(User.Password)))
			};

			bLogin = new Button { Name = "buttonLogin", Label = "Login" };

			PackStart(listUser, true, true);
			PackStart(bLogin, false, false);

			User = new UserCredential();
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

