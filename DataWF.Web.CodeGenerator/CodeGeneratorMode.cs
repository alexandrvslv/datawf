using System;

namespace DataWF.Web.CodeGenerator
{
    [Flags]
    public enum CodeGeneratorMode
    {
        None = 0,
        Controllers = 1,
        Logs = 2
    }

}