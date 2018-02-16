namespace DataWF.Gui
{
    public interface ILayoutMap : ILayoutItem
    {
        LayoutItems Items { get; }

        bool Contains(ILayoutItem item);

        void Sort();

        double Scale { get; set; }
        double Indent { get; set; }
    }
}

