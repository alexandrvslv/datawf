using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NJsonSchema;
using NSwag;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DataWF.Generator
{
    internal class WebSchemaGenerator : BaseGenerator
    {
        private const string cList = "List";
        private const string cApi = "api";
        private const string cXId = "x-id";
        private const string cXType = "x-type";
        private const string c200 = "200";
        private const string cWebClient = "WebClient";
        private const string cWebTable = "WebTable";

        private static readonly HashSet<string> VirtualOperations = new(StringComparer.Ordinal)
        {
            "Get",
            "Put",
            "Post",
            "PostPackage",
            "Search",
            "Delete",
            "Copy",
            "GenerateId",
            "Merge",
            "GetItemLogs",
            "GetLogs",
            "RedoLog",
            "RemoveLog",
            "UndoLog"
        };
        private Dictionary<string, TypeRecord?> cacheModels = new(StringComparer.Ordinal);
        private Dictionary<string, TypeRecord> cacheReferences = new(StringComparer.Ordinal);
        private Dictionary<string, Dictionary<string, TypeRecord?>> cacheAssemblySymbolTypes = new(StringComparer.Ordinal);
        private string clientName = string.Empty;
        private string clientType = string.Empty;
        private Dictionary<string, string> clients = new();
        private HashSet<string> usings = new(StringComparer.Ordinal);
        private Dictionary<JsonSchema, List<RefField>> referenceFields = new();
        private OpenApiDocument document = null!;
        private List<IAssemblySymbol> usingReferences = new();

        public WebSchemaGenerator(TypeRecordProvider provider, INamedTypeSymbol typeSymbol, Compilation compilation)
            : base(provider, typeSymbol, TakeBaseMode.Ignore)
        {
            var attribute = Type.GetAttribute(Attributes.WebSchema);
            SchemaName = TypeName;
            DocumentSource = GetDocumentSource(attribute, compilation);

            LoadReferences(attribute, compilation);
        }

        private static string GetDocumentSource(AttributeRecord? attribute, Compilation compilation)
        {
            var documentSource = attribute?.ConstructorArguments.FirstOrDefault().Value?.ToString();
            if (documentSource == null || !documentSource.StartsWith("http", StringComparison.Ordinal))
            {
                var mainSyntaxTree = compilation.SyntaxTrees
                          .First(x => x.HasCompilationUnitRoot);
                var assemblyName = compilation.AssemblyName ?? "";
                var projectDirectory = mainSyntaxTree.FilePath.Substring(0, mainSyntaxTree.FilePath.IndexOf(assemblyName) + assemblyName.Length);
                documentSource = Path.GetFullPath(Path.Combine(projectDirectory, documentSource ?? string.Empty));
            }
            return documentSource;
        }

        private void LoadReferences(AttributeRecord? attribute, Compilation compilation)
        {
            var usingReferenceParam = attribute?.GetNamedValue("UsingReferences").Value?.ToString();
            var usingReferenceNames = usingReferenceParam != null
                ? new HashSet<string>(usingReferenceParam.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                : null;
            foreach (var assemblyReference in compilation.References)
            {
                var assembly = compilation.GetAssemblyOrModuleSymbol(assemblyReference);
                if (assembly is IAssemblySymbol assemblySymbol)
                {
                    if (usingReferenceNames?.Contains(assemblySymbol.Name) ?? false)
                    {
                        usingReferences.Add(assemblySymbol);
                    }
                }
            }
        }

        public InvokerGenerator Invoker { get; set; } = null!;

        public string DocumentSource { get; set; }
        public string SchemaName { get; set; }

        public override bool ProcessInternal(ref SourceProductionContext context)
        {
            source = new StringBuilder();

            LoadDocument();


            foreach (var definition in document.Definitions)
            {
                definition.Value.Id = definition.Key;
            }
            foreach (var definition in document.Definitions)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return false;
                GetOrGenDefinion(definition.Value, out _, ref context);
            }
            foreach (var operation in document.Operations)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return false;
                AddClientOperation(operation, ref context);
            }
            CompileClient(ref context);

            if (context.CancellationToken.IsCancellationRequested)
                return false;

            usings.Add("DataWF");
            usings.Add("System");
            GenUnit($"{Namespace}.{TypeName}.Gen.cs", GenProvider(), usings, ref context);

            return true;
        }

        private void CompileClient(ref SourceProductionContext context)
        {
            if (source.Length > 0)
            {
                clients.Add(clientName, clientType);
                source.Append($$"""

                        }
                    """);
                GenUnit($"{Namespace}.{clientName}Client.g.cs", source, usings, ref context);
            }
            clientName = string.Empty;
        }

        private void LoadDocument()
        {

            var url = new Uri(DocumentSource);
            if (string.Equals(url.Scheme, "http", StringComparison.Ordinal)
                || string.Equals(url.Scheme, "https", StringComparison.Ordinal))
                document = OpenApiDocument.FromUrlAsync(url.OriginalString).GetAwaiter().GetResult();
            else if (string.Equals(url.Scheme, "file", StringComparison.Ordinal))
                document = OpenApiDocument.FromFileAsync(url.LocalPath).GetAwaiter().GetResult();
        }

        private StringBuilder GenProvider()
        {
            source.Clear();
            source.Append($$"""

                    public partial class {{SchemaName}} {{(Type.BaseType?.Name != "ModelSchema" ? ": ModelSchema" : "")}}
                    {
                """);

            GenProviderConstructor();

            foreach (var client in clients)
            {
                source.Append($$"""

                            public {{client.Value}} {{client.Key}} { get; }
                    """);
            }
            source.Append($$"""

                    }
                """);
            return source;
        }

        private void GenProviderConstructor()
        {
            source.Append($$"""

                        public {{SchemaName}}()
                        {
                            Name = "{{SchemaName}}";
                """);

            foreach (var client in clients)
            {
                source.Append($"""

                                Add({client.Key} = new {client.Value}(this));
                    """);
            }
            source.Append($$"""

                            RefreshTypedCache();
                        }
                """);
        }

        private void AddClientOperation(OpenApiOperationDescription descriptor, ref SourceProductionContext context)
        {
            GetOperationName(descriptor, out var clientName);
            if (!string.Equals(clientName, this.clientName, StringComparison.Ordinal))
            {
                CompileClient(ref context);
                GenClient(clientName, usings);
            }

            GenOperation(source, descriptor, usings);
        }

        private void GenClient(string clientName, HashSet<string> usings)
        {
            usings.Add("DataWF");
            usings.Add("System");
            usings.Add("System.Collections.Generic");
            usings.Add("System.Net.Http");
            usings.Add("System.Threading.Tasks");
            usings.Add("System.Text.Json.Serialization");
            var baseType = GetClientBaseType(clientName, usings, out var idKey, out var typeKey, out var typeId, out var postfix);
            this.clientName = clientName;
            this.clientType = $"{clientName}{postfix}";
            source.Clear();
            source.Append($$"""

                    public partial class {{clientType}}: {{baseType}}
                    {
                """);
            GenClientMembers(source, clientName, idKey, typeKey, typeId, usings);
        }

        private void GenClientMembers(StringBuilder clientSource, string clientName, JsonSchemaProperty? idKey, JsonSchemaProperty? typeKey, int typeId,
            HashSet<string> usings)
        {
            document.Definitions.TryGetValue(clientName, out var clientSchema);
            var typeName = $"{clientName}Client";

            var cache = clientSchema != null ? GetClientReferences(clientSchema) : new HashSet<RefField>();

            GenClientConstructor(clientSource, clientName, idKey, typeKey, typeId, cache);

            clientSource.Append($$"""

                        [JsonIgnore]
                        public new {{SchemaName}} Schema
                        {
                            get => ({{SchemaName}})base.Schema;
                            set => base.Schema = value;
                        }
                """);

            if (cache.Count > 0)
            {
                Helper.AddUsing("System.Collections", usings);

                GenClientRemoveOverrideBody(clientSource, clientName, idKey, typeKey, typeId, cache);

                GenClientAddOverrideBody(clientSource, clientName, idKey, typeKey, typeId, cache);
            }
        }

        private void GenClientConstructor(StringBuilder clientSource, string clientName, JsonSchemaProperty? idKey, JsonSchemaProperty? typeKey, int typeId, HashSet<RefField> cache)
        {
            var idName = idKey == null ? null : GetPropertyName(idKey);
            var typeName = typeKey == null ? null : GetPropertyName(typeKey);
            var baseCtor = idKey == null ? "" : $"""
                {clientName}.{idName}Invoker.Instance,
                                  {clientName}.{typeName}Invoker.Instance,
                                  {typeId}
                """;
            clientSource.Append($$"""

                        public {{clientType}}({{SchemaName}} schema)
                            : base({{baseCtor}})
                        {
                            Name = "{{clientName}}";
                            Schema = schema;
                """);

            foreach (var refField in cache)
            {
                clientSource.Append($"""

                                Items.Indexes.Add({refField.InvokerName});
                    """);
            }
            clientSource.Append($$"""

                        }
                """);
        }

        private void GenClientRemoveOverrideBody(StringBuilder clientSource, string clientName, JsonSchemaProperty? idKey, JsonSchemaProperty? typeKey, int typeId, HashSet<RefField> cache)
        {
            clientSource.Append($$"""

                        protected override void OnRemoved(IList items = null)
                        {
                            base.OnRemoved(items);
                            foreach ({{clientName}} item in items)
                            {
                """);
            foreach (var refField in cache)
            {
                clientSource.Append($$"""

                                    if (item.{{refField.KeyName}} != null)
                                    {
                                        var item{{refField.ValueName}} = (item.{{refField.ValueFieldName}} ?? (item.{{refField.ValueFieldName}} = Schema.{{refField.ValueType}}.Select(item.{{refField.KeyName}}.Value))) as {{refField.Definition}};
                                        item{{refField.ValueName}}?.{{refField.PropertyName}}.Remove(item);
                                    }
                    """);
            }
            clientSource.Append($$"""

                            }
                        }
                """);
        }

        private void GenClientAddOverrideBody(StringBuilder clientSource, string clientName, JsonSchemaProperty? idKey, JsonSchemaProperty? typeKey, int typeId, HashSet<RefField> cache)
        {
            clientSource.Append($$"""

                        protected override void OnAdded(IList items = null)
                        {
                            base.OnAdded(items);
                            foreach ({{clientName}} item in items)
                            {
                """);
            foreach (var refField in cache)
            {
                clientSource.Append($$"""

                                    if (item.{{refField.KeyName}} != null)
                                    {
                                        var item{{refField.ValueName}} = (item.{{refField.ValueFieldName}} ?? (item.{{refField.ValueFieldName}} = Schema.{{refField.ValueType}}.Select(item.{{refField.KeyName}}.Value))) as {{refField.Definition}};
                                        item{{refField.ValueName}}?.{{refField.PropertyName}}.Add(item);
                                    }
                    """);
            }
            clientSource.Append($$"""

                            }
                        }
                """);
        }

        private void GenOperation(StringBuilder source, OpenApiOperationDescription descriptor, HashSet<string> usings)
        {
            var operationName = GetOperationName(descriptor, out var clientName);
            var actualName = operationName;
            var baseType = GetClientBaseType(clientName, usings, out _, out _, out _, out _);
            var isOverride = !string.Equals(baseType, cWebClient, StringComparison.Ordinal)
                            && VirtualOperations.Contains(actualName);
            var returnType = GetReturningTypeCheck(descriptor, operationName, usings);
            returnType = returnType.Length > 0 ? $"Task<{returnType}>" : "Task";

            source.Append($"""

                        public {(isOverride ? "override " : "")}async {returnType} {actualName}(
                """);
            GenOperationParameter(source, descriptor, usings);
            source.Append($$"""
                )
                        {
                """);
            GenOperationBody(source, actualName, descriptor, usings, isOverride);
            source.Append($$"""

                        }
                """);
        }

        private void GenOperationParameter(StringBuilder source, OpenApiOperationDescription descriptor, HashSet<string> usings)
        {
            foreach (var parameter in descriptor.Operation.Parameters)
            {
                if (parameter.Kind == OpenApiParameterKind.Header)
                    continue;
                source.Append($"{GetTypeString(parameter, usings, cList)} {parameter.Name}, ");
            }
            var bodyParameter = descriptor.Operation.Parameters.FirstOrDefault(p => p.Kind == OpenApiParameterKind.Body || p.Kind == OpenApiParameterKind.FormData);
            var returnType = GetReturningType(descriptor, usings);
            //if (bodyParameter == null && returnType.StartsWith("List<", StringComparison.Ordinal))
            //{
            //    source.Append($"HttpPageSettings pages, ");
            //}
            //source.Append($"HttpJsonSettings settings, ");
            source.Append($"ProgressToken progressToken");
        }

        private void GenOperationBody(StringBuilder source, string actualName, OpenApiOperationDescription descriptor, HashSet<string> usings, bool isOverride)
        {
            var method = descriptor.Method.ToInitcap();
            var responceSchema = (JsonSchema?)null;
            var mediatype = "application/json";
            if (descriptor.Operation.Responses.TryGetValue(c200, out var responce))
            {
                responceSchema = responce.Schema;
                mediatype = responce.Content.Keys.FirstOrDefault() ?? "application/json";
            }

            var path = new StringBuilder(descriptor.Path);
            foreach (var parameter in descriptor.Operation.Parameters.Where(p => p.Kind == OpenApiParameterKind.Query))
            {
                if (path.Length == descriptor.Path.Length)
                {
                    path.Append("?");
                }
                else
                {
                    path.Append("&");
                }
                path.Append($"{parameter.Name}={{{parameter.Name}}}");
            }

            var returnType = GetReturningType(descriptor, usings);

            source.Append($"""

                            return await Request<{returnType}>(progressToken, HttpMethod.{method}, "{path}", "{mediatype}" 
                """);
            var bodyParameter = descriptor.Operation.Parameters.FirstOrDefault(p => p.Kind == OpenApiParameterKind.Body || p.Kind == OpenApiParameterKind.FormData);
            if (bodyParameter == null)
            {
                //if (returnType.StartsWith("List<", StringComparison.Ordinal))
                //{
                //    source.Append(", pages");
                //}
                //else
                source.Append(", null");
            }
            else
            {
                source.Append($", {bodyParameter.Name}");
            }
            foreach (var parameter in descriptor.Operation.Parameters.Where(p => p.Kind == OpenApiParameterKind.Path || p.Kind == OpenApiParameterKind.Query))
            {
                source.Append($", {parameter.Name}");
            }
            source.Append(").ConfigureAwait(false);");
        }

        //private bool GetOrGenDefinion(string key, out INamedTypeSymbol type)
        //{
        //    return GetOrGenDefinion(document.Definitions[key], out type);
        //}

        private bool GetOrGenDefinion(JsonSchema definition, out TypeRecord? type, ref SourceProductionContext context)
        {
            bool generated = true;
            var key = definition.Id ?? string.Empty;
            if (!cacheModels.TryGetValue(key, out type))
            {
                generated = false;
                cacheModels[key] = null;
                type = GetReferenceType(definition);
                if (type == null)
                {
                    generated = true;
                    cacheModels[key] = type = GenDefinition(definition, ref context);
                }
            }
            else if (type == null)
            {
                generated = false;
                type = GetReferenceType(definition);
            }
            return generated;
        }

        public string GetClientName(OpenApiOperationDescription decriptor)
        {
            if (decriptor.Operation.Tags?.Count > 0)
                return decriptor.Operation.Tags[0];
            else
            {
                foreach (var step in decriptor.Path.SpanSplit('/'))
                {
                    if (MemoryExtensions.Equals(step.Span, cApi.AsSpan(), StringComparison.Ordinal)
                        || (step.Length > 0 && step.Span[0] == '{'))
                        continue;
                    return step.Span.ToString();
                }
                return decriptor.Path.Replace("/", "").Replace("{", "").Replace("}", "");
            }
        }

        public string GetOperationName(OpenApiOperationDescription descriptor, out string clientName)
        {
            clientName = GetClientName(descriptor);

            foreach (var step in descriptor.Path.SpanSplit('/'))
            {
                if (MemoryExtensions.Equals(step.Span, cApi.AsSpan(), StringComparison.Ordinal)
                    || MemoryExtensions.Equals(step.Span, clientName.AsSpan(), StringComparison.Ordinal)
                    || (step.Length > 0 && step.Span[0] == '{'))
                    continue;
                return step.Span.ToString();
            }
            return descriptor.Method.ToInitcap();
        }

        private HashSet<RefField> GetClientReferences(JsonSchema clientSchema)
        {
            var cache = new HashSet<RefField>();
            foreach (var entry in referenceFields)
            {
                foreach (var refField in entry.Value)
                {
                    if (refField.TypeSchema == clientSchema)
                    {
                        if (!cache.Contains(refField))
                        {
                            cache.Add(refField);
                        }
                    }
                }
            }
            return cache;
        }

        private string GetClientBaseType(string clientName, HashSet<string> usings, out JsonSchemaProperty? idKey, out JsonSchemaProperty? typeKey, out int typeId, out string postfix)
        {
            postfix = cWebClient;
            idKey = default;
            typeKey = default;
            typeId = 0;
            var logged = document.Operations
                .Where(p => p.Operation.Tags.Contains(clientName, StringComparer.OrdinalIgnoreCase))
                .FirstOrDefault(p => p.Path.IndexOf("/GetItemLogs/", StringComparison.OrdinalIgnoreCase) > -1);
            var loggedReturnSchema = logged == null ? null : GetReturningTypeSchema(logged);
            var loggedTypeName = loggedReturnSchema == null ? null : GetArrayElementTypeString(loggedReturnSchema, usings);
            if (document.Definitions.TryGetValue(clientName, out var schema))
            {
                postfix = cWebTable;
                idKey = GetPrimaryKey(schema);
                typeKey = GetTypeKey(schema);
                typeId = GetTypeId(schema);

                return $"{(loggedTypeName != null ? "Logged" : "")}WebTable<{clientName}, {(idKey == null ? "int" : GetTypeString(idKey, usings, cList))}{(logged != null ? $", {loggedTypeName}" : "")}>";
            }
            return cWebClient;
        }

        private JsonSchemaProperty? GetProperty(JsonSchema schema, string propertyName)
        {
            if (schema.Properties.TryGetValue(propertyName, out var property))
            {
                return property;
            }
            foreach (var baseClass in schema.AllInheritedSchemas)
            {
                if (baseClass.Properties.TryGetValue(propertyName, out property))
                {
                    return property;
                }
            }

            return null;
        }

        private JsonSchemaProperty? GetPrimaryKey(JsonSchema schema, bool inherit = true)
        {
            if (schema.ExtensionData != null && schema.ExtensionData.TryGetValue(cXId, out var propertyName))
            {
                return schema.Properties[propertyName?.ToString() ?? string.Empty];
            }
            if (inherit)
            {
                foreach (var baseClass in schema.AllInheritedSchemas)
                {
                    if (baseClass.ExtensionData != null && baseClass.ExtensionData.TryGetValue(cXId, out propertyName))
                    {
                        return baseClass.Properties[propertyName?.ToString() ?? string.Empty];
                    }
                }
                var parentSchema = schema.ParentSchema;
                while (parentSchema != null)
                {
                    if (parentSchema.ExtensionData != null && parentSchema.ExtensionData.TryGetValue(cXId, out propertyName))
                    {
                        return parentSchema.Properties[propertyName?.ToString() ?? string.Empty];
                    }
                    parentSchema = parentSchema.ParentSchema;
                }

            }
            return null;
        }

        private JsonSchemaProperty? GetReferenceProperty(JsonSchema schema, string name, bool inherit = true)
        {
            var find = schema.Properties.Values.FirstOrDefault(p => p.ExtensionData != null
                 && p.ExtensionData.TryGetValue(cXId, out var propertyName)
                 && string.Equals(propertyName?.ToString(), name, StringComparison.Ordinal));
            if (find != null)
            {
                return find;
            }
            if (inherit)
            {
                foreach (var baseClass in schema.AllInheritedSchemas)
                {
                    find = baseClass.Properties.Values.FirstOrDefault(p => p.ExtensionData != null
                 && p.ExtensionData.TryGetValue(cXId, out var propertyName)
                 && string.Equals(propertyName?.ToString(), name, StringComparison.Ordinal));
                    if (find != null)
                    {
                        return find;
                    }
                }
            }
            return null;
        }

        private JsonSchemaProperty? GetReferenceIdProperty(JsonSchemaProperty property, bool inherit = true)
        {
            if (property.ExtensionData != null
                && property.ParentSchema != null
                && property.ExtensionData.TryGetValue(cXId, out var propertyName))
            {
                return GetProperty(property.ParentSchema, propertyName?.ToString() ?? string.Empty);
            }
            return null;
        }

        private JsonSchemaProperty? GetTypeKey(JsonSchema schema)
        {
            if (schema.ExtensionData != null && schema.ExtensionData.TryGetValue(cXType, out var propertyName))
            {
                return schema.Properties[propertyName?.ToString() ?? string.Empty];
            }

            foreach (var baseClass in schema.AllInheritedSchemas)
                if (baseClass.ExtensionData != null && baseClass.ExtensionData.TryGetValue(cXType, out propertyName))
                {
                    return baseClass.Properties[propertyName?.ToString() ?? string.Empty];
                }

            return null;
        }

        private int GetTypeId(JsonSchema schema)
        {
            if (schema.ExtensionData != null && schema.ExtensionData.TryGetValue("x-type-id", out var id))
            {
                return id is int intKey ? intKey
                    : id is string stringKey && int.TryParse(stringKey, out var typeid) ? typeid
                    : Convert.ToInt32(id);
            }

            return 0;
        }

        private IEnumerable<JsonSchema> GetParentSchems(JsonSchema schema)
        {
            while (schema.ParentSchema != null)
            {
                yield return schema.ParentSchema.ActualSchema;
                schema = schema.ParentSchema.ActualSchema;
            }
        }

        private JsonSchema? GetReturningTypeSchema(OpenApiOperationDescription descriptor)
        {
            return descriptor.Operation.Responses.TryGetValue(c200, out var responce) && responce != null
                ? responce.Schema : null;
        }

        private string GetReturningType(OpenApiOperationDescription descriptor, HashSet<string> usings)
        {
            var returnType = Helper.cString;
            if (descriptor.Operation.Responses.TryGetValue(c200, out var responce) && responce != null)
            {
                returnType = $"{GetTypeString(responce.Schema, usings, cList)}";
            }
            return returnType;
        }

        private string GetReturningTypeCheck(OpenApiOperationDescription descriptor, string operationName, HashSet<string> usings)
        {
            var returnType = GetReturningType(descriptor, usings);
            if (operationName == "GenerateId")
                returnType = "object";
            //if (returnType == "AccessValue")
            //    returnType = "IAccessValue";
            //if (returnType == "List<AccessItem>")
            //    returnType = "IEnumerable<IAccessItem>";
            return returnType;
        }

        private TypeRecord GetReferenceType(JsonSchema definition)
        {
            return GetReferenceType(GetDefinitionName(definition), definition);
        }

        private TypeRecord GetReferenceType(string definitionName, JsonSchema definition)
        {
            if (!cacheReferences.TryGetValue(definitionName, out var type))
            {
                if (definitionName.Equals(nameof(TimeSpan), StringComparison.OrdinalIgnoreCase))
                {
                    type = provider.GetOrCreate(typeof(TimeSpan)).Type;
                }
                else
                {
                    foreach (var reference in usingReferences)
                    {
                        try
                        {
                            var parsedType = ParseType(definitionName, reference);
                            if (parsedType != null)
                            {
                                if (parsedType.IsEnum)
                                {
                                    if (definition.EnumerationNames == null)
                                        continue;
                                    var defiEnumeration = definition.EnumerationNames;
                                    var typeEnumeration = parsedType.Fields.Select(p => p.Name);
                                    if (defiEnumeration.SequenceEqual(typeEnumeration))
                                    {
                                        type = parsedType;
                                        break;
                                    }
                                }
                                else if (definition.Properties != null)
                                {
                                    var defiProperties = definition.Properties.Keys;
                                    var typeProperties = parsedType.Properties?
                                        // TODO .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public)
                                        .Select(p => p.Name);
                                    var defiCount = defiProperties.Count();
                                    var percent = (float)defiProperties.Intersect(typeProperties, StringComparer.Ordinal).Count();
                                    percent = defiCount == 0 ? 1 : percent / defiProperties.Count();
                                    if (percent > 0.5f)
                                    {
                                        type = parsedType;
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var error = ($"Can't Check Type {definitionName} on {reference}. {ex.GetType().Name} {ex.Message}");
                        }
                    }
                }
                cacheReferences[definitionName] = type!;
            }
            return type!;
        }

        private TypeRecord GenDefinition(JsonSchema schema, ref SourceProductionContext context)
        {
            usings.Clear();
            usings.Add("DataWF");
            usings.Add("System");

            TypeRecord type = default!;
            var source = schema.IsEnumeration
                ? GenDefinitionEnum(schema, usings, out type)
                : GenDefinitionClass(schema, usings, out type);

            GenUnit($"{Namespace}.{GetDefinitionName(schema)}Definition.g.cs", source, usings, ref context);
            return type;
        }

        private StringBuilder GenDefinitionEnum(JsonSchema schema, HashSet<string> usings, out TypeRecord type)
        {
            Helper.AddUsing("System.Runtime.Serialization", usings);
            var name = GetDefinitionName(schema);
            source.Clear();
            source.Append($$"""

                    {{((schema.ExtensionData?.TryGetValue("x-flags", out _) ?? false) ? "[Flags]" : "")}}
                    public enum {{name}}
                    {
                """);
            GenDefinitionEnumMemebers(source, schema);
            source.Append($$"""

                    }
                """);
            type = new TypeRecord(name, name, Namespace, Keys: TypeRecordKeys.Enum);
            return source;
        }

        private void GenDefinitionEnumMemebers(StringBuilder source, JsonSchema schema)
        {
            object[]? members = null;
            if (schema.ExtensionData != null
                && schema.ExtensionData.TryGetValue("x-enumMembers", out var enumMembers))
            {
                members = (object[]?)enumMembers;
            }
            int i = 0;
            //var definitionName = GetDefinitionName(schema);
            foreach (var item in schema.Enumeration)
            {
                var sitem = schema.EnumerationNames[i];
                var memeber = sitem;
                if (members != null && members.Length > i)
                    memeber = members[i]?.ToString();
                i++;
                source.Append($"""

                            [EnumMember(Value = "{memeber}")]
                            {sitem} = {item},
                    """);
            }
        }

        private StringBuilder GenDefinitionClass(JsonSchema schema, HashSet<string> usings, out TypeRecord type)
        {
            usings.Add("System.Text.Json.Serialization");
            var refFields = referenceFields[schema] = new List<RefField>();
            source.Clear();
            var name = GetDefinitionName(schema);

            source.Append($$"""

                    [Invoker]
                    public partial class {{name}}: {{string.Join(", ", GenDefinitionClassBases(schema, usings))}}
                    {
                """);
            var properties = GenDefinitionClassMemebers(source, schema, refFields, usings);

            source.Append($$"""

                    }
                """);
            type = new TypeRecord(
                OriginName: name,
                Name: name,
                Namespace: Namespace,
                Keys: TypeRecordKeys.Class,
                Properties: properties.ToEquatableList());
            return source;
        }

        private IEnumerable<string> GenDefinitionClassBases(JsonSchema schema, HashSet<string> usings)
        {
            if (schema.InheritedSchema != null)
            {
                var name = GetDefinitionName(schema.InheritedSchema);
                var type = GetReferenceType(name, schema.InheritedSchema);
                if (type != null)
                {
                    Helper.AddUsing(type, usings);
                }
                yield return name;
            }
            else
            {
                //yield return SF.SimpleBaseType(SF.ParseTypeName(nameof(IEntryNotifyPropertyChanged)));
                yield return "ModelEntry";
            }
            var idKey = GetPrimaryKey(schema, false);
            if (idKey != null)
            {
                yield return "IPrimaryKey";
                yield return "IQueryFormatable";
            }
        }

        private IEnumerable<PropertyRecord> GenDefinitionClassMemebers(StringBuilder source, JsonSchema schema, List<RefField> refFields, HashSet<string> usings)
        {
            var idKey = GetPrimaryKey(schema);
            var typeKey = GetTypeKey(schema);
            var typeId = GetTypeId(schema);
            var definition = GetDefinitionName(schema);
            var inheritedName = GetTypeString(schema.InheritedSchema, usings);
            var properties = new List<PropertyRecord>();
            if (inheritedName == Helper.cString
                //|| inheritedName == "DefaultItem"
                || inheritedName == "ModelEntry")
            {
                source.Append($$"""

                            [JsonIgnore]    
                            public new {{SchemaName}} Schema
                            {
                                get => ({{SchemaName}})base.Schema;
                            }
                    """);
            }
            foreach (var property in schema.Properties)
            {
                GenDefinitionClassField(source, property.Value, idKey, refFields, usings);
            }

            GenDefinitionClassConstructorBody(source, schema, idKey, typeKey, typeId, refFields, usings);

            if (refFields.Count > 0 && idKey != null && idKey.ParentSchema != schema)
            {
                properties.Add(GenDefinitionClassProperty(source, idKey, idKey, typeKey, refFields, usings, true));
            }

            foreach (var property in schema.Properties)
            {
                properties.Add(GenDefinitionClassProperty(source, property.Value, idKey, typeKey, refFields, usings));
            }

            if (GetPrimaryKey(schema, false) is { } selfKey)
            {
                var converter = "";
                var idType = GetTypeString(selfKey, usings, cList);
                if (idType == "long")
                    converter = "Convert.ToInt64(value)";
                else if (idType == "int")
                    converter = "Convert.ToInt32(value)";
                else if (idType == "short")
                    converter = "Convert.ToInt16(value)";
                else if (idType == "decimal")
                    converter = "Convert.ToDecimal(value)";
                else
                    converter = $"({idType})value";
                Helper.AddUsing("System.Text.Json.Serialization", usings);
                source.Append($$"""

                            [JsonIgnore]    
                            public object PrimaryKey 
                            {
                                get => {{selfKey.Name}};
                                set => {{selfKey.Name}} = {{converter}};
                            }

                            public string Format() => {{selfKey.Name}}.ToString();
                    """);
            }
            return properties;
        }

        private void GenDefinitionClassField(StringBuilder source, JsonSchemaProperty property, JsonSchemaProperty? idKey, List<RefField> refFields, HashSet<string> usings)
        {
            var refkey = GetPropertyRefKey(property);
            if (refkey != null
                && property.Type == JsonObjectType.Array
                && property.Item != null
                && property.Item.ActualTypeSchema is { } actualSchema
                && GetProperty(actualSchema, refkey) is { } keyProperty
                && GetPropertyName(keyProperty) is { } keyName
                && GetReferenceProperty((JsonSchema)keyProperty.Parent!, keyName) is { } valueProperty)
            {
                var typeName = GetTypeString(property.Item, usings);
                var refField = new RefField
                {
                    Property = property,
                    PropertyName = GetPropertyName(property),
                    Definition = property.ParentSchema?.Id,
                    RefKey = refkey,
                    TypeSchema = actualSchema,
                    TypeName = typeName,
                    InvokerName = $"{typeName}.{refkey}Invoker.Instance",
                    KeyProperty = keyProperty,
                    KeyName = keyName,
                    KeyType = GetTypeString(keyProperty, usings),
                    ValueProperty = valueProperty,
                    ValueName = GetPropertyName(valueProperty),
                    ValueType = GetTypeString(valueProperty, null),
                    ValueFieldName = GetFieldName(valueProperty),
                    FieldType = GetTypeString(property, usings, "ReferenceList"),
                    FieldName = GetFieldName(property),
                };
                refFields.Add(refField);
                //var refTypePrimary = GetPrimaryKey(refTypeSchema);
                //var refTypePrimaryName = GetPropertyName(refTypePrimary);
                //var refTypePrimaryType = GetTypeString(refTypePrimary, true, "SelectableList");
                //refField.ParameterType = $"QueryParameter<{refField.TypeName}>";
                //refField.ParameterName = refField.TypeName + refkey + "Parameter";

                ////QueryParameter<T>                
                //yield return SF.FieldDeclaration(attributeLists: SF.List<AttributeListSyntax>(),
                //    modifiers: SF.TokenList(SF.Token(SyntaxKind.PrivateKeyword)),
                //   declaration: SF.VariableDeclaration(
                //       type: SF.ParseTypeName(refField.ParameterType),
                //       variables: SF.SingletonSeparatedList(
                //           SF.VariableDeclarator(
                //               identifier: SF.ParseToken(refField.ParameterName),
                //               argumentList: null,
                //               initializer: SF.EqualsValueClause(
                //                   SF.ParseExpression($"new {refField.ParameterType}{{ Invoker = {refField.InvokerName}}}"))))));

                source.Append($"""

                            protected {refField.FieldType} {refField.FieldName};
                    """);
            }
            else
            {
                var type = GetTypeString(property, usings);

                source.Append($"""

                            protected internal {type} {GetFieldName(property)}{(property.Type == JsonObjectType.Array ? $" = new {type}()" : "")};
                    """);
            }
        }

        private void GenDefinitionClassConstructorBody(StringBuilder source, JsonSchema schema, JsonSchemaProperty? idKey, JsonSchemaProperty? typeKey, int typeId, List<RefField> refFields, HashSet<string> usings)
        {
            source.Append($$"""

                        public {{GetDefinitionName(schema)}}()
                        {
                """);
            if (typeId != 0 && typeKey != null)
            {
                source.Append($"""

                                {GetPropertyName(typeKey)} = {typeId};
                    """);
            }
            foreach (var refField in refFields)
            {
                //yield return SF.ParseStatement($@"{refField.FieldName} = new {refField.FieldType}(
                //new Query<{refField.TypeName}>(new[]{{{refField.ParameterName}}}),
                //Provider.{refField.TypeName}.Items,
                //false);");
                source.Append($"""

                                {refField.FieldName} = new {refField.FieldType} (this, nameof({refField.PropertyName}));
                    """);
            }
            foreach (var property in schema.Properties.Select(p => p.Value))
            {
                if (property.Default != null)
                {
                    source.Append($"""

                                    {GetPropertyName(property)} = {GenFieldDefault(property, idKey, usings)};
                        """);
                }
            }
            source.Append($$"""

                        }
                """);
        }

        private IEnumerable<StatementSyntax> GenDefintionClassPropertySynckGet(JsonSchemaProperty typeKey, int typeId, List<RefField> refFields)
        {
            yield return SF.ParseStatement($"if (base.SyncStatus != SynchronizedStatus.Actual)");
            yield return SF.ParseStatement($"return base.SyncStatus;");
            yield return SF.ParseStatement($"var edited = ");

            foreach (var refField in refFields)
            {
                var isFirst = refFields[0] == refField;
                var isLast = refFields[refFields.Count - 1] == refField;
                yield return SF.ParseStatement($"{(!isFirst ? "|| " : "")}{refField.PropertyName}.Any(p => p.SyncStatus != SynchronizedStatus.Actual){(isLast ? ";" : "")}");
            }
            yield return SF.ParseStatement($"edited ? SynchronizedStatus.Edit : SynchronizedStatus.Actual;");
        }

        private IEnumerable<ParameterSyntax> GenPropertyChangedParameter()
        {
            var @default = SF.EqualsValueClause(
                    SF.Token(SyntaxKind.EqualsToken),
                    SF.LiteralExpression(SyntaxKind.NullLiteralExpression));
            yield return SF.Parameter(
                attributeLists: SF.List(new[]{
                    SF.AttributeList(
                        SF.SingletonSeparatedList(
                            SF.Attribute(
                                SF.IdentifierName("CallerMemberName")))) }),
                modifiers: SF.TokenList(),
                type: SF.ParseTypeName(Helper.cString),
                identifier: SF.Identifier("propertyName"),
                @default: @default);
        }

        private PropertyRecord GenDefinitionClassProperty(StringBuilder source, JsonSchemaProperty property, JsonSchemaProperty? idKey, JsonSchemaProperty? typeKey, List<RefField> refFields, HashSet<string> usings, bool isOverride = false)
        {
            var refkey = GetPropertyRefKey(property);
            var typeDeclaration = GetTypeString(property, usings, refkey == null ? "SelectableList" : "ReferenceList");
            var propertyName = GetPropertyName(property);
            source.Append($$"""

                        [{{string.Join(", ", GenDefinitionClassPropertyAttributes(property, idKey, typeKey, usings))}}]
                        public {{(isOverride ? "override " : property == idKey ? "virtual " : "")}}{{typeDeclaration}} {{propertyName}} 
                        {
                """);
            GenDefintionClassPropertyGet(source, property, isOverride);
            GenDefinitionClassPropertySet(source, property, idKey, refFields, usings, isOverride);
            source.Append($$"""

                        }
                """);
            return new PropertyRecord(
                Name: propertyName,
                TypeRef: new TypeRecordReference(typeDeclaration)
                {
                    Type = new TypeRecord(typeDeclaration, typeDeclaration, "", Keys: TypeRecordKeys.None)
                }
            );
        }

        private IEnumerable<string> GenDefinitionClassPropertyAttributes(JsonSchemaProperty property, JsonSchemaProperty? idKey, JsonSchemaProperty? typeKey, HashSet<string> usings)
        {
            if (property.IsReadOnly
                || (property.ExtensionData != null
                    && property.ExtensionData.TryGetValue("readOnly", out var isReadOnly)
                    && isReadOnly is true))
            {
                yield return "JsonIgnoreSerialization";
            }
            else if (property == typeKey)
            {
                Helper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                yield return $"Display(Order = -3)";
            }
            else if (property == idKey)
            {
                Helper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                yield return $"Display(Order = -2)";
            }
            else if ((property.Type == JsonObjectType.Object
                && !property.IsEnumeration
                && property.AllInheritedSchemas.Any(p => p.Id == "DBItem"))
                || (property.Type == JsonObjectType.None
                && property.ActualTypeSchema.Type == JsonObjectType.Object
                && !property.ActualTypeSchema.IsEnumeration
                && property.ActualTypeSchema.AllInheritedSchemas.Any(p => p.Id == "DBItem")))
            {
                yield return "JsonSynchronized";
            }
            else //if (!property.IsRequired)
            {
                yield return "JsonSynchronized";
            }

            foreach (var attribute in GenDefinitionClassPropertyValidationAttributes(property, idKey, typeKey, usings))
            {
                yield return attribute;
            }
        }

        private IEnumerable<string> GenDefinitionClassPropertyValidationAttributes(JsonSchemaProperty property, JsonSchemaProperty? idKey, JsonSchemaProperty? typeKey, HashSet<string> usings)
        {
            var propertyName = GetPropertyName(property);
            var objectProperty = GetReferenceProperty((JsonSchema)property.Parent!, property.Name);
            if (objectProperty != null)
            {
                propertyName = GetPropertyName(objectProperty);
            }

            if (property.IsRequired)
            {
                Helper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                if (property == idKey || property == typeKey)
                {
                    yield return $"Required(AllowEmptyStrings = true)";
                }
                else
                {
                    yield return $"Required(ErrorMessage = \"{propertyName} is required\")";
                }
            }

            if (property.MaxLength != null)
            {
                Helper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                yield return $"MaxLength({property.MaxLength}, ErrorMessage = \"{propertyName} only max {property.MaxLength} letters allowed.\")";
            }
            var idProperty = GetReferenceIdProperty(property);
            if (idProperty != null)
            {
                foreach (var attribute in GenDefinitionClassPropertyValidationAttributes(idProperty, idKey, typeKey, usings))
                {
                    yield return attribute;
                }
            }
        }

        private void GenDefintionClassPropertyGet(StringBuilder source, JsonSchemaProperty property, bool isOverride)
        {
            var fieldName = GetFieldName(property);
            if (isOverride)
            {
                source.Append($"""

                                get => base.{GetPropertyName(property)};
                    """);
                return;
            }
            var refkey = GetPropertyRefKey(property);
            if (refkey != null)
            {
                //yield return SF.ParseStatement($"{fieldName}.UpdateFilter();");
            }
            // Change - refence from single json
            //if (property.ExtensionData != null && property.ExtensionData.TryGetValue("x-id", out var idProperty))
            //{
            //    var idFiledName = GetFieldName((string)idProperty);
            //    yield return SF.ParseStatement($"if({fieldName} == null && {idFiledName} != null){{");
            //    yield return SF.ParseStatement($"{fieldName} = {GetTypeString(property, false, "List")}Client.Instance.Select({idFiledName}.Value);");
            //    yield return SF.ParseStatement("}");
            //}
            source.Append($"""

                            get => {fieldName};
                """);
        }

        private void GenDefinitionClassPropertySet(StringBuilder source, JsonSchemaProperty property, JsonSchemaProperty? idKey, List<RefField> refFields, HashSet<string> usings, bool isOverride)
        {
            source.Append($$"""

                            set
                            {
                """);

            if (!isOverride)
            {
                var type = GetTypeString(property, usings, "SelectableList");
                if (type.Equals(Helper.cString, StringComparison.Ordinal))
                {
                    source.Append($"""

                                        if (string.Equals({GetFieldName(property)}, value, StringComparison.Ordinal)) return;
                        """);
                }
                else
                {
                    source.Append($"""

                                        if ({GetFieldName(property)} == value) return;
                        """);
                }
                source.Append($"""

                                    var temp = {GetFieldName(property)};
                                    {GetFieldName(property)} = value;
                    """);
                if (property.ExtensionData != null
                    && property.ExtensionData.TryGetValue(cXId, out var refPropertyName)
                    && refPropertyName is string refName
                    && (property.Reference ?? property.AllOf.FirstOrDefault()?.Reference ?? property.AnyOf.FirstOrDefault()?.Reference) is JsonSchema refSchema
                    && GetProperty((JsonSchema)property.Parent!, refName) is { } refProperty
                    && GetPrimaryKey(refSchema) is { } idProperty)
                {
                    var refType = GetTypeString(refProperty, usings);
                    if (idProperty.IsNullableRaw ?? false)
                        source.Append($"""

                                            {refPropertyName} = value?.{GetPropertyName(idProperty)};
                            """);
                    else
                        source.Append($"""

                                            {refPropertyName} = value?.{GetPropertyName(idProperty)} ?? default({refType});
                            """);
                }
                //Change - refence from single json
                var objectProperty = GetReferenceProperty((JsonSchema)property.Parent!, property.Name);
                if (objectProperty != null)
                {
                    var objectFieldName = GetFieldName(objectProperty);
                    var clientName = GetTypeString(objectProperty, usings, cList);
                    if (objectProperty.ExtensionData != null && objectProperty.ExtensionData.TryGetValue("x-derived", out var derivedClass))
                        clientName = derivedClass?.ToString();
                    source.Append($$"""

                                        if ({{objectFieldName}}?.Id != value)
                                        {
                                            {{objectFieldName}} = value == default({{type}}) ? null : Schema?.{{clientName}}.Select(value{{(type.EndsWith("?") ? ".Value" : "")}});
                                            OnPropertyChanged(nameof({{GetPropertyName(objectProperty)}}));
                                        }
                        """);
                }
                source.Append($"""

                                    OnPropertyChanged(temp, value);
                    """);
            }
            else
            {
                source.Append($"""

                                    base.{GetPropertyName(property)} = value;
                    """);
            }
            //if (property.Name == idKey?.Name)
            //{
            //    foreach (var refField in refFields)
            //    {
            //        yield return SF.ParseStatement($"{refField.ParameterName}.Value = value;");
            //    }
            //}
            source.Append($$"""

                            }
                """);
        }

        private string GetPropertyName(JsonSchemaProperty property)
        {
            return property.Name;
        }

        private string GetDefinitionName(JsonSchema schema)
        {
            return schema.Id ?? string.Empty;
        }

        private string GetDefinitionName(string name)
        {
            return name.ToInitcap();
        }

        private string GetFieldName(JsonSchemaProperty property)
        {
            return !string.IsNullOrEmpty(property.Id)
                ? property.Id!
                : (property.Id = GetFieldName(property.Name))!;
        }

        private string GetFieldName(string property)
        {
            return string.Concat("_", property.ToLowerCap());
        }

        private string? GetPropertyRefKey(JsonSchema schema)
        {
            return schema.ExtensionData != null
                ? schema.ExtensionData.TryGetValue("x-ref-key", out var key) ? key?.ToString() : null
                : null;
        }

        private ExpressionSyntax GenFieldDefault(JsonSchemaProperty property, JsonSchemaProperty? idKey, HashSet<string> usings)
        {
            var text = property.Default?.ToString();
            var type = GetTypeString(property, usings).TrimEnd('?');
            if (text != null && string.Equals(type, "bool", StringComparison.Ordinal))
                text = text.ToLowerInvariant();
            else if (string.Equals(type, Helper.cString, StringComparison.Ordinal))
                text = $"""
                    "{text}"
                    """;
            else if (property.ActualSchema != null && property.ActualSchema.IsEnumeration)
                text = $"{type}.{text}";
            return SF.ParseExpression(text ?? "default");
        }

        private string? GetArrayElementTypeString(JsonSchema schema, HashSet<string> usings)
        {
            return schema.Type == JsonObjectType.Array
                && schema.Item != null
                ? GetTypeString(schema.Item, usings, cList)
                : null;
        }

        private string GetTypeString(JsonSchema? schema, HashSet<string>? usings, string listType = "SelectableList", bool? baseNullable = null)
        {
            if (schema == null)
                return Helper.cString;

            var nullable = baseNullable ?? schema.IsNullableRaw ?? false;
            switch (schema.Type)
            {
                case JsonObjectType.Integer:
                    if (schema.IsEnumeration)
                    {
                        goto case JsonObjectType.Object;
                    }
                    if (schema.Format == "int64")
                    {
                        return "long" + (nullable ? "?" : string.Empty);
                    }
                    return "int" + (nullable ? "?" : string.Empty);
                case JsonObjectType.Boolean:
                    return "bool" + (nullable ? "?" : string.Empty);
                case JsonObjectType.Number:
                    if (schema.IsEnumeration)
                    {
                        goto case JsonObjectType.Object;
                    }
                    if (string.IsNullOrEmpty(schema.Format))
                    {
                        return "decimal" + (nullable ? "?" : string.Empty);
                    }
                    return schema.Format + (nullable ? "?" : string.Empty);
                case JsonObjectType.String:
                    if (schema.IsEnumeration)
                    {
                        goto case JsonObjectType.Object;
                    }
                    switch (schema.Format)
                    {
                        case "byte":
                            return "byte[]";
                        case "binary":
                            if (usings != null)
                                Helper.AddUsing("System.IO", usings);
                            return Helper.cStream;
                        case "date":
                        case "date-time":
                            return "DateTime" + (nullable ? "?" : string.Empty);
                        default:
                            return Helper.cString;
                    }
                case JsonObjectType.Array:
                    return $"{listType}<{GetTypeString(schema.Item!, usings, listType)}>";
                case JsonObjectType.None:
                    if (schema.ActualTypeSchema != schema)
                    {
                        return GetTypeString(schema.ActualTypeSchema, usings, listType, nullable);
                    }
                    else if (schema is JsonSchemaProperty propertySchema
                        && (propertySchema.AllOf.FirstOrDefault()?.Reference
                            ?? propertySchema.AnyOf.FirstOrDefault()?.Reference) is JsonSchema referenceSchema)
                    {
                        return GetTypeString(referenceSchema, usings, listType, nullable);
                    }
                    else
                    {
                        goto case JsonObjectType.Object;
                    }
                case JsonObjectType.Object:
                    if (schema.Id != null)
                    {
                        var type = GetReferenceType(schema);
                        if (type != null && usings != null)
                        {
                            Helper.AddUsing(type, usings);
                        }
                        if (schema.IsEnumeration)
                        {
                            return GetDefinitionName(schema) + (nullable ? "?" : string.Empty);
                        }
                        else
                        {
                            return GetDefinitionName(schema) + (nullable && (type?.IsValueType ?? false) ? "?" : string.Empty);
                        }
                    }
                    else if (schema.Properties.ContainsKey("file"))
                    {
                        //SyntaxHelper.AddUsing("System.IO", usings);
                        return "object";
                    }
                    break;
                case JsonObjectType.File:
                    if (usings != null)
                        Helper.AddUsing("System.IO", usings);
                    return Helper.cStream;
            }

            return Helper.cString;
        }

        public void GenUnit(string name, StringBuilder source, HashSet<string> usings, ref SourceProductionContext context)
        {
            var unitSource = new StringBuilder();
            foreach (var item in usings.OrderBy(p => p))
            {
                unitSource.Append($"""

                    using {item};
                    """);
            }
            unitSource.Append($$"""


                namespace {{Namespace}}
                {

                """);
            source.Insert(0, unitSource);
            source.Append($$"""

                }
                """);
            var sourceText = SourceText.From(source.ToString(), Encoding.UTF8);
            context.AddSource(name, sourceText);
            usings.Clear();
            source.Clear();

        }

        // public INamedTypeSymbol GenUnit(SyntaxTree syntaxTree,Compilation compilation)
        // {
        //     return CSharpSyntaxTree.ParseText(sourceText, (CSharpParseOptions)Options);
        //     if (syntaxTree == null)
        //         return null;

        //     compilation = compilation.AddSyntaxTrees(syntaxTree);
        //     var unitSyntax = (CompilationUnitSyntax)syntaxTree.GetRoot();
        //     var nameSpaceSyntax = unitSyntax.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        //     var classSyntax = nameSpaceSyntax.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault();
        //     if (classSyntax != null)
        //     {
        //         Invoker.Process(classSyntax);
        //         return Invoker.TypeSymbol;
        //     }
        //     var enumSyntax = nameSpaceSyntax.Members.OfType<EnumDeclarationSyntax>().FirstOrDefault();
        //     if (enumSyntax != null)
        //     {
        //         return GetSymbol(enumSyntax);
        //     }
        //     return null;
        // }

        public TypeRecord? ParseType(Type value, IEnumerable<IAssemblySymbol> assemblies)
        {
            return ParseType(value.FullName, assemblies);
        }

        public TypeRecord? ParseType(string value, IEnumerable<IAssemblySymbol> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var type = ParseType(value, assembly);
                if (type != null)
                    return type;
            }
            return null;
        }

        public TypeRecord? ParseType(string value, IAssemblySymbol assembly)
        {
            var byName = value.IndexOf('.') < 0;

            if (!cacheAssemblySymbolTypes.TryGetValue(assembly.Name, out var cache))
            {
                cacheAssemblySymbolTypes[assembly.Name] =
                    cache = new Dictionary<string, TypeRecord?>(StringComparer.Ordinal);


                var definedTypes = assembly.GlobalNamespace.GetTypes();
                foreach (var defined in definedTypes)
                {
                    if (defined.DeclaredAccessibility == Accessibility.Public)
                        cache[defined.Name] = provider.GetOrCreate(
                            typeSymbol: defined,
                            takeBase: TakeBaseMode.Recursive,
                            validateProperty: TypeRecord.AllProperties)!.Type;
                }
            }
            if (cache.TryGetValue(value, out var type))
            {
                return type;
            }
            return cache[value] = !byName && assembly.GetTypeByMetadataName(value) is INamedTypeSymbol symbol
                ? provider.GetOrCreate(
                    typeSymbol: symbol,
                    takeBase: TakeBaseMode.Recursive,
                    validateProperty: TypeRecord.AllProperties)!.Type
                : null;
        }

        private class RefField
        {
            public required string RefKey;
            public required JsonSchemaProperty KeyProperty;
            public string? KeyName;
            public required string KeyType;
            public JsonSchema? TypeSchema;
            public required string TypeName;
            public required string InvokerName;
            //public string InvokerType;
            //public string ParameterType;
            //public string ParameterName;
            public required JsonSchemaProperty Property;
            public required string PropertyName;
            public required string FieldType;
            public required string FieldName;
            public string? Definition;
            public JsonSchemaProperty? ValueProperty;
            public required string ValueName;
            public required string ValueType;
            public required string ValueFieldName;

        }

    }
}
