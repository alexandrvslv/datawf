using DataWF.Common;
using System.Collections.Generic;

namespace DataWF.Gui
{
    public interface ILayoutItem : IEntryNotifyPropertyChanged, INamed, IEnumerable<ILayoutItem>
    {
        //Rectangle Bound { get; set; }

        double Height { get; set; }

        double Width { get; set; }

        int Row { get; set; }

        int Column { get; set; }

        bool Visible { get; set; }

        bool FillHeight { get; set; }

        bool FillWidth { get; set; }

        ILayoutItem Map { get; }

        double Scale { get; set; }

        double Indent { get; set; }

        void Sort();
    }
}

