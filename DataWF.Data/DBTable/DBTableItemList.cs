﻿/*
 DBColumnList.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>  

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBTableItemList<T> : DBSchemaItemList<T> where T : DBTableItem, new()
    {

        public DBTableItemList(DBTable table) : base()
        {
            Table = table;
        }

        [XmlIgnore, JsonIgnore]
        public override DBSchema Schema
        {
            get { return base.Schema ?? Table?.Schema; }
            internal set { base.Schema = value; }
        }

        [XmlIgnore, JsonIgnore]
        public DBTable Table { get; set; }

        public override int AddInternal(T item)
        {
            if (Table == null)
            {
                throw new InvalidOperationException("Table property nead to be specified before add any item!");
            }
            if (item.Table == null)
            {
                item.Table = Table;
            }
            return base.AddInternal(item);
        }

        public override void Dispose()
        {
            Table = null;
            base.Dispose();
        }
    }
}
