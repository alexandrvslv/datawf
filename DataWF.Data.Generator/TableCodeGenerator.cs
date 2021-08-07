﻿using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DataWF.Common.Generator;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataWF.Data.Generator
{
    internal class TableCodeGenerator : BaseTableCodeGenerator
    {
        private const string constTable = "Table";
        private const string constDBLogItem = "DBLogItem";
        private const string constDBTable = "DBTable";
        private const string constCodeKey = "CodeKey";
        private const string constFileNameKey = "FileNameKey";
        private const string constFileLastWriteKey = "FileLastWriteKey";

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
                    className = classSymbol.Name + constTable;
                }
            }

            var itemTypeAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.VirtualTable, SymbolEqualityComparer.Default));
            if (itemTypeAttribute != null)
            {
                className = classSymbol.Name + constTable;
            }

            var logTableAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.LogTable, SymbolEqualityComparer.Default));
            if (logTableAttribute != null)
            {
                className = classSymbol.Name + constTable;
            }

            var logItemTypeAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.LogItemType, SymbolEqualityComparer.Default));
            if (logItemTypeAttribute != null)
            {
                className = classSymbol.Name + constTable;
            }

            var abstractAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.AbstractTable, SymbolEqualityComparer.Default));
            if (abstractAttribute != null)
            {
                className = classSymbol.Name + constTable;
            }

            return className;
        }
        
        protected static bool IsNewProperty(string keyPropertyName)
        {
            return string.Equals(keyPropertyName, constCodeKey, StringComparison.Ordinal)
                || string.Equals(keyPropertyName, constFileNameKey, StringComparison.Ordinal)
                || string.Equals(keyPropertyName, constFileLastWriteKey, StringComparison.Ordinal);
        }

        private string baseClassName;
        private string baseInterfaceName;
        private string className;
        private string interfaceName;
        private string containerSchema;
        private string namespaceName;
        private string whereName;
        private string genericArg;
        protected StringBuilder interfaceSource;

        public TableCodeGenerator(ref GeneratorExecutionContext context, Compilation compilation) : base(ref context, compilation)
        {
            InvokerCodeGenerator = new InvokerCodeGenerator(ref context, compilation);
        }

        public InvokerCodeGenerator InvokerCodeGenerator { get; set; }
        public LogItemCodeGenerator LogItemCodeGenerator { get; set; }
        public SyntaxReceiver Receiver { get; internal set; }

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
                        System.Diagnostics.Debugger.Launch();
                    }
#endif
                }
            }
            return false;
        }

        private bool GenerateNames()
        {
            className = GetTableClassName(classSymbol, attributes);
            if (className == null)
            {
                return false;
            }
            interfaceName = "I" + className;

            baseClassName = constDBTable;
            baseInterfaceName = "IDBTable";

            namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            containerSchema = null;
            if (!classSymbol.IsAbstract)
            {
                containerSchema = Receiver?.SchemaCandidates?
                    .FirstOrDefault(p => string.Equals(p.GetNamespace()?.Name.ToString(), namespaceName, StringComparison.Ordinal))
                    ?.Identifier.ToString();

            }
            
            whereName = null;
            genericArg = classSymbol.Name;
            if (!classSymbol.IsSealed)
            {
                genericArg = "T";
                className += "<T>";
                whereName = $"where T: {classSymbol.Name}";
            }
            
            if (classSymbol.BaseType.Name == constDBLogItem)
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
                    baseClassName = classSymbol.BaseType.Name + constTable;
                    baseInterfaceName = "I" + baseClassName;
                }
                else
                {
                    baseClassName = $"{baseNamespace}.{classSymbol.BaseType.Name}Table";
                    baseInterfaceName = $"{baseNamespace}.I{classSymbol.BaseType.Name}Table";
                }
            }
            return true;
        }

        public override string Generate()
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            if (!GenerateNames())
            {
                return null;
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

            ProcessTableContainerSchema();
            source.Append(@"
    }");
            interfaceSource.Append(@"
    }
");
            source.Append(interfaceSource);

            ProcessClassPartial();

            source.Append(@"
}");
            return source.ToString();
        }

        private void ProcessClassPartial()
        {
            source.Append($@"
    public partial class {classSymbol.Name}
    {{
        [JsonIgnore]
        public new {(classSymbol.IsSealed ? className : interfaceName)} Table
        {{
            get => ({(classSymbol.IsSealed ? className : interfaceName)})base.Table;
            set => base.Table = value;
        }}");
            ProcessClassContainerSchema();

            source.Append($@"
    }}");
        }

        private void ProcessTableContainerSchema()
        {
            if (containerSchema != null)
            {
                source.Append($@"
        [JsonIgnore]
        public new I{containerSchema} Schema
        {{
            get => (I{containerSchema})base.Schema;
            set => base.Schema = value;
        }}");
            }
        }

        private void ProcessClassContainerSchema()
        {
            if (containerSchema != null)
            {
                source.Append($@"
        [JsonIgnore]
        public new I{containerSchema} Schema
        {{
            get => (I{containerSchema})base.Schema;
        }}");
            }
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
        public {(IsNewProperty(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => {keyFieldName} ??= ParseColumn<{propertyType}>(""{sqlName.Value}_{culture.ToLowerInvariant()}"");
");
                        interfaceSource.Append($@"
        {(IsNewProperty(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} {{ get; }}");
                    }
                }
                else
                {
                    string keyPropertyName = $"{propertyName}Key";
                    source.Append($@"
        private DBColumn<{propertyType}> {keyFieldName};
        [JsonIgnore]
        public {(IsNewProperty(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => {keyFieldName} ??= ParseProperty<{propertyType}>(nameof({propertySymbol.ContainingType.Name}.{propertyName}));
");
                    interfaceSource.Append($@"
        {(IsNewProperty(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} {{ get; }}");
                }
            }

            var logColumnAttribute = propertySymbol.GetAttribute(attributes.LogColumn);
            if (logColumnAttribute != null)
            {
                string keyPropertyName = $"{propertyName}Key";
                source.Append($@"
        private DBColumn<{propertyType}> {keyFieldName};
        [JsonIgnore]
        public {(IsNewProperty(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => {keyFieldName} ??= ParseProperty<{propertyType}>(nameof({propertySymbol.ContainingType.Name}.{propertyName}));
");
                interfaceSource.Append($@"
        {(IsNewProperty(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} {{ get; }}");
            }

            //ProcessReferencePropertyTable(propertySymbol, propertyType);

            //ProcessReferecingPropertyTable(propertySymbol, propertyType);
        }

        private void ProcessReferencePropertyTable(IPropertySymbol propertySymbol, ITypeSymbol propertyType)
        {
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
        }

        private void ProcessReferecingPropertyTable(IPropertySymbol propertySymbol, ITypeSymbol propertyType)
        {
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