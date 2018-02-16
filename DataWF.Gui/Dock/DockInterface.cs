using DataWF.Common;
using System.Collections.Generic;
using Xwt;

namespace DataWF.Gui
{
    public interface IDockContainer
    {
        IDockContainer DockParent { get; }

        IEnumerable<IDockContainer> GetDocks();

        bool Contains(Widget c);

        bool Delete(Widget control);

        DockPage Put(Widget control);

        DockPage Put(Widget control, DockType type);

        IEnumerable<Widget> GetControls();

        Widget Find(string name);
    }

    public interface IDocked
    {
        IDockContainer DockPanel { get; }
    }

    public interface IDockContent : ILocalizable, IGlyph
    {
        DockType DockType { get; }

        bool HideOnClose { get; }
    }

    public interface IDockMain : IDocked, ILocalizable
    {
        void SetStatus(StateInfo info);

        void SetStatusAdd(string info);

        void ShowProperty(object sender, object item, bool onTop);

        ProjectHandler CurrentProject { get; set; }

        void AddTask(object sender, TaskExecutor task);
    }

    public enum DockType
    {
        Content,
        Right,
        RightBottom,
        Top,
        Bottom,
        Left,
        LeftBottom
    }
}
