using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IGroup : IComparable
    {
        bool IsExpanded { get; }

        IGroup Group { get; set; }

        bool Expand { get; set; }

        bool IsCompaund { get; }

        IEnumerable<IGroup> GetGroups();
    }
}

