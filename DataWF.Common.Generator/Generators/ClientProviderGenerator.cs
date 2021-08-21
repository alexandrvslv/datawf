using DataWF.Common.Generator;
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

namespace DataWF.Common.Generator
{
    internal class ClientProviderGenerator : BaseGenerator
    {
        private readonly HashSet<string> VirtualOperations = new HashSet<string>
        {
            "GetAsync",
            "PutAsync",
            "PostAsync",
            "PostPackageAsync",
            "SearchAsync",
            "DeleteAsync",
            "CopyAsync",
            "GenerateIdAsync",
            "MergeAsync",
            "GetItemLogsAsync",
            "GetLogsAsync",
            "RedoLogAsync",
            "RemoveLogAsync",
            "UndoLogAsync"
        };
        private readonly Dictionary<string, INamedTypeSymbol> cacheModels = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
        private readonly Dictionary<string, INamedTypeSymbol> cacheReferences = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
        private readonly Dictionary<string, StringBuilder> cacheClients = new Dictionary<string, StringBuilder>(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> cacheUsings = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<AttributeListSyntax>> cacheAttributes = new Dictionary<string, List<AttributeListSyntax>>(StringComparer.Ordinal);

        private readonly Dictionary<JsonSchema, List<RefField>> referenceFields = new Dictionary<JsonSchema, List<RefField>>();
        private OpenApiDocument document;
        private List<IAssemblySymbol> usingReferences = new List<IAssemblySymbol>();

        public ClientProviderGenerator(ref GeneratorExecutionContext context)
            : base(ref context)
        {
        }

        public InvokerGenerator InvokerGenerator { get; set; }

        public string DocumentSource { get; set; }
        public AttributeData Attribute { get; private set; }
        public string Namespace { get; set; }
        public HashSet<IAssemblySymbol> References { get; } = new HashSet<IAssemblySymbol>();
        public string ProviderName { get; set; }

        public override bool Process()
        {
            Attribute = TypeSymbol.GetAttribute(Attributes.ClientProvider);
            Namespace = TypeSymbol.ContainingNamespace.ToDisplayString();
            ProviderName = TypeSymbol.Name;

            LoadDocument();

            LoadReferences();

            foreach (var definition in document.Definitions)
            {
                definition.Value.Id = definition.Key;
            }
            foreach (var definition in document.Definitions)
            {
                GetOrGenDefinion(definition.Value, out _);
            }
            foreach (var operation in document.Operations)
            {
                AddClientOperation(operation);
            }

            foreach (var entry in cacheClients)
            {
                var usings = cacheUsings[entry.Key];
                var unit = GenUnit($"{Namespace}.{entry.Key}Gen.cs", entry.Value, usings.OrderBy(p => p));
            }

            var provider = GenUnit($"{Namespace}.{TypeSymbol.Name}.Gen.cs", GenProvider(), usings: new List<string>()
            {
                "DataWF.Common",
                "DataWF.WebClient.Common",
                "System"
            });

            return true;
        }

        private void LoadDocument()
        {
            DocumentSource = Attribute.ConstructorArguments.FirstOrDefault().Value.ToString();
            if (!DocumentSource.StartsWith("http"))
            {
                var mainSyntaxTree = Compilation.SyntaxTrees
                          .First(x => x.HasCompilationUnitRoot);

                var projectDirectory = Path.GetDirectoryName(mainSyntaxTree.FilePath);
                DocumentSource = Path.GetFullPath(Path.Combine(projectDirectory, DocumentSource));
            }
            var url = new Uri(DocumentSource);
            if (url.Scheme == "http" || url.Scheme == "https")
                document = OpenApiDocument.FromUrlAsync(url.OriginalString).GetAwaiter().GetResult();
            else if (url.Scheme == "file")
                document = OpenApiDocument.FromFileAsync(url.LocalPath).GetAwaiter().GetResult();
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
                    References.Add(assemblySymbol);
                    if (usingReferenceNames?.Contains(assemblySymbol.Name) ?? false)
                    {
                        usingReferences.Add(assemblySymbol);
                    }
                }
            }
        }

        private StringBuilder GenProvider()
        {
            source = new StringBuilder($@"
    public partial class {ProviderName} {(TypeSymbol.BaseType?.Name != "ClientProviderBase" ? ": ClientProviderBase" : "")}
    {{
        public static {ProviderName} Default = new {ProviderName}();");

            GenProviderConstructor();

            foreach (var client in cacheClients.Keys)
            {
                source.Append($@"
        public {client}Client {client} {{ get; }}");
            }
            source.Append($@"
    }}");
            return source;
        }

        private void GenProviderConstructor()
        {
            source.Append($@"
        public {ProviderName}()
        {{");

            foreach (var client in cacheClients.Keys)
            {
                source.Append($@"
            Add({client} = new {client}Client(this);");
            }
            source.Append($@"
            RefreshTypedCache();
        }}");
        }

        private void AddClientOperation(OpenApiOperationDescription descriptor)
        {
            GetOperationName(descriptor, out var clientName);
            if (!cacheUsings.TryGetValue(clientName, out var usings))
            {
                cacheUsings[clientName] =
                    usings = new HashSet<string>(StringComparer.Ordinal)
                {
                    "DataWF.Common" ,
                    "System",
                    "System.Collections.Generic",
                    "System.Net.Http",
                    "System.Threading.Tasks",
                };
            }
            if (!cacheClients.TryGetValue(clientName, out var clientSyntax))
            {
                clientSyntax = GenClient(clientName, usings);
            }

            GenOperation(clientSyntax, descriptor, usings);
        }

        private StringBuilder GenClient(string clientName, HashSet<string> usings)
        {
            var baseType = GetClientBaseType(clientName, usings, out var idKey, out var typeKey, out var typeId);
            var clientSource = new StringBuilder();
            clientSource.Append($@"
namespace {Namespace} 
{{
    public partial class {clientName}Client: {baseType}
    {{");
            GenClientMembers(clientSource, clientName, idKey, typeKey, typeId, usings);
            return clientSource;
        }

        private void GenClientMembers(StringBuilder clientSource, string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId,
            HashSet<string> usings)
        {
            document.Definitions.TryGetValue(clientName, out var clientSchema);
            var typeName = $"{clientName}Client";

            clientSource.Append($@"
            //public static {typeName} Instance {{get; private set;}}
");
            var cache = clientSchema != null ? GetClientReferences(clientSchema) : new HashSet<RefField>();

            GenClientConstructor(clientSource, clientName, idKey, typeKey, typeId, cache);
            if (cache.Count > 0)
            {
                SyntaxHelper.AddUsing("System.Collections", usings);

                GenClientRemoveOverrideBody(clientSource, clientName, idKey, typeKey, typeId, cache);

                GenClientAddOverrideBody(clientSource, clientName, idKey, typeKey, typeId, cache);
            }
        }

        private void GenClientConstructor(StringBuilder clientSource, string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId, HashSet<RefField> cache)
        {
            var idName = idKey == null ? null : GetPropertyName(idKey);
            var typeName = typeKey == null ? null : GetPropertyName(typeKey);

            clientSource.Append($@"
        public {clientName}Client({ProviderName} provider)
            : base({(clientName == "Instance" ? Namespace + "." : "")}{clientName}.{idName}Invoker.Default,
                  {(clientName == "Instance" ? Namespace + "." : "")}{clientName}.{typeName}Invoker.Default,
                  {typeId})
        {{
            Provider = provider;
            //Instance = Instance ?? this;");

            foreach (var refField in cache)
            {
                clientSource.Append($@"
            Items.Indexes.Add({refField.InvokerName});");
            }
            clientSource.Append($@"
        }}");
        }

        private void GenClientRemoveOverrideBody(StringBuilder clientSource, string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId, HashSet<RefField> cache)
        {
            clientSource.Append($@"
        protected override void OnRemoved(IList items = null)
        {{
            base.OnRemoved(items);
            foreach ({clientName} item in items)
            {{");
            foreach (var refField in cache)
            {
                clientSource.Append($@"
                if (item.{refField.KeyName} != null)
                {{
                    var item{refField.ValueName} = (item.{refField.ValueFieldName} ?? (item.{refField.ValueFieldName} = Provider.{refField.ValueType}.Select(item.{refField.KeyName}.Value))) as {refField.Definition};
                    item{refField.ValueName}?.{refField.PropertyName}.Remove(item);
                }}");
            }
            clientSource.Append($@"
            }}
        }}");
        }

        private void GenClientAddOverrideBody(StringBuilder clientSource, string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId, HashSet<RefField> cache)
        {
            clientSource.Append($@"
        protected override void OnAdded(IList items = null)
        {{
            base.OnAdded(items);
            foreach ({clientName} item in items)
            {{");
            foreach (var refField in cache)
            {
                clientSource.Append($@"
                if (item.{refField.KeyName} != null)
                {{
                    var item{refField.ValueName} = (item.{refField.ValueFieldName} ?? (item.{refField.ValueFieldName} = Provider.{refField.ValueType}.Select(item.{refField.KeyName}.Value))) as {refField.Definition};
                    item{refField.ValueName}?.{refField.PropertyName}.Add(item);
                }}");
            }
            clientSource.Append($@"
            }}
        }}");
        }

        private void GenOperation(StringBuilder clientSource, OpenApiOperationDescription descriptor, HashSet<string> usings)
        {
            var operationName = GetOperationName(descriptor, out var clientName);
            var actualName = $"{operationName}Async";
            var baseType = GetClientBaseType(clientName, usings, out _, out _, out _);
            var isOverride = baseType != "ClientBase" && VirtualOperations.Contains(actualName);
            var returnType = GetReturningTypeCheck(descriptor, operationName, usings);
            returnType = returnType.Length > 0 ? $"Task<{returnType}>" : "Task";

            clientSource.Append($@"
        public {(isOverride ? "override " : "")}async {returnType} {actualName}(");
            GenOperationParameter(clientSource, descriptor, usings);
            clientSource.Append($@")
        {{");
            GenOperationBody(clientSource, actualName, descriptor, usings, isOverride);
            clientSource.Append($@"
        }}");
        }

        private void GenOperationParameter(StringBuilder clientSource, OpenApiOperationDescription descriptor, HashSet<string> usings)
        {
            foreach (var parameter in descriptor.Operation.Parameters)
            {
                if (parameter.Kind == OpenApiParameterKind.Header)
                    continue;
                clientSource.Append($"{GetTypeString(parameter, usings, "List")} {parameter.Name}, ");
            }
            var bodyParameter = descriptor.Operation.Parameters.FirstOrDefault(p => p.Kind == OpenApiParameterKind.Body || p.Kind == OpenApiParameterKind.FormData);
            var returnType = GetReturningType(descriptor, usings);
            if (bodyParameter == null && returnType.StartsWith("List<", StringComparison.Ordinal))
            {
                clientSource.Append($"HttpPageSettings pages, ");
            }
            clientSource.Append($"HttpJsonSettings settings, ");
            clientSource.Append($"ProgressToken progressToken, ");
        }

        private void GenOperationBody(StringBuilder clientSource, string actualName, OpenApiOperationDescription descriptor, HashSet<string> usings, bool isOverride)
        {
            var method = descriptor.Method.ToString().ToUpperInvariant();
            var responceSchema = (JsonSchema)null;
            var mediatype = "application/json";
            if (descriptor.Operation.Responses.TryGetValue("200", out var responce))
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

            clientSource.Append($@"
            return await Request<{returnType}>(progressToken, HttpMethod.{method.ToInitcap()}, ""{path}"", ""{mediatype}"", settings");
            var bodyParameter = descriptor.Operation.Parameters.FirstOrDefault(p => p.Kind == OpenApiParameterKind.Body || p.Kind == OpenApiParameterKind.FormData);
            if (bodyParameter == null)
            {
                if (returnType.StartsWith("List<", StringComparison.Ordinal))
                {
                    clientSource.Append(", pages");
                }
                else
                {
                    clientSource.Append(", null");
                }
            }
            else
            {
                clientSource.Append($", {bodyParameter.Name}");
            }
            foreach (var parameter in descriptor.Operation.Parameters.Where(p => p.Kind == OpenApiParameterKind.Path || p.Kind == OpenApiParameterKind.Query))
            {
                clientSource.Append($", {parameter.Name}");
            }
            clientSource.Append(").ConfigureAwait(false);");
        }

        private bool GetOrGenDefinion(string key, out INamedTypeSymbol type)
        {
            return GetOrGenDefinion(document.Definitions[key], out type);
        }

        private bool GetOrGenDefinion(JsonSchema definition, out INamedTypeSymbol type)
        {
            bool generated = true;
            if (!cacheModels.TryGetValue(definition.Id, out type))
            {
                generated = false;
                cacheModels[definition.Id] = null;
                type = GetReferenceType(definition);
                if (type == null)
                {
                    generated = true;
                    cacheModels[definition.Id] = type = GenDefinition(definition);
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
                foreach (var step in decriptor.Path.Split('/'))
                {
                    if (step == "api" || step.StartsWith("{"))
                        continue;
                    return step;
                }
                return decriptor.Path.Replace("/", "").Replace("{", "").Replace("}", "");
            }
        }

        public string GetOperationName(OpenApiOperationDescription descriptor, out string clientName)
        {
            clientName = GetClientName(descriptor);
            var name = new StringBuilder();
            foreach (var step in descriptor.Path.Split('/'))
            {
                if (step == "api"
                    || step == clientName
                    || step.StartsWith("{"))
                    continue;
                name.Append(step);
            }
            if (name.Length == 0)
                name.Append(descriptor.Method.ToString());
            name[0] = char.ToUpperInvariant(name[0]);
            return name.ToString();
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

        private string GetClientBaseType(string clientName, HashSet<string> usings, out JsonSchemaProperty idKey, out JsonSchemaProperty typeKey, out int typeId)
        {
            idKey = null;
            typeKey = null;
            typeId = 0;
            var logged = document.Operations
                .Where(p => p.Operation.Tags.Contains(clientName, StringComparer.OrdinalIgnoreCase))
                .FirstOrDefault(p => p.Path.IndexOf("/GetItemLogs/", StringComparison.OrdinalIgnoreCase) > -1);
            var loggedReturnSchema = logged == null ? null : GetReturningTypeSchema(logged);
            var loggedTypeName = loggedReturnSchema == null ? null : GetArrayElementTypeString(loggedReturnSchema, usings);
            if (document.Definitions.TryGetValue(clientName, out var schema))
            {
                idKey = GetPrimaryKey(schema);
                typeKey = GetTypeKey(schema);
                typeId = GetTypeId(schema);

                return $"{(loggedTypeName != null ? "Logged" : "")}Client<{clientName}, {(idKey == null ? "int" : GetTypeString(idKey, usings, "List"))}{(logged != null ? $", {loggedTypeName}" : "")}>";
            }
            return $"ClientBase";
        }

        private JsonSchemaProperty GetProperty(JsonSchema schema, string propertyName)
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

        private JsonSchemaProperty GetPrimaryKey(JsonSchema schema, bool inherit = true)
        {
            if (schema.ExtensionData != null && schema.ExtensionData.TryGetValue("x-id", out var propertyName))
            {
                return schema.Properties[propertyName.ToString()];
            }
            if (inherit)
            {
                foreach (var baseClass in schema.AllInheritedSchemas)
                {
                    if (baseClass.ExtensionData != null && baseClass.ExtensionData.TryGetValue("x-id", out propertyName))
                    {
                        return baseClass.Properties[propertyName.ToString()];
                    }
                }
                var parentSchema = schema.ParentSchema;
                while (parentSchema != null)
                {
                    if (parentSchema.ExtensionData != null && parentSchema.ExtensionData.TryGetValue("x-id", out propertyName))
                    {
                        return parentSchema.Properties[propertyName.ToString()];
                    }
                    parentSchema = schema.ParentSchema;
                }

            }
            return null;
        }

        private JsonSchemaProperty GetReferenceProperty(JsonSchema schema, string name, bool inherit = true)
        {
            var find = schema.Properties.Values.FirstOrDefault(p => p.ExtensionData != null
                 && p.ExtensionData.TryGetValue("x-id", out var propertyName)
                 && string.Equals(propertyName.ToString(), name, StringComparison.Ordinal));
            if (find != null)
            {
                return find;
            }
            if (inherit)
            {
                foreach (var baseClass in schema.AllInheritedSchemas)
                {
                    find = baseClass.Properties.Values.FirstOrDefault(p => p.ExtensionData != null
                 && p.ExtensionData.TryGetValue("x-id", out var propertyName)
                 && propertyName.Equals(name));
                    if (find != null)
                    {
                        return find;
                    }
                }
            }
            return null;
        }

        private JsonSchemaProperty GetReferenceIdProperty(JsonSchemaProperty property, bool inherit = true)
        {
            if (property.ExtensionData != null && property.ExtensionData.TryGetValue("x-id", out var propertyName))
            {
                return GetProperty(property.ParentSchema, (string)propertyName);
            }
            return null;
        }

        private JsonSchemaProperty GetTypeKey(JsonSchema schema)
        {
            if (schema.ExtensionData != null && schema.ExtensionData.TryGetValue("x-type", out var propertyName))
            {
                return schema.Properties[propertyName.ToString()];
            }

            foreach (var baseClass in schema.AllInheritedSchemas)
                if (baseClass.ExtensionData != null && baseClass.ExtensionData.TryGetValue("x-type", out propertyName))
                {
                    return baseClass.Properties[propertyName.ToString()];
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

        private JsonSchema GetReturningTypeSchema(OpenApiOperationDescription descriptor)
        {
            return descriptor.Operation.Responses.TryGetValue("200", out var responce) && responce.Schema != null
                ? responce.Schema : null;
        }

        private string GetReturningType(OpenApiOperationDescription descriptor, HashSet<string> usings)
        {
            var returnType = "string";
            if (descriptor.Operation.Responses.TryGetValue("200", out var responce) && responce.Schema != null)
            {
                returnType = $"{GetTypeString(responce.Schema, usings, "List")}";
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

        private INamedTypeSymbol GetReferenceType(JsonSchema definition)
        {
            var definitionName = GetDefinitionName(definition);
            if (!cacheReferences.TryGetValue(definitionName, out var type))
            {
                if (definitionName.Equals("DefaultItem", StringComparison.OrdinalIgnoreCase))
                {
                    type = Compilation.GetTypeByMetadataName(typeof(object).FullName);
                }
                else
                if (definitionName.Equals(nameof(TimeSpan), StringComparison.OrdinalIgnoreCase))
                {
                    type = Compilation.GetTypeByMetadataName(typeof(TimeSpan).FullName);
                }
                else
                {
                    foreach (var reference in References)
                    {
                        try
                        {
                            var parsedType = SyntaxHelper.ParseType(definitionName, reference);
                            if (parsedType != null)
                            {
                                if (parsedType.EnumUnderlyingType != null)
                                {
                                    if (definition.EnumerationNames == null)
                                        continue;
                                    var defiEnumeration = definition.EnumerationNames;
                                    var typeEnumeration = parsedType.GetMembers().Select(p => p.Name);
                                    if (defiEnumeration.SequenceEqual(typeEnumeration))
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
                                    var percent = (float)defiProperties.Intersect(typeProperties, StringComparer.Ordinal).Count();
                                    percent /= (float)defiProperties.Count();
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
                            SyntaxHelper.ConsoleWarning($"Can't Check Type {definitionName} on {reference}. {ex.GetType().Name} {ex.Message}");
                        }
                    }
                }
                cacheReferences[definitionName] = type;
            }
            return type;
        }

        private INamedTypeSymbol GenDefinition(JsonSchema schema)
        {
            var usings = new HashSet<string>(StringComparer.Ordinal)
            {
                "DataWF.Common",
                "System",
            };

            var @class = schema.IsEnumeration ? GenDefinitionEnum(schema, usings) : GenDefinitionClass(schema, usings);

            //Context.AddSource(, SourceText.From(source.ToString(), Encoding.UTF8));
            return GenUnit($"{Namespace}.{GetDefinitionName(schema)}DefenitionGen.cs", @class, usings.OrderBy(p => p));
        }

        private StringBuilder GenDefinitionEnum(JsonSchema schema, HashSet<string> usings)
        {
            SyntaxHelper.AddUsing("System.Runtime.Serialization", usings);

            source = new StringBuilder($@"
namespace {Namespace} 
{{
    {((schema.ExtensionData?.TryGetValue("x-flags", out _) ?? false) ? "[Flags]" : "")}
    public enum {GetDefinitionName(schema)}
    {{");
            GenDefinitionEnumMemebers(schema);
            source.Append($@"
    }}
}}");
            return source;
        }

        private void GenDefinitionEnumMemebers(JsonSchema schema)
        {
            //object[] names = null;
            //if (schema.ExtensionData != null
            //    && schema.ExtensionData.TryGetValue("x-enumNames", out var enumNames))
            //{
            //    names = (object[])enumNames;
            //}
            object[] members = null;
            if (schema.ExtensionData != null
                && schema.ExtensionData.TryGetValue("x-enumMembers", out var enumMembers))
            {
                members = (object[])enumMembers;
            }
            int i = 0;
            //var definitionName = GetDefinitionName(schema);
            foreach (var item in schema.Enumeration)
            {
                var sitem = schema.EnumerationNames[i++];
                var memeber = sitem;
                if (members != null && members.Length > i)
                    memeber = members[i]?.ToString();
                //if (!Char.IsLetter(sitem[0]))
                //{
                //    sitem = definitionName[0] + sitem;
                //}
                source.Append($@"
        [EnumMember(Value = ""{ memeber }"")]
        public {sitem} = {item};");
            }
        }

        private StringBuilder GenDefinitionClass(JsonSchema schema, HashSet<string> usings)
        {
            var refFields = referenceFields[schema] = new List<RefField>();
            source = new StringBuilder($@"
namespace {Namespace} 
{{
    public partial class {GetDefinitionName(schema)}:{string.Join(", ", GenDefinitionClassBases(schema, usings))}
    {{");
            GenDefinitionClassMemebers(schema, refFields, usings);

            source.Append($@"
    }}
}}");
            return source;
        }

        private IEnumerable<string> GenDefinitionClassBases(JsonSchema schema, HashSet<string> usings)
        {
            if (schema.InheritedSchema != null)
            {
                if (!GetOrGenDefinion(schema.InheritedSchema, out var type) && type != null)
                {
                    SyntaxHelper.AddUsing(type, usings);
                }
                yield return GetDefinitionName(schema.InheritedSchema);
            }
            else
            {
                //yield return SF.SimpleBaseType(SF.ParseTypeName(nameof(IEntryNotifyPropertyChanged)));
                if (schema.Id == "DBItem")
                    yield return "SynchronizedItem";
                else
                    yield return "DefaultItem";
            }
            var idKey = GetPrimaryKey(schema, false);
            if (idKey != null)
            {
                yield return "IPrimaryKey";
                yield return "IQueryFormatable";
            }
        }

        private void GenDefinitionClassMemebers(JsonSchema schema, List<RefField> refFields, HashSet<string> usings)
        {
            var idKey = GetPrimaryKey(schema);
            var typeKey = GetTypeKey(schema);
            var typeId = GetTypeId(schema);

            foreach (var property in schema.Properties)
            {
                GenDefinitionClassField(property.Value, idKey, refFields, usings);
            }

            GenDefinitionClassConstructorBody(schema, idKey, typeKey, typeId, refFields, usings);

            if (refFields.Count > 0 && idKey.ParentSchema != schema)
            {
                GenDefinitionClassProperty(idKey, idKey, typeKey, refFields, usings, true);
            }

            foreach (var property in schema.Properties)
            {
                GenDefinitionClassProperty(property.Value, idKey, typeKey, refFields, usings);
            }

            if (GetPrimaryKey(schema, false) != null)
            {
                var converter = "";
                var idType = GetTypeString(idKey, usings, "List");
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
                SyntaxHelper.AddUsing("System.Text.Json.Serialization", usings);
                source.Append($@"
        [JsonIgnore]    
        public object PrimaryKey 
        {{
            get => {idKey.Name};
            set => {idKey.Name} = {converter};
        }}

        public string Format() => {idKey.Name}.ToString();");
            }
        }

        private void GenDefinitionClassField(JsonSchemaProperty property, JsonSchemaProperty idKey, List<RefField> refFields, HashSet<string> usings)
        {
            var refkey = GetPropertyRefKey(property);
            if (refkey != null && property.Type == JsonObjectType.Array)
            {
                var refField = new RefField
                {
                    Property = property,
                    PropertyName = GetPropertyName(property),
                    Definition = property.ParentSchema.Id,
                    RefKey = refkey,
                    TypeSchema = property.Item.ActualTypeSchema,
                    TypeName = GetTypeString(property.Item, usings),
                };
                refFields.Add(refField);
                //var refTypePrimary = GetPrimaryKey(refTypeSchema);
                //var refTypePrimaryName = GetPropertyName(refTypePrimary);
                //var refTypePrimaryType = GetTypeString(refTypePrimary, true, "SelectableList");
                refField.KeyProperty = GetProperty(refField.TypeSchema, refkey);
                refField.KeyName = GetPropertyName(refField.KeyProperty);
                refField.KeyType = GetTypeString(refField.KeyProperty, usings);

                refField.ValueProperty = GetReferenceProperty((JsonSchema)refField.KeyProperty.Parent, refField.KeyName);
                refField.ValueName = GetPropertyName(refField.ValueProperty);

                refField.ValueType = GetTypeString(refField.ValueProperty, null);
                refField.ValueFieldName = GetFieldName(refField.ValueProperty);

                refField.InvokerName = $"{refField.TypeName}.{refkey}Invoker.Default";

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

                refField.FieldType = GetTypeString(property, usings, "ReferenceList");
                refField.FieldName = GetFieldName(property);
                source.Append($@"
        protected {refField.FieldType} {refField.FieldName};");
            }
            else
            {
                var type = GetTypeString(property, usings);

                source.Append($@"
        protected internal {type} {GetFieldName(property)}{(property.Type == JsonObjectType.Array ? $" = new {type}()" : "")};");
            }
        }

        private void GenDefinitionClassConstructorBody(JsonSchema schema, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId, List<RefField> refFields, HashSet<string> usings)
        {
            source.Append($@"
        public {GetDefinitionName(schema)}()
        {{");
            if (typeId != 0)
            {
                source.Append($@"
            {GetPropertyName(typeKey)} = {typeId};");
            }
            foreach (var refField in refFields)
            {
                //yield return SF.ParseStatement($@"{refField.FieldName} = new {refField.FieldType}(
                //new Query<{refField.TypeName}>(new[]{{{refField.ParameterName}}}),
                //ClientProvider.Default.{refField.TypeName}.Items,
                //false);");
                source.Append($@"
            {refField.FieldName} = new {refField.FieldType} (this, nameof({refField.PropertyName}));");
            }
            foreach (var property in schema.Properties.Select(p => p.Value))
            {
                if (property.Default != null)
                {
                    source.Append($@"
            {GetPropertyName(property)} = {GenFieldDefault(property, idKey, usings)};");
                }
            }
            source.Append($@"
        }}");
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
                type: SF.ParseTypeName("string"),
                identifier: SF.Identifier("propertyName"),
                @default: @default);
        }

        private void GenDefinitionClassProperty(JsonSchemaProperty property, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, List<RefField> refFields, HashSet<string> usings, bool isOverride = false)
        {
            var refkey = GetPropertyRefKey(property);
            var typeDeclaration = GetTypeString(property, usings, refkey == null ? "SelectableList" : "ReferenceList");
            source.Append($@"
        [{string.Join(", ", GenDefinitionClassPropertyAttributes(property, idKey, typeKey, usings))}]
        public {(isOverride ? "override " : property == idKey ? "virtual " : "")}{typeDeclaration} {GetPropertyName(property)} 
        {{");
            GenDefintionClassPropertyGet(property, isOverride);
            GenDefinitionClassPropertySet(property, idKey, refFields, usings, isOverride);
            source.Append($@"
        }}");
        }

        private IEnumerable<string> GenDefinitionClassPropertyAttributes(JsonSchemaProperty property, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, HashSet<string> usings)
        {
            if (property.IsReadOnly
                || (property.ExtensionData != null && property.ExtensionData.TryGetValue("readOnly", out var isReadOnly) && (bool)isReadOnly))
            {
                yield return "JsonIgnoreSerialization";
            }
            else if (property == typeKey)
            {
                SyntaxHelper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                yield return $"Display(Order = -3)";
            }
            else if (property == idKey)
            {
                SyntaxHelper.AddUsing("System.ComponentModel.DataAnnotations", usings);
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

        private IEnumerable<string> GenDefinitionClassPropertyValidationAttributes(JsonSchemaProperty property, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, HashSet<string> usings)
        {
            var propertyName = GetPropertyName(property);
            var objectProperty = GetReferenceProperty((JsonSchema)property.Parent, property.Name);
            if (objectProperty != null)
            {
                propertyName = GetPropertyName(objectProperty);
            }

            if (property.IsRequired)
            {
                SyntaxHelper.AddUsing("System.ComponentModel.DataAnnotations", usings);
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
                SyntaxHelper.AddUsing("System.ComponentModel.DataAnnotations", usings);
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

        private void GenDefintionClassPropertyGet(JsonSchemaProperty property, bool isOverride)
        {
            var fieldName = GetFieldName(property);
            if (isOverride)
            {
                source.Append($@"
            get => base.{GetPropertyName(property)};");
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
            source.Append($@"
            get => {fieldName};");
        }

        private void GenDefinitionClassPropertySet(JsonSchemaProperty property, JsonSchemaProperty idKey, List<RefField> refFields, HashSet<string> usings, bool isOverride)
        {
            source.Append($@"
            set
            {{");

            if (!isOverride)
            {
                var type = GetTypeString(property, usings, "SelectableList");
                if (type.Equals("string", StringComparison.Ordinal))
                {
                    source.Append($@"
                if (string.Equals({GetFieldName(property)}, value, StringComparison.Ordinal)) return;");
                }
                else
                {
                    source.Append($@"
                if ({GetFieldName(property)} == value) return;");
                }
                source.Append($@"
                var temp = {GetFieldName(property)};
                {GetFieldName(property)} = value;");
                if (property.ExtensionData != null && property.ExtensionData.TryGetValue("x-id", out var refPropertyName))
                {
                    var idProperty = GetPrimaryKey(property.Reference ?? property.AllOf.FirstOrDefault()?.Reference ?? property.AnyOf.FirstOrDefault()?.Reference);
                    var refProperty = GetProperty((JsonSchema)property.Parent, (string)refPropertyName);
                    var refType = GetTypeString(refProperty, usings);
                    if (idProperty.IsNullableRaw ?? false)
                        source.Append($@"
                {refPropertyName} = value?.{GetPropertyName(idProperty)};");
                    else
                        source.Append($@"
                {refPropertyName} = value?.{GetPropertyName(idProperty)} ?? default({refType});");
                }
                //Change - refence from single json
                var objectProperty = GetReferenceProperty((JsonSchema)property.Parent, property.Name);
                if (objectProperty != null)
                {
                    var objectFieldName = GetFieldName(objectProperty);
                    source.Append($@"
                if ({objectFieldName}?.Id != value)
                {{
                    {objectFieldName} = value == default({type}) ? null : Provider.{GetTypeString(objectProperty, usings, "List")}.Select(value{(type.EndsWith("?") ? ".Value" : "")});
                    OnPropertyChanged(nameof({GetPropertyName(objectProperty)}));
                }}");
                }
                source.Append($@"
                OnPropertyChanged(temp, value);");
            }
            else
            {
                source.Append($@"
                base.{GetPropertyName(property)} = value;");
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

        private string GetPropertyName(JsonSchemaProperty property)
        {
            return GetDefinitionName(property.Name);
        }

        private string GetDefinitionName(JsonSchema schema)
        {
            return GetDefinitionName(schema.Id);
        }

        private string GetDefinitionName(string name)
        {
            return string.Concat(char.ToUpperInvariant(name[0]).ToString(), name.Substring(1));
        }

        private string GetFieldName(JsonSchemaProperty property)
        {
            return GetFieldName(property.Name);
        }

        private string GetFieldName(string property)
        {
            return string.Concat("_", char.ToLowerInvariant(property[0]).ToString(), property.Substring(1));
        }

        private string GetPropertyRefKey(JsonSchema schema)
        {
            return schema.ExtensionData != null
                ? schema.ExtensionData.TryGetValue("x-ref-key", out var key) ? (string)key : null
                : null;
        }

        private ExpressionSyntax GenFieldDefault(JsonSchemaProperty property, JsonSchemaProperty idKey, HashSet<string> usings)
        {
            var text = property.Default.ToString();
            var type = GetTypeString(property, usings).TrimEnd('?');
            if (type == "bool")
                text = text.ToLowerInvariant();
            else if (type == "string")
                text = $"\"{text}\"";
            else if (property.ActualSchema != null && property.ActualSchema.IsEnumeration)
                text = $"{type}.{text}";
            return SF.ParseExpression(text);
        }

        private string GetArrayElementTypeString(JsonSchema schema, HashSet<string> usings)
        {
            return schema.Type == JsonObjectType.Array
                ? GetTypeString(schema.Item, usings, "List")
                : null;
        }

        private string GetTypeString(JsonSchema schema, HashSet<string> usings, string listType = "SelectableList", bool nullDefault = false)
        {
            var nullable = schema.IsNullableRaw ?? nullDefault;
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
                            SyntaxHelper.AddUsing("System.IO", usings);
                            return "Stream";
                        case "date":
                        case "date-time":
                            return "DateTime" + (nullable ? "?" : string.Empty);
                        default:
                            return "string";
                    }
                case JsonObjectType.Array:
                    return $"{listType}<{GetTypeString(schema.Item, usings, listType, nullDefault)}>";
                case JsonObjectType.None:
                    if (schema.ActualTypeSchema != schema)
                    {
                        return GetTypeString(schema.ActualTypeSchema, usings, listType, nullable);
                    }
                    else if (schema is JsonSchemaProperty propertySchema)
                    {
                        return GetTypeString(propertySchema.AllOf.FirstOrDefault()?.Reference
                            ?? propertySchema.AnyOf.FirstOrDefault()?.Reference, usings, listType, nullable);
                    }
                    else
                    {
                        goto case JsonObjectType.Object;
                    }
                case JsonObjectType.Object:
                    if (schema.Id != null)
                    {
                        if (!GetOrGenDefinion(schema, out var type) && type != null)
                        {
                            SyntaxHelper.AddUsing(type, usings);
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
                    SyntaxHelper.AddUsing("System.IO", usings);
                    return "Stream";
            }
            return "string";
        }

        public INamedTypeSymbol GenUnit(string name, StringBuilder @class, IEnumerable<string> usings)
        {
            var unitSource = new StringBuilder();
            foreach (var item in usings)
            {
                unitSource.Append($@"
using {item};");
            }
            unitSource.AppendLine();
            unitSource.Append(@class);

            var sourceText = SourceText.From(unitSource.ToString(), Encoding.UTF8);
            Context.AddSource(name, sourceText);

            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, (CSharpParseOptions)Options);

            Compilation = Compilation.AddSyntaxTrees(syntaxTree);
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

        private class RefField
        {
            public string RefKey;
            public JsonSchemaProperty KeyProperty;
            public string KeyName;
            public string KeyType;
            public JsonSchema TypeSchema;
            public string TypeName;
            public string InvokerName;
            //public string InvokerType;
            //public string ParameterType;
            //public string ParameterName;
            public JsonSchemaProperty Property;
            public string PropertyName;
            public string FieldType;
            public string FieldName;
            public string Definition;
            public JsonSchemaProperty ValueProperty;
            public string ValueName;
            public string ValueType;
            public string ValueFieldName;

        }

    }
}
