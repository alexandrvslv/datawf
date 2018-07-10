using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;

namespace DataWF.Gui
{
    public class LayoutSort : IContainerNotifyPropertyChanged, IComparable, IComparable<LayoutSort>
    {
        private int order;
        private string name;
        private ListSortDirection direction = ListSortDirection.Ascending;
        private bool group;
        private LayoutColumn column;

        public LayoutSort()
            : this(null, ListSortDirection.Ascending, false)
        {
        }

        public LayoutSort(string name, ListSortDirection direction = ListSortDirection.Ascending, bool group = false)
        {
            this.name = name;
            this.direction = direction;
            this.group = group;
        }

        [XmlIgnore, Browsable(false)]
        public INotifyListPropertyChanged Container { get; set; }

        [XmlIgnore, Browsable(false)]
        public LayoutListInfo Info
        {
            get { return ((LayoutSortList)Container)?.Info; }
        }

        public int Order
        {
            get { return order; }
            set
            {
                if (order != value)
                {
                    order = value;
                    OnPropertyChanged(nameof(Order));
                }
            }
        }

        public string ColumnName
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(ColumnName));
                }
            }
        }

        [DefaultValue(ListSortDirection.Ascending)]
        public ListSortDirection Direction
        {
            get { return direction; }
            set
            {
                if (direction != value)
                {
                    direction = value;
                    OnPropertyChanged(nameof(Direction));
                }
            }
        }

        [DefaultValue(false)]
        public bool IsGroup
        {
            get { return group; }
            set
            {
                if (group != value)
                {
                    group = value;
                    OnPropertyChanged(nameof(IsGroup));
                }
            }
        }

        [XmlIgnore]
        public LayoutColumn Column
        {
            get
            {
                if (column == null)
                    column = Info?.Columns[name] as LayoutColumn;
                return column;
            }
            set
            {
                name = value?.Name;
                column = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            var args = new PropertyChangedEventArgs(property);
            PropertyChanged?.Invoke(this, args);
            Container?.OnPropertyChanged(this, args);
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as LayoutSort);
        }

        public int CompareTo(LayoutSort other)
        {
            int rez = other.group.CompareTo(this.group);
            if (rez == 0)
                rez = order.CompareTo(other.order);
            return rez;
        }
    }
}
