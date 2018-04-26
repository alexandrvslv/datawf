using DataWF.Common;
using System.Collections.Generic;
using Xwt;

namespace DataWF.Gui
{
    public interface ILayoutItem : IContainerNotifyPropertyChanged, INamed, IEnumerable<ILayoutItem>
    {
        Rectangle Bound { get; set; }

        double Height { get; set; }

        double Width { get; set; }

        int Row { get; set; }

        int Col { get; set; }

        bool Visible { get; set; }

        bool FillWidth { get; set; }

        bool FillHeight { get; set; }

        ILayoutItem Map { get; }

        double Scale { get; set; }

        double Indent { get; set; }

        void Sort();
    }
}

