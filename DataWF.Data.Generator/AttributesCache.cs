using Microsoft.CodeAnalysis;

namespace DataWF.Data.Generator
{
    internal class AttributesCache
    {
        public INamedTypeSymbol Table;
        public INamedTypeSymbol ItemType;
        public INamedTypeSymbol Culture;
        public INamedTypeSymbol Reference;
        public INamedTypeSymbol Referencing;
        public INamedTypeSymbol AbstractTable;
        public INamedTypeSymbol LogTable;
        public INamedTypeSymbol LogItemType;
        public INamedTypeSymbol Column;
        public INamedTypeSymbol LogColumn;
        public INamedTypeSymbol Schema;
        public INamedTypeSymbol SchemaEntry;

        public AttributesCache(Compilation compilation)
        {
            Table = compilation.GetTypeByMetadataName("DataWF.Data.TableAttribute");
            LogTable = compilation.GetTypeByMetadataName("DataWF.Data.LogTableAttribute");
            AbstractTable = compilation.GetTypeByMetadataName("DataWF.Data.AbstractTableAttribute");
            ItemType = compilation.GetTypeByMetadataName("DataWF.Data.ItemTypeAttribute");
            LogItemType = compilation.GetTypeByMetadataName("DataWF.Data.LogItemTypeAttribute");
            Column = compilation.GetTypeByMetadataName("DataWF.Data.ColumnAttribute");
            LogColumn = compilation.GetTypeByMetadataName("DataWF.Data.LogColumnAttribute");
            Culture = compilation.GetTypeByMetadataName("DataWF.Data.CultureKeyAttribute");
            Reference = compilation.GetTypeByMetadataName("DataWF.Data.ReferenceAttribute");
            Referencing = compilation.GetTypeByMetadataName("DataWF.Data.ReferencingAttribute");
            Schema = compilation.GetTypeByMetadataName("DataWF.Data.SchemaAttribute");
            SchemaEntry = compilation.GetTypeByMetadataName("DataWF.Data.SchemaEntryAttribute");
        }
    }

}