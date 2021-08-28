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

namespace DataWF.Common.Generator
{
    internal class SchemaControllerGenerator : BaseGenerator
    {
        private const string prStream = "uploaded";
        private const string prUser = "CurrentUser";
        private const string prTransaction = "transaction";
        private readonly HashSet<string> generated = new HashSet<string>(StringComparer.Ordinal);
        private INamedTypeSymbol[] tableAttributes;

        public SchemaControllerGenerator(ref GeneratorExecutionContext context) : base(ref context)
        { 
        }

        public string Namespace { get; private set; }

        public override bool Process()
        {
            Namespace = TypeSymbol.ContainingNamespace.ToDisplayString();
            var attribute = TypeSymbol.GetAttribute(Attributes.SchemaController);
            var schemaType = attribute.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol;
            var schemaEntries = schemaType.GetAllAttributes(Attributes.SchemaEntry);
            tableAttributes = new[] { Attributes.Table, Attributes.VirtualTable, Attributes.VirtualTable, Attributes.AbstractTable,
                Attributes.LogTable };
            foreach (var schemaEntry in schemaEntries)
            {
                var type = schemaEntry.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol;
                ProcessController(type);
            }
            return true;
        }

        public IEnumerable<INamedTypeSymbol> FindTables()
        {
            var tableAttributes = new[] { Attributes.Table, Attributes.AbstractTable, Attributes.VirtualTable };
            foreach (var assemblyReference in Compilation.References)
            {
                var assembly = Compilation.GetAssemblyOrModuleSymbol(assemblyReference);
                if (assembly is IAssemblySymbol assemblySymbol)
                {
                    var moduleInitialize = assemblySymbol.GetAttribute(Attributes.ModuleInitialize);
                    if (moduleInitialize != null || assemblySymbol.Name == "DataWF.Data")
                    {
                        foreach (var type in assemblySymbol.GlobalNamespace.GetTypes())
                        {
                            var tableAttribute = type.GetAttribute(tableAttributes);
                            if (tableAttribute != null)
                            {
                                yield return type;
                            }
                        }
                    }
                }
            }
        }

        private void ProcessController(INamedTypeSymbol type)
        {
            if (type.BaseType.Name.Equals("DBItemLog", StringComparison.Ordinal)
                || type.Name.Equals("DBItem", StringComparison.Ordinal)
                || type.Name.Equals("DBGroupItem", StringComparison.Ordinal)
                || type.Name.EndsWith("Log", StringComparison.Ordinal)
                || generated.Contains(type.Name))
            {
                return;
            }
            ProcessController(type.BaseType);
            Dictionary<string, string> usings = new Dictionary<string, string>();

            var attribute = type.GetAttribute(tableAttributes);
            var controllerClassName = $"{type.Name}Controller";


            if (attribute.AttributeClass.Name == "TableAttribute")
            {
                var keysArg = attribute.GetNamedValue("Keys");
                if (!keysArg.IsNull && ((int)keysArg.Value & (1 << 3)) != 0)
                    return;
            }
            string baseName = $"BaseController";
            if (type.BaseType.Name != "DBItem" && type.BaseType.Name != "DBGroupItem")
            {
                baseName = $"{type.BaseType.Name}Controller";
            }
            var typeNamespace = type.ContainingNamespace.ToDisplayString();
            var usingNamespace = typeNamespace == "DataWF.Data" ? "" : $"using {typeNamespace};";
            var logType = Compilation.GetTypeByMetadataName($"{typeNamespace}.{type.Name}Log")
                ?? Compilation.GetTypeByMetadataName($"{type.BaseType.ContainingNamespace}.{type.BaseType.Name}Log")
                ?? Compilation.GetTypeByMetadataName("DataWF.Data.DBItemLog");
            var logTypeName = logType.Name;

            var tableTypeName = $"{type.Name}Table";
            var tableType = Compilation.GetTypeByMetadataName($"{typeNamespace}.{tableTypeName}");
            var tableIsGeneric = false;
            if (tableType == null)
            {
                tableType = Compilation.GetTypeByMetadataName($"{typeNamespace}.{tableTypeName}`1");
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
{usingNamespace}

namespace {Namespace}
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
                var primaryKey = type.GetPrimaryKey();
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
                var controllerMethodAttribute = method.GetAttribute(Attributes.ControllerMethod);
                if (controllerMethodAttribute != null)
                {
                    ProcessControllerMethod(source, method, controllerMethodAttribute, keyType, true);
                }
            }

            if (tableType != null)
            {
                foreach (var method in tableType.GetMembers().OfType<IMethodSymbol>().Where(p => p.MethodKind == MethodKind.Ordinary))
                {
                    var controllerMethodAttribute = method.GetAttribute(Attributes.ControllerMethod);
                    if (controllerMethodAttribute != null)
                    {
                        ProcessControllerMethod(source, method, controllerMethodAttribute, keyType, false);
                    }
                }
            }
            source.Append(@"
    }");
            if (!type.IsAbstract && !type.IsSealed)
            {
                var primaryKey = type.GetPrimaryKey();
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
            Context.AddSource($"{type.Name}Controller", SourceText.From(source.ToString(), Encoding.UTF8));
            generated.Add(type.Name);
        }

        private void ProcessControllerMethod(StringBuilder source, IMethodSymbol method, AttributeData controllerMethodAttribute, string keyType, bool inLine)
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

            var parameters = GetParametersInfo(method);
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

            if (returnResultType.IsBaseType("Stream"))
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
                if (returnResultType.IsEnumerable())
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

        private List<MethodParametrInfo> GetParametersInfo(IMethodSymbol method)
        {
            var parametersInfo = new List<MethodParametrInfo>();
            foreach (var parameter in method.Parameters)
            {
                parametersInfo.Add(new MethodParametrInfo(parameter));
            }
            return parametersInfo;
        }



    }
}