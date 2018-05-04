using DataWF.Common;

namespace DataWF.Gui
{
    public interface ILayoutCell
    {
        ILayoutCell Owner { get; set; }

        ILayoutCellEditor GetEditor(object source);

        ILayoutCellEditor CellEditor { get; set; }

        CellStyle Style { get; set; }

        string Name { get; set; }

        string Text { get; set; }

        string Format { get; set; }

        IInvoker Invoker { get; set; }

        bool ReadOnly { get; set; }

        bool Editable { get; set; }

        bool Visible { get; set; }

        bool Validate { get; set; }

        bool Password { get; set; }

        string Description { get; set; }

        object ReadValue(object listItem);

        void WriteValue(object listItem, object value);
    }
}
