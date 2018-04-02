using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
using Xwt.Drawing;

namespace DataWF.Module.FlowGui
{
    public class MenuItemStage : ToolMenuItem
    {
        private Stage stage;

        public MenuItemStage(Stage stage)
        {
            Stage = stage;
            Name = stage.Code;
            Text = stage.Name;
            Image = (Image)Locale.GetImage("FlowEnvir", "Stage");
        }

        public Stage Stage
        {
            get { return stage; }
            set
            {
                stage = value;
            }
        }
    }
}
