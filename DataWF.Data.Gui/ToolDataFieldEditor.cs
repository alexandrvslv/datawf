using DataWF.Gui;

namespace DataWF.Data.Gui
{
    public class ToolDataFieldEditor : ToolContentItem
    {
        public int fieldWidth = 100;

        public ToolDataFieldEditor()
            : base(new DataFieldEditor())
        {
            Field.Text = string.Empty;
            Field.MinWidth = fieldWidth;
        }

        public DataFieldEditor Field
        {
            get { return base.Content as DataFieldEditor; }
        }

        public int FieldWidth
        {
            get { return fieldWidth; }
            set
            {
                if (fieldWidth != value)
                {
                    fieldWidth = value;
                }
            }
        }

    }
}
