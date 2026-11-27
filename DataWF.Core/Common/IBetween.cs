namespace DataWF
{
    public interface IBetween
    {
        object GetMax(object? context = null);
        object GetMin(object? context = null);
    }

    public interface IBetween<T> : IBetween
    {
        T Max { get; }
        T Min { get; }
    }
}

