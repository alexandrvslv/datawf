
using System;

namespace DataWF;

[Flags]
public enum SynchronizedStatus
{
    New = 1,
    Edit = 2,
    Load = 4,
    Actual = 8,
}