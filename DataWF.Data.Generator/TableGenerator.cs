using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DataWF.Data.Generator
{
    [Generator]
    public class TableGenerator : ISourceGenerator
    {

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the attribute text
            //context.AddSource("AutoNotifyAttribute", SourceText.From(attributeText, Encoding.UTF8));

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;
            //TODO Pass as argument
            var cultures = new List<string>(new[] { "RU", "EN" });
            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            //Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));
            try
            {
                // get the newly bound attributes
                var attributes = new AttributesCache(context.Compilation);


                // loop over the candidate fields, and keep the ones that are actually annotated
                foreach (ClassDeclarationSyntax classSyntax in receiver.Candidates)
                {
                    SemanticModel model = context.Compilation.GetSemanticModel(classSyntax.SyntaxTree);
                    var classSymbol = model.GetDeclaredSymbol(classSyntax);
                    var properties = classSymbol.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic).ToList();
                    string classSource = ProcessTable(classSymbol, properties, attributes, context, cultures);
                    if (classSource != null)
                    {
                        try
                        {
                            context.AddSource($"{classSymbol.Name}Table.cs", SourceText.From(classSource, Encoding.UTF8));
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    string logClassSource = ProcessLogTable(classSymbol, properties, attributes, context, cultures);
                    if (logClassSource != null)
                    {
                        var sourceText = SourceText.From(logClassSource, Encoding.UTF8);
                        context.AddSource($"{classSymbol.Name}Log.cs", sourceText);

                        var sourceSyntax = CSharpSyntaxTree.ParseText(sourceText, (CSharpParseOptions)context.ParseOptions);
                        var compilation = context.Compilation.AddSyntaxTrees(sourceSyntax);
                        var newAttributes = new AttributesCache(compilation);

                        var unitSyntax = (CompilationUnitSyntax)sourceSyntax.GetRoot();
                        var logClassSyntax = unitSyntax.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault();
                        if (logClassSyntax == null)
                        {
                            continue;
                        }
                        model = compilation.GetSemanticModel(logClassSyntax.SyntaxTree);
                        var logClassSymbol = model.GetDeclaredSymbol(logClassSyntax);
                        if (logClassSymbol != null)
                        {
                            var invokerGeneratorAtribute = compilation.GetTypeByMetadataName("DataWF.Common.InvokerGeneratorAttribute");
                            Common.Generator.InvokerGenerator.ProcessClass(logClassSymbol, invokerGeneratorAtribute, context);

                            properties = logClassSymbol.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic).ToList();
                            classSource = ProcessTable(logClassSymbol, properties, newAttributes, context, cultures);
                            if (classSource != null)
                            {
                                try
                                {
                                    context.AddSource($"{classSymbol.Name}LogTable.cs", SourceText.From(classSource, Encoding.UTF8));
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Generator Fail: {ex.Message} at {ex.StackTrace}");
#if DEBUG
                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Launch();
                }
#endif
            }
        }

        private string ProcessLogTable(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, AttributesCache attributes, GeneratorExecutionContext context, List<string> cultures)
        {
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            string className = null;
            string tableSqlName = null;
            var tableAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.Table, SymbolEqualityComparer.Default));
            if (tableAttribute != null)
            {
                var keys = tableAttribute.NamedArguments.FirstOrDefault(p => string.Equals(p.Key, "Keys", StringComparison.Ordinal)).Value;
                if (keys.IsNull || ((int)keys.Value & (1 << 0)) == 0)
                {
                    className = classSymbol.Name + "Log";
                }
                var name = tableAttribute.NamedArguments.FirstOrDefault(p => string.Equals(p.Key, "TableName", StringComparison.Ordinal)).Value;
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

            var abstractTableAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.AbstractTable, SymbolEqualityComparer.Default));
            if (abstractTableAttribute != null)
            {
                className = classSymbol.Name + "Log";
                if (string.Equals(classSymbol.Name, "DBLogItem", StringComparison.Ordinal)
                    || string.Equals(classSymbol.BaseType?.Name, "DBLogItem", StringComparison.Ordinal))
                {
                    return null;
                }
            }

            var itemTypeAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.ItemType, SymbolEqualityComparer.Default));
            if (itemTypeAttribute != null)
            {
                className = classSymbol.Name + "Log";
            }

            if (className != null)
            {
                var tableName = $"{className}Table";
                var tableTypeName = $"I{className}Table";
                string baseClassName = "DBLogItem";

                if (classSymbol.BaseType.Name != "DBItem"
                    && classSymbol.BaseType.Name != "DBGroupItem")
                {
                    var baseNamespace = classSymbol.BaseType.ContainingNamespace.ToDisplayString();
                    baseClassName = classSymbol.BaseType.Name + "Log";

                    if (baseNamespace != namespaceName)
                    {
                        baseClassName = $"{baseNamespace}.{classSymbol.BaseType.Name}Log";
                    }
                }
                // begin building the generated source
                StringBuilder source = new StringBuilder($@"using DataWF.Common;
{(namespaceName != "DataWF.Data" ? "using DataWF.Data;" : string.Empty)}
");

                source.Append($@"
namespace {namespaceName}
{{
    ");
                if (tableAttribute != null)
                {
                    source.Append($"[LogTable(typeof({classSymbol.Name}), \"{tableSqlName}_log\"), InvokerGenerator]");
                }
                else if (abstractTableAttribute != null)
                {
                    source.Append($"[AbstractTable, InvokerGenerator]");
                }
                else if (itemTypeAttribute != null)
                {
                    var itemType = itemTypeAttribute.NamedArguments.FirstOrDefault(p => string.Equals(p.Key, "Id", StringComparison.Ordinal)).Value;
                    if (itemType.IsNull)
                    {
                        itemType = itemTypeAttribute.ConstructorArguments.FirstOrDefault();
                    }
                    source.Append($"[LogItemType({itemType.Value}), InvokerGenerator]");
                }

                source.Append($@"
    public {(classSymbol.IsSealed ? "sealed " : string.Empty)} {(classSymbol.IsAbstract ? "abstract " : string.Empty)}partial class {className} : {baseClassName}
    {{");
                source.Append($@"
        public {className}(DBTable table):base(table)
        {{ }}
");
                //{(itemTypeAttribute != null ? "Typed" : string.Empty)}
                foreach (IPropertySymbol propertySymbol in properties)
                {
                    ProcessLogProperty(source, propertySymbol, attributes, tableName, cultures);
                }
                source.Append(@"
    } 
}");
                return source.ToString();
            }
            return null;
        }

        private void ProcessLogProperty(StringBuilder source, IPropertySymbol propertySymbol, AttributesCache attributes, string tableName, List<string> cultures)
        {
            // get the name and type
            string propertyName = propertySymbol.Name;
            ITypeSymbol propertyType = propertySymbol.Type;


            // get the attribute from the property, and any associated data
            AttributeData columnAttribute = propertySymbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass.Equals(attributes.Column, SymbolEqualityComparer.Default));
            if (columnAttribute != null)
            {
                TypedConstant columnType = columnAttribute.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, "ColumnType", StringComparison.Ordinal)).Value;
                if (!columnType.IsNull && (int)columnType.Value != 0)
                    return;

                TypedConstant keys = columnAttribute.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, "Keys", StringComparison.Ordinal)).Value;
                if (!keys.IsNull && ((int)keys.Value & (1 << 21)) != 0)
                    return;

                TypedConstant overridenPropertyType = columnAttribute.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, "DataType", StringComparison.Ordinal)).Value;
                if (!overridenPropertyType.IsNull)
                    propertyType = (ITypeSymbol)overridenPropertyType.Value;

                TypedConstant sqlName = columnAttribute.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, "ColumnName", StringComparison.Ordinal)).Value;
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

            AttributeData referenceAttribute = propertySymbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass.Equals(attributes.Reference, SymbolEqualityComparer.Default));
            if (referenceAttribute != null)
            {
                string keyFieldName = $"_{propertyName}";
                TypedConstant refName = referenceAttribute.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, "ColumnProperty", StringComparison.Ordinal)).Value;
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
                    columnAttribute = columnProperty.GetAttributes().FirstOrDefault(ad => ad.AttributeClass.Equals(attributes.Column, SymbolEqualityComparer.Default));
                    if (columnAttribute != null)
                    {
                        TypedConstant columnType = columnAttribute.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, "ColumnType", StringComparison.Ordinal)).Value;
                        if (!columnType.IsNull && (int)columnType.Value != 0)
                            return;

                        TypedConstant keys = columnAttribute.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, "Keys", StringComparison.Ordinal)).Value;
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

        private string ProcessTable(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, AttributesCache attributes, GeneratorExecutionContext context, List<string> cultures)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            string className = null;
            string whereName = null;
            var baseClassName = "DBTable";
            className = GetTableClassName(classSymbol, attributes);

            if (className != null)
            {
                var genericArg = classSymbol.Name;
                var interfaceName = "I" + className;
                var baseInterfaceName = "IDBTable";
                if (!classSymbol.IsSealed)
                {
                    genericArg = "T";
                    className += "<T>";
                    whereName = $"where T: {classSymbol.Name}";
                }
                if (classSymbol.BaseType.Name == "DBLogItem")
                {
                    baseClassName = "DBLogTable";
                    baseInterfaceName = "IDBLogTable";
                }
                else //&& classSymbol.BaseType.Name != "DBGroupItem"
                if (classSymbol.BaseType.Name != "DBItem")
                {
                    var baseNamespace = classSymbol.BaseType.ContainingNamespace.ToDisplayString();
                    if (baseNamespace == namespaceName
                        || baseNamespace == "<global namespace>")
                    {
                        baseClassName = classSymbol.BaseType.Name + "Table";
                        baseInterfaceName = "I" + baseClassName;
                    }
                    else
                    {
                        baseClassName = $"{baseNamespace}.{classSymbol.BaseType.Name}Table";
                        baseInterfaceName = $"{baseNamespace}.I{classSymbol.BaseType.Name}Table";
                    }
                }
                // begin building the generated source
                var source = new StringBuilder($@"using System.Text.Json.Serialization;
using DataWF.Common;
{(namespaceName != "DataWF.Data" ? "using DataWF.Data;" : string.Empty)}
");

                source.Append($@"
namespace {namespaceName}
{{
    public {(classSymbol.IsAbstract ? "abstract " : string.Empty)}partial class {className}: {baseClassName}<{genericArg}>{(interfaceName != null ? $", {interfaceName}" : string.Empty)} {whereName}
    {{");
                var interfaceSource = new StringBuilder($@"
    public partial interface {interfaceName}: {baseInterfaceName}
    {{");
                // create properties for each field 
                foreach (IPropertySymbol propertySymbol in properties)
                {
                    ProcessColumnProperty(source, interfaceSource, propertySymbol, attributes, cultures);
                }
                source.Append(@"
    }
");
                interfaceSource.Append(@"
    }
");
                source.Append(interfaceSource);
                source.Append($@"
    public partial class {classSymbol.Name}
    {{
        [JsonIgnore]
        public {(classSymbol.IsSealed ? className : interfaceName)} {GetTableClassName(classSymbol, attributes)} => ({(classSymbol.IsSealed ? className : interfaceName)})Table;
    }}
");
                source.Append(@"
}");
                return source.ToString();
            }


            return null;
        }

        private static string GetTableClassName(INamedTypeSymbol classSymbol, AttributesCache attributes)
        {
            string className = null;
            var tableAttribyte = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.Table, SymbolEqualityComparer.Default));
            if (tableAttribyte != null)
            {
                var typeName = tableAttribyte.NamedArguments.FirstOrDefault(p => string.Equals(p.Key, "Type", StringComparison.Ordinal)).Value;
                if (!typeName.IsNull && typeName.Value is ITypeSymbol typeSymbol)
                {
                    className = typeSymbol.Name;
                }
                else
                {
                    className = classSymbol.Name + "Table";
                }
            }

            var itemTypeAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.ItemType, SymbolEqualityComparer.Default));
            if (itemTypeAttribute != null)
            {
                className = classSymbol.Name + "Table";
            }

            var logTableAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.LogTable, SymbolEqualityComparer.Default));
            if (logTableAttribute != null)
            {
                className = classSymbol.Name + "Table";
            }

            var logItemTypeAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.LogItemType, SymbolEqualityComparer.Default));
            if (logItemTypeAttribute != null)
            {
                className = classSymbol.Name + "Table";
            }

            var abstractAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.AbstractTable, SymbolEqualityComparer.Default));
            if (abstractAttribute != null)
            {
                className = classSymbol.Name + "Table";
            }

            return className;
        }

        private void ProcessColumnProperty(StringBuilder source, StringBuilder interfaceSource, IPropertySymbol propertySymbol, AttributesCache attributes, List<string> cultures)
        {
            // get the name and type of the field
            string propertyName = propertySymbol.Name;
            ITypeSymbol propertyType = propertySymbol.Type;
            string keyFieldName = $"_{propertyName}Key";

            // get the AutoNotify attribute from the field, and any associated data
            AttributeData columnAttribute = propertySymbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass.Equals(attributes.Column, SymbolEqualityComparer.Default));
            if (columnAttribute != null)
            {
                var overridenPropertyType = columnAttribute.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, "DataType", StringComparison.Ordinal)).Value;
                if (!overridenPropertyType.IsNull)
                    propertyType = (ITypeSymbol)overridenPropertyType.Value;

                TypedConstant sqlName = columnAttribute.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, "ColumnName", StringComparison.Ordinal)).Value;
                if (sqlName.IsNull)
                {
                    sqlName = columnAttribute.ConstructorArguments.FirstOrDefault();
                }

                TypedConstant keys = columnAttribute.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, "Keys", StringComparison.Ordinal)).Value;
                if (!keys.IsNull && ((int)keys.Value & (1 << 16)) != 0)
                {
                    foreach (var culture in cultures)
                    {
                        keyFieldName = $"_{propertyName}{culture}Key";
                        string keyPropertyName = $"{propertyName}{culture}Key";
                        source.Append($@"
        private DBColumn<{propertyType}> {keyFieldName};
        [JsonIgnore]
        public {(IsNew(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => ParseColumn(""{sqlName.Value}_{culture.ToLowerInvariant()}"", ref {keyFieldName});
");
                        interfaceSource.Append($@"
        {(IsNew(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} {{ get; }}");
                    }
                }
                else
                {
                    string keyPropertyName = $"{propertyName}Key";
                    source.Append($@"
        private DBColumn<{propertyType}> {keyFieldName};
        [JsonIgnore]
        public {(IsNew(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => ParseProperty(nameof({propertySymbol.ContainingType.Name}.{propertyName}), ref {keyFieldName});
");
                    interfaceSource.Append($@"
        {(IsNew(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} {{ get; }}");
                }
            }

            var logColumnAttribute = propertySymbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass.Equals(attributes.LogColumn, SymbolEqualityComparer.Default));
            if (logColumnAttribute != null)
            {
                string keyPropertyName = $"{propertyName}Key";
                source.Append($@"
        private DBColumn<{propertyType}> {keyFieldName};
        [JsonIgnore]
        public {(IsNew(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => ParseProperty(nameof({propertySymbol.ContainingType.Name}.{propertyName}), ref {keyFieldName});
");
                interfaceSource.Append($@"
        {(IsNew(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} {{ get; }}");
            }
            var referenceAttribute = propertySymbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass.Equals(attributes.Reference, SymbolEqualityComparer.Default));
            if (referenceAttribute != null)
            {
                if (propertyType is INamedTypeSymbol refItemType)
                {
                    var refTableName = GetTableClassName(refItemType, attributes);
                    if (refTableName != null && interfaceSource.ToString().IndexOf(refTableName, StringComparison.Ordinal) < 0)
                    {
                        var refTableClassName = refTableName;
                        if (!refItemType.IsAbstract)
                        {
                            var typeName = refItemType.Name;
                            var refNamespace = refItemType.ContainingNamespace.ToDisplayString();
                            if (refNamespace != propertySymbol.ContainingType.ContainingNamespace.ToDisplayString())
                            {
                                typeName = refItemType.ToString();
                                refTableClassName = $"{refNamespace}.{refTableName}";
                            }
                            if (!refItemType.IsSealed)
                            {
                                refTableClassName += $"<{typeName}>";
                            }

                            source.Append($@"
        private {refTableClassName} _{refTableName};
        [JsonIgnore]
        public {refTableClassName} {refTableName} => _{refTableName} ??= ({refTableClassName})Schema.GetTable<{typeName}>();
");
                            interfaceSource.Append($@"
        {refTableClassName} {refTableName} {{ get; }}");
                        }
                    }
                }
            }
            var referencingAttribute = propertySymbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass.Equals(attributes.Referencing, SymbolEqualityComparer.Default));
            if (referencingAttribute != null)
            {
                if (propertyType is INamedTypeSymbol named
                    && named.TypeArguments.Length > 0
                    && named.TypeArguments.First() is INamedTypeSymbol refItemType)
                {
                    var refTableName = GetTableClassName(refItemType, attributes);
                    if (refTableName != null && interfaceSource.ToString().IndexOf(refTableName, StringComparison.Ordinal) < 0)
                    {
                        var refTableClassName = refTableName;
                        if (!refItemType.IsSealed)
                        {
                            refTableClassName += $"<{refItemType.Name}>";
                        }
                        source.Append($@"
        private {refTableClassName} _{refTableName};
        [JsonIgnore]
        public {refTableClassName} {refTableName} => _{refTableName} ??= ({refTableClassName})Schema.GetTable<{refItemType.Name}>();
");
                        interfaceSource.Append($@"
        {refTableClassName} {refTableName} {{ get; }}");
                    }
                }
            }
        }

        private bool IsNew(string keyPropertyName)
        {
            return string.Equals(keyPropertyName, "CodeKey", StringComparison.Ordinal)
                || string.Equals(keyPropertyName, "FileNameKey", StringComparison.Ordinal)
                || string.Equals(keyPropertyName, "FileLastWriteKey", StringComparison.Ordinal);
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> Candidates { get; } = new List<ClassDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
                    && classDeclarationSyntax.AttributeLists.Any(p => p.Attributes
                    .Select(p => p.Name.ToString())
                    .Any(p => string.Equals(p, "Table", StringComparison.Ordinal)
                    || string.Equals(p, "TableAttribute", StringComparison.Ordinal)
                    || string.Equals(p, "AbstractTable", StringComparison.Ordinal)
                    || string.Equals(p, "AbstractTableAttribute", StringComparison.Ordinal)
                    || string.Equals(p, "VirtualTable", StringComparison.Ordinal)
                    || string.Equals(p, "VirtualTableAttribute", StringComparison.Ordinal)
                    || string.Equals(p, "ItemType", StringComparison.Ordinal)
                    || string.Equals(p, "ItemTypeAttribute", StringComparison.Ordinal)
                    || string.Equals(p, "LogTable", StringComparison.Ordinal)
                    || string.Equals(p, "LogTableAttribute", StringComparison.Ordinal)
                    )))
                {
                    Candidates.Add(classDeclarationSyntax);
                }
            }
        }

        class AttributesCache
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
            }
        }
    }


}