using System.ComponentModel;

namespace DataWF.Common
{
    public class QueryOrder : QueryItem
    {
        private ListSortDirection direction;

        public ListSortDirection Direction
        {
            get { return this.direction; }
            set { direction = value; }
        }
    }

}

