using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;

namespace DataWF.Generator
{
    internal class TableLogGenerator : TableGenerator
    {
        private string? className;
        private string? attribute;

        public TableLogGenerator(TypeRecordProvider provider, INamedTypeSymbol typeSymbol)
            : base(provider, typeSymbol)
        {
            string? tableSqlName = null;
            var tableAttribute = typeSymbol.GetAttribute(Attributes.Table);
            if (tableAttribute != null)
            {
                var keys = tableAttribute.GetNamedValue("Keys");
                if (keys.IsNull || ((int?)keys.Value & (1 << 0)) == 0)
                {
                    className = $"{TypeName}Log";
                }
                var name = tableAttribute.GetNamedValue("TableName");
                if (name.IsNull)
                {
                    name = tableAttribute.ConstructorArguments.FirstOrDefault();
                }
                if (name.IsNull)
                {
                    className = null;
                }
                tableSqlName = name.Value?.ToString();
            }

            var abstractTableAttribute = typeSymbol.GetAttribute(Attributes.AbstractTable);
            if (abstractTableAttribute != null)
            {
                className = $"{TypeName}Log";
                if (string.Equals(TypeName, Helper.cLogItem, StringComparison.Ordinal)
                    || string.Equals(typeSymbol.BaseType?.Name, Helper.cLogItem, StringComparison.Ordinal))
                {
                    className = null;
                }
            }

            var virtualTableAttribute = typeSymbol.GetAttribute(Attributes.VirtualTable);
            if (virtualTableAttribute != null)
            {
                className = $"{typeSymbol.Name}Log";
            }

            if (tableAttribute != null)
            {
                attribute = $"[LogTable(typeof({TypeName}), \"{tableSqlName}_log\")]";
            }
            else if (abstractTableAttribute != null)
            {
                attribute = $"[AbstractTable]";
            }
            else if (virtualTableAttribute != null)
            {
                var itemType = virtualTableAttribute.GetNamedValue("Id");
                if (itemType.IsNull)
                {
                    itemType = virtualTableAttribute.ConstructorArguments.FirstOrDefault();
                }
                attribute = $"[VirtualTable({itemType.Value}, BaseType = typeof({TypeName}))]";
            }
        }

        public TypeRecord? BaseType => Type.BaseType;

        public override bool ProcessInternal(ref SourceProductionContext context)
        {
            var logClassSource = Generate();
            if (logClassSource != null)
            {
                var logItemSource = SourceText.From(logClassSource, Encoding.UTF8);
                context.AddSource($"{Namespace}.{TypeName}Log.g.cs", logItemSource);

                return true;
            }
            return false;
        }

        public new string? Generate()
        {
            string namespaceName = $"{Namespace}";

            if (className == null)
                return null;
            var tableName = Helper.cTable;// $"{className}Table";
            var tableTypeName = $"I{className}Table";
            string baseClassName = Helper.cLogItem;

            if (BaseType!= null
                && !string.Equals(BaseType.Name, Helper.cDBItem, StringComparison.Ordinal)
                && !string.Equals(BaseType.Name, Helper.cObject, StringComparison.Ordinal)
                && !string.Equals(BaseType.Name, Helper.cDBGroupItem, StringComparison.Ordinal))
            {
                var baseNamespace = $"{BaseType.Namespace}";
                baseClassName = $"{BaseType.Name}Log";

                if (baseNamespace != namespaceName)
                {
                    baseClassName = $"{baseNamespace}.{baseClassName}";
                }
            }
            // begin building the generated source
            source = new StringBuilder($@"using DataWF.Core;
{(namespaceName != "DataWF.Models" ? "using DataWF.Data;" : string.Empty)}
");

            source.Append($@"
namespace {namespaceName}
{{
    ");
            source.Append(attribute);


            source.Append($@"
    public {(Type.IsSealed ? "sealed " : string.Empty)} {(Type.IsAbstract ? "abstract " : string.Empty)}partial class {className} : {baseClassName}
    {{");
            source.Append($@"
        public {className}({tableTypeName} table): base(table)
        {{ }}
        
        public {className}({TypeName} item): base(item)
        {{ }}
");
            //{(itemTypeAttribute != null ? "Typed" : string.Empty)}
            foreach (var propertySymbol in Type.Properties!)
            {
                ProcessLogProperty(propertySymbol, tableName);
            }
            source.Append(@"
    } 
}");
            return source.ToString();
        }

        private void ProcessLogProperty(PropertyRecord propertySymbol, string tableName)
        {
            // get the name and type
            string propertyName = propertySymbol.Name;
            var propertyType = propertySymbol.Type;


            // get the attribute from the property, and any associated data
            var columnAttribute = propertySymbol.GetAttribute(Attributes.Column);
            if (columnAttribute != null)
            {
                var columnType = columnAttribute.GetNamedValue("ColumnType");
                if (!columnType.IsNull && (byte?)columnType.Value != 0)
                    return;

                var keys = columnAttribute.GetNamedValue("Keys");
                if (!keys.IsNull && ((int?)keys.Value & (1 << 21)) != 0)
                    return;

                var overridenPropertyType = columnAttribute.GetNamedValue("DataType");
                if (!overridenPropertyType.IsNull && overridenPropertyType.Value is ITypeSymbol typeSymbol)
                    propertyType = provider.GetOrCreate(typeSymbol)!.Type;

                var sqlName = columnAttribute.GetNamedValue("ColumnName");
                if (sqlName.IsNull)
                {
                    sqlName = columnAttribute.ConstructorArguments.FirstOrDefault();
                }
                if (!keys.IsNull && ((int?)keys.Value & (1 << 16)) != 0 && cultures != null)
                {
                    foreach (var culture in cultures)
                    {
                        source.Append($@"
        [LogColumn(""{sqlName.Value}_{culture.ToLowerInvariant()}"", ""{sqlName.Value}_{culture.ToLowerInvariant()}_log"")]
        public {propertyType} {propertyName}{culture.ToUpperInvariant()}
        {{
            get => GetValue<{propertyType}>({tableName}.{propertyName}{culture.ToUpperInvariant()}Key);
            set => SetValue(value, {tableName}.{propertyName}{culture.ToUpperInvariant()}Key);
        }}
");
                    }
                }
                else
                {
                    source.Append($@"
        [LogColumn(""{sqlName.Value}"", ""{sqlName.Value}_log"")]
        public {propertyType} {propertyName}
        {{
            get => GetValue<{propertyType}>({tableName}.{propertyName}Key);
            set => SetValue(value, {tableName}.{propertyName}Key);
        }}
");
                }
            }

            var referenceAttribute = propertySymbol.GetAttribute(Attributes.Reference);
            if (referenceAttribute != null)
            {
                string keyFieldName = $"_{propertyName}";
                var refName = referenceAttribute.GetNamedValue("ColumnProperty");
                if (refName.IsNull)
                {
                    refName = referenceAttribute.ConstructorArguments.FirstOrDefault();
                }
                if (refName.IsNull)
                {
                    return;
                }
                var columnPropertyName = refName.Value?.ToString();
                var columnProperty = Type.Properties?.FirstOrDefault(x => x.Name == columnPropertyName);
                if (columnProperty != null)
                {
                    columnAttribute = columnProperty.GetAttribute(Attributes.Column);
                    if (columnAttribute != null)
                    {
                        var columnType = columnAttribute.GetNamedValue("ColumnType");
                        if (!columnType.IsNull && (byte?)columnType.Value != 0)
                            return;

                        var keys = columnAttribute.GetNamedValue("Keys");
                        if (!keys.IsNull && ((int?)keys.Value & (1 << 21)) != 0)
                            return;
                    }
                }

                source.Append($@"
        private {propertyType} {keyFieldName};");
                source.Append($@"
        [LogReference(nameof({columnPropertyName}))]
        public {propertyType} {propertyName}
        {{
            get => GetReference<{propertyType}>({tableName}.{columnPropertyName}Key, ref {keyFieldName});
            set => SetReference({keyFieldName} = value, {tableName}.{columnPropertyName}Key);
        }}
");
            }
        }
    }

}