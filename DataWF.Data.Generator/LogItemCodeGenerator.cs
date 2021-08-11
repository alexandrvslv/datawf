using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using DataWF.Common.Generator;

namespace DataWF.Data.Generator
{
    internal class LogItemCodeGenerator : BaseTableCodeGenerator
    {

        public LogItemCodeGenerator(ref GeneratorExecutionContext context, Compilation compilation) : base(ref context, compilation)
        {
            TableCodeGenerator = new TableCodeGenerator(ref context, compilation);
        }

        public TableCodeGenerator TableCodeGenerator { get; set; }


        public override bool Process(INamedTypeSymbol classSymbol)
        {
            ClassSymbol = classSymbol;
            Properties = classSymbol.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic).ToList();
            string logClassSource = Generate();
            if (logClassSource != null)
            {
                var logItemSource = SourceText.From(logClassSource, Encoding.UTF8);
                Context.AddSource($"{classSymbol.ContainingNamespace.ToDisplayString()}.Log.{classSymbol.Name}Gen.cs", logItemSource);

                var logItemSyntax = CSharpSyntaxTree.ParseText(logItemSource, (CSharpParseOptions)Options);

                TableCodeGenerator.Cultures = Cultures;
                TableCodeGenerator.InvokerCodeGenerator.Compilation =
                    TableCodeGenerator.Compilation = TableCodeGenerator.Compilation.AddSyntaxTrees(logItemSyntax);

                var unitSyntax = (CompilationUnitSyntax)logItemSyntax.GetRoot();
                var logClassSyntax = unitSyntax.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (logClassSyntax != null)
                {
                    TableCodeGenerator.Process(logClassSyntax);
                }
                return true;
            }
            return false;
        }

        public override string Generate()
        {
            string namespaceName = $"{classSymbol.ContainingNamespace.ToDisplayString()}.Log";
            string className = null;
            string tableSqlName = null;
            var tableAttribute = classSymbol.GetAttribute(attributes.Table);
            if (tableAttribute != null)
            {
                var keys = tableAttribute.GetNamedValue("Keys");
                if (keys.IsNull || ((int)keys.Value & (1 << 0)) == 0)
                {
                    className = classSymbol.Name + "Log";
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

            var abstractTableAttribute = classSymbol.GetAttribute(attributes.AbstractTable);
            if (abstractTableAttribute != null)
            {
                className = classSymbol.Name + "Log";
                if (string.Equals(classSymbol.Name, "DBItemLog", StringComparison.Ordinal)
                    || string.Equals(classSymbol.BaseType?.Name, "DBItemLog", StringComparison.Ordinal))
                {
                    return null;
                }
            }

            var itemTypeAttribute = classSymbol.GetAttribute(attributes.VirtualTable);
            if (itemTypeAttribute != null)
            {
                className = classSymbol.Name + "Log";
            }

            if (className != null)
            {
                var tableName = "Table";// $"{className}Table";
                var tableTypeName = $"I{className}Table";
                string baseClassName = "DBItemLog";

                if (classSymbol.BaseType.Name != "DBItem"
                    && classSymbol.BaseType.Name != "DBGroupItem")
                {
                    var baseNamespace = $"{classSymbol.BaseType.ContainingNamespace.ToDisplayString()}.Log";
                    baseClassName = classSymbol.BaseType.Name + "Log";

                    if (baseNamespace != namespaceName)
                    {
                        baseClassName = $"{baseNamespace}.{classSymbol.BaseType.Name}Log";
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
                    source.Append($"[LogTable(typeof({classSymbol.Name}), \"{tableSqlName}_log\")]");
                }
                else if (abstractTableAttribute != null)
                {
                    source.Append($"[AbstractTable, InvokerGenerator]");
                }
                else if (itemTypeAttribute != null)
                {
                    var itemType = itemTypeAttribute.GetNamedValue("Id");
                    if (itemType.IsNull)
                    {
                        itemType = itemTypeAttribute.ConstructorArguments.FirstOrDefault();
                    }
                    source.Append($"[LogItemType({itemType.Value})]");
                }

                source.Append($@"
    public {(classSymbol.IsSealed ? "sealed " : string.Empty)} {(classSymbol.IsAbstract ? "abstract " : string.Empty)}partial class {className} : {baseClassName}
    {{");
                source.Append($@"
        public {className}({tableTypeName} table): base(table)
        {{ }}
        
        public {className}({classSymbol.Name} item): base(item)
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
            var columnAttribute = propertySymbol.GetAttribute(attributes.Column);
            if (columnAttribute != null)
            {
                TypedConstant columnType = columnAttribute.GetNamedValue("ColumnType");
                if (!columnType.IsNull && (int)columnType.Value != 0)
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

            var referenceAttribute = propertySymbol.GetAttribute(attributes.Reference);
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
                    columnAttribute = columnProperty.GetAttribute(attributes.Column);
                    if (columnAttribute != null)
                    {
                        TypedConstant columnType = columnAttribute.GetNamedValue("ColumnType");
                        if (!columnType.IsNull && (int)columnType.Value != 0)
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