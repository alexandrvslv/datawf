using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DataWF.Common.Generator;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace DataWF.Common.Generator
{
    internal enum TableCodeGeneratorMode
    {
        Default,
        Virtual,
        Abstract,
        Log
    }
    internal class TableGenerator : BaseTableGenerator
    {
        private const string constTable = "Table";
        private const string constDBLogItem = "DBItemLog";
        private const string constDBTable = "DBTable";
        private const string constCodeKey = "CodeKey";
        private const string constFileNameKey = "FileNameKey";
        private const string constFileLastWriteKey = "FileLastWriteKey";

        protected static string GetTableClassName(INamedTypeSymbol classSymbol, out TableCodeGeneratorMode mode)
        {
            mode = TableCodeGeneratorMode.Default;
            string className = null;
            var tableAttribyte = GetDefaultTableAttribute(classSymbol);
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

            var itemTypeAttribute = GetVirtualTableAttribute(classSymbol);
            if (itemTypeAttribute != null)
            {
                mode = TableCodeGeneratorMode.Virtual;
                className = classSymbol.Name + constTable;
            }

            var logTableAttribute = classSymbol.GetAttribute(Attributes.LogTable);
            if (logTableAttribute != null)
            {
                mode = TableCodeGeneratorMode.Log;
                className = classSymbol.Name + constTable;
            }

            var abstractAttribute = classSymbol.GetAttribute(Attributes.AbstractTable);
            if (abstractAttribute != null)
            {
                mode = TableCodeGeneratorMode.Abstract;
                className = classSymbol.Name + constTable;
            }

            return className;
        }

        private static AttributeData GetDefaultTableAttribute(INamedTypeSymbol classSymbol)
        {
            return classSymbol.GetAttribute(Attributes.Table);
        }

        private static AttributeData GetVirtualTableAttribute(INamedTypeSymbol classSymbol)
        {
            return classSymbol.GetAttribute(Attributes.VirtualTable);
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

        public TableGenerator(ref GeneratorExecutionContext context, InvokerGenerator invokerGenerator)
            : base(ref context, invokerGenerator)
        {
        }

        public TableLogGenerator TableLogGenerator { get; set; }

        public override bool Process()
        {
            Properties = TypeSymbol.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic).ToList();
            string classSource = Generate();
            if (classSource != null)
            {
                Context.AddSource($"{TypeSymbol.ContainingNamespace.ToDisplayString()}.{TypeSymbol.Name}TableGen.cs", SourceText.From(classSource, Encoding.UTF8));

                var invokerAttribute = TypeSymbol.GetAttribute(Attributes.Invoker);
                if (invokerAttribute == null)
                {
                    InvokerGenerator.Process(TypeSymbol);
                }

                TableLogGenerator?.Process(TypeSymbol);

                return true;
            }

            return false;
        }

        private bool GenerateNames()
        {
            className = GetTableClassName(TypeSymbol, out var mode);
            if (className == null)
            {
                return false;
            }
            this.mode = mode;
            interfaceName = "I" + className;
            isLogType = mode == TableCodeGeneratorMode.Log || TypeSymbol.Name.EndsWith("Log", StringComparison.Ordinal);

            baseClassName = constDBTable;
            baseInterfaceName = "IDBTable";

            namespaceName = TypeSymbol.ContainingNamespace.ToDisplayString();

            containerSchema = null;
            foreach (var type in TypeSymbol.ContainingNamespace.GetTypeMembers())
            {
                if (type.TypeKind == TypeKind.Class
                    && type.AllInterfaces.Any(p => p.Name == "IDBSchema"))
                {
                    var isLogSchema = type.AllInterfaces.Any(p => p.Name == "IDBSchemaLog");
                    if ((isLogSchema == isLogType || isLogType) && type.Name != "DBSchema")
                    {
                        containerSchema = $"{type.Name}{(isLogType && !isLogSchema ? "Log" : "")}";
                        break;
                    }
                }
            }

            whereName = null;
            genericArg = TypeSymbol.Name;
            if (!TypeSymbol.IsSealed)
            {
                genericArg = "T";
                className += "<T>";
                whereName = $"where T: {TypeSymbol.Name}";
            }

            if (TypeSymbol.BaseType.Name == constDBLogItem)
            {
                baseClassName = "DBTableLog";
                baseInterfaceName = "IDBTableLog";
            }
            else //&& classSymbol.BaseType.Name != "DBGroupItem"
            if (TypeSymbol.BaseType.Name != "DBItem")
            {
                var baseNamespace = TypeSymbol.BaseType.ContainingNamespace.ToDisplayString();
                if (baseNamespace == namespaceName
                    || baseNamespace == "<global namespace>")
                {
                    baseClassName = TypeSymbol.BaseType.Name + constTable;
                    baseInterfaceName = "I" + baseClassName;
                }
                else
                {
                    baseClassName = $"{baseNamespace}.{TypeSymbol.BaseType.Name}Table";
                    baseInterfaceName = $"{baseNamespace}.I{TypeSymbol.BaseType.Name}Table";
                }
            }

            return true;
        }

        public virtual string Generate()
        {
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
    public {(TypeSymbol.IsAbstract ? "abstract " : string.Empty)}partial class {className}: {baseClassName}<{genericArg}>, {interfaceName}
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
        [JsonIgnore]    
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
        [JsonIgnore]
        public new {targetClass} TargetTable
        {{
            get => ({targetClass})base.TargetTable;
            set => base.TargetTable = value;
        }}");
            }
        }

        private void ProcessClassPartial()
        {
            source.Append($@"
    public partial class {TypeSymbol.Name}
    {{
        public {TypeSymbol.Name}(IDBSchema schema):base(schema)
        {{}}");
            if (!TypeSymbol.Constructors.Any(p => p.Parameters.Any()))
            {
                source.Append($@"
        public {TypeSymbol.Name}({interfaceName} table): base(table)
        {{ }}");
            }
            source.Append($@"
        [JsonIgnore]
        public new {(TypeSymbol.IsSealed ? className : interfaceName)} Table
        {{
            get => ({(TypeSymbol.IsSealed ? className : interfaceName)})base.Table;
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
            AttributeData columnAttribute = propertySymbol.GetAttribute(Attributes.Column);
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

            var logColumnAttribute = propertySymbol.GetAttribute(Attributes.LogColumn);
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
            var referenceAttribute = propertySymbol.GetAttribute(Attributes.Reference);
            if (referenceAttribute != null)
            {
                if (propertyType is INamedTypeSymbol refItemType)
                {
                    var refTableName = GetTableClassName(refItemType, out var refMode);
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
            var referencingAttribute = propertySymbol.GetAttribute(Attributes.Referencing);
            if (referencingAttribute != null)
            {
                if (propertyType is INamedTypeSymbol named
                    && named.TypeArguments.Length > 0
                    && named.TypeArguments.First() is INamedTypeSymbol refItemType)
                {
                    var refTableName = GetTableClassName(refItemType, out var refModel);
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