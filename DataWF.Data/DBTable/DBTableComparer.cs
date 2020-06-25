/*
 DBTableList.cs
 
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
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Data
{
    public class DBTableComparer : IComparer<DBTable>
    {
        public static readonly DBTableComparer Instance = new DBTableComparer();

        public int Compare(DBTable a, DBTable b)
        {
            return Compare(a, b, false);
        }

        public int Compare(DBTable a, DBTable b, bool referenceCheck)
        {
            var rez = 0;
            if (a is IDBLogTable)
            {
                if (!(b is IDBLogTable))
                {
                    rez = 1;
                }
            }
            else if (b is IDBLogTable)
            {
                rez = -1;
            }
            if (rez == 0)
            {
                if (a is IDBVirtualTable)
                {
                    if (!(b is IDBVirtualTable))
                    {
                        rez = 1;
                    }
                }
                else if (b is IDBVirtualTable)
                {
                    rez = -1;
                }
            }
            if (rez == 0)
            {
                if (referenceCheck)
                {
                    var referenceA = a.Columns.GetIsReference().Select(p => p.ReferenceTableName);
                    var referenceB = b.Columns.GetIsReference().Select(p => p.ReferenceTableName);

                    if (referenceA.Contains(b.FullName))
                    {
                        rez = 1;
                    }
                    else if (referenceB.Contains(a.FullName))
                    {
                        rez = -1;
                    }
                }
                if (rez == 0)
                {
                    rez = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                }
            }

            return rez;
        }
    }
}
