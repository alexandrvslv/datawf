using System.ComponentModel;

namespace DataWF.Common
{
    public class ListPropertyChangedEventArgs : ListChangedEventArgs
    {
        private readonly string property;

        public ListPropertyChangedEventArgs(ListChangedType type, int newIndex, int oldIndex, string property = null)
            : base(type, newIndex, oldIndex)
        {
            this.property = property;
        }

        public string Property
        {
            get { return property; }
        }
    }
}
