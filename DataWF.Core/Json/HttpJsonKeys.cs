using System;

namespace DataWF
{
    [Flags]
    public enum HttpJsonKeys
    {
        None = 0,
        Refing = 1,
        Refed = 2,
        Ref = 3,
        Full = Refing | Refed | Ref
    }
}