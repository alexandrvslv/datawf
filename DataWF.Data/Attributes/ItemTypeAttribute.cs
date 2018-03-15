/*
 Account.cs
 
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

namespace DataWF.Data
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ItemTypeAttribute : Attribute
    {
        public ItemTypeAttribute(int id)
        {
            Id = id;
        }

        public int Id { get; private set; }

        public Type Type { get; private set; }

        public TableAttribute Table { get; private set; }

        public void Initialize(Type type)
        {
            Type = type;
            do
            {
                type = type.BaseType;
                Table = type == null ? null : DBService.GetTableAttribute(type);
            }
            while (Table == null && type != null);
            if (Table == null)
            {
                throw new Exception($"Class with {nameof(ItemTypeAttribute)} must have are {nameof(Type.BaseType)} with {nameof(TableAttribute)}!");
            }
            Table.InitializeItemType(this);
        }
    }
}