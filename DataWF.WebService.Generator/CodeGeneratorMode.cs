using System;

namespace DataWF.WebService.Generator
{
    [Flags]
    public enum CodeGeneratorMode
    {
        None = 0,
        Controllers = 1,
        Logs = 2,
        Invokers = 4
    }

}