namespace DataWF.Collections;

public class ArrayPointerSequenceEntry<T>
{
    public static readonly ArrayPointerSequenceEntry<T> Empty = new(null!);

    public ArrayPointerSequenceEntry()
    { }

    public ArrayPointerSequenceEntry(ArrayPointerList<T> items)
    {
        Items = items;
    }

    public ArrayPointerList<T> Items { get; private set; }
    public ArrayPointerSequenceEntry<T> Next { get; private set; }

    public ArrayPointerSequenceEntry<T> Append(ArrayPointerList<T> items)
    {
        return Items == null
            ? SetItems(items)
            : Next = new ArrayPointerSequenceEntry<T>(items);
    }

    private ArrayPointerSequenceEntry<T> SetItems(ArrayPointerList<T> items)
    {
        Items = items;
        return this;
    }
}

