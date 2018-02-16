using DataWF.Common;
using System;
using System.Collections.Generic;

namespace DataWF.Gui
{
    public class LayoutColumnList : SelectableList<LayoutColumn>
    {
        public LayoutColumnList()
            : base()
        {
        }

        public LayoutColumn this[string property]
        {
            get
            {
                foreach (LayoutColumn col in this)
                    if (col.Name == property)
                        return col;
                return null;
            }
        }

        public bool Contains(string property)
        {
            return this[property] != null;
        }
    }
}

