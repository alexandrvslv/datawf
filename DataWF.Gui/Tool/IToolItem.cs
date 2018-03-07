
using Xwt;

namespace DataWF.Gui
{
    internal interface IToolItem
    {
		Toolsbar Bar { get; set; }

        Widget Content { get; set; }
    }
}