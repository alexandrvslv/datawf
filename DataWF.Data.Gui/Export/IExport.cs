using DataWF.Gui;

namespace DataWF.Data.Gui
{
    public interface IExport
    {
        void Export(string filename, LayoutList list);
    }
}
