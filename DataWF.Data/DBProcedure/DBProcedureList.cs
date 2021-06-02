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
            query.Parameters.Add(new QueryParameter<DBProcedure, ProcedureTypes>() { Invoker = DBProcedure.ProcedureTypeInvoker.Instance, Value = ProcedureTypes.Source });
            query.Parameters.Add(new QueryParameter<DBProcedure, string>() { Invoker = DBProcedure.DataNameInvoker.Instance, Value = fileName });
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
                : DDLType.None;
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
                    Generate(type, gprocedure);
                }
            }
            return gprocedure;
        }

        public DBProcedure Generate(Type type)
        {
            return Generate(type, GenerateGroup(type.Assembly));
        }

        public DBProcedure Generate(Type type, DBProcedure group)
        {
            var name = type.FullName;
            var procedure = new DBProcedure
            {
                Group = group,
                Name = name,
                DataName = group.DataName,
                ProcedureType = ProcedureTypes.Assembly
            };
            procedure = AddOrUpdate(procedure);
            procedure.DisplayName = type.Name.ToSepInitcap();
            procedure.TempAssembly = type.Assembly;

            procedure.Attributes.Clear();
            procedure.Attributes.AddRange(type.GetCustomAttributes<ParameterAttribute>());
            AddCodes(procedure);
            return procedure;
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

