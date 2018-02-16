using DataWF.Module.Common;
using DataWF.Gui;
using DataWF.Common;
using Xwt.Drawing;

namespace DataWF.Module.FlowGui
{
    public class MenuItemUser : GlyphMenuItem
    {
        private User user;

        public MenuItemUser(User user)
        {
            this.user = user;
            Name = user.Id.ToString();
            Text = user.Name;
            Image = (Image)Locale.GetImage("FlowEnvir", "User");
        }

        public User User
        {
            get { return user; }
            set { user = value; }
        }

        public MenuItemStage OwnerStage { get { return Owner as MenuItemStage; } }
    }
}
