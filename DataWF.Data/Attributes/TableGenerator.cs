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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace DataWF.Data
{
    public class TableGenerator
    {
        private DBSchema cacheSchema;
        private DBTable cacheTable;
        private DBTableGroup cacheGroup;
        protected SelectableList<ColumnGenerator> cacheColumns;
        protected SelectableList<ReferenceGenerator> cacheReferences;
        protected SelectableList<ReferencingGenerator> cacheReferencings;
        protected SelectableList<IndexGenerator> cacheIndexes;
        protected SelectableList<ItemTypeGenerator> cacheItemTypes;
        protected List<Type> cachedTypes = new List<Type>();
        protected ColumnGenerator cachePrimaryKey;
        protected ColumnGenerator cacheTypeKey;
        protected ColumnGenerator cacheFileKey;

        public TableAttribute Attribute { get; set; }

        public virtual DBSchema Schema
        {
            get { return cacheSchema; }
            set { cacheSchema = value; }
        }

        public DBTable Table
        {
            get { return cacheTable ?? (cacheTable = DBService.Schems.ParseTable(Attribute.TableName)); }
            internal set { cacheTable = value; }
        }

        public DBTableGroup TableGroup
        {
            get { return cacheGroup ?? (cacheGroup = Schema?.TableGroups[Attribute.GroupName]); }
            internal set { cacheGroup = value; }
        }

        public Type ItemType { get; internal set; }

        public IEnumerable<ColumnGenerator> Columns { get { return cacheColumns; } }

        public IEnumerable<ReferenceGenerator> References { get { return cacheReferences; } }

        public IEnumerable<ReferencingGenerator> Referencings { get { return cacheReferencings; } }

        public IEnumerable<Type> Types { get { return cachedTypes; } }

        public ColumnGenerator PrimaryKey
        {
            get
            {
                if (cachePrimaryKey == null)
                {
                    cachePrimaryKey = cacheColumns.FirstOrDefault(p => (p.Attribute.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary);
                }
                return cachePrimaryKey;
            }
        }

        public ColumnGenerator TypeKey
        {
            get
            {
                if (cacheTypeKey == null)
                {
                    cacheTypeKey = cacheColumns.FirstOrDefault(p => (p.Attribute.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType);
                }
                return cacheTypeKey;
            }
        }

        public ColumnGenerator FileKey
        {
            get
            {
                if (cacheFileKey == null)
                {
                    cacheFileKey = cacheColumns.FirstOrDefault(p => (p.Attribute.Keys & DBColumnKeys.File) == DBColumnKeys.File);
                }
                return cacheFileKey;
            }
        }

        public SelectableList<ParameterInvoker> Parameters { get; private set; } = new SelectableList<ParameterInvoker>();

        public virtual DBTable CreateTable()
        {
            Debug.WriteLine($"Generate {Attribute.TableName} - {this.ItemType.Name}");

            var type = typeof(DBTable<>).MakeGenericType(ItemType);
            // var logicType = ItemType.Assembly.ExportedTypes.FirstOrDefault(p => p.BaseType == type);
            var table = (DBTable)EmitInvoker.CreateObject(type);
            table.Name = Attribute.TableName;
            table.Schema = Schema;
            return table;
        }

        public DBTable Generate()
        {
            if (Schema == null)
                throw new InvalidOperationException("Can't generate as Schema not defined!");

            return Generate(Schema);
        }

        public virtual DBTable Generate(DBSchema schema)
        {
            GenerateBasic(schema);
            GenerateColumns();
            if (!Schema.Tables.Contains(Table.Name))
            {
                Schema.Tables.Add(Table);
            }
            GenerateReferences();
            Generateindexes();
            GenerateVirtualTables(schema);

            Table.IsLoging = Attribute.IsLoging;

            return Table;
        }

        private void GenerateVirtualTables(DBSchema schema)
        {
            foreach (var itemType in cacheItemTypes)
            {
                Table.ItemTypes[itemType.Attribute.Id] = new DBItemType { Type = itemType.Type };
                itemType.Generate(schema);
            }
        }

        private void Generateindexes()
        {
            foreach (var index in cacheIndexes)
            {
                index.Generate();
            }
        }

        private void GenerateReferences()
        {
            foreach (var reference in cacheReferences)
            {
                reference.CheckReference();
            }

            foreach (var reference in cacheReferences)
            {
                reference.Generate();
            }
        }

        private void GenerateBasic(DBSchema schema)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));

            if (TableGroup == null)
            {
                TableGroup = new DBTableGroup(Attribute.GroupName)
                {
                    Schema = Schema,
                    DisplayName = Attribute.GroupName
                };
                Schema.TableGroups.Add(TableGroup);
            }

            if (Table == null)
            {
                Table = CreateTable();
            }
            if (Table.DisplayName.Equals(Table.Name, StringComparison.Ordinal))
            {
                Table.DisplayName = ItemType.Name;
            }
            Table.Generator = this;
            Table.Group = TableGroup;
            Table.Type = Attribute.TableType;
            Table.BlockSize = Attribute.BlockSize;
            Table.Sequence = Table.GenerateSequence(Attribute.SequenceName);
        }

        public void GenerateColumns()
        {
            cacheColumns.Sort((a, b) =>
            {
                var aOrder = a.IsTypeKey ? -3 : a.IsPrimaryKey ? -2 : a.Attribute.Order;
                var bOrder = b.IsTypeKey ? -3 : b.IsPrimaryKey ? -2 : b.Attribute.Order;
                return aOrder.CompareTo(bOrder);
            });
            foreach (var column in cacheColumns)
            {
                column.Generate();
            }
        }

        public virtual void Initialize(Type type)
        {
            if (ItemType != null)
                return;
            cacheColumns = new SelectableList<ColumnGenerator>();
            cacheColumns.Indexes.Add(ColumnGenerator.ColumnNameInvoker.Instance);
            cacheColumns.Indexes.Add(ColumnGenerator.PropertyNameInvoker.Instance);
            cacheReferences = new SelectableList<ReferenceGenerator>();
            cacheReferences.Indexes.Add(ReferenceGenerator.PropertyNameInvoker.Instance);
            cacheReferencings = new SelectableList<ReferencingGenerator>();
            cacheReferencings.Indexes.Add(ReferencingGenerator.PropertyNameInvoker.Instance);
            cacheIndexes = new SelectableList<IndexGenerator>();
            cacheIndexes.Indexes.Add(IndexGenerator.IndexNameInvoker.Instance);
            cacheItemTypes = new SelectableList<ItemTypeGenerator>();

            ItemType = type;
            var types = TypeHelper.GetTypeHierarchi(type);
            foreach (var item in types)
            {
                InitializeType(item);
            }
        }

        public void InitializeItemType(ItemTypeGenerator itemType)
        {
            if (cacheItemTypes.Contains(itemType))
                return;
            cacheItemTypes.Add(itemType);
            var types = TypeHelper.GetTypeHierarchi(itemType.Type);
            foreach (var item in types)
            {
                InitializeType(item);
            }
        }

        public virtual void InitializeType(Type type)
        {
            var typeName = type.Name;
            if (cachedTypes.Contains(type))
                return;

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var property in properties)
            {
                var columns = InitializeColumn(property).ToArray();
                if (columns.Length > 0)
                {
                    foreach (var column in columns)
                    {
                        var exist = GetColumnByProperty(column.PropertyName);
                        if (exist != null)
                        {
                            cacheColumns.Remove(exist);
                        }
                        if ((column.Attribute.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType)
                            cacheColumns.Insert(0, column);
                        else if ((column.Attribute.Keys & DBColumnKeys.Primary) == DBColumnKeys.Primary)
                            cacheColumns.Insert(1, column);
                        else
                            cacheColumns.Add(column);
                    }
                    if (InitializeIndex(property, columns, out var index))
                    {
                        cacheIndexes.Add(index);
                    }
                }
                else if (InitializeSeparateIndex(property, out var separateIndex))
                {
                    cacheIndexes.Add(separateIndex);
                }
                InitializeDefault(property);
                InitializeParameters(property);
            }
            foreach (var property in properties)
            {
                if (InitializeReference(property, out var reference))
                {
                    cacheReferences.Add(reference);
                }
                else if (InitializeReferencing(property, out var referencing))
                {
                    cacheReferencings.Add(referencing);
                }
            }
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                InitializeParameters(method);
            }
            cachedTypes.Add(type);
        }

        private bool InitializeSeparateIndex(PropertyInfo property, out IndexGenerator index)
        {
            var indexAttribute = property.GetCustomAttribute<IndexAttribute>(false);
            if (indexAttribute != null)
            {
                index = cacheIndexes.SelectOne(nameof(IndexGenerator.IndexName), indexAttribute.IndexName)
                    ?? new IndexGenerator { Attribute = indexAttribute };
                index.Table = this;
                var columnAttribute = GetColumnByProperty(property.Name);
                index.Columns.Add(columnAttribute);
                return true;
            }
            index = null;
            return false;
        }


        private bool InitializeIndex(PropertyInfo property, IEnumerable<ColumnGenerator> columns, out IndexGenerator index)
        {
            var indexAttribute = property.GetCustomAttribute<IndexAttribute>(false);
            if (indexAttribute != null)
            {
                index = cacheIndexes.SelectOne(nameof(IndexGenerator.IndexName), indexAttribute.IndexName)
                    ?? new IndexGenerator { Attribute = indexAttribute };
                index.Table = this;
                index.Columns.AddRange(columns);
                return true;
            }
            index = null;
            return false;
        }

        public virtual bool InitializeReference(PropertyInfo property, out ReferenceGenerator reference)
        {
            var referenceAttrubute = property.GetCustomAttribute<ReferenceAttribute>(false);
            if (referenceAttrubute is LogReferenceAttribute logReferenceAttrubute)
            {
                reference = new LogReferenceGenerator(this, property, logReferenceAttrubute);
                return true;
            }
            else if (referenceAttrubute != null)
            {
                reference = new ReferenceGenerator(this, property, referenceAttrubute);
                return true;
            }
            reference = null;
            return false;
        }

        public virtual bool InitializeReferencing(PropertyInfo property, out ReferencingGenerator referencing)
        {
            var referencingAttribuite = property.GetCustomAttribute<ReferencingAttribute>(false);
            if (referencingAttribuite != null)
            {
                referencing = new ReferencingGenerator(this, property, referencingAttribuite);
                return true;
            }
            referencing = null;
            return false;
        }

        public virtual void InitializeParameters(MemberInfo member)
        {
            var parameters = member.GetCustomAttributes<ParameterAttribute>(false);
            foreach (var parameter in parameters)
            {
                if (!Parameters.Any(p => string.Equals(p.Parameter.Name, parameter.Name, StringComparison.Ordinal)
                                      && string.Equals(p.Parameter.Category, parameter.Category, StringComparison.Ordinal)
                                      && p.Member == member))
                {
                    Parameters.Add(new ParameterInvoker(parameter, member));
                }
            }
        }

        public virtual IEnumerable<ColumnGenerator> InitializeColumn(PropertyInfo property)
        {
            var columnAttribute = property.GetCustomAttribute<ColumnAttribute>(false);
            if (columnAttribute != null)
            {
                if (columnAttribute.DataType == null)
                    columnAttribute.DataType = property.PropertyType;
                if (columnAttribute.Order <= 0)
                    columnAttribute.Order = cacheColumns.Count;

                if ((columnAttribute.Keys & DBColumnKeys.Culture) == DBColumnKeys.Culture)
                {
                    foreach (var culture in Locale.Instance.Cultures)
                    {
                        if (columnAttribute is LogColumnAttribute logColumnAttribute)
                            yield return new LogColumnGenerator(this as LogTableGenerator, property, logColumnAttribute, culture);
                        else
                            yield return new ColumnGenerator(this, property, columnAttribute, culture);
                    }

                }
                else
                {
                    if (columnAttribute is LogColumnAttribute logColumnAttribute)
                        yield return new LogColumnGenerator(this as LogTableGenerator, property, logColumnAttribute);
                    else
                        yield return new ColumnGenerator(this, property, columnAttribute);
                }
            }
        }


        public virtual void InitializeDefault(PropertyInfo property)
        {
            var defaultAttribute = property.GetCustomAttribute<DefaultValueAttribute>(false);
            if (defaultAttribute != null)
            {
                var columnAttribute = GetColumnByProperty(property.Name);
                if (columnAttribute != null)
                {
                    if (columnAttribute.DefaultValues == null)
                        columnAttribute.DefaultValues = new Dictionary<Type, string>();
                    columnAttribute.DefaultValues[property.DeclaringType] = defaultAttribute.Value.ToString();
                }
            }
        }

        public ColumnGenerator GetColumn(string name)
        {
            return cacheColumns.SelectOne(nameof(ColumnGenerator.ColumnName), name);
        }

        public ColumnGenerator GetColumnByProperty(string property)
        {
            return cacheColumns.SelectOne(nameof(ColumnGenerator.PropertyName), property);
        }

        public ReferenceGenerator GetReferenceByProperty(string property)
        {
            return cacheReferences.SelectOne(nameof(ReferenceGenerator.PropertyName), property);
        }

        public ReferencingGenerator GetReferencingByProperty(string property)
        {
            return cacheReferencings.SelectOne(nameof(ReferencingGenerator.PropertyName), property);
        }
    }
}
