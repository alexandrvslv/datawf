using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataWF.Generator
{
    internal class SchemaControllerGenerator : BaseGenerator
    {
        private const string prStream = "uploaded";
        private const string prUser = "CurrentUser";
        private const string prTransaction = "transaction";
        private readonly HashSet<string> generated = new(StringComparer.Ordinal);
        private static readonly string[] tableAttributes = new[] {
            Attributes.Table,
            Attributes.VirtualTable,
            Attributes.VirtualTable,
            Attributes.AbstractTable,
            Attributes.LogTable };
        private EquatableList<PropertyRecord> schemaEntries;

        public SchemaControllerGenerator(TypeRecordProvider provider, INamedTypeSymbol typeSymbol)
            : base(provider, typeSymbol, TakeBaseMode.Recursive, null)
        {
            var attribute = typeSymbol.GetAttribute(Attributes.SchemaController);
            var schemaType = attribute?.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol;
            schemaEntries = schemaType?.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.Type.IsBaseType("ModelTable"))
                .Select(p => new PropertyRecord(p, provider))
                .ToEquatableList()
                ?? EquatableList<PropertyRecord>.Empty;
        }

        public override bool ProcessInternal(ref SourceProductionContext context)
        {
            foreach (var schemaProperty in schemaEntries)
            {
                var itemType = schemaProperty.Type.GenericParameters?.FirstOrDefault()?.Type
                    ?? schemaProperty.Type.BaseType?.GenericParameters?.FirstOrDefault()?.Type
                    ?? (schemaProperty.Type.GetAttribute("ItemType")?.ConstructorArguments.FirstOrDefault().Value is ITypeSymbol typeSymbol
                    ? provider.GetOrCreate(
                        typeSymbol: typeSymbol,
                        takeBase: TakeBaseMode.Recursive | TakeBaseMode.Property | TakeBaseMode.Method | TakeBaseMode.Attribute,
                        validateProperty: TypeRecord.AllProperties,
                        validateMethod: TypeRecord.AllMethods,
                        validateAttribute: TypeRecord.AllAttributes)!.Type
                    : null);
                if (itemType != null)
                {
                    ProcessController(schemaProperty.Type, itemType.Type!, ref context);
                }
            }
            return true;
        }

        private void ProcessController(TypeRecord? tableType, TypeRecord type, ref SourceProductionContext context)
        {
            if (string.Equals(type.BaseType?.Name, Helper.cLogItem, StringComparison.Ordinal)
                || type.Name.Equals(Helper.cDBItem, StringComparison.Ordinal)
                || type.Name.Equals(Helper.cDBGroupItem, StringComparison.Ordinal)
                || type.Name.EndsWith(Helper.cLog, StringComparison.Ordinal)
                || generated.Contains(type.Name))
            {
                return;
            }

            if (type.BaseType != null && type.BaseType != TypeRecord.Object)
            {
                ProcessController(tableType?.BaseType, type.BaseType, ref context);
            }

            var attribute = type.Attributes?.First();
            var usings = new Dictionary<string, string>();

            var controllerClassName = $"{type.Name}Controller";

            if (attribute != null && string.Equals(attribute.Class?.Name, Helper.cTableAttribute, StringComparison.Ordinal))
            {
                var keysArg = attribute.GetNamedValue(Helper.cKeys);
                if (!keysArg.IsNull && ((int?)keysArg.Value & (1 << 3)) != 0)
                    return;
            }
            string baseName = $"BaseController";
            if (!string.Equals(type.BaseType?.Name, Helper.cDBItem, StringComparison.Ordinal)
                && !string.Equals(type.BaseType?.Name, Helper.cDBGroupItem, StringComparison.Ordinal))
            {
                baseName = $"{type.BaseType?.Name}Controller";
            }

            var logTypeSymbol = tableType?.GetAttribute("LogType")?.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol;
            var logType = logTypeSymbol != null ? provider.GetOrCreate(logTypeSymbol)!.Type : null;
            var logTypeName = logType?.Name;


            var providerType = Helper.cIModelProvider;
            var primaryKey = type.GetPrimaryKey();
            var keyType = primaryKey?.GetTupleType().ToString();

            var source = new StringBuilder($$"""
                //Source generator for {{type.Name}}
                using DataWF;
                using DataWF.WebService.Common;
                using Microsoft.AspNetCore.Mvc;
                using Microsoft.AspNetCore.Authentication.JwtBearer;
                using Microsoft.AspNetCore.Authorization;
                using System;
                using System.IO;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using {{type.Namespace}};
                {{(tableType != null && type.Namespace != tableType.Namespace ? $"using {tableType.Namespace};" : string.Empty)}}

                namespace {{Namespace}}
                {
                """);
            if (!type.IsSealed)
            {
                source.Append($"""

                        //Prototype Controller    
                        public {(type.IsAbstract ? "abstract " : string.Empty)}partial class {controllerClassName}<T, K{(logType != null ? ", L" : string.Empty)}>
                            : {baseName}<T, K{(logType != null ? ", L" : string.Empty)}> 
                            where T:{type.Name}
                            {(logType != null ? $"where L:{logType.Name}" : string.Empty)}
                    """);
            }
            else
            {
                if (primaryKey == null)
                {
                    return;
                }

                source.Append($"""

                        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
                        [Route("api/[controller]")]
                        [ApiController]
                        public partial class {controllerClassName}: {baseName}<{type.Name}, {keyType}{(logType != null ? ", " + logTypeName : string.Empty)}>
                    """);
            }
            source.Append($$"""

                    {
                        public {{controllerClassName}}({{providerType}} provider) :base(provider)
                        { }
                """);

            if (tableType != null)
            {
                source.Append($"""

                            public new {tableType} Table => ({tableType})base.Table;
                    """);
            }

            foreach (var method in type.Methods!)
            {
                var controllerMethodAttribute = method.GetAttribute(Attributes.ControllerMethod);
                if (controllerMethodAttribute != null)
                {
                    ProcessControllerMethod(source, method, controllerMethodAttribute, keyType, true);
                }
            }

            if (tableType != null)
            {
                foreach (var method in tableType.Methods!)
                {
                    var controllerMethodAttribute = method.GetAttribute(Attributes.ControllerMethod);
                    if (controllerMethodAttribute != null)
                    {
                        ProcessControllerMethod(source, method, controllerMethodAttribute, keyType, false);
                    }
                }
            }
            source.Append("""

                    }
                """);
            if (!type.IsAbstract && !type.IsSealed)
            {
                if (primaryKey != null)
                {
                    source.Append($$"""

                            [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
                            [Route("api/[controller]")]
                            [ApiController]
                            public partial class {{controllerClassName}}: {{controllerClassName}}<{{type.Name}}, {{keyType}}{{(logType != null ? ", " + logTypeName : string.Empty)}}>
                            {
                                public {{controllerClassName}}({{providerType}} provider) :base(provider)
                                { }
                            }
                        """);
                }
            }
            source.Append("""

                }
                """);
            context.AddSource($"{type.Name}Controller", SourceText.From(source.ToString(), Encoding.UTF8));
            generated.Add(type.Name);
        }

        private void ProcessControllerMethod(StringBuilder source, MethodRecord method, AttributeRecord controllerMethodAttribute, string? keyType, bool inLine)
        {
            var isHtmlArg = controllerMethodAttribute.NamedArguments.FirstOrDefault(p => string.Equals(p.Key, "ReturnHtml", StringComparison.Ordinal)).Value;
            var isHtml = !isHtmlArg.IsNull && isHtmlArg.Value is true;
            var isAnonArg = controllerMethodAttribute.NamedArguments.FirstOrDefault(p => string.Equals(p.Key, "Anonymous", StringComparison.Ordinal)).Value;
            var isAnon = !isAnonArg.IsNull && isAnonArg.Value is true;
            var isVoid = method.ReturnsVoid;
            var returnResultType = method.ReturnType;
            var isAsync = returnResultType.Name == "Task";

            var returnType = method.ReturnsVoid ? "IActionResult"
                : isHtml ? "IActionResult"
                : $"ActionResult<{method.ReturnType}>";
            if (isAsync)
            {
                if (isHtml)
                    returnType = "Task<IActionResult>";
                if (!isVoid)
                {
                    returnType = "Task<ActionResult>";
                    if (method.ReturnType.GenericParameters is { } genericParameters
                        && genericParameters.Count > 0)
                    {
                        returnResultType = genericParameters.First()?.Type;
                        returnType = $"Task<ActionResult<{returnResultType}>>";
                    }
                    else
                    {
                        isVoid = true;
                    }
                }
            }

            var parameters = GetParametersInfo(method);
            var isTransact = method.Parameters!.Any(p => string.Equals(p.Type?.Name, Helper.cDBTransaction, StringComparison.Ordinal));

            source.Append($"""

                        [
                """);
            ProcessControllerMethodAttributes(source, method, parameters, inLine, isAnon);
            source.Append("]");
            source.Append($"""

                        public {(isAsync ? "async " : string.Empty)}{returnType} {method.Name}(
                """);

            var position = source.Length;
            if (inLine && keyType != null)
            {
                source.Append($"[FromQuery] {keyType} id, ");
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
            source.Append("""
                )
                        {
                """);
            if (inLine && keyType != null)
            {
                source.Append($"""

                                var idValue = Table.LoadById(id, DBLoadParam.Load | DBLoadParam.Referencing);
                                if (idValue == null) return NotFound();
                    """);
            }
            if (isTransact)
            {
                source.Append($$"""

                                using(var {{prTransaction}} = new DBTransaction(Table, {{prUser}}))
                                {
                    """);
            }

            source.Append("""

                                try 
                                {
                """);
            var parametersBuilder = new StringBuilder();
            foreach (var parameter in parameters)
            {
                if (parameter.Table)
                {
                    source.Append($"""

                                            var {parameter.ValueName} = Schema.GetTable<{parameter.Info.Type}>().LoadById({parameter.Info.Name}, DBLoadParam.Load | DBLoadParam.Referencing);
                        """);
                }
                else if (string.Equals(parameter.ValueName, prStream, StringComparison.Ordinal))
                {
                    if (isAsync)
                    {
                        source.Append($"""

                                                var {parameter.ValueName} = (await Upload(true))?.Stream;
                            """);
                    }
                    else
                    {
                        source.Append($"""

                                                var {parameter.ValueName} = Upload(true).GetAwaiter().GetResult()?.Stream;
                            """);
                    }
                }
                parametersBuilder.Append($"{parameter.ValueName}, ");
            }
            if (parametersBuilder.Length > 0)
            {
                parametersBuilder.Length -= 2;
            }

            if (returnResultType!.IsSubclassOf(Helper.cStream) == true)
            {
                source.Append($"""

                                        var exportStream = {(isAsync ? "(await " : "")}{(!inLine ? Helper.cTable : " idValue")}.{method.Name}({parametersBuilder}){(isAsync ? ")" : "")} as FileStream;
                    """);
                if (isTransact)
                {
                    source.Append($"""

                                            {prTransaction}.Commit();
                        """);
                }
                source.Append($$"""

                                        return new FileStreamResult(exportStream, System.Net.Mime.MediaTypeNames.Application.Octet){ FileDownloadName = Path.GetFileName(exportStream.Name) };
                    """);
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

                source.Append($"""

                                        {builder}
                    """);
                if (isTransact)
                {
                    source.Append($"""

                                            {prTransaction}.Commit();
                        """);
                }
                if (returnResultType!.IsEnumerable == true)
                {
                    source.Append($"""

                                            result = Pagination(result);
                        """);
                }
                if (!isVoid)
                {
                    if (isHtml)
                    {
                        source.Append($$"""

                                                return new ContentResult { ContentType = "text/html", Content = result };
                            """);
                    }
                    else
                    {
                        source.Append($"""

                                                return new ActionResult<{returnResultType}>(result);
                            """);
                    }
                }
                else
                {
                    source.Append($"""

                                            return Ok();
                        """);
                }
            }

            source.Append("""

                                }
                """);
            source.Append("""

                                catch (Exception ex) 
                                {
                """);
            if (isTransact)
            {
                source.Append($"""

                                        {prTransaction}.Rollback();
                    """);
            }
            source.Append("""

                                    return BadRequest(ex);
                """);
            source.Append("""

                                }
                """);
            if (isTransact)
            {
                source.Append("""

                                }
                    """);
            }
            source.Append("""

                        }
                """);
        }

        private void ProcessControllerMethodAttributes(StringBuilder source, MethodRecord method, List<MethodParametrInfo> parametersList, bool inline, bool isAnon)
        {
            if (isAnon)
            {
                source.Append("AllowAnonymous, ");
            }
            var parameters = method.Name + (!inline ? "" : "?={id}");
            var post = false;
            foreach (var parameter in parametersList)
            {
                if (string.Equals(parameter.Info.Type?.Name, Helper.cDBTransaction, StringComparison.Ordinal))
                    continue;
                if (string.Equals(parameter.ValueName, prStream, StringComparison.Ordinal))
                {
                    source.Append("DisableFormValueModelBinding, ");
                    post = true;
                    continue;
                }
                if (!string.Equals(parameter.Type.Name, Helper.cString, StringComparison.Ordinal)
                    && !parameter.Type.IsValueType
                    || parameter.AttributeType != null
                    && parameter.AttributeType == "FromBody")
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

        private List<MethodParametrInfo> GetParametersInfo(MethodRecord method)
        {
            var parametersInfo = new List<MethodParametrInfo>();
            foreach (var parameter in method.Parameters!)
            {
                parametersInfo.Add(new MethodParametrInfo(parameter));
            }
            return parametersInfo;
        }
    }
}