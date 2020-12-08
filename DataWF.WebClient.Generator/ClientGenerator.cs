using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema;
using NSwag;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DataWF.WebClient.Generator
{
    public class ClientGenerator
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
        private readonly Dictionary<string, CompilationUnitSyntax> cacheModels = new Dictionary<string, CompilationUnitSyntax>(StringComparer.Ordinal);
        private readonly Dictionary<string, Type> cacheReferences = new Dictionary<string, Type>(StringComparer.Ordinal);
        private readonly Dictionary<string, ClassDeclarationSyntax> cacheClients = new Dictionary<string, ClassDeclarationSyntax>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, UsingDirectiveSyntax>> cacheUsings = new Dictionary<string, Dictionary<string, UsingDirectiveSyntax>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<AttributeListSyntax>> cacheAttributes = new Dictionary<string, List<AttributeListSyntax>>(StringComparer.Ordinal);

        private readonly Dictionary<JsonSchema, List<RefField>> referenceFields = new Dictionary<JsonSchema, List<RefField>>();
        private OpenApiDocument document;
        private CompilationUnitSyntax provider;
        private string lastReferenceDirectory;

        public ClientGenerator(string source, string output, string @namespace, string references)
        {
            Namespace = @namespace;
            Output = string.IsNullOrEmpty(output) ? null : Path.GetFullPath(output);
            Source = source;
            if (!string.IsNullOrEmpty(references))
            {
                AssemblyLoadContext.Default.Resolving += OnDefaultResolving;
                var referenceArray = references.Split(";");
                foreach (var reference in referenceArray)
                {
                    if (File.Exists(reference))
                    {
                        LoadAssembly(reference);
                    }
                    else if (Directory.Exists(reference))
                    {
                        var directory = reference.TrimEnd(Path.DirectorySeparatorChar);
                        directory = Path.GetFileName(directory);
                        var bin = Path.Combine(reference, "bin");
                        foreach (var dll in Directory.GetFiles(reference, "*.dll", SearchOption.AllDirectories))
                        {
                            var dllName = Path.GetFileName(dll);
                            if (dllName.StartsWith(directory))
                            {
                                LoadAssembly(dll);
                            }
                        }
                    }
                    else
                    {
                        SyntaxHelper.ConsoleWarning($"Can't Load Reference {reference} File or Folder NotFound!");
                    }
                }
            }
        }

        public string Output { get; }
        public string Source { get; }
        public string Namespace { get; }
        public HashSet<Assembly> References { get; } = new HashSet<Assembly>();
        public string ProviderName { get; set; } = "ClientProvider";

        private Assembly LoadAssembly(string reference, bool addReference = true)
        {
            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(reference));
                if (addReference)
                {
                    References.Add(assembly);
                }
                SyntaxHelper.ConsoleInfo($"Load reference assembly {assembly} from {reference}");
                lastReferenceDirectory = Path.GetDirectoryName(Path.GetFullPath(reference));
                assembly.GetExportedTypes();
                return assembly;
            }
            catch (Exception ex)
            {
                SyntaxHelper.ConsoleWarning($"Can't Load Assembly {reference}. {ex.GetType().Name} {ex.Message}");
                return null;
            }
        }

        private Assembly OnDefaultResolving(AssemblyLoadContext arg1, AssemblyName arg2)
        {
            string packagePath;
            if (!string.IsNullOrEmpty(lastReferenceDirectory))
            {
                packagePath = Path.Combine(lastReferenceDirectory, arg2.Name + ".dll");
                if (File.Exists(packagePath))
                {
                    SyntaxHelper.ConsoleWarning($"Try Resolving {arg2} from {packagePath}");
                    var assembly = LoadAssembly(packagePath, false);
                    if (assembly != null)
                    {
                        return assembly;
                    }
                }
            }
            packagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $@".nuget\packages\{arg2.Name.ToLower()}");
            if (Directory.Exists(packagePath))
            {
                var prevVersion = new Version(0, 0, 0, 0);
                foreach (var versionPath in Directory.GetDirectories(packagePath))
                {
                    var versionName = Path.GetFileName(versionPath);
                    if (Version.TryParse(versionName, out var version)
                        && version > prevVersion)
                    {
                        prevVersion = version;
                        packagePath = versionPath;
                        if (version == arg2.Version)
                        {
                            break;
                        }
                    }
                }
                packagePath = Path.Combine(packagePath, @"lib\netstandard2.0", arg2.Name + ".dll");
            }

            if (!string.IsNullOrEmpty(packagePath)
                && File.Exists(packagePath))
            {
                SyntaxHelper.ConsoleWarning($"Try Resolving {arg2} from {packagePath}");
                return LoadAssembly(packagePath, false);
            }

            SyntaxHelper.ConsoleWarning($"Fail Resolving {arg2}");
            return null;
        }

        public void Generate()
        {
            var url = new Uri(Source);
            if (url.Scheme == "http" || url.Scheme == "https")
                document = OpenApiDocument.FromUrlAsync(url.OriginalString).GetAwaiter().GetResult();
            else if (url.Scheme == "file")
                document = OpenApiDocument.FromFileAsync(url.LocalPath).GetAwaiter().GetResult();
            foreach (var definition in document.Definitions)
            {
                definition.Value.Id = definition.Key;
            }
            foreach (var definition in document.Definitions)
            {
                GetOrGenDefinion(definition.Key, out _);
            }
            foreach (var operation in document.Operations)
            {
                AddClientOperation(operation);
            }

            provider = SyntaxHelper.GenUnit(GenProvider(), Namespace, usings: new List<UsingDirectiveSyntax>()
            {
                SyntaxHelper.CreateUsingDirective("DataWF.Common") ,
                SyntaxHelper.CreateUsingDirective("DataWF.WebClient.Common") ,
                SyntaxHelper.CreateUsingDirective("System")
            }, Enumerable.Empty<AttributeListSyntax>());
        }

        private ClassDeclarationSyntax GenProvider()
        {
            return SF.ClassDeclaration(
                    attributeLists: SF.List(ClientAttributeList()),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                    identifier: SF.Identifier(ProviderName),
                    typeParameterList: null,
                    baseList: SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(
                        SF.SimpleBaseType(SF.ParseTypeName("ClientProviderBase")))),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    members: SF.List(GenProviderMemebers())
                    );
        }

        private IEnumerable<MemberDeclarationSyntax> GenProviderMemebers()
        {
            yield return SyntaxHelper.GenProperty(ProviderName, "Default", false, $"new {ProviderName}()")
                .AddModifiers(SF.Token(SyntaxKind.StaticKeyword));

            yield return SF.ConstructorDeclaration(
                           attributeLists: SF.List(ClientAttributeList()),
                           modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                           identifier: SF.Identifier(ProviderName),
                           parameterList: SF.ParameterList(),
                           initializer: null,
                           body: SF.Block(GenProviderConstructorBody()));

            foreach (var client in cacheClients.Keys)
            {
                yield return SyntaxHelper.GenProperty($"{client}Client", client, false);
            }

        }

        private IEnumerable<StatementSyntax> GenProviderClientsBody()
        {
            foreach (var client in cacheClients.Keys)
            {
                yield return SF.ParseStatement($"yield return {client};");
            }
        }

        private IEnumerable<StatementSyntax> GenProviderConstructorBody()
        {
            //SF.EqualsValueClause(SF.ParseExpression())
            //yield return SF.ParseStatement($"BaseUrl = \"{Url.Scheme}://{Url.Authority}\";");
            foreach (var client in cacheClients.Keys)
            {
                yield return SF.ParseStatement($"Add({client} = new {client}Client{{Provider = this}});");
            }
            yield return SF.ParseStatement("RefreshTypedCache();");
        }

        public List<SyntaxTree> GetUnits(bool save)
        {
            var list = new List<SyntaxTree>();
            var assembly = typeof(ClientGenerator).Assembly;
            var baseName = assembly.GetName().Name + ".ClientTemplate.";
            list.AddRange(SyntaxHelper.LoadResources(assembly, baseName, Namespace, save ? Output : null).Select(p => p.SyntaxTree));

            list.Add(provider.SyntaxTree);
            if (save)
            {
                WriteFile(Path.Combine(Output, "Provider.cs"), provider);
            }

            var modelPath = Path.Combine(Output, "Models");
            Directory.CreateDirectory(modelPath);
            foreach (var entry in cacheModels)
            {
                if (entry.Value == null)
                    continue;
                if (save)
                {
                    WriteFile(Path.Combine(modelPath, entry.Key + ".cs"), entry.Value);
                }
                list.Add(entry.Value.SyntaxTree);
            }

            var clientPath = Path.Combine(Output, "Clients");
            Directory.CreateDirectory(clientPath);
            foreach (var entry in cacheClients)
            {
                var usings = cacheUsings[entry.Key];
                var unit = SyntaxHelper.GenUnit(entry.Value, Namespace, usings.Values.OrderBy(p => p.ToString()), new List<AttributeListSyntax>());
                if (save)
                {
                    WriteFile(Path.Combine(clientPath, entry.Key + "Client.cs"), unit);
                }
                list.Add(unit.SyntaxTree);
            }

            return list;
        }

        private void WriteFile(string name, CompilationUnitSyntax unit)
        {
            int tryCount = 6;
            for (int i = 1; i < tryCount; i++)
            {
                if (TryWriteFile(name, unit))
                    break;
                Console.WriteLine($"Can not access file {name}, try {i}");
                Thread.Sleep(200);
            }
        }

        private static bool TryWriteFile(string name, CompilationUnitSyntax unit)
        {
            try
            {
                using (var fileStream = new FileStream(name, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(fileStream))
                    unit.WriteTo(writer);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public virtual string GetClientName(OpenApiOperationDescription decriptor)
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

        public virtual string GetOperationName(OpenApiOperationDescription descriptor, out string clientName)
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

        private void AddClientOperation(OpenApiOperationDescription descriptor)
        {
            GetOperationName(descriptor, out var clientName);
            if (!cacheUsings.TryGetValue(clientName, out var usings))
            {
                cacheUsings[clientName] =
                    usings = new Dictionary<string, UsingDirectiveSyntax>(StringComparer.Ordinal)
                {
                    { "DataWF.Common", SyntaxHelper.CreateUsingDirective("DataWF.Common") } ,
                    { "DataWF.WebClient.Common",  SyntaxHelper.CreateUsingDirective("DataWF.WebClient.Common") } ,
                    { "System", SyntaxHelper.CreateUsingDirective("System") },
                    { "System.Collections.Generic", SyntaxHelper.CreateUsingDirective("System.Collections.Generic") },
                    { "System.Net.Http", SyntaxHelper.CreateUsingDirective("System.Net.Http") },
                    { "System.Threading.Tasks", SyntaxHelper.CreateUsingDirective("System.Threading.Tasks") },
                };
            }
            if (!cacheClients.TryGetValue(clientName, out var clientSyntax))
            {
                clientSyntax = GenClient(clientName, usings);
            }

            cacheClients[clientName] = clientSyntax.AddMembers(GenOperation(descriptor, usings).ToArray());
        }

        private ClassDeclarationSyntax GenClient(string clientName, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            var baseType = SF.ParseTypeName(GetClientBaseType(clientName, usings, out var idKey, out var typeKey, out var typeId));

            return SF.ClassDeclaration(
                        attributeLists: SF.List(ClientAttributeList()),
                        modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)),
                        identifier: SF.Identifier($"{clientName}Client"),
                        typeParameterList: null,
                        baseList: SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(
                            SF.SimpleBaseType(baseType))),
                        constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                        members: SF.List(GenClientMembers(clientName, idKey, typeKey, typeId, usings))
                        );
        }

        private IEnumerable<MemberDeclarationSyntax> GenClientMembers(string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId,
            Dictionary<string, UsingDirectiveSyntax> usings)
        {
            document.Definitions.TryGetValue(clientName, out var clientSchema);
            var typeName = $"{clientName}Client";
            yield return SF.PropertyDeclaration(attributeLists: SF.List<AttributeListSyntax>(),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.StaticKeyword)),
                    type: SF.ParseTypeName(typeName),
                    identifier: SF.Identifier("Instance"),
                    explicitInterfaceSpecifier: null,
                    accessorList: SF.AccessorList(SF.List(new[]
                    {
                        SF.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                         .WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken)),
                        SF.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration,
                            attributeLists:SF.List<AttributeListSyntax>(),
                            modifiers: SF.TokenList(SF.Token(SyntaxKind.PrivateKeyword)),
                            body:null)
                         .WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken))
                    })),
                    expressionBody: null,
                    initializer: null,
                    semicolonToken: SF.Token(SyntaxKind.None));

            var cache = clientSchema != null ? GetClientReferences(clientSchema) : new HashSet<RefField>();
            yield return GenClientConstructor(clientName, idKey, typeKey, typeId, cache);
            if (cache.Count > 0)
            {
                SyntaxHelper.AddUsing("System.Collections", usings);
                yield return SF.MethodDeclaration(
                    attributeLists: SF.List<AttributeListSyntax>(),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.ProtectedKeyword), SF.Token(SyntaxKind.OverrideKeyword)),
                    returnType: SF.ParseTypeName("void"),
                    explicitInterfaceSpecifier: null,
                    identifier: SF.Identifier("OnRemoved"),
                    typeParameterList: null,
                    parameterList: SF.ParameterList(
                        SF.SeparatedList(new[] {
                            SF.Parameter(
                                attributeLists: SF.List<AttributeListSyntax>(),
                                modifiers: SF.TokenList(),
                                type: SF.ParseTypeName("IList"),
                                identifier: SF.Identifier("items"),
                                @default: null) })),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    body: SF.Block(GenClientRemoveOverrideBody(clientName, idKey, typeKey, typeId, cache)),
                    semicolonToken: SF.Token(SyntaxKind.None));
                yield return SF.MethodDeclaration(
                    attributeLists: SF.List<AttributeListSyntax>(),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.ProtectedKeyword), SF.Token(SyntaxKind.OverrideKeyword)),
                    returnType: SF.ParseTypeName("void"),
                    explicitInterfaceSpecifier: null,
                    identifier: SF.Identifier("OnAdded"),
                    typeParameterList: null,
                    parameterList: SF.ParameterList(
                        SF.SeparatedList(new[] {
                                SF.Parameter(
                                    attributeLists: SF.List<AttributeListSyntax>(),
                                    modifiers: SF.TokenList(),
                                    type: SF.ParseTypeName("IList"),
                                    identifier: SF.Identifier("items"),
                                    @default: null) })),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    body: SF.Block(GenClientAddOverrideBody(clientName, idKey, typeKey, typeId, cache)),
                    semicolonToken: SF.Token(SyntaxKind.None));
            }
        }

        private IEnumerable<StatementSyntax> GenClientRemoveOverrideBody(string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId, HashSet<RefField> cache)
        {
            yield return SF.ParseStatement($"base.OnRemoved(items);");
            yield return SF.ParseStatement($"foreach ({clientName} item in items){{");
            foreach (var refField in cache)
            {
                yield return SF.ParseStatement($"if(item.{refField.KeyName} != null){{");
                yield return SF.ParseStatement($"var item{refField.ValueName} = (item.{refField.ValueFieldName} ?? (item.{refField.ValueFieldName} = {ProviderName}.Default.{refField.ValueType}.Select(item.{refField.KeyName}.Value))) as {refField.Definition};");
                yield return SF.ParseStatement($"item{refField.ValueName}?.{refField.PropertyName}.Remove(item);");
                yield return SF.ParseStatement("}");
            }
            yield return SF.ParseStatement("}");
        }

        private IEnumerable<StatementSyntax> GenClientAddOverrideBody(string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId, HashSet<RefField> cache)
        {
            yield return SF.ParseStatement($"base.OnAdded(items);");
            yield return SF.ParseStatement($"foreach ({clientName} item in items){{");
            foreach (var refField in cache)
            {
                yield return SF.ParseStatement($"if(item.{refField.KeyName} != null){{");
                yield return SF.ParseStatement($"var item{refField.ValueName} = (item.{refField.ValueFieldName} ?? (item.{refField.ValueFieldName} = {ProviderName}.Default.{refField.ValueType}.Select(item.{refField.KeyName}.Value))) as {refField.Definition};");
                yield return SF.ParseStatement($"item{refField.ValueName}?.{refField.PropertyName}.Add(item);");
                yield return SF.ParseStatement("}");
            }
            yield return SF.ParseStatement("}");
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

        private string GetClientBaseType(string clientName, Dictionary<string, UsingDirectiveSyntax> usings, out JsonSchemaProperty idKey, out JsonSchemaProperty typeKey, out int typeId)
        {
            idKey = null;
            typeKey = null;
            typeId = 0;
            var logged = document.Operations
                .Where(p => p.Operation.Tags.Contains(clientName, StringComparer.OrdinalIgnoreCase))
                .FirstOrDefault(p => p.Path.Contains("/GetItemLogs/", StringComparison.OrdinalIgnoreCase));
            var loggedReturnSchema = logged == null ? null : GetReturningTypeSchema(logged);
            var loggedTypeName = loggedReturnSchema == null ? null : GetArrayElementTypeString(loggedReturnSchema, usings);
            if (document.Definitions.TryGetValue(clientName, out var schema))
            {
                idKey = GetPrimaryKey(schema);
                typeKey = GetTypeKey(schema);
                typeId = GetTypeId(schema);

                return $"{(loggedTypeName != null ? "Logged" : "")}Client<{clientName}, {(idKey == null ? "int" : GetTypeString(idKey, false, usings, "List"))}{(logged != null ? $", {loggedTypeName}" : "")}>";
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

        private ConstructorDeclarationSyntax GenClientConstructor(string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId, HashSet<RefField> cache)
        {
            var idName = idKey == null ? null : GetPropertyName(idKey);
            var typeName = typeKey == null ? null : GetPropertyName(typeKey);
            var initialize = idKey == null ? null : SF.ConstructorInitializer(
                    SyntaxKind.BaseConstructorInitializer,
                    SF.ArgumentList(
                        SF.SeparatedList(new[] {
                            SF.Argument(SF.ParseExpression($"{(clientName=="Instance"?Namespace+".":"")}{clientName}.{idName}Invoker.Default")),
                            SF.Argument(SF.ParseExpression($"{(clientName=="Instance"?Namespace+".":"")}{clientName}.{typeName}Invoker.Default")),
                            SF.Argument(SF.ParseExpression($"{typeId}")),
                        })));
            return SF.ConstructorDeclaration(
                attributeLists: SF.List(ClientAttributeList()),
                modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                identifier: SF.Identifier($"{clientName}Client"),
                parameterList: SF.ParameterList(),
                initializer: initialize,
                body: SF.Block(GenClientConstructorBody(clientName, idKey, typeKey, cache)));
        }

        private IEnumerable<StatementSyntax> GenClientConstructorBody(string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, HashSet<RefField> cache)
        {
            yield return SF.ParseStatement($"Instance = Instance ?? this;");
            foreach (var refField in cache)
            {
                yield return SF.ParseStatement($"Items.Indexes.Add({refField.InvokerName});");
            }
        }

        private IEnumerable<JsonSchema> GetParentSchems(JsonSchema schema)
        {
            while (schema.ParentSchema != null)
            {
                yield return schema.ParentSchema.ActualSchema;
                schema = schema.ParentSchema.ActualSchema;
            }
        }

        private IEnumerable<AttributeListSyntax> ClientAttributeList()
        {
            yield break;
        }

        private JsonSchema GetReturningTypeSchema(OpenApiOperationDescription descriptor)
        {
            return descriptor.Operation.Responses.TryGetValue("200", out var responce) && responce.Schema != null
                ? responce.Schema : null;
        }

        private string GetReturningType(OpenApiOperationDescription descriptor, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            var returnType = "string";
            if (descriptor.Operation.Responses.TryGetValue("200", out var responce) && responce.Schema != null)
            {
                returnType = $"{GetTypeString(responce.Schema, false, usings, "List")}";
            }
            return returnType;
        }

        private string GetReturningTypeCheck(OpenApiOperationDescription descriptor, string operationName, Dictionary<string, UsingDirectiveSyntax> usings)
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

        private IEnumerable<MemberDeclarationSyntax> GenOperation(OpenApiOperationDescription descriptor, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            var operationName = GetOperationName(descriptor, out var clientName);
            var actualName = $"{operationName}Async";
            var baseType = GetClientBaseType(clientName, usings, out _, out _, out _);
            var isOverride = baseType != "ClientBase" && VirtualOperations.Contains(actualName);
            var returnType = GetReturningTypeCheck(descriptor, operationName, usings);
            returnType = returnType.Length > 0 ? $"Task<{returnType}>" : "Task";
            //if (isOverride)
            //    throw new Exception("Operation Name :" + operationName);
            //yield return SF.MethodDeclaration(
            //    attributeLists: SF.List<AttributeListSyntax>(),
            //        modifiers: SF.TokenList(
            //            baseType != "ClientBase" && AbstractOperations.Contains(actualName)
            //            ? new[] { SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword) }
            //            : new[] { SF.Token(SyntaxKind.PublicKeyword) }),
            //        returnType: SF.ParseTypeName(returnType),
            //        explicitInterfaceSpecifier: null,
            //        identifier: SF.Identifier(actualName),
            //        typeParameterList: null,
            //        parameterList: SF.ParameterList(SF.SeparatedList(GenOperationParameter(descriptor, false))),
            //        constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
            //        body: SF.Block(GenOperationWrapperBody(actualName, descriptor)),
            //        semicolonToken: SF.Token(SyntaxKind.None));

            yield return SF.MethodDeclaration(
                attributeLists: SF.List<AttributeListSyntax>(),
                    modifiers: SF.TokenList(
                        isOverride
                        ? new[] { SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword), SF.Token(SyntaxKind.AsyncKeyword) }
                        : new[] { SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.AsyncKeyword) }),
                    returnType: SF.ParseTypeName(returnType),
                    explicitInterfaceSpecifier: null,
                    identifier: SF.Identifier(actualName),
                    typeParameterList: null,
                    parameterList: SF.ParameterList(SF.SeparatedList(GenOperationParameter(descriptor, usings))),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    body: SF.Block(GenOperationBody(actualName, descriptor, usings, isOverride)),
                    semicolonToken: SF.Token(SyntaxKind.None));
        }

        //private StatementSyntax GenOperationWrapperBody(string actualName, OpenApiOperationDescription descriptor)
        //{
        //    var builder = new StringBuilder();
        //    builder.Append($"return {actualName}(");

        //    foreach (var parameter in descriptor.Operation.Parameters)
        //    {
        //        builder.Append($"{parameter.Name}, ");
        //    }
        //    builder.Append("ProgressToken.None);");
        //    return SF.ParseStatement(builder.ToString());
        //}

        private IEnumerable<StatementSyntax> GenOperationBody(string actualName, OpenApiOperationDescription descriptor, Dictionary<string, UsingDirectiveSyntax> usings, bool isOverride)
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

            //if (isOverride)
            //{
            //    //yield return SF.ParseStatement($"var result = {requestBuilder.ToString()}");
            //    var paramBuilder = new StringBuilder();
            //    foreach (var parameter in descriptor.Operation.Parameters)
            //    {
            //        paramBuilder.Append(parameter.Name);
            //        paramBuilder.Append(", ");
            //    }
            //    paramBuilder.Append("progressToken");
            //    yield return SF.ParseStatement($"await base.{actualName}({paramBuilder.ToString()}).ConfigureAwait(false);");
            //}
            var requestBuilder = new StringBuilder();
            requestBuilder.Append($"await Request<{returnType}>(progressToken, HttpMethod.{method.ToInitcap()}, \"{path}\", \"{mediatype}\", settings");
            var bodyParameter = descriptor.Operation.Parameters.FirstOrDefault(p => p.Kind == OpenApiParameterKind.Body || p.Kind == OpenApiParameterKind.FormData);
            if (bodyParameter == null)
            {
                if (returnType.StartsWith("List<", StringComparison.Ordinal))
                {
                    requestBuilder.Append(", pages");
                }
                else
                {
                    requestBuilder.Append(", null");
                }
            }
            else
            {
                requestBuilder.Append($", {bodyParameter.Name}");
            }
            foreach (var parameter in descriptor.Operation.Parameters.Where(p => p.Kind == OpenApiParameterKind.Path || p.Kind == OpenApiParameterKind.Query))
            {
                requestBuilder.Append($", {parameter.Name}");
            }
            requestBuilder.Append(").ConfigureAwait(false);");

            yield return SF.ParseStatement($"return {requestBuilder}");
        }

        private IEnumerable<ParameterSyntax> GenOperationParameter(OpenApiOperationDescription descriptor, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            foreach (var parameter in descriptor.Operation.Parameters)
            {
                if (parameter.Kind == OpenApiParameterKind.Header)
                    continue;
                yield return SF.Parameter(attributeLists: SF.List<AttributeListSyntax>(),
                                                         modifiers: SF.TokenList(),
                                                         type: GetTypeDeclaration(parameter, false, usings, "List"),
                                                         identifier: SF.Identifier(parameter.Name),
                                                         @default: null);
            }

            var bodyParameter = descriptor.Operation.Parameters.FirstOrDefault(p => p.Kind == OpenApiParameterKind.Body || p.Kind == OpenApiParameterKind.FormData);
            var returnType = GetReturningType(descriptor, usings);
            if (bodyParameter == null && returnType.StartsWith("List<", StringComparison.Ordinal))
            {
                yield return SF.Parameter(attributeLists: SF.List<AttributeListSyntax>(),
                                                        modifiers: SF.TokenList(),
                                                        type: SF.ParseTypeName("HttpPageSettings"),
                                                        identifier: SF.Identifier("pages"),
                                                        @default: null);
            }
            yield return SF.Parameter(attributeLists: SF.List<AttributeListSyntax>(),
                                                        modifiers: SF.TokenList(),
                                                        type: SF.ParseTypeName("HttpJsonSettings"),
                                                        identifier: SF.Identifier("settings"),
                                                        @default: null);
            yield return SF.Parameter(attributeLists: SF.List<AttributeListSyntax>(),
                                                        modifiers: SF.TokenList(),
                                                        type: SF.ParseTypeName("ProgressToken"),
                                                        identifier: SF.Identifier("progressToken"),
                                                        @default: null);
        }

        private CompilationUnitSyntax GetOrGenDefinion(string key, out Type type)
        {
            type = null;
            if (!cacheModels.TryGetValue(key, out var tree))
            {
                cacheModels[key] = null;
                var definition = document.Definitions[key];
                type = GetReferenceType(definition);
                if (type == null)
                {
                    cacheModels[key] = tree = GenDefinition(definition);
                }
            }
            else if (tree == null)
            {
                type = GetReferenceType(document.Definitions[key]);
            }
            return tree;
        }

        private Type GetReferenceType(JsonSchema definition)
        {
            var definitionName = GetDefinitionName(definition);
            if (!cacheReferences.TryGetValue(definitionName, out var type))
            {
                if (definitionName.Equals("DefaultItem", StringComparison.OrdinalIgnoreCase))
                {
                    type = typeof(object);
                }
                else
                if (definitionName.Equals(nameof(TimeSpan), StringComparison.OrdinalIgnoreCase))
                {
                    type = typeof(TimeSpan);
                }
                else
                {
                    foreach (var reference in References)
                    {
                        try
                        {
                            var parsedType = Helper.ParseType(definitionName, reference);
                            if (parsedType != null)
                            {
                                if (parsedType.IsEnum)
                                {
                                    if (definition.EnumerationNames == null)
                                        continue;
                                    var defiEnumeration = definition.EnumerationNames;
                                    var typeEnumeration = Enum.GetNames(parsedType);
                                    if (defiEnumeration.SequenceEqual(typeEnumeration))
                                    {
                                        type = parsedType;
                                        break;
                                    }
                                }
                                else if (definition.Properties != null)
                                {
                                    var defiProperties = definition.Properties.Keys.Select(p => p.ToLower());
                                    var typeProperties = parsedType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name.ToLower());
                                    var percent = (float)defiProperties.Intersect(typeProperties).Count();
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

        private CompilationUnitSyntax GenDefinition(JsonSchema schema)
        {
            var usings = new Dictionary<string, UsingDirectiveSyntax>
            {
                { "DataWF.Common", SyntaxHelper.CreateUsingDirective("DataWF.Common") } ,
                { "DataWF.WebClient.Common",  SyntaxHelper.CreateUsingDirective("DataWF.WebClient.Common") } ,
                { "System", SyntaxHelper.CreateUsingDirective("System") },
            };
            var attributes = new List<AttributeListSyntax>();

            var @class = schema.IsEnumeration ? GenDefinitionEnum(schema, usings) : GenDefinitionClass(schema, usings, attributes);
            return SyntaxHelper.GenUnit(@class, Namespace, usings.Values.OrderBy(p => p.ToString()), attributes);
        }

        private MemberDeclarationSyntax GenDefinitionEnum(JsonSchema schema, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            SyntaxHelper.AddUsing("System.Runtime.Serialization", usings);
            return SF.EnumDeclaration(
                    attributeLists: SF.List(GenDefinitionEnumAttributes(schema)),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                    identifier: SF.Identifier(GetDefinitionName(schema)),
                    baseList: null,
                    members: SF.SeparatedList(GenDefinitionEnumMemebers(schema))
                    );
        }

        private IEnumerable<AttributeListSyntax> GenDefinitionEnumAttributes(JsonSchema schema)
        {
            if (schema.ExtensionData?.TryGetValue("x-flags", out _) ?? false)
            {
                yield return SyntaxHelper.GenAttributeList("Flags");
            }
        }

        private IEnumerable<EnumMemberDeclarationSyntax> GenDefinitionEnumMemebers(JsonSchema schema)
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
                var sitem = schema.EnumerationNames[i];
                var memeber = members?[i].ToString() ?? sitem;
                //if (!Char.IsLetter(sitem[0]))
                //{
                //    sitem = definitionName[0] + sitem;
                //}
                yield return SF.EnumMemberDeclaration(attributeLists: SF.SingletonList(SyntaxHelper.GenAttributeList("EnumMember", $"Value = \"{memeber}\"")),
                        identifier: SF.Identifier(sitem),
                        equalsValue: SF.EqualsValueClause(SF.Token(SyntaxKind.EqualsToken), SF.ParseExpression(item.ToString())));

                i++;
            }
        }

        private MemberDeclarationSyntax GenDefinitionClass(JsonSchema schema, Dictionary<string, UsingDirectiveSyntax> usings, List<AttributeListSyntax> attributes)
        {
            var refFields = referenceFields[schema] = new List<RefField>();
            return SF.ClassDeclaration(
                    attributeLists: SF.List(DefinitionAttributeList()),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)),
                    identifier: SF.Identifier(GetDefinitionName(schema)),
                    typeParameterList: null,
                    baseList: SF.BaseList(SF.SeparatedList(GenDefinitionClassBases(schema, usings))),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    members: SF.List(GenDefinitionClassMemebers(schema, refFields, usings, attributes)));
        }

        private IEnumerable<BaseTypeSyntax> GenDefinitionClassBases(JsonSchema schema, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            if (schema.InheritedSchema != null)
            {
                GetOrGenDefinion(schema.InheritedSchema.Id, out var type);
                if (type != null)
                {
                    SyntaxHelper.AddUsing(type, usings);
                }
                yield return SF.SimpleBaseType(SF.ParseTypeName(GetDefinitionName(schema.InheritedSchema)));
            }
            else
            {
                //yield return SF.SimpleBaseType(SF.ParseTypeName(nameof(IEntryNotifyPropertyChanged)));
                if (schema.Id == "DBItem")
                    yield return SF.SimpleBaseType(SF.ParseTypeName("SynchronizedItem"));
                else
                    yield return SF.SimpleBaseType(SF.ParseTypeName("DefaultItem"));
            }
            var idKey = GetPrimaryKey(schema, false);
            if (idKey != null)
            {
                yield return SF.SimpleBaseType(SF.ParseTypeName("IPrimaryKey"));
                yield return SF.SimpleBaseType(SF.ParseTypeName("IQueryFormatable"));
            }
        }

        private IEnumerable<MemberDeclarationSyntax> GenDefinitionClassMemebers(JsonSchema schema, List<RefField> refFields, Dictionary<string, UsingDirectiveSyntax> usings, List<AttributeListSyntax> attributes)
        {
            var idKey = GetPrimaryKey(schema);
            var typeKey = GetTypeKey(schema);
            var typeId = GetTypeId(schema);

            foreach (var property in schema.Properties)
            {
                foreach (var item in GenDefinitionClassField(property.Value, idKey, refFields, usings))
                {
                    yield return item;
                }
            }

            yield return SF.ConstructorDeclaration(
                      attributeLists: SF.List<AttributeListSyntax>(),
                      modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                      identifier: SF.Identifier(GetDefinitionName(schema)),
                      parameterList: SF.ParameterList(),
                      initializer: null,
                      body: SF.Block(GenDefinitionClassConstructorBody(schema, idKey, typeKey, typeId, refFields, usings)));

            if (refFields.Count > 0 && idKey.ParentSchema != schema)
            {
                yield return GenDefinitionClassProperty(idKey, idKey, typeKey, refFields, usings, true);
            }

            foreach (var property in schema.Properties)
            {
                yield return GenDefinitionClassProperty(property.Value, idKey, typeKey, refFields, usings);
            }

            if (GetPrimaryKey(schema, false) != null)
            {
                SyntaxHelper.AddUsing("System.Text.Json.Serialization", usings);
                yield return SF.PropertyDeclaration(
                    attributeLists: SF.List(new[] {
                        SF.AttributeList(SF.SingletonSeparatedList(SF.Attribute(SF.IdentifierName("JsonIgnore")))) }),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                    type: SF.ParseTypeName("object"),
                    explicitInterfaceSpecifier: null,
                    identifier: SF.Identifier("PrimaryKey"),
                    accessorList: SF.AccessorList(SF.List(new[]
                    {
                        SF.AccessorDeclaration(
                            kind: SyntaxKind.GetAccessorDeclaration,
                            body: SF.Block(new[]{ SF.ParseStatement($"return {idKey.Name};") })),
                        SF.AccessorDeclaration(
                            kind: SyntaxKind.SetAccessorDeclaration,
                            body: SF.Block(new[]{ SF.ParseStatement($"{idKey.Name} = ({GetTypeString(idKey, true, usings, "List")})value;")}))
                    })),
                    expressionBody: null,
                    initializer: null,
                    semicolonToken: SF.Token(SyntaxKind.None));

                yield return SF.MethodDeclaration(
                    attributeLists: SF.List<AttributeListSyntax>(),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                    returnType: SF.ParseTypeName("string"),
                    explicitInterfaceSpecifier: null,
                    identifier: SF.Identifier("Format"),
                    typeParameterList: null,
                    parameterList: SF.ParameterList(),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    body: SF.Block(new[] { SF.ParseStatement($"return {idKey.Name}.ToString();") }),
                    semicolonToken: SF.Token(SyntaxKind.None));
            }
            var definitionName = GetDefinitionName(schema);

            foreach (var property in schema.Properties)
            {
                var name = GetInvokerName(property.Value);
                var refkey = GetPropertyRefKey(property.Value);
                var propertyType = GetTypeString(property.Value, property.Value.IsNullableRaw ?? true, usings, refkey == null ? "SelectableList" : "ReferenceList");
                var propertyName = GetPropertyName(property.Value);

                yield return GenDefinitionClassPropertyInvoker(name, definitionName, propertyName, propertyType, attributes);
            }

            if (GetPrimaryKey(schema, false) != null)
            {
                yield return GenDefinitionClassPropertyInvoker("PrimaryKeyInvoker", definitionName, "PrimaryKey", "object", attributes);
            }
        }

        private ClassDeclarationSyntax GenDefinitionClassPropertyInvoker(string name, string definitionName, string propertyName, string propertyType, List<AttributeListSyntax> attributes)
        {
            attributes.AddRange(GenDefinitionClassPropertyInvokerAttribute(definitionName, propertyName, $"{definitionName}.{name}"));
            return SF.ClassDeclaration(
                     attributeLists: SF.List<AttributeListSyntax>(),
                     modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),//, SF.Token(SyntaxKind.PartialKeyword)
                     identifier: SF.Identifier(name),
                     typeParameterList: null,
                     baseList: SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(
                            SF.SimpleBaseType(SF.ParseTypeName($"Invoker<{definitionName}, {propertyType}>")))),
                     constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                     //SF.List(new TypeParameterConstraintClauseSyntax[] {
                     //    SF.TypeParameterConstraintClause(
                     //        name: SF.IdentifierName("T"),
                     //        constraints: SF.SeparatedList(new TypeParameterConstraintSyntax[] {
                     //            SF.TypeConstraint(SF.ParseTypeName(definitionName))
                     //        }))
                     //}),
                     members: SF.List(GenDefinitionClassPropertyInvokerMemebers(name, propertyName, propertyType, definitionName)));
        }

        private IEnumerable<MemberDeclarationSyntax> GenDefinitionClassPropertyInvokerMemebers(string name, string propertyName, string propertyType, string definitionName)
        {
            yield return SF.FieldDeclaration(attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.StaticKeyword), SF.Token(SyntaxKind.ReadOnlyKeyword)),
                  declaration: SF.VariableDeclaration(
                      type: SF.ParseTypeName(name),
                      variables: SF.SingletonSeparatedList(
                          SF.VariableDeclarator(
                              identifier: SF.Identifier("Default"),
                              argumentList: null,
                              initializer: SF.EqualsValueClause(SF.ParseExpression($"new {name}()"))))));

            //public override string Name { get; }
            yield return SF.PropertyDeclaration(
                   attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword)),
                   type: SF.ParseTypeName("string"),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier("Name"),
                   accessorList: null,
                   expressionBody: SF.ArrowExpressionClause(SF.ParseExpression($"nameof({definitionName}.{propertyName})")),
                   initializer: null,
                   semicolonToken: SF.Token(SyntaxKind.SemicolonToken));

            //public override bool CanWrite => true;
            yield return SF.PropertyDeclaration(
                   attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword)),
                   type: SF.ParseTypeName("bool"),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier("CanWrite"),
                   accessorList: null,
                   expressionBody: SF.ArrowExpressionClause(SF.ParseExpression("true")),
                   initializer: null,
                   semicolonToken: SF.Token(SyntaxKind.SemicolonToken));

            //public override string GetValue(T target) => target.Name;
            yield return SF.MethodDeclaration(
                   attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword)),
                   returnType: SF.ParseTypeName(propertyType),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier("GetValue"),
                   constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                   typeParameterList: null,
                   body: null,
                   parameterList: SF.ParameterList(SF.SeparatedList(new[] {SF.Parameter(
                       attributeLists: SF.List<AttributeListSyntax>(),
                       modifiers: SF.TokenList(),
                       type: SF.ParseTypeName(definitionName),
                       identifier: SF.Identifier("target"),
                       @default: null
                       ) })),
                   expressionBody: SF.ArrowExpressionClause(SF.ParseExpression($"target.{propertyName}")),
                   semicolonToken: SF.Token(SyntaxKind.SemicolonToken));
            //public override void SetValue(T target, string value) => target.Name = value;
            yield return SF.MethodDeclaration(
                   attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword)),
                   returnType: SF.ParseTypeName("void"),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier("SetValue"),
                   constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                   typeParameterList: null,
                   body: null,
                   parameterList: SF.ParameterList(SF.SeparatedList(new[] {
                       SF.Parameter(
                           attributeLists: SF.List<AttributeListSyntax>(),
                           modifiers: SF.TokenList(),
                           type: SF.ParseTypeName(definitionName),
                           identifier: SF.Identifier("target"),
                           @default: null),
                       SF.Parameter(
                           attributeLists: SF.List<AttributeListSyntax>(),
                           modifiers: SF.TokenList(),
                           type: SF.ParseTypeName(propertyType),
                           identifier: SF.Identifier("value"),
                           @default: null)
                   })),
                   expressionBody: SF.ArrowExpressionClause(SF.ParseExpression($"target.{propertyName} = value")),
                   semicolonToken: SF.Token(SyntaxKind.SemicolonToken));

        }

        private IEnumerable<AttributeListSyntax> GenDefinitionClassPropertyInvokerAttribute(string definitionName, string propertyName, string invokerName)
        {
            yield return SyntaxHelper.GenAttributeList("assembly: Invoker", $"typeof({Namespace}.{definitionName}), nameof({Namespace}.{definitionName}.{propertyName}), typeof({Namespace}.{invokerName})");
        }

        private string GetInvokerName(JsonSchemaProperty property)
        {
            return GetPropertyName(property) + "Invoker";
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

        private IEnumerable<StatementSyntax> GenDefinitionClassConstructorBody(JsonSchema schema, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId, List<RefField> refFields, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            if (typeId != 0)
            {
                yield return SF.ParseStatement($"{GetPropertyName(typeKey)} = {typeId};");
            }
            foreach (var refField in refFields)
            {
                //yield return SF.ParseStatement($@"{refField.FieldName} = new {refField.FieldType}(
                //new Query<{refField.TypeName}>(new[]{{{refField.ParameterName}}}),
                //ClientProvider.Default.{refField.TypeName}.Items,
                //false);");
                yield return SF.ParseStatement($@"{refField.FieldName} = new {refField.FieldType} (this, nameof({refField.PropertyName}));");
            }
            foreach (var property in schema.Properties.Select(p => p.Value))
            {
                if (property.Default != null)
                {
                    yield return SF.ParseStatement($@"{GetPropertyName(property)} = {GenFieldDefault(property, idKey, usings)};");
                }
            }
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

        private PropertyDeclarationSyntax GenDefinitionClassProperty(JsonSchemaProperty property, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, List<RefField> refFields,
            Dictionary<string, UsingDirectiveSyntax> usings, bool isOverride = false)
        {
            var refkey = GetPropertyRefKey(property);
            var typeDeclaration = GetTypeDeclaration(property, property.IsNullableRaw ?? true, usings, refkey == null ? "SelectableList" : "ReferenceList");

            return SF.PropertyDeclaration(
                attributeLists: SF.List(GenDefinitionClassPropertyAttributes(property, idKey, typeKey, usings)),
                modifiers: SF.TokenList(GenDefinitionClassPropertyModifiers(property, idKey, isOverride)),
                type: typeDeclaration,
                explicitInterfaceSpecifier: null,
                identifier: SF.Identifier(GetPropertyName(property)),
                accessorList: SF.AccessorList(SF.List(GenDefinitionClassPropertyAccessors(property, idKey, refFields, usings, isOverride))),
                expressionBody: null,
                initializer: null,
                semicolonToken: SF.Token(SyntaxKind.None)
               );
        }

        private IEnumerable<SyntaxToken> GenDefinitionClassPropertyModifiers(JsonSchemaProperty property, JsonSchemaProperty idKey, bool isOverride)
        {
            yield return SF.Token(SyntaxKind.PublicKeyword);
            if (isOverride)
            {
                yield return SF.Token(SyntaxKind.OverrideKeyword);
            }
            else if (property == idKey)
            {
                yield return SF.Token(SyntaxKind.VirtualKeyword);
            }
        }

        private IEnumerable<AttributeListSyntax> GenDefinitionClassPropertyAttributes(JsonSchemaProperty property, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            if (property.IsReadOnly
                || (property.ExtensionData != null && property.ExtensionData.TryGetValue("readOnly", out var isReadOnly) && (bool)isReadOnly))
            {
                yield return SF.AttributeList(
                             SF.SingletonSeparatedList(
                                 SF.Attribute(
                                     SF.IdentifierName("JsonIgnoreSerialization"))));
            }
            else if (property == typeKey)
            {
                SyntaxHelper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                yield return SyntaxHelper.GenAttributeList("Display", $"Order = -3");
            }
            else if (property == idKey)
            {
                SyntaxHelper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                yield return SyntaxHelper.GenAttributeList("Display", $"Order = -2");
            }
            else if ((property.Type == JsonObjectType.Object
                && !property.IsEnumeration
                && property.AllInheritedSchemas.Any(p => p.Id == "DBItem"))
                || (property.Type == JsonObjectType.None
                && property.ActualTypeSchema.Type == JsonObjectType.Object
                && !property.ActualTypeSchema.IsEnumeration
                && property.ActualTypeSchema.AllInheritedSchemas.Any(p => p.Id == "DBItem")))
            {
                yield return SyntaxHelper.GenAttributeList("JsonSynchronized", null);
            }
            else //if (!property.IsRequired)
            {
                yield return SyntaxHelper.GenAttributeList("JsonSynchronized", null);
            }

            foreach (var attribute in GenDefinitionClassPropertyValidationAttributes(property, idKey, typeKey, usings))
            {
                yield return attribute;
            }
        }

        private IEnumerable<AttributeListSyntax> GenDefinitionClassPropertyValidationAttributes(JsonSchemaProperty property, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, Dictionary<string, UsingDirectiveSyntax> usings)
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
                    yield return SyntaxHelper.GenAttributeList("Required", $"AllowEmptyStrings = true");
                }
                else
                {
                    yield return SyntaxHelper.GenAttributeList("Required", $"ErrorMessage = \"{propertyName} is required\"");
                }
            }

            if (property.MaxLength != null)
            {
                SyntaxHelper.AddUsing("System.ComponentModel.DataAnnotations", usings);
                yield return SyntaxHelper.GenAttributeList("MaxLength",
                    $"{property.MaxLength}, ErrorMessage = \"{propertyName} only max {property.MaxLength} letters allowed.\"");
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

        private IEnumerable<AccessorDeclarationSyntax> GenDefinitionClassPropertyAccessors(
            JsonSchemaProperty property,
            JsonSchemaProperty idKey,
            List<RefField> refFields,
            Dictionary<string, UsingDirectiveSyntax> usings,
            bool isOverride)
        {
            yield return SF.AccessorDeclaration(
                kind: SyntaxKind.GetAccessorDeclaration,
                body: SF.Block(GenDefintionClassPropertyGet(property, isOverride)));
            yield return SF.AccessorDeclaration(
                kind: SyntaxKind.SetAccessorDeclaration,
                body: SF.Block(GenDefinitionClassPropertySet(property, idKey, refFields, usings, isOverride)));
        }

        private IEnumerable<StatementSyntax> GenDefintionClassPropertyGet(JsonSchemaProperty property, bool isOverride)
        {
            var fieldName = GetFieldName(property);
            if (isOverride)
            {
                yield return SF.ParseStatement($"return base.{GetPropertyName(property)};");
                yield break;
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
            yield return SF.ParseStatement($"return {fieldName};");
        }

        private IEnumerable<StatementSyntax> GenDefinitionClassPropertySet(
            JsonSchemaProperty property,
            JsonSchemaProperty idKey,
            List<RefField> refFields,
            Dictionary<string, UsingDirectiveSyntax> usings,
            bool isOverride)
        {
            if (!isOverride)
            {
                var type = GetTypeString(property, true, usings, "SelectableList");
                if (type.Equals("string", StringComparison.Ordinal))
                {
                    yield return SF.ParseStatement($"if(string.Equals({GetFieldName(property)}, value, StringComparison.Ordinal)) return;");
                }
                else
                {
                    yield return SF.ParseStatement($"if({GetFieldName(property)} == value) return;");
                }
                yield return SF.ParseStatement($"var temp = {GetFieldName(property)};");
                yield return SF.ParseStatement($"{GetFieldName(property)} = value;");
                if (property.ExtensionData != null && property.ExtensionData.TryGetValue("x-id", out var refPropertyName))
                {
                    var idProperty = GetPrimaryKey(property.Reference ?? property.AllOf.FirstOrDefault()?.Reference ?? property.AnyOf.FirstOrDefault()?.Reference);
                    yield return SF.ParseStatement($"{refPropertyName} = value?.{GetPropertyName(idProperty)};");
                }
                //Change - refence from single json
                var objectProperty = GetReferenceProperty((JsonSchema)property.Parent, property.Name);
                if (objectProperty != null)
                {
                    var objectFieldName = GetFieldName(objectProperty);
                    yield return SF.ParseStatement($"if({objectFieldName}?.Id != value)");
                    yield return SF.ParseStatement("{");
                    yield return SF.ParseStatement($"{objectFieldName} = value == null ? null : {GetTypeString(objectProperty, false, usings, "List")}Client.Instance.Select(value.Value);");
                    yield return SF.ParseStatement($"OnPropertyChanged(nameof({GetPropertyName(objectProperty)}));");
                    yield return SF.ParseStatement("}");
                }

                yield return SF.ParseStatement($"OnPropertyChanged(temp, value);");
            }
            else
            {
                yield return SF.ParseStatement($"base.{GetPropertyName(property)} = value;");
            }
            //if (property.Name == idKey?.Name)
            //{
            //    foreach (var refField in refFields)
            //    {
            //        yield return SF.ParseStatement($"{refField.ParameterName}.Value = value;");
            //    }
            //}
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

        private IEnumerable<FieldDeclarationSyntax> GenDefinitionClassField(JsonSchemaProperty property, JsonSchemaProperty idKey, List<RefField> refFields, Dictionary<string, UsingDirectiveSyntax> usings)
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
                    TypeName = GetTypeString(property.Item, false, usings),
                };
                refFields.Add(refField);
                //var refTypePrimary = GetPrimaryKey(refTypeSchema);
                //var refTypePrimaryName = GetPropertyName(refTypePrimary);
                //var refTypePrimaryType = GetTypeString(refTypePrimary, true, "SelectableList");
                refField.KeyProperty = GetProperty(refField.TypeSchema, refkey);
                refField.KeyName = GetPropertyName(refField.KeyProperty);
                refField.KeyType = GetTypeString(refField.KeyProperty, true, usings);

                refField.ValueProperty = GetReferenceProperty((JsonSchema)refField.KeyProperty.Parent, refField.KeyName);
                refField.ValueName = GetPropertyName(refField.ValueProperty);

                refField.ValueType = GetTypeString(refField.ValueProperty, false, null);
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

                refField.FieldType = GetTypeString(property, property.IsNullableRaw ?? true, usings, "ReferenceList");
                refField.FieldName = GetFieldName(property);
                yield return SF.FieldDeclaration(attributeLists: SF.List<AttributeListSyntax>(),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.ProtectedKeyword)),
                   declaration: SF.VariableDeclaration(
                       type: SF.ParseTypeName(refField.FieldType),
                       variables: SF.SingletonSeparatedList(
                           SF.VariableDeclarator(
                               identifier: SF.Identifier(refField.FieldName),
                               argumentList: null,
                               initializer: null))));
            }
            else
            {
                var type = GetTypeString(property, property.IsNullableRaw ?? true, usings);
                yield return SF.FieldDeclaration(attributeLists: SF.List<AttributeListSyntax>(),
                    modifiers: SF.TokenList(type.EndsWith('?')
                    ? new[] { SF.Token(SyntaxKind.ProtectedKeyword) }
                    : new[] { SF.Token(SyntaxKind.ProtectedKeyword), SF.Token(SyntaxKind.InternalKeyword) }),
                   declaration: SF.VariableDeclaration(
                       type: SF.ParseTypeName(type),
                       variables: SF.SingletonSeparatedList(
                           SF.VariableDeclarator(
                               identifier: SF.Identifier(GetFieldName(property)),
                               argumentList: null,
                               initializer: property.Type == JsonObjectType.Array
                               ? SF.EqualsValueClause(SF.ParseExpression($"new {type}()"))
                               : null))));
            }
        }

        private ExpressionSyntax GenFieldDefault(JsonSchemaProperty property, JsonSchemaProperty idKey, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            var text = property.Default.ToString();
            var type = GetTypeString(property, false, usings);
            if (type == "bool")
                text = text.ToLowerInvariant();
            else if (type == "string")
                text = $"\"{text}\"";
            else if (property.ActualSchema != null && property.ActualSchema.IsEnumeration)
                text = $"{type}.{text}";
            return SF.ParseExpression(text);
        }

        private string GetArrayElementTypeString(JsonSchema schema, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            return schema.Type == JsonObjectType.Array
                ? GetTypeString(schema.Item, false, usings, "List")
                : null;
        }

        private string GetTypeString(JsonSchema schema, bool nullable, Dictionary<string, UsingDirectiveSyntax> usings, string listType = "SelectableList")
        {
            if (!nullable && schema.IsNullableRaw == true)
            {
                nullable = true;
            }
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
                    return $"{listType}<{GetTypeString(schema.Item, false, usings, listType)}>";
                case JsonObjectType.None:
                    if (schema.ActualTypeSchema != schema)
                    {
                        return GetTypeString(schema.ActualTypeSchema, nullable, usings, listType);
                    }
                    else if (schema is JsonSchemaProperty propertySchema)
                    {
                        return GetTypeString(propertySchema.AllOf.FirstOrDefault()?.Reference
                            ?? propertySchema.AnyOf.FirstOrDefault()?.Reference, nullable, usings, listType);
                    }
                    else
                    {
                        goto case JsonObjectType.Object;
                    }
                case JsonObjectType.Object:
                    if (schema.Id != null)
                    {
                        GetOrGenDefinion(schema.Id, out var type);
                        if (type != null)
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



        private TypeSyntax GetTypeDeclaration(JsonSchema property, bool nullable, Dictionary<string, UsingDirectiveSyntax> usings, string listType)
        {
            return SF.ParseTypeName(GetTypeString(property, nullable, usings, listType));
        }

        private IEnumerable<AttributeListSyntax> DefinitionAttributeList()
        {
            yield break;
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

    public static class Helper
    {
        private static readonly Dictionary<Assembly, Dictionary<string, Type>> cacheAssemblyTypes = new Dictionary<Assembly, Dictionary<string, Type>>();
        //https://stackoverflow.com/a/24768641
        public static string ToInitcap(this string str, params char[] separator)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            var charArray = new List<char>(str.Length);
            bool newWord = true;
            foreach (Char currentChar in str)
            {
                var newChar = currentChar;
                if (Char.IsLetter(currentChar))
                {
                    if (newWord)
                    {
                        newWord = false;
                        newChar = Char.ToUpper(currentChar);
                    }
                    else
                    {
                        newChar = Char.ToLower(currentChar);
                    }
                }
                else if (separator.Contains(currentChar))
                {
                    newWord = true;
                    continue;
                }
                charArray.Add(newChar);
            }
            return new string(charArray.ToArray());
        }

        public static Type ParseType(string value, Assembly assembly)
        {
            var byName = value.IndexOf('.') < 0;
            if (byName)
            {
                if (!cacheAssemblyTypes.TryGetValue(assembly, out var cache))
                {
                    var definedTypes = assembly.GetExportedTypes();
                    cacheAssemblyTypes[assembly] =
                        cache = new Dictionary<string, Type>(definedTypes.Length, StringComparer.Ordinal);
                    foreach (var defined in definedTypes)
                    {
                        cache[defined.Name] = defined;
                    }
                }

                return cache.TryGetValue(value, out var type) ? type : null;
            }
            else
            {
                return assembly.GetType(value);
            }
        }
    }
}
