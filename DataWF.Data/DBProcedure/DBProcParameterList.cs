/*
 ProcedureParameter.cs
 
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

namespace DataWF.Data
{
    public class DBProcParameterList : DBSchemaItemList<DBProcParameter>
    {
        private DBProcedure procedure;

        public DBProcParameterList(DBProcedure procedure)
        {
            this.Procedure = procedure;
        }

        public DBProcedure Procedure { get => procedure; set => procedure = value; }

        public override void Add(DBProcParameter item)
        {
            item.Procedure = Procedure;
            base.Add(item);
        }
    }
}
