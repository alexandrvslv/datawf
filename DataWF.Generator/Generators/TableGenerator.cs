using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Text;

namespace DataWF.Generator;

internal class TableGenerator : ExtendedGenerator
{
    protected static string GetTableClassName(INamedTypeSymbol classSymbol, out TableCodeGeneratorMode mode)
    {
        mode = TableCodeGeneratorMode.Default;
        string className = string.Empty;
        var tableAttribyte = classSymbol.GetAttribute(Helper.cTableAttribute);
        if (tableAttribyte != null)
        {
            var typeName = tableAttribyte.NamedArguments.FirstOrDefault(p => string.Equals(p.Key, Helper.cType, StringComparison.Ordinal)).Value;
            if (!typeName.IsNull && typeName.Value is ITypeSymbol typeSymbol)
            {
                className = typeSymbol.Name;
            }
            else
            {
                className = classSymbol.Name + Helper.cTable;
            }
        }

        var itemTypeAttribute = classSymbol.GetAttribute(Attributes.VirtualTable);
        if (itemTypeAttribute != null)
        {
            mode = TableCodeGeneratorMode.Virtual;
            className = classSymbol.Name + Helper.cTable;
        }

        var logTableAttribute = classSymbol.GetAttribute(Attributes.LogTable);
        if (logTableAttribute != null)
        {
            mode = TableCodeGeneratorMode.Log;
            className = classSymbol.Name + Helper.cTable;
        }

        var abstractAttribute = classSymbol.GetAttribute(Attributes.AbstractTable);
        if (abstractAttribute != null)
        {
            mode = TableCodeGeneratorMode.Abstract;
            className = classSymbol.Name + Helper.cTable;
        }

        return className;
    }

    protected static bool IsNewProperty(string keyPropertyName)
    {
        return string.Equals(keyPropertyName, Helper.cCodeKey, StringComparison.Ordinal)
            || string.Equals(keyPropertyName, Helper.cFileNameKey, StringComparison.Ordinal)
            || string.Equals(keyPropertyName, Helper.cFileLastWriteKey, StringComparison.Ordinal);
    }

    private string baseClassName;
    private string baseInterfaceName;
    private string className;
    private TableCodeGeneratorMode mode;
    private string interfaceName;
    private bool isLogType;
    private string? containerSchema;
    private string? whereName;
    private string genericArg;
    protected StringBuilder? interfaceSource;

    public TableGenerator(TypeRecordProvider provider, INamedTypeSymbol typeSymbol)
        : base(
            provider,
            typeSymbol,
            TakeBaseMode.Recursive | TakeBaseMode.Attribute | TakeBaseMode.Property | TakeBaseMode.PropertyInherited,
            static p => !p.IsStatic)
    {
        className = GetTableClassName(typeSymbol, out var mode);
        this.mode = mode;
        interfaceName = "I" + className;
        isLogType = mode == TableCodeGeneratorMode.Log || TypeName.EndsWith("Log", StringComparison.Ordinal);
        baseClassName = Helper.cModelTable;
        baseInterfaceName = Helper.cIModelTable;

        containerSchema = null;
        foreach (var type in typeSymbol.ContainingNamespace.GetTypeMembers())
        {
            if (type.TypeKind == TypeKind.Class
                && type.AllInterfaces.Any(p => string.Equals(p.Name, Helper.cIModelSchema, StringComparison.Ordinal)))
            {
                var isLogSchema = type.AllInterfaces.Any(p => string.Equals(p.Name, Helper.cIModelLogSchema, StringComparison.Ordinal));
                if ((isLogSchema == isLogType || isLogType) && !string.Equals(type.Name, Helper.cModelSchema, StringComparison.Ordinal))
                {
                    containerSchema = $"{type.Name}{(isLogType && !isLogSchema ? "Log" : "")}";
                    break;
                }
            }
        }
        whereName = null;
        genericArg = TypeName;
        if (!typeSymbol.IsSealed)
        {
            genericArg = "T";
            className += "<T>";
            whereName = $"where T: {TypeName}";
        }
        if (string.Equals(typeSymbol.BaseType?.Name, Helper.cLogItem, StringComparison.Ordinal))
        {
            baseClassName = Helper.cModelLogTable;
            baseInterfaceName = Helper.cIModelLogTable;
        }
        else //&& classSymbol.BaseType.Name != "DBGroupItem"
            if (typeSymbol.BaseType != null
                && !string.Equals(typeSymbol.BaseType.Name, Helper.cDBItem, StringComparison.Ordinal)
                && !string.Equals(typeSymbol.BaseType.Name, Helper.cObject, StringComparison.Ordinal))
            {
                var baseNamespace = typeSymbol.BaseType.ContainingNamespace.ToDisplayString();
                if (baseNamespace == Namespace
                    || baseNamespace == "<global namespace>")
                {
                    baseClassName = typeSymbol.BaseType.Name + Helper.cTable;
                    baseInterfaceName = "I" + baseClassName;
                }
                else
                {
                    baseClassName = $"{baseNamespace}.{typeSymbol.BaseType.Name}Table";
                    baseInterfaceName = $"{baseNamespace}.I{typeSymbol.BaseType.Name}Table";
                }
            }
    }


    public override bool ProcessInternal(ref SourceProductionContext context)
    {

        string classSource = Generate();
        if (classSource != null)
        {
            context.AddSource($"{Namespace}.{TypeName}Table.g.cs", classSource);

            return true;
        }

        return false;
    }


    public virtual string Generate()
    {
        // begin building the generated source
        source = new StringBuilder($@"using System.Text.Json.Serialization;
using DataWF;
{(Namespace != "DataWF.Data" ? "using DataWF.Data;" : string.Empty)}
");

        source.Append($@"
namespace {Namespace}
{{
    public {(Type.IsAbstract ? "abstract " : string.Empty)}partial class {className}: {baseClassName}<{genericArg}>, {interfaceName}
    {whereName}
    {{");
        interfaceSource = new StringBuilder($@"
    public partial interface {interfaceName}: {baseInterfaceName}
    {{");
        // create properties for each field 
        foreach (var property in Type.Properties!)
        {
            ProcessColumnProperty(property);
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
        var baseTypeDeclaration = string.Empty;
        if (string.Equals(Type.BaseType?.Name, Helper.cObject, StringComparison.Ordinal))
            baseTypeDeclaration = $": DBItem";
        source.Append($@"
    public partial class {TypeName}{baseTypeDeclaration}
    {{
        public {TypeName}(IDBSchema schema):base(schema)
        {{}}");
        if (!Type.Constructors.Any(p => p.Parameters.Any()))
        {
            source.Append($@"
        public {TypeName}({interfaceName} table): base(table)
        {{ }}");
        }
        source.Append($@"
        [JsonIgnore]
        public new {(Type.IsSealed ? className : interfaceName)} Table
        {{
            get => ({(Type.IsSealed ? className : interfaceName)})base.Table;
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

    private void ProcessColumnProperty(PropertyRecord propertySymbol)
    {
        // get the name and type of the field
        string propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type;
        string keyFieldName = $"_{propertyName}Key";

        // get the AutoNotify attribute from the field, and any associated data
        var columnAttribute = propertySymbol.GetAttribute(Attributes.Column);
        if (columnAttribute != null)
        {
            var overridenPropertyType = columnAttribute.GetNamedValue("DataType");
            if (!overridenPropertyType.IsNull && overridenPropertyType.Value is ITypeSymbol typeSymbol)
                propertyType = provider.GetOrCreate(typeSymbol)!.Type;

            var sqlName = columnAttribute.GetNamedValue("ColumnName");
            if (sqlName.IsNull)
            {
                sqlName = columnAttribute.ConstructorArguments.FirstOrDefault();
                if (sqlName.IsNull)
                {
                }
            }

            var keys = columnAttribute.GetNamedValue("Keys");
            if (!keys.IsNull && ((int?)keys.Value & (1 << 16)) != 0 && cultures != null)
            {
                foreach (var culture in cultures)
                {
                    keyFieldName = $"_{propertyName}{culture}Key";
                    string keyPropertyName = $"{propertyName}{culture}Key";
                    source.Append($@"
        private DBColumn<{propertyType}> {keyFieldName};
        [JsonIgnore]
        public {(IsNewProperty(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => {keyFieldName} ??= GetColumn<{propertyType}>(""{sqlName.Value}_{culture.ToLowerInvariant()}"");
");
                    interfaceSource!.Append($@"
        {(IsNewProperty(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} {{ get; }}");
                }
            }
            else
            {
                string keyPropertyName = $"{propertyName}Key";
                source.Append($@"
        private DBColumn<{propertyType}> {keyFieldName};
        [JsonIgnore]
        public {(IsNewProperty(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => {keyFieldName} ??= GetColumnByProperty<{propertyType}>(nameof({Type.Name}.{propertyName}));
");
                interfaceSource!.Append($@"
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
        public {(IsNewProperty(keyPropertyName) ? "new " : string.Empty)}DBColumn<{propertyType}> {keyPropertyName} => {keyFieldName} ??= GetColumnByProperty<{propertyType}>(nameof({Type.Name}.{propertyName}));
");
            interfaceSource!.Append($@"
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
                if (refTableName != null && interfaceSource!.ToString().IndexOf(refTableName, StringComparison.Ordinal) < 0)
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
                if (refTableName != null && interfaceSource!.ToString().IndexOf(refTableName, StringComparison.Ordinal) < 0)
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
