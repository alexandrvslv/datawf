using System;

namespace DataWF.Gui
{
    public class LayoutSelectionRow : IComparable<LayoutSelectionRow>
    {
        public object Item;
        public int Index;
        public LayoutColumn Column;

        public LayoutSelectionRow()
        { }

        public LayoutSelectionRow(object item, int index, LayoutColumn column = null)
        {
            Item = item;
            Index = index;
            Column = column;
        }

        public int CompareTo(LayoutSelectionRow other)
        {
            return this.Index.CompareTo(other.Index);
        }

        public override bool Equals(object obj)
        {
            return obj is LayoutSelectionRow && Index.Equals(((LayoutSelectionRow)obj).Index);
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }
    }
}
