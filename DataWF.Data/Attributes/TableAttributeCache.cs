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
    public class TableAttributeCache
    {
        static readonly Invoker<ColumnAttributeCache, string> columnNameInvoker = new Invoker<ColumnAttributeCache, string>(nameof(ColumnAttributeCache.ColumnName), (item) => item.ColumnName);
        static readonly Invoker<ColumnAttributeCache, string> columnPropertyInvoker = new Invoker<ColumnAttributeCache, string>(nameof(ColumnAttributeCache.PropertyName), (item) => item.PropertyName);
        static readonly Invoker<ReferenceAttributeCache, string> referencePropertyInvoker = new Invoker<ReferenceAttributeCache, string>(nameof(ReferenceAttributeCache.PropertyName), (item) => item.PropertyName);
        static readonly Invoker<ReferencingAttributeCache, string> referencingPropertyInvoker = new Invoker<ReferencingAttributeCache, string>(nameof(ReferencingAttributeCache.PropertyName), (item) => item.PropertyName);
        static readonly Invoker<IndexAttributeCache, string> IndexNameinvoker = new Invoker<IndexAttributeCache, string>(nameof(IndexAttributeCache.IndexName), (item) => item.IndexName);

        private DBSchema cacheSchema;
        private DBTable cacheTable;
        private DBTableGroup cacheGroup;
        private SelectableList<ColumnAttributeCache> cacheColumns;
        private SelectableList<ReferenceAttributeCache> cacheReferences;
        private SelectableList<ReferencingAttributeCache> cacheReferencings;
        private SelectableList<IndexAttributeCache> cacheIndexes;
        private SelectableList<ItemTypeAttributeCache> cacheItemTypes;
        private List<Type> cachedTypes = new List<Type>();
        private ColumnAttributeCache cachePrimaryKey;
        private ColumnAttributeCache cacheTypeKey;
        private ColumnAttributeCache cacheFileKey;

        public TableAttribute Attribute { get; set; }

        public DBSchema Schema
        {
            get { return cacheSchema; }
            set { cacheSchema = value; }
        }

        public DBTable Table
        {
            get { return cacheTable ?? (cacheTable = DBService.ParseTable(Attribute.TableName)); }
            internal set { cacheTable = value; }
        }


        public DBTableGroup TableGroup
        {
            get { return cacheGroup ?? (cacheGroup = Schema?.TableGroups[Attribute.GroupName]); }
            internal set { cacheGroup = value; }
        }

        public Type ItemType { get; internal set; }

        public IEnumerable<ColumnAttributeCache> Columns { get { return cacheColumns; } }

        public IEnumerable<ReferenceAttributeCache> References { get { return cacheReferences; } }

        public IEnumerable<ReferencingAttributeCache> Referencings { get { return cacheReferencings; } }

        public IEnumerable<Type> Types { get { return cachedTypes; } }

        public ColumnAttributeCache PrimaryKey
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

        public ColumnAttributeCache TypeKey
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

        public ColumnAttributeCache FileKey
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

        public virtual DBTable CreateTable()
        {
            var table = (DBTable)EmitInvoker.CreateObject(typeof(DBTable<>).MakeGenericType(ItemType));
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

        public DBTable Generate(DBSchema schema)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));

            Debug.WriteLine($"Generate {Attribute.TableName}");

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

            foreach (var reference in cacheReferences)
            {
                reference.CheckReference();
            }
            cacheColumns.Sort((a, b) => a.Attribute.Order.CompareTo(b.Attribute.Order));
            foreach (var column in cacheColumns)
            {
                column.Generate();
            }
            foreach (var reference in cacheReferences)
            {
                reference.Generate();
            }
            foreach (var index in cacheIndexes)
            {
                index.Generate();
            }

            if (!Schema.Tables.Contains(Attribute.TableName))
            {
                Schema.Tables.Add(Table);
            }
            Table.IsLoging = Attribute.IsLoging;

            foreach (var itemType in cacheItemTypes)
            {
                Table.ItemTypes[itemType.Attribute.Id] = new DBItemType { Type = itemType.Type };
                itemType.Generate();

            }

            return Table;
        }

        public IInvoker ParseProperty(string property)
        {
            var column = GetColumnByProperty(property);
            if (column != null)
            {
                return column.PropertyInvoker;
            }

            var reference = GetReferenceByProperty(property);
            if (reference != null)
            {
                return reference.Column.ReferencePropertyInvoker;
            }

            var refing = GetReferencingByProperty(property);
            if (refing != null)
            {
                return refing.PropertyInvoker;
            }

            return null;
        }



        public virtual void Initialize(Type type)
        {
            if (ItemType != null)
                return;
            cacheColumns = new SelectableList<ColumnAttributeCache>();
            cacheColumns.Indexes.Add(columnNameInvoker);
            cacheColumns.Indexes.Add(columnPropertyInvoker);
            cacheReferences = new SelectableList<ReferenceAttributeCache>();
            cacheReferences.Indexes.Add(referencePropertyInvoker);
            cacheReferencings = new SelectableList<ReferencingAttributeCache>();
            cacheReferencings.Indexes.Add(referencingPropertyInvoker);
            cacheIndexes = new SelectableList<IndexAttributeCache>();
            cacheIndexes.Indexes.Add(IndexNameinvoker);
            cacheItemTypes = new SelectableList<ItemTypeAttributeCache>();

            ItemType = type;
            var types = TypeHelper.GetTypeHierarchi(type);
            foreach (var item in types)
            {
                InitializeType(item);
            }
        }

        public void InitializeItemType(ItemTypeAttributeCache itemType)
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

        public void InitializeType(Type type)
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

            cachedTypes.Add(type);
        }

        private bool InitializeSeparateIndex(PropertyInfo property, out IndexAttributeCache index)
        {
            var indexAttribute = property.GetCustomAttribute<IndexAttribute>(false);
            if (indexAttribute != null)
            {
                index = cacheIndexes.SelectOne(nameof(IndexAttributeCache.IndexName), indexAttribute.IndexName)
                    ?? new IndexAttributeCache { Attribute = indexAttribute };
                index.Table = this;
                var columnAttribute = GetColumnByProperty(property.Name);
                index.Columns.Add(columnAttribute);
                return true;
            }
            index = null;
            return false;
        }


        private bool InitializeIndex(PropertyInfo property, IEnumerable<ColumnAttributeCache> columns, out IndexAttributeCache index)
        {
            var indexAttribute = property.GetCustomAttribute<IndexAttribute>(false);
            if (indexAttribute != null)
            {
                index = cacheIndexes.SelectOne(nameof(IndexAttributeCache.IndexName), indexAttribute.IndexName)
                    ?? new IndexAttributeCache { Attribute = indexAttribute };
                index.Table = this;
                index.Columns.AddRange(columns);
                return true;
            }
            index = null;
            return false;
        }

        public virtual bool InitializeReference(PropertyInfo property, out ReferenceAttributeCache reference)
        {
            var referenceAttrubute = property.GetCustomAttribute<ReferenceAttribute>(false);
            if (referenceAttrubute != null)
            {
                reference = new ReferenceAttributeCache(this, property, referenceAttrubute);
                return true;
            }
            reference = null;
            return false;
        }

        public virtual bool InitializeReferencing(PropertyInfo property, out ReferencingAttributeCache referencing)
        {
            var referencingAttribuite = property.GetCustomAttribute<ReferencingAttribute>(false);
            if (referencingAttribuite != null)
            {
                referencing = new ReferencingAttributeCache(this, property, referencingAttribuite);
                return true;
            }
            referencing = null;
            return false;
        }

        public virtual IEnumerable<ColumnAttributeCache> InitializeColumn(PropertyInfo property)
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
                        yield return new ColumnAttributeCache(this, property, columnAttribute, culture);
                    }

                }
                else
                {
                    yield return new ColumnAttributeCache(this, property, columnAttribute);
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

        public ColumnAttributeCache GetColumn(string name)
        {
            return cacheColumns.SelectOne(nameof(ColumnAttributeCache.ColumnName), name);
        }

        public ColumnAttributeCache GetColumnByProperty(string property)
        {
            return cacheColumns.SelectOne(nameof(ColumnAttributeCache.PropertyName), property);
        }

        public ReferenceAttributeCache GetReferenceByProperty(string property)
        {
            return cacheReferences.SelectOne(nameof(ReferenceAttributeCache.PropertyName), property);
        }

        public ReferencingAttributeCache GetReferencingByProperty(string property)
        {
            return cacheReferencings.SelectOne(nameof(ReferencingAttributeCache.PropertyName), property);
        }
    }
}
