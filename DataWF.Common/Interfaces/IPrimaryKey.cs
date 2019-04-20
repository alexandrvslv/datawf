using System;

namespace DataWF.Common
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
        DateTime? Stamp { get; set; }
    }
}

