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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DataWF.Data
{
    public class DBProcedureList : DBSchemaItemList<DBProcedure>
    {

        private readonly Dictionary<string, Dictionary<string, DBProcedure>> attributeIndex = new Dictionary<string, Dictionary<string, DBProcedure>>(StringComparer.OrdinalIgnoreCase);

        public DBProcedureList(DBSchema schema) : base(schema)
        {
            Indexes.Add(DBProcedure.GroupNameInvoker.Instance);
            Indexes.Add(DBProcedure.DataNameInvoker.Instance);
            Indexes.Add(DBProcedure.ProcedureTypeInvoker.Instance);
        }

        public IEnumerable<DBProcedure> SelectByFile(string fileName)
        {
            var query = new Query<DBProcedure>();
            query.Parameters.Add(new QueryParameter<DBProcedure>() { Invoker = DBProcedure.ProcedureTypeInvoker.Instance, Value = ProcedureTypes.Source });
            query.Parameters.Add(new QueryParameter<DBProcedure>() { Invoker = DBProcedure.DataNameInvoker.Instance, Value = fileName });
            return Select(query);
        }

        public IEnumerable<DBProcedure> SelectByParent(DBProcedure procedure)
        {
            return Select(DBProcedure.GroupNameInvoker.Instance, CompareType.Equal, procedure?.Name);
        }

        public IEnumerable<KeyValuePair<string, DBProcedure>> SelectByCategory(string category = "General")
        {
            if (attributeIndex.TryGetValue(category, out var categoryIndex))
            {
                foreach (var kvp in categoryIndex)
                {
                    yield return kvp;
                }
            }
        }

        public DBProcedure SelectByAttribute(string name, string category = "General")
        {
            if (attributeIndex.TryGetValue(category, out var categoryIndex))
                return categoryIndex.TryGetValue(name, out var procedure) ? procedure : null;
            return null;
        }

        public override void InsertInternal(int index, DBProcedure item)
        {
            base.InsertInternal(index, item);
            AddCodes(item);
        }

        public override DDLType GetInsertType(DBProcedure item)
        {
            return item.ProcedureType == ProcedureTypes.StoredFunction || item.ProcedureType == ProcedureTypes.StoredProcedure
                ? DDLType.Create
                : DDLType.Default;
        }

        public void AddCodes(DBProcedure item)
        {
            foreach (var attribute in item.Attributes)
            {
                if (!attributeIndex.TryGetValue(attribute.Category, out var categoryIndex))
                {
                    attributeIndex[attribute.Category] = categoryIndex = new Dictionary<string, DBProcedure>(StringComparer.OrdinalIgnoreCase);
                }
                categoryIndex[attribute.Name] = item;
            }
        }

        public DBProcedure AddOrUpdate(DBProcedure item)
        {
            var exist = this[item.Name];
            if (exist == null)
            {
                Add(item);
                return item;
            }

            exist.ProcedureType = item.ProcedureType;
            exist.Group = item.Group;
            exist.Source = item.Source;
            exist.Attributes = item.Attributes;
            exist.Parameters.Clear();
            exist.Parameters.AddRange(item.Parameters);
            AddCodes(exist);

            return exist;
        }

        public DBProcedure GenerateGroup(Assembly assembly)
        {
            var uri = new UriBuilder(assembly.CodeBase);
            var path = Uri.UnescapeDataString(uri.Path).Replace(".DLL", ".dll");
            var filename = Path.GetFileName(path);

            var gname = assembly.GetName().Name;
            var procedure = this[gname];
            if (procedure == null)
            {
                procedure = new DBProcedure
                {
                    Name = gname,
                    DataName = filename,
                    ProcedureType = ProcedureTypes.File
                };
                Add(procedure);
            }
            procedure.TempAssembly = assembly;
            return procedure;
        }

        public DBProcedure Generate(Assembly assembly)
        {
            var gprocedure = GenerateGroup(assembly);
            foreach (var type in assembly.ExportedTypes)
            {
                if (TypeHelper.IsInterface(type, typeof(IExecutable)))
                {
                    var name = type.FullName;
                    var procedure = new DBProcedure
                    {
                        Group = gprocedure,
                        Name = name,
                        DataName = gprocedure.DataName,
                        ProcedureType = ProcedureTypes.Assembly
                    };
                    procedure = AddOrUpdate(procedure);
                    procedure.DisplayName = type.Name;
                    procedure.TempAssembly = assembly;
                    procedure.Attributes.Clear();
                    procedure.Attributes.AddRange(type.GetCustomAttributes<ParameterAttribute>());
                    AddCodes(procedure);
                }
            }
            return gprocedure;
        }

        public void CheckDeleted()
        {
            foreach (var procedure in this.ToList())
            {
                if (procedure.ProcedureType == ProcedureTypes.Assembly
                    && procedure.TempAssembly == null)
                {
                    Remove(procedure);
                }
            }
        }
    }
}

