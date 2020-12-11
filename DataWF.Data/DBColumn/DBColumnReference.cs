//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using DataWF.Common;
using DataWF.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

[assembly: Invoker(typeof(DBColumnReference), nameof(DBColumnReference.ColumnName), typeof(DBColumnReference.ColumnNameInvoker))]
namespace DataWF.Data
{
    public class DBColumnReference : IEntryNotifyPropertyChanged
    {
        private string columnName;
        private DBColumn column;

        [XmlIgnore, JsonIgnore]
        public DBSchema Schema { get { return List?.Schema; } }

        [Browsable(false)]
        public string ColumnName
        {
            get { return columnName; }
            set
            {
                if (columnName != value)
                {
                    columnName = value;
                    column = null;
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public DBColumn Column
        {
            get { return column ?? DBService.Schems.ParseColumn(columnName, Schema); }
            set
            {
                if (Column != value)
                {
                    ColumnName = value?.FullName;
                    column = value;
                }
            }
        }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers<INotifyListPropertyChanged>(PropertyChanged);

        [Browsable(false), XmlIgnore, JsonIgnore]
        public DBColumnReferenceList List
        {
            get { return Containers.FirstOrDefault() as DBColumnReferenceList; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public override string ToString()
        {
            return Column?.ToString() ?? columnName;
        }

        public DBColumnReference Clone()
        {
            return new DBColumnReference { ColumnName = ColumnName };
        }

        public class ColumnNameInvoker : Invoker<DBColumnReference, string>
        {
            public static readonly ColumnNameInvoker Instance = new ColumnNameInvoker();
            public override string Name => nameof(DBColumnReference.ColumnName);

            public override bool CanWrite => true;

            public override string GetValue(DBColumnReference target) => target.ColumnName;

            public override void SetValue(DBColumnReference target, string value) => target.ColumnName = value;
        }
    }
}
