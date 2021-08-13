using Microsoft.CodeAnalysis;

namespace DataWF.Data.Generator
{
    internal class AttributesCache
    {
        public INamedTypeSymbol Table;
        public INamedTypeSymbol AbstractTable;
        public INamedTypeSymbol VirtualTable;
        public INamedTypeSymbol LogTable;
        public INamedTypeSymbol Culture;
        public INamedTypeSymbol Reference;
        public INamedTypeSymbol Referencing;
        public INamedTypeSymbol Column;
        public INamedTypeSymbol LogColumn;
        public INamedTypeSymbol Schema;
        public INamedTypeSymbol SchemaEntry;

        public AttributesCache(Compilation compilation)
        {
            Table = compilation.GetTypeByMetadataName("DataWF.Data.TableAttribute");
            AbstractTable = compilation.GetTypeByMetadataName("DataWF.Data.AbstractTableAttribute");
            VirtualTable = compilation.GetTypeByMetadataName("DataWF.Data.VirtualTableAttribute");
            LogTable = compilation.GetTypeByMetadataName("DataWF.Data.LogTableAttribute");
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