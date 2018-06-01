using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
using Xwt.Drawing;
using System;
using DataWF.Module.Common;

namespace DataWF.Module.FlowGui
{
    public class StageMenuItem : ToolMenuItem
    {
        public static StageMenuItem Init(Stage stage, EventHandler clickHandler, bool iniUsers, bool checkCurrent)
        {
            var item = new StageMenuItem(stage, clickHandler);
            if (iniUsers)
            {
                item.DropDown = new Menubar { Name = item.Name };
                foreach (User user in stage.GetUsers())
                {
                    if (user.Status != DBStatus.Error && user.Status != DBStatus.Archive)
                    {
                        if (!checkCurrent || !user.IsCurrent)
                        {
                            if (item.DropDown.Items[user.Login] == null)
                            {
                                item.DropDown.Items.Add(new UserMenuItem(user, clickHandler));
                            }
                        }
                    }
                }
            }
            return item;
        }

        private Stage stage;

        public StageMenuItem(Stage stage, EventHandler click) : base(click)
        {
            Stage = stage;
        }

        public Stage Stage
        {
            get { return stage; }
            set
            {
                stage = value;
                Name = stage.Code;
                Text = stage.Name;
                Glyph = GlyphType.EditAlias;
                Image = (Image)Locale.GetImage("FlowEnvir", "Stage");
            }
        }
    }
}
