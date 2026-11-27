using Avalonia;
using Avalonia.Controls;

namespace DataWF.Gui
{
    public interface ILayoutEditor
    {
        ILayoutCellEditor CurrentEditor { get; set; }

        object Value { get; set; }

        Visual Control { get; set; }

        bool DropDownVisible { get; set; }

        bool DropDownExVisible { get; set; }

        bool DropDownAutoHide { get; set; }
    }
}
