using DataWF.Common;
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
        private CompareType comparer = CompareType.Equal;
        private LogicType logic = LogicType.And;
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

        public LogicType Logic
        {
            get { return logic; }
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
            get { return name; }
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
                if (column == null)
                    column = List?.ListInfo?.Columns.GetItem(name) as LayoutColumn;
                return column;
            }
            set
            {
                Name = value?.Name;
                column = value;
            }
        }

        public string Header { get { return Column?.Text; } }

        public CompareType Comparer
        {
            get { return comparer; }
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
            get { return value; }
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
