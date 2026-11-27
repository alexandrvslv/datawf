using System;
using System.Collections.Generic;
using DataWF.Invokers;

namespace DataWF;

[Invoker]
public interface IGroup : IComparable
{
    bool Expand { get; set; }

    IGroup? Group { get; set; }

    bool IsCompound { get; }

    bool IsExpanded { get; }

    IEnumerable<IGroup> GetGroups();
}

