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
    public class LogItemTypeGenerator : ItemTypeGenerator
    {
        public LogItemTypeAttribute LogAttribute
        {
            get => Attribute as LogItemTypeAttribute;
            set => Attribute = value;
        }

        public LogTableGenerator LogTableAttribute
        {
            get => TableAttribute as LogTableGenerator;
            set => TableAttribute = value;
        }

        public override DBTable Table { get => LogTableAttribute.Table; internal set => base.Table = value; }

        public override DBTable CreateTable()
        {
            return null;
        }

        public override DBTable Generate(DBSchema schema)
        {
            return null;
        }
    }
}