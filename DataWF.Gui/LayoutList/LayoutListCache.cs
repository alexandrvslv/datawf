namespace DataWF.Gui
{

    public interface ILayoutCacheProvider
    {
        LayoutListCache Cache { get; }
    }

    public class LayoutListCache
    {
        public LayoutListCacheField Fields { get; set; } = new LayoutListCacheField();

        public LayoutListCacheList Lists { get; set; } = new LayoutListCacheList();
    }

}

