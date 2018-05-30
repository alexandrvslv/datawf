using Xwt;

namespace DataWF.Gui
{
    public class ToolComboBox : ToolContentItem
    {
        public ToolComboBox() : base(new ComboBox())
        { }

        public ComboBox ComboBox
        {
            get { return base.Content as ComboBox; }
        }
    }
}
