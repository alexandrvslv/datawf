using System;

namespace DataWF.Collections.Query
{
    [Flags]
    public enum JoinOperatorType : byte
    {
        Undefined = 0,
        Inner = 1,
        Left = 2,
        Right = 4,
        Outer = 8,
        Join = 16
    }
}

