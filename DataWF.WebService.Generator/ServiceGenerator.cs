using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DataWF.WebService.Generator
{
    [Generator]
    public partial class ServiceGenerator : ISourceGenerator
    {
        private const string prStream = "uploaded";
        private const string prUser = "CurrentUser";
        private const string prTransaction = "transaction";
        private readonly Dictionary<string, ClassDeclarationSyntax> controllers = new Dictionary<string, ClassDeclarationSyntax>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, UsingDirectiveSyntax>> controllersUsings = new Dictionary<string, Dictionary<string, UsingDirectiveSyntax>>(StringComparer.Ordinal);
        public List<Assembly> Assemblies { get; private set; }
        public string Output { get; }
        public string Namespace { get; private set; }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retreive the populated receiver
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            var nameSpace = receiver.NameSpaces.FirstOrDefault() ?? "Controller";
            var moduleInitializeAtributeType = context.Compilation.GetTypeByMetadataName("DataWF.Common.ModuleInitializeAttribute");
            var attributeTypes = new AttributeTypes();
            attributeTypes.Table = context.Compilation.GetTypeByMetadataName("DataWF.Data.TableAttribute");
            attributeTypes.AbstractTable = context.Compilation.GetTypeByMetadataName("DataWF.Data.AbstractTableAttribute");
            attributeTypes.VirtualTable = context.Compilation.GetTypeByMetadataName("DataWF.Data.VirtualTableAttribute");
            attributeTypes.Column = context.Compilation.GetTypeByMetadataName("DataWF.Data.ColumnAttribute");
            attributeTypes.ControllerMethod = context.Compilation.GetTypeByMetadataName("DataWF.Data.ControllerMethodAttribute");
            attributeTypes.ControllerParameter = context.Compilation.GetTypeByMetadataName("DataWF.Data.ControllerParameterAttribute");
            attributeTypes.Schema = context.Compilation.GetTypeByMetadataName("DataWF.Data.SchemaAttribute");
            attributeTypes.SchemaEntry = context.Compilation.GetTypeByMetadataName("DataWF.Data.SchemaEntryAttribute");
            try
            {
                foreach (var assemblyReference in context.Compilation.References)
                {
                    var assembly = context.Compilation.GetAssemblyOrModuleSymbol(assemblyReference);
                    if (assembly is IAssemblySymbol assemblySymbol)
                    {
                        var moduleInitialize = assemblySymbol.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(moduleInitializeAtributeType, SymbolEqualityComparer.Default));
                        if (moduleInitialize != null || assemblySymbol.Name == "DataWF.Data")
                        {
                            foreach (var type in GetTypes(assemblySymbol.GlobalNamespace))
                            {
                                var tableAttribute = type.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributeTypes.Table, SymbolEqualityComparer.Default)
                                                             || p.AttributeClass.Equals(attributeTypes.AbstractTable, SymbolEqualityComparer.Default)
                                                             || p.AttributeClass.Equals(attributeTypes.VirtualTable, SymbolEqualityComparer.Default));
                                if (tableAttribute != null)
                                {
                                    ProcessController(type, tableAttribute, attributeTypes, nameSpace, context);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
#if DEBUG
                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Debugger.Launch();
                }
#endif
            }
        }

        private IEnumerable<INamedTypeSymbol> GetTypes(INamespaceSymbol nameSpace)
        {
            foreach (var memeber in nameSpace.GetMembers())
            {
                if (memeber is INamespaceSymbol subNamespace)
                {
                    foreach (var subType in GetTypes(subNamespace))
                    {
                        yield return subType;
                    }
                }
                else if (memeber is INamedTypeSymbol type)
                {
                    yield return type;
                }
            }
        }

        private void ProcessController(INamedTypeSymbol type, AttributeData attribute, AttributeTypes attributeTypes, string nameSpace, GeneratorExecutionContext context)
        {
            Dictionary<string, string> usings = new Dictionary<string, string>();

            var controllerClassName = $"{type.Name}Controller";

            if (type.BaseType.Name.Equals("DBItemLog", StringComparison.Ordinal)
                || type.Name.Equals("DBItem", StringComparison.Ordinal)
                || type.Name.Equals("DBGroupItem", StringComparison.Ordinal)
                || type.Name.EndsWith("Log", StringComparison.Ordinal))
            {
                return;
            }
            if (attribute.AttributeClass.Name == "TableAttribute")
            {
                var keysArg = attribute.NamedArguments.FirstOrDefault(p => p.Key == "Keys").Value;
                if (!keysArg.IsNull && ((int)keysArg.Value & (1 << 3)) != 0)
                    return;
            }
            string baseName = $"BaseController";
            if (type.BaseType.Name != "DBItem" && type.BaseType.Name != "DBGroupItem")
            {
                baseName = $"{type.BaseType.Name}Controller";
            }
            var typeNamespace = type.ContainingNamespace.ToDisplayString();

            var logType = context.Compilation.GetTypeByMetadataName($"{typeNamespace}.{type.Name}Log")
                ?? context.Compilation.GetTypeByMetadataName($"{type.BaseType.ContainingNamespace}.{type.BaseType.Name}Log")
                ?? context.Compilation.GetTypeByMetadataName("DataWF.Data.DBItemLog");
            var logTypeName = logType.Name;

            var tableTypeName = $"{type.Name}Table";
            var tableType = context.Compilation.GetTypeByMetadataName($"{typeNamespace}.{tableTypeName}");
            var tableIsGeneric = false;
            if (tableType == null)
            {
                tableType = context.Compilation.GetTypeByMetadataName($"{typeNamespace}.{tableTypeName}`1");
                tableIsGeneric = true;
            }
            if (tableType == null)
            {
                return;
            }
            var tableDeclareType = $"{tableType.Name}{(tableIsGeneric ? "<T>" : string.Empty)}";
            var schemaType = "IDBSchema";
            var keyType = "K";

            var source = new StringBuilder($@"//Source generator for {type.Name}
using DataWF.Common;
using DataWF.Data;
using DataWF.WebService.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using {typeNamespace};

namespace {nameSpace}
{{");
            if (!type.IsSealed)
            {
                source.Append($@"
    //Prototype Controller    
    public {(type.IsAbstract ? "abstract " : string.Empty)}partial class {controllerClassName}<T, K, L>: {baseName}<T, K, L> 
    where T:{type.Name}
    where L:{logType.Name}");
            }
            else
            {
                var primaryKey = GetPrimaryKey(type, attributeTypes);
                if (primaryKey == null)
                {
                    return;
                }
                keyType = primaryKey.Type.ToDisplayString();
                source.Append($@"
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route(""api/[controller]"")]
    [ApiController]
    public partial class {controllerClassName}: {baseName}<{type.Name}, {primaryKey.Type}, {logTypeName}>");
            }
            source.Append($@"
    {{
        public {controllerClassName}({schemaType} schema) :base(schema)
        {{ }}");

            if (tableType != null)
            {
                source.Append($@"
        public new {tableDeclareType} Table => ({tableDeclareType})base.Table;");
            }

            foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(p => p.MethodKind == MethodKind.Ordinary))
            {
                var controllerMethodAttribute = method.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributeTypes.ControllerMethod, SymbolEqualityComparer.Default));
                if (controllerMethodAttribute != null)
                {
                    ProcessControllerMethod(source, method, controllerMethodAttribute, attributeTypes, keyType, true, context);
                }
            }

            if (tableType != null)
            {
                foreach (var method in tableType.GetMembers().OfType<IMethodSymbol>().Where(p => p.MethodKind == MethodKind.Ordinary))
                {
                    var controllerMethodAttribute = method.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributeTypes.ControllerMethod, SymbolEqualityComparer.Default));
                    if (controllerMethodAttribute != null)
                    {
                        ProcessControllerMethod(source, method, controllerMethodAttribute, attributeTypes, keyType, false, context);
                    }
                }
            }
            source.Append(@"
    }");
            if (!type.IsAbstract && !type.IsSealed)
            {
                var primaryKey = GetPrimaryKey(type, attributeTypes);
                if (primaryKey != null)
                {
                    source.Append($@"
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route(""api/[controller]"")]
    [ApiController]
    public partial class {controllerClassName}: {controllerClassName}<{type.Name}, {primaryKey.Type}, {logTypeName}>
    {{
        public {controllerClassName}(DBSchema schema) :base(schema)
        {{ }}
    }}");
                }
            }
            source.Append(@"
}");
            context.AddSource($"{type.Name}Controller", SourceText.From(source.ToString(), Encoding.UTF8));
        }

        private void ProcessControllerMethod(StringBuilder source, IMethodSymbol method, AttributeData controllerMethodAttribute, AttributeTypes attributeTypes, string keyType, bool inLine, GeneratorExecutionContext context)
        {
            var isHtmlArg = controllerMethodAttribute.NamedArguments.FirstOrDefault(p => p.Key == "ReturnHtml").Value;
            var isHtml = !isHtmlArg.IsNull && (bool)isHtmlArg.Value;
            var isAnonArg = controllerMethodAttribute.NamedArguments.FirstOrDefault(p => p.Key == "Anonymous").Value;
            var isAnon = !isAnonArg.IsNull && (bool)isAnonArg.Value;
            var isVoid = method.ReturnsVoid;
            var returnResultType = method.ReturnType;
            var isAsync = returnResultType?.Name == "Task";

            var returnType = method.ReturnsVoid ? "IActionResult"
                : isHtml ? "IActionResult"
                : $"ActionResult<{method.ReturnType}>";
            if (isAsync)
            {
                if (isHtml)
                    returnType = "Task<IActionResult>";
                if (method.ReturnType is INamedTypeSymbol namedTypeSymbol)
                {
                    returnType = "Task<ActionResult>";
                    if (namedTypeSymbol.TypeArguments.Length > 0)
                    {
                        returnResultType = namedTypeSymbol.TypeArguments.First();
                        returnType = $"Task<ActionResult<{returnResultType}>>";
                    }
                    else
                    {
                        isVoid = true;
                    }
                }
            }

            var parameters = GetParametersInfo(method, attributeTypes);
            var isTransact = method.Parameters.Any(p => p.Type.Name == "DBTransaction");

            source.Append($@"
        [");
            ProcessControllerMethodAttributes(source, method, parameters, inLine, isAnon);
            source.Append("]");
            source.Append($@"
        public {(isAsync ? "async " : string.Empty)}{returnType} {method.Name}(");

            var position = source.Length;
            if (inLine)
            {
                source.Append($"[FromRoute] {keyType} id, ");
            }

            foreach (var parameter in parameters)
            {
                if (!parameter.Declare)
                {
                    continue;
                }
                source.Append($"{(parameter.AttributeType != null ? $"[{parameter.AttributeType}]" : string.Empty)} {parameter.Type} {parameter.Info.Name}, ");
            }

            if (position < source.Length)
            {
                source.Length -= 2;
            }
            source.Append(@")
        {");
            if (inLine)
            {
                source.Append($@"
            var idValue = Table.LoadById(id, DBLoadParam.Load | DBLoadParam.Referencing);
            if (idValue == null) return NotFound();");
            }
            if (isTransact)
            {
                source.Append($@"
            using(var {prTransaction} = new DBTransaction(Table, {prUser}))
            {{");
            }

            source.Append(@"
                try 
                {");
            var parametersBuilder = new StringBuilder();
            foreach (var parameter in parameters)
            {
                if (parameter.Table)
                {
                    source.Append($@"
                    var {parameter.ValueName} = Schema.GetTable<{parameter.Info.Type}>().LoadById({parameter.Info.Name}, DBLoadParam.Load | DBLoadParam.Referencing);");
                }
                else if (parameter.ValueName == prStream)
                {
                    if (isAsync)
                    {
                        source.Append($@"
                    var {parameter.ValueName} = (await Upload(true))?.Stream;");
                    }
                    else
                    {
                        source.Append($@"
                    var {parameter.ValueName} = Upload(true).GetAwaiter().GetResult()?.Stream;");
                    }
                }
                parametersBuilder.Append($"{parameter.ValueName}, ");
            }
            if (parametersBuilder.Length > 0)
            {
                parametersBuilder.Length -= 2;
            }

            if (IsBaseType(returnResultType, "Stream"))
            {
                source.Append($@"
                    var exportStream = {(isAsync ? "(await " : "")}{(!inLine ? "Table" : " idValue")}.{method.Name}({parametersBuilder}){(isAsync ? ")" : "")} as FileStream;");
                if (isTransact)
                {
                    source.Append($@"
                    {prTransaction}.Commit();");
                }
                source.Append($@"
                    return new FileStreamResult(exportStream, System.Net.Mime.MediaTypeNames.Application.Octet){{ FileDownloadName = Path.GetFileName(exportStream.Name) }};");
            }
            else
            {
                var builder = new StringBuilder();
                if (!isVoid)
                {
                    builder.Append("var result = ");
                }
                if (isAsync)
                {
                    builder.Append("await ");
                }

                builder.Append($"{(!inLine ? "Table" : "idValue")}.{method.Name}({parametersBuilder}");
                builder.Append(");");

                source.Append($@"
                    {builder}");
                if (isTransact)
                {
                    source.Append($@"
                    {prTransaction}.Commit();");
                }
                if (IsEnumerable(returnResultType))
                {
                    source.Append($@"
                    result = Pagination(result);");
                }
                if (!isVoid)
                {
                    if (isHtml)
                    {
                        source.Append($@"
                    return new ContentResult {{ ContentType = ""text/html"", Content = result }};");
                    }
                    else
                    {
                        source.Append($@"
                    return new ActionResult<{returnResultType}>(result);");
                    }
                }
                else
                {
                    source.Append($@"
                    return Ok();");
                }
            }

            source.Append(@"
                }");
            source.Append(@"
                catch (Exception ex) 
                {");
            if (isTransact)
            {
                source.Append($@"
                    {prTransaction}.Rollback();");
            }
            source.Append(@"
                    return BadRequest(ex);");
            source.Append(@"
                }");
            if (isTransact)
            {
                source.Append(@"
            }");
            }
            source.Append(@"
        }");
        }

        private void ProcessControllerMethodAttributes(StringBuilder source, IMethodSymbol method, List<MethodParametrInfo> parametersList, bool inline, bool isAnon)
        {
            if (isAnon)
            {
                source.Append("AllowAnonymous, ");
            }
            var parameters = method.Name + (!inline ? "" : "/{id}");
            var post = false;
            foreach (var parameter in parametersList)
            {
                if (parameter.Info.Type.Name == "DBTransaction")
                    continue;
                if (parameter.ValueName == prStream)
                {
                    source.Append("DisableFormValueModelBinding, ");
                    post = true;
                    continue;
                }
                if (parameter.Type.Name != "string"
                    && !parameter.Type.IsValueType)
                {
                    post = true;
                    continue;
                }
                parameters += $"/{{{parameter.Info.Name}}}";
            }
            source.Append($"Route(\"{parameters}\"), ");
            if (post)
            {
                source.Append($"HttpPost, ");
            }
            else
            {
                source.Append($"HttpGet, ");
            }
            source.Length -= 2;
        }

        private List<MethodParametrInfo> GetParametersInfo(IMethodSymbol method, AttributeTypes attributeTypes)
        {
            var parametersInfo = new List<MethodParametrInfo>();
            foreach (var parameter in method.Parameters)
            {
                parametersInfo.Add(new MethodParametrInfo(parameter, attributeTypes));
            }
            return parametersInfo;
        }

        private static bool IsEnumerable(ITypeSymbol returnResultType)
        {
            if (string.Equals(returnResultType.Name, "string", StringComparison.OrdinalIgnoreCase))
                return false;
            return returnResultType.AllInterfaces.Any(p => p.Name == "IEnumerable" && p.TypeParameters.Length > 0);
        }

        public static bool IsBaseType(ITypeSymbol type, string v)
        {
            if (type.Name == v)
                return true;

            while (type.BaseType != null)
            {
                if (type.BaseType.Name == v)
                    return true;
                type = type.BaseType;
            }
            return false;
        }

        public static IPropertySymbol GetPrimaryKey(ITypeSymbol type, AttributeTypes attributeTypes)
        {
            var property = GetPrimaryKey(type.GetMembers().OfType<IPropertySymbol>());
            if (property != null)
                return property;
            if (type.BaseType != null)
            {
                property = GetPrimaryKey(type.BaseType.GetMembers().OfType<IPropertySymbol>());
                if (property != null)
                    return property;
                type = type.BaseType;
            }
            return null;

            IPropertySymbol GetPrimaryKey(IEnumerable<IPropertySymbol> properties)
            {
                foreach (var property in properties)
                {
                    var columnAttribute = property.GetAttributes().FirstOrDefault(p => p.AttributeClass.Equals(attributeTypes.Column, SymbolEqualityComparer.Default));
                    if (columnAttribute != null)
                    {
                        var keys = columnAttribute.NamedArguments.FirstOrDefault(p => p.Key == "Keys").Value;
                        if (!keys.IsNull && (((int)keys.Value) & (1 << 0)) != 0)
                        {
                            return property;
                        }
                    }
                }
                return null;
            }
        }


    }





}