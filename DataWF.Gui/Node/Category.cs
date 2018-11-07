using System;

namespace DataWF.Gui
{
    public class Category : IComparable
    {
        public Category()
        {
        }

        public Category(string code)
        {
            Name = code;
            Header = code;
        }

        public override string ToString()
        {
            return Header;
        }

        public bool Expand { get; set; } = true;

        public string Header { get; set; }

        public string Name { get; set; }

        public int Order { get; set; }

        #region IComparable implementation
        public int CompareTo(object obj)
        {
            return CompareTo((Category)obj);
        }

        public int CompareTo(Category obj)
        {
            return Order.CompareTo(obj.Order);
        }
        #endregion
    }
}

