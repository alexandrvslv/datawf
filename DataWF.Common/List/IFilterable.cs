using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataWF.Common
{
    public interface IFilterable : IList
    {
        Query FilterQuery { get; }
        void UpdateFilter();
    }
}
