/*
 BaseConfig.cs
 
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

        public SelectableList<CodeAttributeCache> Codes { get; private set; } = new SelectableList<CodeAttributeCache>();

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
            GenerateVirtualTables();

            Table.IsLoging = Attribute.IsLoging;

            return Table;
        }

        private void GenerateVirtualTables()
        {
            foreach (var itemType in cacheItemTypes)
            {
                Table.ItemTypes[itemType.Attribute.Id] = new DBItemType { Type = itemType.Type };
                itemType.Generate(Schema);
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
            Table.TableAttribute = this;
            Table.Group = TableGroup;
            Table.Type = Attribute.TableType;
            Table.BlockSize = Attribute.BlockSize;
            Table.Sequence = Table.GenerateSequence();
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
            cacheColumns.Indexes.Add(ColumnGeneratorColumnNameInvoker.Instance);
            cacheColumns.Indexes.Add(ColumnGeneratorPropertyNameInvoker.Instance);
            cacheReferences = new SelectableList<ReferenceGenerator>();
            cacheReferences.Indexes.Add(ReferenceGeneratorPropertyNameInvoker.Instance);
            cacheReferencings = new SelectableList<ReferencingGenerator>();
            cacheReferencings.Indexes.Add(ReferencingGeneratorPropertyNameInvoker.Instance);
            cacheIndexes = new SelectableList<IndexGenerator>();
            cacheIndexes.Indexes.Add(IndexGeneratorNameInvoker.Instance);
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
                InitializeCodes(property);
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
                InitializeCodes(method);
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

        public virtual void InitializeCodes(MemberInfo member)
        {
            var codeAttribuites = member.GetCustomAttributes<CodeAttribute>(false);
            foreach (var code in codeAttribuites)
            {
                if (!Codes.Any(p => p.Attribute.Code == code.Code && p.Attribute.Category == code.Category && p.Member == member))
                {
                    Codes.Add(new CodeAttributeCache(code, member));
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
