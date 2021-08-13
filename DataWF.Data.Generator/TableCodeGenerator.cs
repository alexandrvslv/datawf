using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DataWF.Common.Generator;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace DataWF.Data.Generator
{
    internal enum TableCodeGeneratorMode
    {
        Default,
        Virtual,
        Abstract,
        Log
    }
    internal class TableCodeGenerator : BaseTableCodeGenerator
    {
        private static readonly DiagnosticDescriptor diagnosticDescriptor = new DiagnosticDescriptor("TCG001", "Couldn't generate Table", "Couldn't generate Table", nameof(TableCodeGenerator), DiagnosticSeverity.Warning, true);
        private const string constTable = "Table";
        private const string constDBLogItem = "DBItemLog";
        private const string constDBTable = "DBTable";
        private const string constCodeKey = "CodeKey";
        private const string constFileNameKey = "FileNameKey";
        private const string constFileLastWriteKey = "FileLastWriteKey";

        protected static string GetTableClassName(INamedTypeSymbol classSymbol, AttributesCache attributes, out TableCodeGeneratorMode mode)
        {
            mode = TableCodeGeneratorMode.Default;
            string className = null;
            var tableAttribyte = GetDefaultTableAttribute(classSymbol, attributes);
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

            var itemTypeAttribute = GetVirtualTableAttribute(classSymbol, attributes);
            if (itemTypeAttribute != null)
            {
                mode = TableCodeGeneratorMode.Virtual;
                className = classSymbol.Name + constTable;
            }

            var logTableAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.LogTable, SymbolEqualityComparer.Default));
            if (logTableAttribute != null)
            {
                mode = TableCodeGeneratorMode.Log;
                className = classSymbol.Name + constTable;
            }

            var abstractAttribute = classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.AbstractTable, SymbolEqualityComparer.Default));
            if (abstractAttribute != null)
            {
                mode = TableCodeGeneratorMode.Abstract;
                className = classSymbol.Name + constTable;
            }

            return className;
        }

        private static AttributeData GetDefaultTableAttribute(INamedTypeSymbol classSymbol, AttributesCache attributes)
        {
            return classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.Table, SymbolEqualityComparer.Default));
        }

        private static AttributeData GetVirtualTableAttribute(INamedTypeSymbol classSymbol, AttributesCache attributes)
        {
            return classSymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributes.VirtualTable, SymbolEqualityComparer.Default));
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
        private TableCodeGeneratorMode mode;
        private string interfaceName;
        private bool isLogType;
        private string containerSchema;
        private string namespaceName;
        private string whereName;
        private string genericArg;
        protected StringBuilder interfaceSource;

        public TableCodeGenerator(ref GeneratorExecutionContext context, Compilation compilation) : base(ref context, compilation)
        {
            InvokerCodeGenerator = new InvokerCodeGenerator(ref context, compilation);
        }

        public TableLogCodeGenerator TableLogCodeGenerator { get; set; }

        public override bool Process(INamedTypeSymbol classSymbol)
        {
            ClassSymbol = classSymbol;
            Properties = ClassSymbol.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic).ToList();
            string classSource = Generate();
            if (classSource != null)
            {
                try
                {
                    Context.AddSource($"{ClassSymbol.ContainingNamespace.ToDisplayString()}.{ClassSymbol.Name}TableGen.cs", SourceText.From(classSource, Encoding.UTF8));

                    var invokerAttribute = ClassSymbol.GetAttribute(InvokerCodeGenerator.AtributeType);
                    if (invokerAttribute == null)
                    {
                        InvokerCodeGenerator.Process(classSymbol);
                    }

                    if(TableLogCodeGenerator?.Process(classSymbol) == true)
                        Compilation = TableLogCodeGenerator.Compilation;

                    return true;
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, Location.None, classSymbol.Name, ex.Message));
                    
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
            className = GetTableClassName(classSymbol, attributes, out var mode);
            if (className == null)
            {
                return false;
            }
            this.mode = mode;
            interfaceName = "I" + className;
            isLogType = mode == TableCodeGeneratorMode.Log || className.EndsWith("Log", StringComparison.Ordinal);

            baseClassName = constDBTable;
            baseInterfaceName = "IDBTable";

            namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            containerSchema = null;
            foreach (var type in classSymbol.ContainingNamespace.GetTypeMembers())
            {
                if (type.TypeKind == TypeKind.Class
                    && type.AllInterfaces.Any(p => p.Name == "IDBSchema"))
                {
                    var isLogSchema = type.AllInterfaces.Any(p => p.Name == "IDBSchemaLog");
                    if (isLogType == isLogSchema && type.Name != "DBSchema")
                    {
                        containerSchema = type.Name;
                    }
                }
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
                baseClassName = "DBTableLog";
                baseInterfaceName = "IDBTableLog";
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
    public {(classSymbol.IsAbstract ? "abstract " : string.Empty)}partial class {className}: {baseClassName}<{genericArg}>, {interfaceName}
    {whereName}
    {{");
            interfaceSource = new StringBuilder($@"
    public partial interface {interfaceName}: {baseInterfaceName}
    {{");
            // create properties for each field 
            foreach (IPropertySymbol propertySymbol in properties)
            {
                ProcessColumnProperty(propertySymbol);
            }
            interfaceSource.Append(@"
    }");
            ProcessTableContainerSchema();
            ProcessParentTable();
            ProcessTargetTable();
            source.Append(@"
    }");

            source.Append(interfaceSource);

            ProcessClassPartial();

            source.Append(@"
}");
            return source.ToString();
        }

        private void ProcessParentTable()
        {
            if (isLogType)
            {
                source.Append($@"
        public new {baseInterfaceName} ParentTable
        {{
            get => ({baseInterfaceName})base.ParentTable;
            set => base.ParentTable = value;
        }}");
            }
        }

        private void ProcessTargetTable()
        {
            if (interfaceName != null
                && isLogType)
            {
                var targetClass = interfaceName.Replace("Log", "");
                source.Append($@"
        public new {targetClass} TargetTable
        {{
            get => ({targetClass})base.TargetTable;
            set => base.ParentTable = value;
        }}");
            }
        }

        private void ProcessClassPartial()
        {
            source.Append($@"
    public partial class {classSymbol.Name}
    {{");
            if (!classSymbol.Constructors.Any(p => p.Parameters.Any()))
            {
                source.Append($@"
        public {classSymbol.Name}({interfaceName} table): base(table)
        {{ }}");
            }
            source.Append($@"
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
                    var refTableName = GetTableClassName(refItemType, attributes, out var refMode);
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
                    var refTableName = GetTableClassName(refItemType, attributes, out var refModel);
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