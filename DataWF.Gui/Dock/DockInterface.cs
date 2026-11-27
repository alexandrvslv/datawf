using DataWF;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace DataWF.Gui
{
    public interface IDockContainer
    {
        IDockContainer DockParent { get; }

        IEnumerable<IDockContainer> GetDocks();

        bool Contains(IControl c);

        bool Delete(IControl control);

        DockPage Put(IControl control);

        DockPage Put(IControl control, DockType type);

        IEnumerable<IControl> GetControls();

        IControl Find(string name);
    }

    public interface IDocked
    {
        IDockContainer DockPanel { get; }
    }

    public interface IDockContent : ILocalizable, IGlyph
    {
        DockType DockType { get; }

        bool HideOnClose { get; }

        bool Closing();

        void Activating();
    }

    public interface IDockMain : IDocked, ILocalizable, ILayoutCacheProvider
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
