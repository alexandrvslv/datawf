using System;

namespace DataWF.Access;

public interface IAccessable
{
    string AccessorName { get; }
    IAccessValue Access { get; set; }
}
