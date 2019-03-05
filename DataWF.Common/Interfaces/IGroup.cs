using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IGroup : IComparable
    {
        bool Expand { get; set; }

        IGroup Group { get; set; }

        bool IsCompaund { get; }

        bool IsExpanded { get; }

        IEnumerable<IGroup> GetGroups();
    }
}

