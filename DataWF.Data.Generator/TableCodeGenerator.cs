using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DataWF.Common.Generator;

namespace DataWF.Data.Generator
{
    internal class TableCodeGenerator : BaseTableCodeGenerator
    {
        protected static string GetTableClassName(INamedTypeSymbol classSymbol, AttributesCache attributes)
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

        protected static bool IsNew(string keyPropertyName)
        {
            return string.Equals(keyPropertyName, "CodeKey", StringComparison.Ordinal)
                || string.Equals(keyPropertyName, "FileNameKey", StringComparison.Ordinal)
                || string.Equals(keyPropertyName, "FileLastWriteKey", StringComparison.Ordinal);
        }

        protected StringBuilder interfaceSource;

        public TableCodeGenerator(ref GeneratorExecutionContext context, Compilation compilation) : base(ref context, compilation)
        {
            InvokerCodeGenerator = new InvokerCodeGenerator(ref context, compilation);
        }

        public InvokerCodeGenerator InvokerCodeGenerator { get; set; }
        public LogItemCodeGenerator LogItemCodeGenerator { get; set; }

        public override bool Process(INamedTypeSymbol classSymbol)
        {
            ClassSymbol = classSymbol;
            Properties = ClassSymbol.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic).ToList();
            string classSource = Generate();
            if (classSource != null)
            {
                try
                {
                    Context.AddSource($"{ClassSymbol.Name}TableGen.cs", SourceText.From(classSource, Encoding.UTF8));

                    var invokerAttribute = ClassSymbol.GetAttribute(InvokerCodeGenerator.AtributeType);
                    if (invokerAttribute == null)
                    {
                        InvokerCodeGenerator.Process(classSymbol);
                    }

                    LogItemCodeGenerator?.Process(classSymbol);

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Generator Fail: {ex.Message} at {ex.StackTrace}");
#if DEBUG
                    if (!System.Diagnostics.Debugger.IsAttached)
                    {
                        //System.Diagnostics.Debugger.Launch();
                    }
#endif
                }
            }
            return false;
        }

        public override string Generate()
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

            if (className == null)
            {
                return null;
            }
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
            source = new StringBuilder($@"using System.Text.Json.Serialization;
using DataWF.Common;
{(namespaceName != "DataWF.Data" ? "using DataWF.Data;" : string.Empty)}
");

            source.Append($@"
namespace {namespaceName}
{{
    public {(classSymbol.IsAbstract ? "abstract " : string.Empty)}partial class {className}: {baseClassName}<{genericArg}>{(interfaceName != null ? $", {interfaceName}" : string.Empty)} {whereName}
    {{");
            interfaceSource = new StringBuilder($@"
    public partial interface {interfaceName}: {baseInterfaceName}
    {{");
            // create properties for each field 
            foreach (IPropertySymbol propertySymbol in properties)
            {
                ProcessColumnProperty(propertySymbol);
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
        public new {(classSymbol.IsSealed ? className : interfaceName)} Table
        {{
            get => ({(classSymbol.IsSealed ? className : interfaceName)})base.Table;
            set => base.Table = value;
        }}
    }}
");
            source.Append(@"
}");
            return source.ToString();
        }

        private void ProcessColumnProperty(IPropertySymbol propertySymbol)
        {
            // get the name and type of the field
            string propertyName = propertySymbol.Name;
            ITypeSymbol propertyType = propertySymbol.Type;
            string keyFieldName = $"_{propertyName}Key";

            // get the AutoNotify attribute from the field, and any associated data
            AttributeData columnAttribute = propertySymbol.GetAttribute(attributes.Column);
            if (columnAttribute != null)
            {
                var overridenPropertyType = columnAttribute.GetNamedValue("DataType");
                if (!overridenPropertyType.IsNull)
                    propertyType = (ITypeSymbol)overridenPropertyType.Value;

                TypedConstant sqlName = columnAttribute.GetNamedValue("ColumnName");
                if (sqlName.IsNull)
                {
                    sqlName = columnAttribute.ConstructorArguments.FirstOrDefault();
                }

                TypedConstant keys = columnAttribute.GetNamedValue("Keys");
                if (!keys.IsNull && ((int)keys.Value & (1 << 16)) != 0)
                {
                    foreach (var culture in cultures)
                    {
                        keyFieldName = $"_{propertyName}{culture}Key";
                        string keyPropertyName = $"{propertyName}{culture}Key";
                        source.Append($@"
        private DBColumn<{propertyType}> {keyFieldName};
        [JsonIgnore]
        public {(IsNew(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => {keyFieldName} ??= ParseColumn<{propertyType}>(""{sqlName.Value}_{culture.ToLowerInvariant()}"");
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
        public {(IsNew(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => {keyFieldName} ??= ParseProperty<{propertyType}>(nameof({propertySymbol.ContainingType.Name}.{propertyName}));
");
                    interfaceSource.Append($@"
        {(IsNew(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} {{ get; }}");
                }
            }

            var logColumnAttribute = propertySymbol.GetAttribute(attributes.LogColumn);
            if (logColumnAttribute != null)
            {
                string keyPropertyName = $"{propertyName}Key";
                source.Append($@"
        private DBColumn<{propertyType}> {keyFieldName};
        [JsonIgnore]
        public {(IsNew(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => {keyFieldName} ??= ParseProperty<{propertyType}>(nameof({propertySymbol.ContainingType.Name}.{propertyName}));
");
                interfaceSource.Append($@"
        {(IsNew(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} {{ get; }}");
            }
            var referenceAttribute = propertySymbol.GetAttribute(attributes.Reference);
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
            var referencingAttribute = propertySymbol.GetAttribute(attributes.Referencing);
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

    }

}