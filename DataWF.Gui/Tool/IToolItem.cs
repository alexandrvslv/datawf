
using Xwt;

namespace DataWF.Gui
{
    internal interface IToolItem
    {
        Canvas Bar { get; set; }

        Widget Content { get; set; }
    }
}