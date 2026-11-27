namespace DataWF.Collections.Query;

public interface IQBinaryItem : IQItem, IQItemList
{
    IQItem LeftItem { get; set; }
    IQItem RightItem { get; set; }
}

public interface IQBinaryItem<T> : IQBinaryItem, IQItemList<T> where T : IQItem
{
    new T LeftItem { get; set; }
    new T RightItem { get; set; }

    IQItem IQBinaryItem.LeftItem
    {
        get => LeftItem;
        set => LeftItem = (T)value;
    }

    IQItem IQBinaryItem.RightItem
    {
        get => RightItem;
        set => RightItem = (T)value;
    }
}

