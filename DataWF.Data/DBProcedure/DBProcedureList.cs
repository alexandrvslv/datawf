/*
 ProcedureList.cs
 
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
using DataWF.Common;
using System.ComponentModel;
using System.Collections.Generic;
using System;

namespace DataWF.Data
{
    public class DBProcedureList : DBSchemaItemList<DBProcedure>
    {
        static readonly Invoker<DBProcedure, string> parentNameInvoker = new Invoker<DBProcedure, string>(nameof(DBProcedure.ParentName), (item) => item.ParentName);
        static readonly Invoker<DBProcedure, string> dataNameInvoker = new Invoker<DBProcedure, string>(nameof(DBProcedure.DataName), (item) => item.DataName);
        static readonly Invoker<DBProcedure, ProcedureTypes> typeInvoker = new Invoker<DBProcedure, ProcedureTypes>(nameof(DBProcedure.ProcedureType), (item) => item.ProcedureType);

        private Dictionary<string, DBProcedure> codeIndex = new Dictionary<string, DBProcedure>(StringComparer.OrdinalIgnoreCase);

        public DBProcedureList(DBSchema schema) : base(schema)
        {
            Indexes.Add(parentNameInvoker);
            Indexes.Add(dataNameInvoker);
            Indexes.Add(typeInvoker);
        }

        public IEnumerable<DBProcedure> SelectByFile(string fileName)
        {
            var query = new Query();
            query.Parameters.Add(new QueryParameter() { Property = nameof(DBProcedure.ProcedureType), Value = ProcedureTypes.Source });
            query.Parameters.Add(new QueryParameter() { Property = nameof(DBProcedure.DataName), Value = fileName });
            return Select(query);
        }

        public IEnumerable<DBProcedure> SelectByParent(DBProcedure procedure)
        {
            return Select(nameof(DBProcedure.ParentName), CompareType.Equal, procedure?.Name);
        }

        public DBProcedure SelectByCode(string code)
        {
            return codeIndex.TryGetValue(code, out var procedure) ? procedure : null;
        }

        public override void InsertInternal(int index, DBProcedure item)
        {
            base.InsertInternal(index, item);
            foreach (var code in item.Codes)
            {
                codeIndex[code] = item;
            }
        }
    }
}

