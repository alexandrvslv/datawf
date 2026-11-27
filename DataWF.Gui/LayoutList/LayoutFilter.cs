using DataWF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace DataWF.Gui
{
    public class LayoutFilter : DefaultItem
    {
        private string name;
        [DefaultValue(false)]
        private CompareOperator comparer = CompareOperator.Equal;
        private CompareOperator logic = CompareOperator.And;
        [NonSerialized]
        private LayoutColumn column;
        private object value;

        public LayoutFilter()
        { }

        public LayoutFilter(string name)
        {
            Name = name;
        }

        public LayoutFilter(LayoutColumn column)
        {
            Column = column;
        }

        [XmlIgnore]
        public LayoutList List
        {
            get { return ((LayoutFilterList)Containers.FirstOrDefault())?.List; }
        }

        public CompareOperator Logic
        {
            get => logic;
            set
            {
                if (!logic.Equals(value))
                {
                    logic = value;
                    OnPropertyChanged(nameof(Logic));
                }
            }
        }

        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [XmlIgnore]
        public LayoutColumn Column
        {
            get
            {
                column ??= List?.ListInfo?.Columns.GetItem(name) as LayoutColumn;
                return column;
            }
            set
            {
                Name = value?.Name;
                column = value;
            }
        }

        public string Header { get { return Column?.Text; } }

        public CompareOperator Comparer
        {
            get => comparer;
            set
            {
                if (!comparer.Equals(value))
                {
                    comparer = value;
                    OnPropertyChanged(nameof(Comparer));
                }
            }
        }

        public object Value
        {
            get => value;
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }
    }
}
