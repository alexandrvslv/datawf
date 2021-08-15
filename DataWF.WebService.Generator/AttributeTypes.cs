using Microsoft.CodeAnalysis;

namespace DataWF.WebService.Generator
{
    public class AttributeTypes
    {
        internal INamedTypeSymbol Table;
        internal INamedTypeSymbol AbstractTable;
        internal INamedTypeSymbol VirtualTable;
        internal INamedTypeSymbol ControllerMethod;
        internal INamedTypeSymbol ControllerParameter;
        internal INamedTypeSymbol Column;
        internal INamedTypeSymbol Schema;
        internal INamedTypeSymbol SchemaEntry;

        public AttributeTypes()
        {
        }
    }
}