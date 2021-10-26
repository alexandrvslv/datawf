namespace DataWF.Gui
{
    public interface ILayoutEditor
    {
        ILayoutCellEditor CurrentEditor { get; set; }

        object Value { get; set; }

        Xwt.Widget Widget { get; set; }

        bool DropDownVisible { get; set; }

        bool DropDownExVisible { get; set; }

        bool DropDownAutoHide { get; set; }
    }
}
