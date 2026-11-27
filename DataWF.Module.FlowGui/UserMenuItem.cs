using DataWF.Module.Common;
using DataWF.Gui;
using DataWF;
using Avalonia.Controls;
using System;

namespace DataWF.Module.FlowGui
{
    public class UserMenuItem : ToolMenuItem
    {
        private User user;

        public UserMenuItem(User user, EventHandler click) : base(click)
        {
            User = user;
        }

        public User User
        {
            get { return user; }
            set
            {
                user = value;
                Name = user.Login;
                Text = user.Name;
                Glyph = GlyphType.User;
                Image = (Image)Locale.GetImage("FlowEnvir", "User");
            }
        }

        public StageMenuItem OwnerStage { get { return Owner as StageMenuItem; } }
    }
}
