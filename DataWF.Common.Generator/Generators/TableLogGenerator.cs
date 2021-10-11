using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using DataWF.Common.Generator;
using System.Diagnostics;

namespace DataWF.Common.Generator
{
    internal class TableLogGenerator : ExtendedGenerator
    {
        public TableLogGenerator(CompilationContext compilationContext, InvokerGenerator invokerGenerator)
            : base(compilationContext, invokerGenerator)
        {
            TableGenerator = new TableGenerator(compilationContext, invokerGenerator);
        }

        public TableGenerator TableGenerator { get; set; }


        public override bool Process()
        {
            Properties = TypeSymbol.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic).ToList();
            string logClassSource = Generate();
            if (logClassSource != null)
            {
                var logItemSource = SourceText.From(logClassSource, Encoding.UTF8);
                CompilationContext.Context.AddSource($"{TypeSymbol.ContainingNamespace.ToDisplayString()}.{TypeSymbol.Name}LogGen.cs", logItemSource);

                var logItemSyntax = CSharpSyntaxTree.ParseText(logItemSource, (CSharpParseOptions)Options);

                TableGenerator.Cultures = Cultures;
                CompilationContext.Compilation = TableGenerator.Compilation.AddSyntaxTrees(logItemSyntax);

                var unitSyntax = (CompilationUnitSyntax)logItemSyntax.GetRoot();
                var logClassSyntax = unitSyntax.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (logClassSyntax != null)
                {
                    TableGenerator.Process(logClassSyntax);
                }
                return true;
            }
            return false;
        }

        public string Generate()
        {
            string namespaceName = $"{TypeSymbol.ContainingNamespace.ToDisplayString()}";
            string className = null;
            string tableSqlName = null;
            var tableAttribute = TypeSymbol.GetAttribute(Attributes.Table);
            if (tableAttribute != null)
            {
                var keys = tableAttribute.GetNamedValue("Keys");
                if (keys.IsNull || ((int)keys.Value & (1 << 0)) == 0)
                {
                    className = $"{TypeSymbol.Name}Log";
                }
                var name = tableAttribute.GetNamedValue("TableName");
                if (name.IsNull)
                {
                    name = tableAttribute.ConstructorArguments.FirstOrDefault();
                }
                if (name.IsNull)
                {
                    return null;
                }
                tableSqlName = name.Value.ToString();
            }

            var abstractTableAttribute = TypeSymbol.GetAttribute(Attributes.AbstractTable);
            if (abstractTableAttribute != null)
            {
                className = $"{TypeSymbol.Name}Log";
                if (string.Equals(TypeSymbol.Name, Helper.cDBItemLog, StringComparison.Ordinal)
                    || string.Equals(TypeSymbol.BaseType?.Name, Helper.cDBItemLog, StringComparison.Ordinal))
                {
                    return null;
                }
            }

            var virtualTableAttribute = TypeSymbol.GetAttribute(Attributes.VirtualTable);
            if (virtualTableAttribute != null)
            {
                className = $"{TypeSymbol.Name}Log";
            }

            if (className != null)
            {
                var tableName = Helper.cTable;// $"{className}Table";
                var tableTypeName = $"I{className}Table";
                string baseClassName = Helper.cDBItemLog;

                if (!string.Equals(TypeSymbol.BaseType.Name, Helper.cDBItem, StringComparison.Ordinal)
                    && !string.Equals(TypeSymbol.BaseType.Name, Helper.cObject, StringComparison.Ordinal)
                    && !string.Equals(TypeSymbol.BaseType.Name, Helper.cDBGroupItem, StringComparison.Ordinal))
                {
                    var baseNamespace = $"{TypeSymbol.BaseType.ContainingNamespace.ToDisplayString()}";
                    baseClassName = $"{TypeSymbol.BaseType.Name}Log";

                    if (baseNamespace != namespaceName)
                    {
                        baseClassName = $"{baseNamespace}.{baseClassName}";
                    }
                }
                // begin building the generated source
                source = new StringBuilder($@"using DataWF.Common;
{(namespaceName != "DataWF.Data" ? "using DataWF.Data;" : string.Empty)}
");

                source.Append($@"
namespace {namespaceName}
{{
    ");
                if (tableAttribute != null)
                {
                    source.Append($"[LogTable(typeof({TypeSymbol.Name}), \"{tableSqlName}_log\")]");
                }
                else if (abstractTableAttribute != null)
                {
                    source.Append($"[AbstractTable]");
                }
                else if (virtualTableAttribute != null)
                {
                    var itemType = virtualTableAttribute.GetNamedValue("Id");
                    if (itemType.IsNull)
                    {
                        itemType = virtualTableAttribute.ConstructorArguments.FirstOrDefault();
                    }
                    source.Append($"[VirtualTable({itemType.Value})]");
                }

                source.Append($@"
    public {(TypeSymbol.IsSealed ? "sealed " : string.Empty)} {(TypeSymbol.IsAbstract ? "abstract " : string.Empty)}partial class {className} : {baseClassName}
    {{");
                source.Append($@"
        public {className}({tableTypeName} table): base(table)
        {{ }}
        
        public {className}({TypeSymbol.Name} item): base(item)
        {{ }}
");
                //{(itemTypeAttribute != null ? "Typed" : string.Empty)}
                foreach (IPropertySymbol propertySymbol in properties)
                {
                    ProcessLogProperty(propertySymbol, tableName);
                }
                source.Append(@"
    } 
}");
                return source.ToString();
            }
            return null;
        }

        private void ProcessLogProperty(IPropertySymbol propertySymbol, string tableName)
        {
            // get the name and type
            string propertyName = propertySymbol.Name;
            ITypeSymbol propertyType = propertySymbol.Type;


            // get the attribute from the property, and any associated data
            var columnAttribute = propertySymbol.GetAttribute(Attributes.Column);
            if (columnAttribute != null)
            {
                TypedConstant columnType = columnAttribute.GetNamedValue("ColumnType");
                if (!columnType.IsNull && (byte)columnType.Value != 0)
                    return;

                TypedConstant keys = columnAttribute.GetNamedValue("Keys");
                if (!keys.IsNull && ((int)keys.Value & (1 << 21)) != 0)
                    return;

                TypedConstant overridenPropertyType = columnAttribute.GetNamedValue("DataType");
                if (!overridenPropertyType.IsNull)
                    propertyType = (ITypeSymbol)overridenPropertyType.Value;

                TypedConstant sqlName = columnAttribute.GetNamedValue("ColumnName");
                if (sqlName.IsNull)
                {
                    sqlName = columnAttribute.ConstructorArguments.FirstOrDefault();
                }
                if (!keys.IsNull && ((int)keys.Value & (1 << 16)) != 0)
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
                TypedConstant refName = referenceAttribute.GetNamedValue("ColumnProperty");
                if (refName.IsNull)
                {
                    refName = referenceAttribute.ConstructorArguments.FirstOrDefault();
                }
                if (refName.IsNull)
                {
                    return;
                }
                var columnPropertyName = (string)refName.Value;
                var columnProperty = propertySymbol.ContainingType.GetMembers(columnPropertyName).FirstOrDefault() as IPropertySymbol;
                if (columnProperty != null)
                {
                    columnAttribute = columnProperty.GetAttribute(Attributes.Column);
                    if (columnAttribute != null)
                    {
                        TypedConstant columnType = columnAttribute.GetNamedValue("ColumnType");
                        if (!columnType.IsNull && (byte)columnType.Value != 0)
                            return;

                        TypedConstant keys = columnAttribute.GetNamedValue("Keys");
                        if (!keys.IsNull && ((int)keys.Value & (1 << 21)) != 0)
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