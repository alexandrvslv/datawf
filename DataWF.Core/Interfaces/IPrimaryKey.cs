using System;

namespace DataWF
{
    public interface IPrimaryKey
    {
        object PrimaryKey { get; set; }
    }

    public interface IPrimaryKey<K> : IPrimaryKey
    {
        new K PrimaryKey { get; set; }
    }

    public interface IStampKey
    {
        DateTime Stamp { get; set; }
    }

    public interface IDateKey
    {
        DateTime DateCreate { get; set; }
    }
}

