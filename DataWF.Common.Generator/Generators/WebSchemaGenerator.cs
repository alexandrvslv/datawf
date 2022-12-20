﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Net.Http;
using System.Collections;
using System.Globalization;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Any;

namespace DataWF.Common.Generator
{
    internal class WebSchemaGenerator : BaseGenerator
    {
        private const string CList = "List";
        private const string CApi = "api";

        private const string C200 = "200";
        private const string CWebClient = "WebClient";
        private const string CWebTable = "WebTable";
        private const string CNsDataWFCommon = "DataWF.Common";
        private const string CNsSystem = "System";
        private const string CTypeArray = "array";
        private const string CTypeObject = "object";
        private static readonly HashSet<string> VirtualOperations = new HashSet<string>(StringComparer.Ordinal)
        {
            "Get",
            "GetAll",
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
        private Dictionary<string, INamedTypeSymbol> cacheModels = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
        private Dictionary<string, INamedTypeSymbol> cacheReferences = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
        private Dictionary<string, Dictionary<string, INamedTypeSymbol>> cacheAssemblySymbolTypes = new Dictionary<string, Dictionary<string, INamedTypeSymbol>>(StringComparer.Ordinal);
        private string clientName = string.Empty;
        private string clientType = string.Empty;
        private string operationPath;
        private string operationName;
        private KeyValuePair<string, OpenApiSchema> idKey;
        private KeyValuePair<string, OpenApiSchema> typeKey;
        private int typeId;
        private Dictionary<string, string> clients = new Dictionary<string, string>(StringComparer.Ordinal);
        private HashSet<string> usings = new HashSet<string>(StringComparer.Ordinal);
        private Dictionary<OpenApiSchema, List<RefField>> referenceFields = new Dictionary<OpenApiSchema, List<RefField>>();
        private OpenApiDocument document;
        private List<IAssemblySymbol> usingReferences = new List<IAssemblySymbol>();

        public WebSchemaGenerator(CompilationContext compilationContext)
            : base(compilationContext)
        {
        }

        public InvokerGenerator InvokerGenerator { get; set; }

        public string DocumentSource { get; set; }
        public AttributeData Attribute { get; private set; }
        public string Namespace { get; set; }
        public string SchemaName { get; set; }

        public override bool Process()
        {
            Attribute = TypeSymbol.GetAttribute(Attributes.WebSchema);
            Namespace = TypeSymbol.ContainingNamespace.ToDisplayString();
            SchemaName = TypeSymbol.Name;
            source = new StringBuilder();

            LoadDocument();
            LoadReferences();

            foreach (var definition in document.Components.Schemas)
            {
                definition.Value.Title = definition.Key;
            }
            foreach (var definition in document.Components.Schemas)
            {
                if (CompilationContext.Context.CancellationToken.IsCancellationRequested)
                    return false;
                GetOrGenDefinion(definition.Key, definition.Value, out _);
            }
            foreach (var operation in document.Paths)
            {
                AddClientOperation(operation.Key, operation.Value);
            }
            CompileClient();

            if (CompilationContext.Context.CancellationToken.IsCancellationRequested)
                return false;

            usings.Add(CNsDataWFCommon);
            usings.Add(CNsSystem);
            GenUnit($"{Namespace}.{TypeSymbol.Name}.Gen.cs", GenProvider(), usings);

            return true;
        }

        private void CompileClient()
        {
            if (source.Length > 0)
            {
                clients.Add(clientName, clientType);
                source.Append($@"
    }}");
                GenUnit($"{Namespace}.{clientName}ClientGen.cs", source, usings);
            }
            clientName = string.Empty;
        }

        private void LoadDocument()
        {
            DocumentSource = Attribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
            if (DocumentSource == null)
                throw new Exception($"No arguments on Attribute {Attribute}");
            if (!DocumentSource.StartsWith("http", StringComparison.Ordinal))
            {
                var mainSyntaxTree = Compilation.SyntaxTrees
                          .First(x => x.HasCompilationUnitRoot);

                var projectDirectory = mainSyntaxTree.FilePath.Substring(0, mainSyntaxTree.FilePath.IndexOf(Compilation.AssemblyName) + Compilation.AssemblyName.Length);
                DocumentSource = Path.GetFullPath(Path.Combine(projectDirectory, DocumentSource));
            }
            var url = new Uri(DocumentSource);
            if (string.Equals(url.Scheme, "http", StringComparison.Ordinal)
                || string.Equals(url.Scheme, "https", StringComparison.Ordinal))
            {
                var httpClient = new HttpClient();
                var stream = httpClient.GetStreamAsync(url).GetAwaiter().GetResult();
                document = new OpenApiStreamReader().Read(stream, out _);
            }
            else if (string.Equals(url.Scheme, "file", StringComparison.Ordinal))
            {
                using (var fileStream = new FileStream(url.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    document = new OpenApiStreamReader().Read(fileStream, out _);
            }
        }

        private void LoadReferences()
        {
            var usingReferenceParam = Attribute.GetNamedValue("UsingReferences").Value?.ToString();
            var usingReferenceNames = usingReferenceParam != null
                ? new HashSet<string>(usingReferenceParam.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                : null;
            foreach (var assemblyReference in Compilation.References)
            {
                var assembly = Compilation.GetAssemblyOrModuleSymbol(assemblyReference);
                if (assembly is IAssemblySymbol assemblySymbol)
                {
                    if (usingReferenceNames?.Contains(assemblySymbol.Name) ?? false)
                    {
                        usingReferences.Add(assemblySymbol);
                    }
                }
            }
        }

        private StringBuilder GenProvider()
        {
            source.Clear();
            source.Append($@"
    public partial class {SchemaName} {(TypeSymbol.BaseType?.Name != "WebSchema" ? ": WebSchema" : "")}
    {{");

            GenProviderConstructor();

            foreach (var client in clients)
            {
                source.Append($@"
        public {client.Value} {client.Key} {{ get; }}");
            }
            source.Append($@"
    }}");
            return source;
        }

        private void GenProviderConstructor()
        {
            source.Append($@"
        public {SchemaName}()
        {{
            Name = ""{SchemaName}"";");

            foreach (var client in clients)
            {
                source.Append($@"
            Add({client.Key} = new {client.Value}(this));");
            }
            source.Append($@"
            RefreshTypedCache();
        }}");
        }

        private void AddClientOperation(string path, OpenApiPathItem pathItem)
        {
            operationPath = path;
            operationName = GetOperationName(path, pathItem, out var clientName);
            if (!string.Equals(clientName, this.clientName, StringComparison.Ordinal))
            {
                CompileClient();
                GenClient(clientName, usings);
            }

            GenOperation(pathItem, usings);
        }

        private void GenClient(string clientName, HashSet<string> usings)
        {
            usings.Add(CNsDataWFCommon);
            usings.Add(CNsSystem);
            usings.Add("System.Collections.Generic");
            usings.Add("System.Net.Http");
            usings.Add("System.Threading.Tasks");
            usings.Add("System.Text.Json.Serialization");
            var baseType = GetTableBaseType(clientName, usings, out var postfix);
            this.clientName = clientName;
            this.clientType = $"{clientName}{postfix}";
            source.Clear();
            source.Append($@"
    public partial class {clientType}: {baseType}
    {{");
            GenClientMembers(usings);
        }

        private void GenClientMembers(HashSet<string> usings)
        {
            document.Components.Schemas.TryGetValue(clientName, out var clientSchema);
            var typeName = $"{clientName}Client";

            var cache = clientSchema != null ? GetClientReferences(clientSchema) : new HashSet<RefField>();

            GenClientConstructor(cache);

            source.Append($@"
        [JsonIgnore]
        public new {SchemaName} Schema
        {{
            get => ({SchemaName})base.Schema;
            set => base.Schema = value;
        }}");

            if (cache.Count > 0)
            {
                Helper.AddUsing("System.Collections", usings);
                GenClientRemoveOverrideBody(cache);
                GenClientAddOverrideBody(cache);
            }
        }

        private void GenClientConstructor(HashSet<RefField> cache)
        {
            var idName = idKey.Key;
            var typeName = typeKey.Key;
            var baseCtor = idName == null ? "" : $@"{clientName}.{idName}Invoker.Instance,
                  {clientName}.{typeName}Invoker.Instance,
                  {typeId}";
            source.Append($@"
        public {clientType}({SchemaName} schema)
            : base({baseCtor})
        {{
            Name = ""{clientName}"";
            Schema = schema;");

            foreach (var refField in cache)
            {
                source.Append($@"
            Items.Indexes.Add({refField.InvokerName});");
            }
            source.Append($@"
        }}");
        }

        private void GenClientRemoveOverrideBody(HashSet<RefField> cache)
        {
            source.Append($@"
        protected override void OnRemoved(IList items = null)
        {{
            base.OnRemoved(items);
            foreach ({clientName} item in items)
            {{");
            foreach (var refField in cache)
            {
                source.Append($@"
                if (item.{refField.KeyName} != null)
                {{
                    var item{refField.ValueName} = (item.{refField.ValueFieldName} ?? (item.{refField.ValueFieldName} = Schema.{refField.ValueType}.Select(item.{refField.KeyName}.Value))) as {refField.Definition};
                    item{refField.ValueName}?.{refField.PropertyName}.Remove(item);
                }}");
            }
            source.Append($@"
            }}
        }}");
        }

        private void GenClientAddOverrideBody(HashSet<RefField> cache)
        {
            source.Append($@"
        protected override void OnAdded(IList items = null)
        {{
            base.OnAdded(items);
            foreach ({clientName} item in items)
            {{");
            foreach (var refField in cache)
            {
                source.Append($@"
                if (item.{refField.KeyName} != null)
                {{
                    var item{refField.ValueName} = (item.{refField.ValueFieldName} ?? (item.{refField.ValueFieldName} = Schema.{refField.ValueType}.Select(item.{refField.KeyName}.Value))) as {refField.Definition};
                    item{refField.ValueName}?.{refField.PropertyName}.Add(item);
                }}");
            }
            source.Append($@"
            }}
        }}");
        }

        private void GenOperation(OpenApiPathItem pathItem, HashSet<string> usings)
        {
            var isTableClient = IsSchemaClient(clientName, out _);
            var isOverride = isTableClient
                            && VirtualOperations.Contains(operationName);
            var returnType = GetReturningType(pathItem.Operation(), usings);
            var returnTypeCheck = GetReturningTypeCheck(pathItem.Operation(), operationName, usings);
            source.Append($@"
        public {(isOverride ? "override " : "")}async {(returnTypeCheck.Length > 0 ? $"Task<{returnTypeCheck}>" : "Task")} {operationName}(");
            GenOperationParameter(pathItem, usings);
            source.Append($@")
        {{");
            GenOperationBody(pathItem, returnType, usings, isOverride);
            source.Append($@"
        }}");
        }

        private void GenOperationParameter(OpenApiPathItem pathItem, HashSet<string> usings)
        {
            foreach (var parameter in pathItem.Operation().Parameters)
            {
                source.Append($"{GetTypeString(parameter, usings, CList)} {parameter.Name}, ");
            }
            var bodyParameter = pathItem.Operation().RequestBody;
            if (bodyParameter != null)
            {
                var mediaType = bodyParameter.Content.FirstOrDefault().Value;
                source.Append($"{GetTypeString(mediaType.Schema, usings, CList)} body, ");
            }
            //var returnType = GetReturningType(pathItem, usings);
            //if (bodyParameter == null && returnType.StartsWith("List<", StringComparison.Ordinal))
            //{
            //    source.Append($"HttpPageSettings pages, ");
            //}
            //source.Append($"HttpJsonSettings settings, ");
            source.Append($"ProgressToken progressToken");
        }

        private void GenOperationBody(OpenApiPathItem pathItem, string returnType, HashSet<string> usings, bool isOverride)
        {
            var operation = pathItem.Operation();
            var method = pathItem.OperationType();
            var responce = operation.Responce();
            var responceSchema = (OpenApiSchema)null;
            var mediatype = "application/json";
            if (responce != null)
            {
                var responceContent = responce.Content.FirstOrDefault();
                responceSchema = responceContent.Value?.Schema;
                mediatype = responceContent.Key ?? "application/json";
            }

            var path = new StringBuilder(operationPath);
            foreach (var parameter in operation.Parameters.Where(p => p.In == ParameterLocation.Query))
            {
                if (path.Length == operationPath.Length)
                {
                    path.Append('?');
                }
                else
                {
                    path.Append('&');
                }
                path.Append($"{parameter.Name}={{{parameter.Name}}}");
            }

            source.Append($@"
            return await Request<{returnType}>(progressToken, HttpMethod.{method}, ""{path}"", ""{mediatype}"" ");
            var bodyParameter = operation.RequestBody;
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
                source.Append($", body");
            }
            foreach (var parameter in operation.Parameters)
            {
                source.Append($", {parameter.Name}");
            }
            source.Append(").ConfigureAwait(false);");
        }

        //private bool GetOrGenDefinion(string key, out INamedTypeSymbol type)
        //{
        //    return GetOrGenDefinion(document.Definitions[key], out type);
        //}

        private bool GetOrGenDefinion(string name, OpenApiSchema definition, out INamedTypeSymbol type)
        {
            bool generated = true;
            if (!cacheModels.TryGetValue(name, out type))
            {
                generated = false;
                cacheModels[name] = null;
                type = GetReferenceType(name, definition);
                if (type == null)
                {
                    generated = true;
                    cacheModels[name] = type = GenDefinition(name, definition);
                }
            }
            else if (type == null)
            {
                generated = false;
                type = GetReferenceType(name, definition);
            }
            return generated;
        }

        public string GetClientName(string path, OpenApiPathItem pathItem)
        {
            return GetClientName(path, pathItem.Operations.First().Value);
        }

        public string GetClientName(string path, OpenApiOperation operation)
        {
            if (operation.Tags?.Count > 0)
                return operation.Tags[0].Name;
            else
            {
                foreach (var step in path.SpanSplit('/'))
                {
                    if (MemoryExtensions.Equals(step.Span, CApi.AsSpan(), StringComparison.Ordinal)
                        || (step.Length > 0 && step.Span[0] == '{'))
                        continue;
                    return step.Span.ToString();
                }
                return path.Replace("/", "").Replace("{", "").Replace("}", "");
            }
        }

        public string GetOperationName(string path, OpenApiPathItem pathItem, out string clientName)
        {
            clientName = GetClientName(path, pathItem);

            foreach (var step in path.SpanSplit('/'))
            {
                if (MemoryExtensions.Equals(step.Span, CApi.AsSpan(), StringComparison.Ordinal)
                    || MemoryExtensions.Equals(step.Span, clientName.AsSpan(), StringComparison.Ordinal)
                    || (step.Length > 0 && step.Span[0] == '{'))
                    continue;
                return step.Span.ToString();
            }
            return pathItem.Operations.FirstOrDefault().Value?.OperationId;
        }

        private HashSet<RefField> GetClientReferences(OpenApiSchema clientSchema)
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

        private string GetTableBaseType(string clientName, HashSet<string> usings, out string postfix)
        {
            postfix = CWebClient;
            idKey = default;
            typeKey = default;
            typeId = 0;
            var logged = document.Paths
                .Where(p => p.Value.Operations.Values.Any(op => op.Tags.Any(t => string.Equals(t.Name, clientName, StringComparison.OrdinalIgnoreCase))))
                .FirstOrDefault(p => p.Key.IndexOf("/GetItemLogs/", StringComparison.OrdinalIgnoreCase) > -1);
            var loggedReturnSchema = logged.Value == null ? null : GetReturningTypeSchema(logged.Value);
            var loggedTypeName = loggedReturnSchema == null ? null : GetArrayElementTypeString(loggedReturnSchema, usings);
            OpenApiSchema schema;
            if (IsSchemaClient(clientName, out schema))
            {
                postfix = CWebTable;
                idKey = schema.GetPrimaryKey();
                typeKey = schema.GetTypeKey();
                typeId = schema.GetTypeId();

                return $"{(loggedTypeName != null ? "Logged" : "")}WebTable<{clientName}, {(idKey.Value == null ? "int" : GetTypeString(idKey.Value, usings, CList))}{(loggedTypeName != null ? $", {loggedTypeName}" : "")}>";
            }
            return CWebClient;
        }

        private bool IsSchemaClient(string clientName, out OpenApiSchema schema)
        {
            return document.Components.Schemas.TryGetValue(clientName, out schema);
        }

        private OpenApiSchema GetReturningTypeSchema(OpenApiPathItem pathItem)
        {
            return GetReturningTypeSchema(pathItem.Operation());
        }

        private OpenApiSchema GetReturningTypeSchema(OpenApiOperation operation)
        {
            return operation.Responses.TryGetValue(C200, out var responce)
                ? responce.Content.FirstOrDefault().Value?.Schema
                : null;
        }

        private string GetReturningType(OpenApiPathItem pathItem, HashSet<string> usings)
        {
            return GetReturningType(pathItem.Operation(), usings);
        }

        private string GetReturningType(OpenApiOperation operation, HashSet<string> usings)
        {
            var returnType = Helper.cString;
            var schema = GetReturningTypeSchema(operation);
            if (schema != null)
            {
                returnType = GetTypeString(schema, usings, CList);
            }
            return returnType;
        }

        private string GetReturningTypeCheck(OpenApiOperation operation, string operationName, HashSet<string> usings)
        {
            var returnType = GetReturningType(operation, usings);
            if (string.Equals(operationName, "GenerateId", StringComparison.Ordinal))
                returnType = CTypeObject;
            //if (returnType == "AccessValue")
            //    returnType = "IAccessValue";
            //if (returnType == "List<AccessItem>")
            //    returnType = "IEnumerable<IAccessItem>";
            return returnType;
        }

        private INamedTypeSymbol GetReferenceType(OpenApiSchema definition)
        {
            return GetReferenceType(GetDefinitionName(definition), definition);
        }

        private INamedTypeSymbol GetReferenceType(string definitionName, OpenApiSchema definition)
        {
            if (!cacheReferences.TryGetValue(definitionName, out var type))
            {
                if (definitionName.Equals(nameof(TimeSpan), StringComparison.OrdinalIgnoreCase))
                {
                    type = Compilation.GetTypeByMetadataName(typeof(TimeSpan).FullName);
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
                                if (parsedType.EnumUnderlyingType != null)
                                {
                                    if (definition.Enum == null || definition.Enum.Count == 0)
                                        continue;
                                    definition.Extensions.TryGetValue("x-enumNames", out var defiEnumeration);
                                    var typeEnumeration = parsedType.GetMembers().OfType<IFieldSymbol>().Select(p => p.Name);
                                    var apiEnumeration = ((IList)defiEnumeration).OfType<OpenApiString>().Select(p => p.Value);
                                    if (apiEnumeration.SequenceEqual(typeEnumeration))
                                    {
                                        type = parsedType;
                                        break;
                                    }
                                }
                                else if (definition.Properties != null)
                                {
                                    var defiProperties = definition.Properties.Keys;
                                    var typeProperties = parsedType.GetMembers()
                                        .OfType<IPropertySymbol>()
                                        .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public)
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
                            Helper.ConsoleWarning($"Can't Check Type {definitionName} on {reference}. {ex.GetType().Name} {ex.Message}");
                        }
                    }
                }
                cacheReferences[definitionName] = type;
            }
            return type;
        }

        private INamedTypeSymbol GenDefinition(string name, OpenApiSchema schema)
        {
            usings.Clear();
            usings.Add(CNsDataWFCommon);
            usings.Add(CNsSystem);

            var source = schema.Enum != null && schema.Enum.Count > 0
                ? GenDefinitionEnum(name, schema, usings)
                : GenDefinitionClass(name, schema, usings);

            var syntaxTree = GenUnit($"{Namespace}.{GetDefinitionName(schema)}DefenitionGen.cs", source, usings);
            return GenUnit(syntaxTree);
        }

        private StringBuilder GenDefinitionEnum(string name, OpenApiSchema schema, HashSet<string> usings)
        {
            Helper.AddUsing("System.Runtime.Serialization", usings);

            source.Clear();
            source.Append($@"
    {((schema.Extensions?.TryGetValue("x-flags", out _) ?? false) ? "[Flags]" : "")}
    public enum {GetDefinitionName(schema)}
    {{");
            GenDefinitionEnumMemebers(source, schema);
            source.Append($@"
    }}");
            return source;
        }

        private void GenDefinitionEnumMemebers(StringBuilder source, OpenApiSchema schema)
        {
            IList members = null;
            if (schema.Extensions != null
                && schema.Extensions.TryGetValue("x-enumMembers", out var enumMembers))
            {
                members = (IList)enumMembers;
            }
            int i = 0;
            IList names = null;
            schema.Extensions.TryGetValue("x-enumNames", out var enumNames);
            names = (IList)enumNames;

            foreach (OpenApiInteger item in schema.Enum)
            {
                var sitem = (OpenApiString)names[i];
                var memeber = sitem;
                if (members != null && members.Count > i)
                    memeber = members[i] as OpenApiString;
                i++;
                source.Append($@"
        [EnumMember(Value = ""{ memeber.Value }"")]
        {sitem.Value} = {item.Value},");
            }
        }

        private StringBuilder GenDefinitionClass(string name, OpenApiSchema schema, HashSet<string> usings)
        {
            usings.Add("System.Text.Json.Serialization");
            var refFields = referenceFields[schema] = new List<RefField>();
            source.Clear();
            source.Append($@"
    public partial class {name}: {string.Join(", ", GenDefinitionClassBases(schema, usings))}
    {{");
            GenDefinitionClassMemebers(schema, refFields, usings);

            source.Append($@"
    }}");
            return source;
        }

        private IEnumerable<string> GenDefinitionClassBases(OpenApiSchema schema, HashSet<string> usings)
        {
            var inderited = schema.InheritedSchema();
            if (inderited != null)
            {
                var name = GetDefinitionName(inderited);
                var type = GetReferenceType(name, inderited);
                if (type != null)
                {
                    Helper.AddUsing(type, usings);
                }
                yield return name;
            }
            else
            {
                //yield return SF.SimpleBaseType(SF.ParseTypeName(nameof(IEntryNotifyPropertyChanged)));
                yield return "SynchronizedItem";
            }
            var idKey = schema.GetPrimaryKey(false);
            if (idKey.Value != null)
            {
                yield return "IPrimaryKey";
                yield return "IQueryFormatable";
            }
        }

        private void GenDefinitionClassMemebers(OpenApiSchema schema, List<RefField> refFields, HashSet<string> usings)
        {
            idKey = schema.GetPrimaryKey();
            typeKey = schema.GetTypeKey();
            typeId = schema.GetTypeId();
            var localIdKey = schema.GetPrimaryKey(false);
            var definition = GetDefinitionName(schema);
            var inheritedName = GetTypeString(schema.InheritedSchema(), usings);
            if (inheritedName == Helper.cString
                //|| inheritedName == "DefaultItem"
                || inheritedName == "SynchronizedItem")
            {
                source.Append($@"
        [JsonIgnore]    
        public new {SchemaName} Schema
        {{
            get => ({SchemaName})base.Schema;
        }}");
            }
            foreach (var property in schema.Properties)
            {
                GenDefinitionClassField(schema, property, refFields, usings);
            }

            GenDefinitionClassConstructorBody(schema, refFields, usings);

            if (refFields.Count > 0 && localIdKey.Value == null)
            {
                GenDefinitionClassProperty(schema, idKey, refFields, usings, true);
            }

            foreach (var property in schema.Properties)
            {
                GenDefinitionClassProperty(schema, property, refFields, usings);
            }

            if (localIdKey.Value != null)
            {
                var converter = "";
                var idType = GetTypeString(localIdKey.Value, usings, CList);
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
                source.Append($@"
        [JsonIgnore]    
        public object PrimaryKey 
        {{
            get => {idKey.Key};
            set => {idKey.Key} = {converter};
        }}

        public string Format() => {idKey.Key}.ToString();");
            }
        }

        private void GenDefinitionClassField(OpenApiSchema schema, KeyValuePair<string, OpenApiSchema> property, List<RefField> refFields, HashSet<string> usings)
        {
            var refkey = property.Value.GetPropertyRefKey();
            var isArray = property.Value.Type == CTypeArray;
            if (refkey != null && isArray)
            {
                var refProperty = property.Value.Items.GetReferenceProperty(refkey);
                var refField = new RefField
                {
                    Property = property.Value,
                    PropertyName = property.Key,
                    Definition = schema.Title,
                    TypeSchema = property.Value.Items,
                    TypeName = GetTypeString(property.Value.Items, usings),
                    KeyName = refkey,
                    KeyProperty = property.Value.Items.GetProperty(refkey).Value,
                    ValueName = refProperty.Key,
                    ValueProperty = refProperty.Value,
                    FieldType = GetTypeString(property.Value, usings, "ReferenceList"),
                    FieldName = GetFieldName(property.Key),
                };
                refFields.Add(refField);
                //var refTypePrimary = GetPrimaryKey(refTypeSchema);
                //var refTypePrimaryName = GetPropertyName(refTypePrimary);
                //var refTypePrimaryType = GetTypeString(refTypePrimary, true, "SelectableList");                
                refField.KeyType = GetTypeString(refField.KeyProperty, usings);
                refField.ValueType = GetTypeString(refField.ValueProperty, null);
                refField.ValueFieldName = GetFieldName(refField.ValueName);
                refField.InvokerName = $"{refField.TypeName}.{refkey}Invoker.Instance";

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

                source.Append($@"
        protected {refField.FieldType} {refField.FieldName};");
            }
            else
            {
                var type = GetTypeString(property.Value, usings);

                source.Append($@"
        protected internal {type} {GetFieldName(property.Key)}{(isArray ? $" = new {type}()" : "")};");
            }
        }

        private void GenDefinitionClassConstructorBody(OpenApiSchema schema, List<RefField> refFields, HashSet<string> usings)
        {
            source.Append($@"
        public {GetDefinitionName(schema)}()
        {{");
            if (typeId != 0)
            {
                source.Append($@"
            {typeKey.Key} = {typeId};");
            }
            foreach (var refField in refFields)
            {
                //yield return SF.ParseStatement($@"{refField.FieldName} = new {refField.FieldType}(
                //new Query<{refField.TypeName}>(new[]{{{refField.ParameterName}}}),
                //Provider.{refField.TypeName}.Items,
                //false);");
                source.Append($@"
            {refField.FieldName} = new {refField.FieldType} (this, nameof({refField.PropertyName}));");
            }
            foreach (var property in schema.Properties)
            {
                if (property.Value.Default != null)
                {
                    source.Append($@"
            {property.Key} = {GenFieldDefault(property, idKey, usings)};");
                }
            }
            source.Append($@"
        }}");
        }

        private IEnumerable<StatementSyntax> GenDefintionClassPropertySynckGet(List<RefField> refFields)
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

        private void GenDefinitionClassProperty(OpenApiSchema schema, KeyValuePair<string, OpenApiSchema> property, List<RefField> refFields, HashSet<string> usings, bool isOverride = false)
        {
            var refkey = property.Value.GetPropertyRefKey();
            var typeDeclaration = GetTypeString(property.Value, usings, refkey == null ? "SelectableList" : "ReferenceList");
            source.Append($@"
        [{string.Join(", ", GenDefinitionClassPropertyAttributes(schema, property, usings))}]
        public {(isOverride ? "override " : property.Key == idKey.Key ? "virtual " : "")}{typeDeclaration} {property.Key} 
        {{");
            GenDefintionClassPropertyGet(schema, property, isOverride);
            GenDefinitionClassPropertySet(schema, property, refFields, usings, isOverride);
            source.Append($@"
        }}");
        }

        private IEnumerable<string> GenDefinitionClassPropertyAttributes(OpenApiSchema schema, KeyValuePair<string, OpenApiSchema> property, HashSet<string> usings)
        {
            var propertySchema = property.Value;
            if (propertySchema.ReadOnly
                || (propertySchema.Extensions != null
                    && propertySchema.Extensions.TryGetValue("readOnly", out var isReadOnly)
                    && isReadOnly.ToString() == "true"))
            {
                yield return "JsonIgnoreSerialization";
            }
            else if (string.Equals(property.Key, typeKey.Key, StringComparison.Ordinal))
            {
                Helper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                yield return $"Display(Order = -3)";
            }
            else if (string.Equals(property.Key, idKey.Key, StringComparison.Ordinal))
            {
                Helper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                yield return $"Display(Order = -2)";
            }
            else if ((propertySchema.Type == CTypeObject
                && !propertySchema.IsEnumeration()
                && propertySchema.AllInheritedSchemas().Any(p => p.Title == "DBItem"))
                || (propertySchema.InheritedSchema() != null
                && propertySchema.InheritedSchema().Type == CTypeObject
                && !propertySchema.InheritedSchema().IsEnumeration()
                && propertySchema.InheritedSchema().AllInheritedSchemas().Any(p => p.Title == "DBItem")))
            {
                yield return "JsonSynchronized";
            }
            else //if (!property.IsRequired)
            {
                yield return "JsonSynchronized";
            }

            foreach (var attribute in GenDefinitionClassPropertyValidationAttributes(schema, property, usings))
            {
                yield return attribute;
            }
        }

        private IEnumerable<string> GenDefinitionClassPropertyValidationAttributes(OpenApiSchema schema, KeyValuePair<string, OpenApiSchema> property, HashSet<string> usings)
        {
            var propertyName = property.Key;
            var objectProperty = schema.GetReferenceProperty(propertyName);
            if (objectProperty.Value != null)
            {
                propertyName = objectProperty.Key;
            }
            var isRequered = schema.Required.Contains(propertyName);
            if (isRequered)
            {
                Helper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                if (property.Key == idKey.Key || property.Key == typeKey.Key)
                {
                    yield return $"Required(AllowEmptyStrings = true)";
                }
                else
                {
                    yield return $"Required(ErrorMessage = \"{propertyName} is required\")";
                }
            }

            if (property.Value.MaxLength != null)
            {
                Helper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                yield return $"MaxLength({property.Value.MaxLength}, ErrorMessage = \"{propertyName} only max {property.Value.MaxLength} letters allowed.\")";
            }
            var idProperty = schema.GetReferenceIdProperty(property);
            if (idProperty.Value != null)
            {
                foreach (var attribute in GenDefinitionClassPropertyValidationAttributes(schema, idProperty, usings))
                {
                    yield return attribute;
                }
            }
        }

        private void GenDefintionClassPropertyGet(OpenApiSchema schema, KeyValuePair<string, OpenApiSchema> property, bool isOverride)
        {
            var fieldName = GetFieldName(property.Key);
            if (isOverride)
            {
                source.Append($@"
            get => base.{property.Key};");
                return;
            }
            //var refkey = property.Value.GetPropertyRefKey();
            //if (refkey != null)
            //{
            //    yield return SF.ParseStatement($"{fieldName}.UpdateFilter();");
            //}
            // Change - refence from single json
            //if (property.Extensions != null && property.Extensions.TryGetValue("x-id", out var idProperty))
            //{
            //    var idFiledName = GetFieldName((string)idProperty);
            //    yield return SF.ParseStatement($"if({fieldName} == null && {idFiledName} != null){{");
            //    yield return SF.ParseStatement($"{fieldName} = {GetTypeString(property, false, "List")}Client.Instance.Select({idFiledName}.Value);");
            //    yield return SF.ParseStatement("}");
            //}
            source.Append($@"
            get => {fieldName};");
        }

        private void GenDefinitionClassPropertySet(OpenApiSchema schema, KeyValuePair<string, OpenApiSchema> property, List<RefField> refFields, HashSet<string> usings, bool isOverride)
        {
            source.Append($@"
            set
            {{");

            if (!isOverride)
            {
                var peopertySchema = property.Value;
                var type = GetTypeString(peopertySchema, usings, "SelectableList");
                var fieldName = GetFieldName(property.Key);
                if (type.Equals(Helper.cString, StringComparison.Ordinal))
                {
                    source.Append($@"
                if (string.Equals({fieldName}, value, StringComparison.Ordinal)) return;");
                }
                else
                {
                    source.Append($@"
                if ({fieldName} == value) return;");
                }
                source.Append($@"
                var temp = {fieldName};
                {fieldName} = value;");
                if (peopertySchema.Extensions != null && peopertySchema.Extensions.TryGetValue(OpenApiExtendions.cXId, out var refPropertyString))
                {
                    var refPropertyName = ((OpenApiString)refPropertyString).Value;
                    var idProperty = peopertySchema.InheritedSchema().GetPrimaryKey();
                    var refProperty = schema.GetProperty(refPropertyName.ToString());
                    var refType = GetTypeString(refProperty.Value, usings);
                    if (idProperty.Value.Nullable)
                        source.Append($@"
                {refPropertyName} = value?.{idProperty.Key};");
                    else
                        source.Append($@"
                {refPropertyName} = value?.{idProperty.Key} ?? default({refType});");
                }
                //Change - refence from single json
                var objectProperty = schema.GetReferenceProperty(property.Key);
                if (objectProperty.Value != null)
                {
                    var objectFieldName = GetFieldName(objectProperty.Key);
                    var clientName = GetTypeString(objectProperty.Value, usings, CList);
                    if (objectProperty.Value.Extensions != null
                        && objectProperty.Value.Extensions.TryGetValue("x-derived", out var derivedClass))
                        clientName = ((OpenApiString)derivedClass).Value;
                    source.Append($@"
                if ({objectFieldName}?.Id != value)
                {{
                    {objectFieldName} = value == default({type}) ? null : Schema?.{clientName}.Select(value{(type.EndsWith("?") ? ".Value" : "")});
                    OnPropertyChanged(nameof({objectProperty.Key}));
                }}");
                }
                source.Append($@"
                OnPropertyChanged(temp, value);");
            }
            else
            {
                source.Append($@"
                base.{property.Key} = value;");
            }
            //if (property.Name == idKey?.Name)
            //{
            //    foreach (var refField in refFields)
            //    {
            //        yield return SF.ParseStatement($"{refField.ParameterName}.Value = value;");
            //    }
            //}
            source.Append($@"
            }}");
        }

        private string GetDefinitionName(OpenApiSchema schema)
        {
            return schema.Title;
        }

        private string GetDefinitionName(string name)
        {
            return name.ToInitcap();
        }

        private string GetFieldName(string property)
        {
            return string.Concat("_", property.ToLowerCap());
        }

        private string GenFieldDefault(KeyValuePair<string, OpenApiSchema> property,
            KeyValuePair<string, OpenApiSchema> idKey, HashSet<string> usings)
        {
            var text = property.Value.Default.Format();
            var type = GetTypeString(property.Value, usings).TrimEnd('?');
            if (string.Equals(type, "bool", StringComparison.Ordinal))
                text = text.ToLowerInvariant();
            else if (string.Equals(type, Helper.cString, StringComparison.Ordinal))
                text = $"\"{text}\"";
            else if (property.Value.InheritedSchema()?.IsEnumeration() == true)
                text = $"{type}.{text}";
            return text;
        }

        private string GetArrayElementTypeString(OpenApiSchema schema, HashSet<string> usings)
        {
            return schema.Items != null
                ? GetTypeString(schema.Items, usings, CList)
                : null;
        }

        private string GetTypeString(OpenApiParameter parameter, HashSet<string> usings, string listType = "SelectableList", bool? baseNullable = null)
        {
            return GetTypeString(parameter.Schema, usings, listType, baseNullable);
        }

        private string GetTypeString(OpenApiSchema schema, HashSet<string> usings, string listType = "SelectableList", bool? baseNullable = null)
        {
            if (schema == null)
                return Helper.cString;

            var nullable = baseNullable ?? schema.Nullable;
            switch (schema.Type)
            {
                case "integer":
                    if (schema.IsEnumeration())
                    {
                        goto case CTypeObject;
                    }
                    if (schema.Format == "int64")
                    {
                        return "long" + (nullable ? "?" : string.Empty);
                    }
                    return "int" + (nullable ? "?" : string.Empty);
                case "boolean":
                    return "bool" + (nullable ? "?" : string.Empty);
                case "number":
                    if (schema.IsEnumeration())
                    {
                        goto case CTypeObject;
                    }
                    if (string.IsNullOrEmpty(schema.Format))
                    {
                        return "decimal" + (nullable ? "?" : string.Empty);
                    }
                    return schema.Format + (nullable ? "?" : string.Empty);
                case "string":
                    if (schema.IsEnumeration())
                    {
                        goto case CTypeObject;
                    }
                    switch (schema.Format)
                    {
                        case "byte":
                            return "byte[]";
                        case "binary":
                            Helper.AddUsing("System.IO", usings);
                            return Helper.cStream;
                        case "date":
                        case "date-time":
                            return "DateTime" + (nullable ? "?" : string.Empty);
                        default:
                            return Helper.cString;
                    }
                case "array":
                    return $"{listType}<{GetTypeString(schema.Items, usings, listType)}>";
                case "":
                case null:
                case "none":
                    if (schema.InheritedSchema() != schema)
                    {
                        return GetTypeString(schema.InheritedSchema(), usings, listType, nullable);
                    }
                    else
                    {
                        goto case CTypeObject;
                    }
                case CTypeObject:
                    if (schema.Title != null)
                    {
                        var type = GetReferenceType(schema);
                        if (type != null)
                        {
                            Helper.AddUsing(type, usings);
                        }
                        if (schema.IsEnumeration())
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
                        return CTypeObject;
                    }
                    break;
                case "file":
                    Helper.AddUsing("System.IO", usings);
                    return Helper.cStream;
            }

            return Helper.cString;
        }

        public SyntaxTree GenUnit(string name, StringBuilder source, HashSet<string> usings)
        {
            var unitSource = new StringBuilder();
            foreach (var item in usings.OrderBy(p => p))
            {
                unitSource.Append($@"
using {item};");
            }
            unitSource.Append($@"

namespace {Namespace}
{{
");
            source.Insert(0, unitSource);
            source.Append($@"
}}");
            var sourceText = SourceText.From(source.ToString(), Encoding.UTF8);
            CompilationContext.Context.AddSource(name, sourceText);
            usings.Clear();
            base.source.Clear();
            return CSharpSyntaxTree.ParseText(sourceText, (CSharpParseOptions)Options);
        }

        public INamedTypeSymbol GenUnit(SyntaxTree syntaxTree)
        {
            if (syntaxTree == null)
                return null;

            CompilationContext.Compilation = Compilation.AddSyntaxTrees(syntaxTree);
            var unitSyntax = (CompilationUnitSyntax)syntaxTree.GetRoot();
            var nameSpaceSyntax = unitSyntax.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            var classSyntax = nameSpaceSyntax.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classSyntax != null)
            {
                InvokerGenerator.Process(classSyntax);
                return InvokerGenerator.TypeSymbol;
            }
            var enumSyntax = nameSpaceSyntax.Members.OfType<EnumDeclarationSyntax>().FirstOrDefault();
            if (enumSyntax != null)
            {
                return GetSymbol(enumSyntax);
            }
            return null;
        }

        public INamedTypeSymbol ParseType(Type value, IEnumerable<IAssemblySymbol> assemblies)
        {
            return ParseType(value.FullName, assemblies);
        }

        public INamedTypeSymbol ParseType(string value, IEnumerable<IAssemblySymbol> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var type = ParseType(value, assembly);
                if (type != null)
                    return type;
            }
            return null;
        }

        public INamedTypeSymbol ParseType(string value, IAssemblySymbol assembly)
        {
            var byName = value.IndexOf('.') < 0;
            if (byName)
            {
                if (!cacheAssemblySymbolTypes.TryGetValue(assembly.Name, out var cache))
                {
                    var definedTypes = assembly.GlobalNamespace.GetTypes();
                    cacheAssemblySymbolTypes[assembly.Name] =
                        cache = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
                    foreach (var defined in definedTypes)
                    {
                        if (defined.DeclaredAccessibility == Accessibility.Public)
                            cache[defined.Name] = defined;
                    }
                }

                return cache.TryGetValue(value, out var type) ? type : null;
            }
            else
            {
                return assembly.GetTypeByMetadataName(value);
            }
        }

        private class RefField
        {
            public OpenApiSchema KeyProperty;
            public string KeyName;
            public string KeyType;
            public OpenApiSchema TypeSchema;
            public string TypeName;
            public string InvokerName;
            //public string InvokerType;
            //public string ParameterType;
            //public string ParameterName;
            public OpenApiSchema Property;
            public string PropertyName;
            public string FieldType;
            public string FieldName;
            public string Definition;
            public OpenApiSchema ValueProperty;
            public string ValueName;
            public string ValueType;
            public string ValueFieldName;

        }
    }

    public static class OpenApiExtendions
    {
        public const string cXId = "x-id";
        public const string cXType = "x-type";

        public static string Format(this IOpenApiAny apiAny)
        {
            switch (apiAny.AnyType)
            {
                case AnyType.Primitive:
                    var primitive = (IOpenApiPrimitive)apiAny;
                    switch (primitive.PrimitiveType)
                    {
                        case PrimitiveType.Integer:
                            return ((OpenApiInteger)primitive).Value.ToString();
                        case PrimitiveType.Long:
                            return ((OpenApiLong)primitive).Value.ToString();
                        case PrimitiveType.Float:
                            return ((OpenApiFloat)primitive).Value.ToString(CultureInfo.InvariantCulture);
                        case PrimitiveType.Double:
                            return ((OpenApiDouble)primitive).Value.ToString(CultureInfo.InvariantCulture);
                        case PrimitiveType.String:
                            return ((OpenApiString)primitive).Value;
                        case PrimitiveType.Byte:
                            return ((OpenApiByte)primitive).Value.ToString();
                        case PrimitiveType.Binary:
                            return ((OpenApiBinary)primitive).Value.ToString();
                        case PrimitiveType.Boolean:
                            return ((OpenApiBoolean)primitive).Value.ToString();
                        case PrimitiveType.Date:
                            return ((OpenApiDate)primitive).Value.ToString(CultureInfo.InvariantCulture);
                        case PrimitiveType.DateTime:
                            return ((OpenApiDateTime)primitive).Value.ToString();
                        case PrimitiveType.Password:
                            return ((OpenApiPassword)primitive).Value.ToString();
                        default:
                            return primitive.ToString();
                    }
                case AnyType.Null:
                    return "null";
                case AnyType.Array:
                    return "Array";
                case AnyType.Object:
                default:
                    return "object";
            };
        }

        public static KeyValuePair<string, OpenApiSchema> GetProperty(this OpenApiSchema schema, string propertyName)
        {
            if (schema.Properties.TryGetValue(propertyName, out var property))
            {
                property.Title = propertyName;

                return new KeyValuePair<string, OpenApiSchema>(propertyName, property);
            }
            foreach (var baseClass in schema.AllInheritedSchemas())
            {
                if (baseClass.Properties.TryGetValue(propertyName, out property))
                {
                    property.Title = propertyName;

                    return new KeyValuePair<string, OpenApiSchema>(propertyName, property);
                }
            }

            return default;
        }

        public static KeyValuePair<string, OpenApiSchema> GetPrimaryKey(this OpenApiSchema schema, bool inherit = true)
        {
            IOpenApiExtension propertyName = null;
            if (schema.Extensions?.TryGetValue(cXId, out propertyName) == true)
            {
                return schema.GetProperty(((OpenApiString)propertyName).Value);
            }
            if (inherit)
            {
                foreach (var baseClass in schema.AllInheritedSchemas())
                {
                    if (baseClass.Extensions?.TryGetValue(cXId, out propertyName) == true)
                    {
                        return baseClass.GetProperty(((OpenApiString)propertyName).Value);
                    }
                }
            }
            return default;
        }

        public static KeyValuePair<string, OpenApiSchema> GetReferenceIdProperty(this OpenApiSchema schema, KeyValuePair<string, OpenApiSchema> property, bool inherit = true)
        {
            if (property.Value.Extensions != null && property.Value.Extensions.TryGetValue(cXId, out var propertyName))
            {
                return schema.GetProperty(((OpenApiString)propertyName).Value);
            }
            return default;
        }

        public static KeyValuePair<string, OpenApiSchema> GetReferenceProperty(this OpenApiSchema schema, string name)
        {
            var find = schema.Properties.FirstOrDefault(p => p.Value.Extensions != null
                 && p.Value.Extensions.TryGetValue(cXId, out var propertyName)
                 && string.Equals(((OpenApiString)propertyName).Value, name, StringComparison.Ordinal));
            if (find.Value != null)
            {
                return find;
            }
            foreach (var baseClass in schema.AllInheritedSchemas())
            {
                find = baseClass.Properties.FirstOrDefault(p => p.Value.Extensions != null
                        && p.Value.Extensions.TryGetValue(cXId, out var propertyName)
                        && string.Equals(((OpenApiString)propertyName).Value, name, StringComparison.Ordinal));
                if (find.Value != null)
                {
                    return find;
                }
            }
            return new KeyValuePair<string, OpenApiSchema>();
        }

        public static string GetPropertyRefKey(this OpenApiSchema schema)
        {
            return schema.Extensions != null
                ? schema.Extensions.TryGetValue("x-ref-key", out var key) ? ((OpenApiString)key).Value : null
                : null;
        }

        public static KeyValuePair<string, OpenApiSchema> GetTypeKey(this OpenApiSchema schema)
        {
            if (schema.Extensions != null && schema.Extensions.TryGetValue(cXType, out var propertyName))
            {
                return schema.GetProperty(((OpenApiString)propertyName).Value);
            }

            foreach (var baseClass in schema.AllInheritedSchemas())
                if (baseClass.Extensions != null && baseClass.Extensions.TryGetValue(cXType, out propertyName))
                {
                    return baseClass.GetProperty(((OpenApiString)propertyName).Value);
                }

            return default;
        }

        public static int GetTypeId(this OpenApiSchema schema)
        {
            if (schema.Extensions != null && schema.Extensions.TryGetValue("x-type-id", out var id))
            {
                return id is OpenApiInteger openApiInteger
                    ? openApiInteger.Value
                    : int.TryParse(id.ToString(), out var typeid) 
                        ? typeid
                        : Convert.ToInt32(id);
            }

            return 0;
        }

        public static OpenApiSchema InheritedSchema(this OpenApiSchema apiSchema)
        {
            return apiSchema.AllOf?.Count != 0 ? apiSchema.AllOf[0] : null;
        }

        public static IEnumerable<OpenApiSchema> AllInheritedSchemas(this OpenApiSchema apiSchema)
        {
            var inherited = apiSchema.InheritedSchema();
            while (inherited != null)
            {
                yield return inherited;
                inherited = inherited.InheritedSchema();
            }
        }

        public static bool IsEnumeration(this OpenApiSchema apiSchema)
        {
            return apiSchema.Enum?.Count > 0;
        }

        public static OpenApiOperation Operation(this OpenApiPathItem pathItem)
        {
            return pathItem.Operations.FirstOrDefault().Value;
        }

        public static OperationType OperationType(this OpenApiPathItem pathItem)
        {
            return pathItem.Operations.FirstOrDefault().Key;
        }

        public static OpenApiResponse Responce(this OpenApiOperation operation)
        {
            return operation.Responses.TryGetValue("200", out var responce) ? responce : null;
        }
    }
}
