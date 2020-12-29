using System;

namespace DataWF.Module.Flow
{
    [Flags]
    public enum StageKey
    {
        None = 0,
        Stop = 1,
        Start = 2,
        System = 4,
        Return = 8,
        AutoComplete = 16
    }
}
