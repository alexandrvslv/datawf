//using Xwt.Drawing.Drawing2D;
using System.Text;
using DataWF.Gui;
using DataWF.Common;
using Xwt;

namespace DataWF.Data.Gui
{
    public class LoginPage : VPanel
    {
        private LayoutList listUser;
        private Button buttonLogin;

        public LoginPage()
        {
            listUser = new LayoutList()
            {
                EditMode = EditModes.ByClick,
                GenerateFields = false,
                Mode = LayoutListMode.Fields,
                FieldInfo = new LayoutFieldInfo(
                    new LayoutField("Login"),
                    new LayoutField("Password"))
            };

            buttonLogin = new Button { Name = "buttonLogin", Label = "Login" };

            PackStart(listUser, true, true);
            PackStart(buttonLogin, false, false);
        }

        private static string GetString(byte[] data)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
                builder.Append(data[i].ToString("x2"));

            return builder.ToString();
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

