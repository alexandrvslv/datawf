using Avalonia;
using Avalonia.Controls;

namespace DataWF.Gui
{
    public class ToolProgressBar : ToolContentItem
    {
        public ToolProgressBar() : base(new ProgressBar() { MinHeight = 15 })
        {
            Visible = false;
            DisplayStyle = ToolItemDisplayStyle.Content;
            ProgressBar.IsIndeterminate = true;
        }

        public ProgressBar ProgressBar
        {
            get { return (ProgressBar)base.Content; }
        }

        public int Value
        {
            get => (int)ProgressBar.Value * 100;
            set => ProgressBar.Value = value / 100D;
        }

        public override void Localize()
        { }

    }
}
