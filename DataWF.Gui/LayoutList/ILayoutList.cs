using System.Collections;

namespace DataWF.Gui
{
    public interface ILayoutList
    {
        IList ListSource { get; }

        LayoutListInfo ListInfo { get; }

        object ReadValue(object litem, ILayoutCell column);

        object FormatValue(object litem, object val, ILayoutCell column);

        void RefreshGroupsBound();
    }
}
