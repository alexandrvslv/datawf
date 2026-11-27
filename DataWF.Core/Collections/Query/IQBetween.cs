namespace DataWF.Collections.Query
{

    public interface IQBetween : IQBinaryItem, IBetween
    {
        object IBetween.GetMax(object? context) => LeftItem.GetObjectValue(context);
        object IBetween.GetMin(object? context) => RightItem.GetObjectValue(context);
    }

    public interface IQBetween<V> : IQBetween, IBetween<V>
    { }
}

