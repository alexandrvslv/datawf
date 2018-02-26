using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public interface ILayoutItem : IContainerNotifyPropertyChanged, INamed
    {
        Rectangle Bound { get; set; }

        double Height { get; set; }

        double Width { get; set; }

        int Row { get; set; }

        int Col { get; set; }

        bool Visible { get; set; }

        bool FillWidth { get; set; }

        bool FillHeight { get; set; }

        ILayoutMap Map { get; }
    }
}

