using System;

namespace DataWF.Common
{
    public interface IAccessable
    {
        string AccessorName { get; }
        IAccessValue Access { get; set; }
    }
}
