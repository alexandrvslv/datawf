using DataWF.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema;
using NSwag;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DataWF.Web.ClientGenerator
{
    public class ClientGenerator
    {
        private readonly HashSet<string> VirtualOperations = new HashSet<string> {
            "GetAsync",
            "PutAsync",
            "PostAsync",
            "FindAsync",
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
        private Dictionary<string, CompilationUnitSyntax> cacheModels = new Dictionary<string, CompilationUnitSyntax>(StringComparer.Ordinal);
        private Dictionary<string, ClassDeclarationSyntax> cacheClients = new Dictionary<string, ClassDeclarationSyntax>(StringComparer.Ordinal);
        private List<UsingDirectiveSyntax> usings = new List<UsingDirectiveSyntax>();
        private Dictionary<JsonSchema, List<RefField>> referenceFields = new Dictionary<JsonSchema, List<RefField>>();
        private OpenApiDocument document;
        private CompilationUnitSyntax provider;

        public ClientGenerator(string source, string output, string nameSpace = "DataWF.Web.Client")
        {
            Namespace = nameSpace;
            Output = string.IsNullOrEmpty(output) ? null : Path.GetFullPath(output);
            Source = source;
        }

        public string Output { get; }
        public string Source { get; }
        public string Namespace { get; }

        public void Generate()
        {
            usings = new List<UsingDirectiveSyntax>() {
                SyntaxHelper.CreateUsingDirective("DataWF.Common") ,
                SyntaxHelper.CreateUsingDirective("System") ,
                SyntaxHelper.CreateUsingDirective("System.Collections.Generic") ,
                SyntaxHelper.CreateUsingDirective("System.ComponentModel") ,
                SyntaxHelper.CreateUsingDirective("System.ComponentModel.DataAnnotations") ,
                SyntaxHelper.CreateUsingDirective("System.Linq") ,
                SyntaxHelper.CreateUsingDirective("System.IO") ,
                SyntaxHelper.CreateUsingDirective("System.Runtime.Serialization") ,
                SyntaxHelper.CreateUsingDirective("System.Runtime.CompilerServices") ,
                SyntaxHelper.CreateUsingDirective("System.Threading") ,
                SyntaxHelper.CreateUsingDirective("System.Threading.Tasks") ,
                SyntaxHelper.CreateUsingDirective("System.Net.Http") ,
                SyntaxHelper.CreateUsingDirective("System.Net.Http.Headers") ,
                SyntaxHelper.CreateUsingDirective("Newtonsoft.Json")
            };
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
                GetOrGenDefinion(definition.Key);
            }
            foreach (var operation in document.Operations)
            {
                AddClientOperation(operation);
            }

            provider = SyntaxHelper.GenUnit(GenProvider(), Namespace, usings);
        }

        private ClassDeclarationSyntax GenProvider()
        {
            return SF.ClassDeclaration(
                    attributeLists: SF.List(ClientAttributeList()),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                    identifier: SF.Identifier($"ClientProvider"),
                    typeParameterList: null,
                    baseList: SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(
                        SF.SimpleBaseType(SF.ParseTypeName("ClientProviderBase")))),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    members: SF.List(GenProviderMemebers())
                    );
        }

        private IEnumerable<MemberDeclarationSyntax> GenProviderMemebers()
        {
            yield return SyntaxHelper.GenProperty("ClientProvider", "Default", true, "new ClientProvider()")
                .AddModifiers(SF.Token(SyntaxKind.StaticKeyword));

            yield return SF.ConstructorDeclaration(
                           attributeLists: SF.List(ClientAttributeList()),
                           modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                           identifier: SF.Identifier($"ClientProvider"),
                           parameterList: SF.ParameterList(),
                           initializer: null,
                           body: SF.Block(GenProviderConstructorBody()));

            //yield return SyntaxHelper.GenProperty("string", "BaseUrl", true);
            //yield return SyntaxHelper.GenProperty("AuthorizationInfo", "Authorization", true);

            //yield return SF.PropertyDeclaration(
            //        attributeLists: SF.List<AttributeListSyntax>(),
            //        modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
            //        type: SF.ParseTypeName("IEnumerable<IClient>"),
            //        explicitInterfaceSpecifier: null,
            //        identifier: SF.Identifier("Clients"),
            //        accessorList: SF.AccessorList(SF.List(new[] {
            //            SF.AccessorDeclaration( SyntaxKind.GetAccessorDeclaration,  SF.Block(GenProviderClientsBody()))
            //        })),
            //        expressionBody: null,
            //        initializer: null,
            //        semicolonToken: SF.Token(SyntaxKind.None));

            foreach (var client in cacheClients.Keys)
            {
                yield return SyntaxHelper.GenProperty($"{client}Client", client, false);
            }

            //yield return SF.MethodDeclaration(
            //   attributeLists: SF.List<AttributeListSyntax>(),
            //       modifiers: SF.TokenList(new[] { SF.Token(SyntaxKind.PublicKeyword) }),
            //       returnType: SF.ParseTypeName("ICRUDClient<T>"),
            //       explicitInterfaceSpecifier: null,
            //       identifier: SF.Identifier("GetClient"),
            //       typeParameterList: SF.TypeParameterList(SF.SingletonSeparatedList(SF.TypeParameter("T"))),
            //       parameterList: SF.ParameterList(),
            //       constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
            //       body: SF.Block(new[] { SF.ParseStatement("return Clients.OfType<ICRUDClient<T>>().FirstOrDefault();") }),
            //       semicolonToken: SF.Token(SyntaxKind.SemicolonToken));

            //yield return SF.MethodDeclaration(
            //   attributeLists: SF.List<AttributeListSyntax>(),
            //       modifiers: SF.TokenList(new[] { SF.Token(SyntaxKind.PublicKeyword) }),
            //       returnType: SF.ParseTypeName("ICRUDClient"),
            //       explicitInterfaceSpecifier: null,
            //       identifier: SF.Identifier("GetClient"),
            //       typeParameterList: null,
            //       parameterList: SF.ParameterList(SF.SingletonSeparatedList(SF.Parameter(
            //           attributeLists: SF.List<AttributeListSyntax>(),
            //           modifiers: SF.TokenList(),
            //           type: SF.ParseTypeName("Type"),
            //           identifier: SF.Identifier("type"),
            //           @default: null))),
            //       constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
            //       body: SF.Block(new[] { SF.ParseStatement("return Clients.OfType<ICRUDClient>().FirstOrDefault(p=>p.ItemType == type);") }),
            //       semicolonToken: SF.Token(SyntaxKind.SemicolonToken));

            //yield return SF.MethodDeclaration(
            //   attributeLists: SF.List<AttributeListSyntax>(),
            //       modifiers: SF.TokenList(new[] { SF.Token(SyntaxKind.PublicKeyword) }),
            //       returnType: SF.ParseTypeName("ICRUDClient"),
            //       explicitInterfaceSpecifier: null,
            //       identifier: SF.Identifier("GetClient"),
            //       typeParameterList: null,
            //       parameterList: SF.ParameterList(SF.SeparatedList(new[]{
            //           SF.Parameter( attributeLists: SF.List<AttributeListSyntax>(), modifiers: SF.TokenList(), type: SF.ParseTypeName("Type"), identifier: SF.Identifier("type"), @default: null),
            //           SF.Parameter( attributeLists: SF.List<AttributeListSyntax>(), modifiers: SF.TokenList(), type: SF.ParseTypeName("int"), identifier: SF.Identifier("typeId"), @default: null)
            //       })),
            //       constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
            //       body: SF.Block(new[] { SF.ParseStatement("return Clients.OfType<ICRUDClient>().FirstOrDefault(p => TypeHelper.IsBaseType(p.ItemType, type) && p.TypeId == typeId);") }),
            //       semicolonToken: SF.Token(SyntaxKind.SemicolonToken));
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
                var unit = SyntaxHelper.GenUnit(entry.Value, Namespace, usings);
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
            var operationName = GetOperationName(descriptor, out var clientName);
            if (!cacheClients.TryGetValue(clientName, out var clientSyntax))
            {
                clientSyntax = GenClient(clientName);
            }

            cacheClients[clientName] = clientSyntax.AddMembers(GenOperation(descriptor).ToArray());
        }

        private ClassDeclarationSyntax GenClient(string clientName)
        {
            var baseType = SF.ParseTypeName(GetClientBaseType(clientName, out var idKey, out var typeKey, out var typeId));

            return SF.ClassDeclaration(
                        attributeLists: SF.List(ClientAttributeList()),
                        modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)),
                        identifier: SF.Identifier($"{clientName}Client"),
                        typeParameterList: null,
                        baseList: SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(
                            SF.SimpleBaseType(baseType))),
                        constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                        members: SF.List(GenClientMembers(clientName, idKey, typeKey, typeId))
                        );
        }

        private IEnumerable<MemberDeclarationSyntax> GenClientMembers(string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId)
        {
            document.Definitions.TryGetValue(clientName, out var clientSchema);

            var cache = clientSchema != null ? GetClientReferences(clientSchema) : new HashSet<RefField>();
            yield return GenClientConstructor(clientName, idKey, typeKey, typeId, cache);
            if (cache.Count > 0)
            {
                yield return SF.MethodDeclaration(
               attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword)),
                   returnType: SF.ParseTypeName("bool"),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier("Remove"),
                   typeParameterList: null,
                   parameterList: SF.ParameterList(
                       SF.SeparatedList(new[] {
                           SF.Parameter(
                               attributeLists: SF.List<AttributeListSyntax>(),
                               modifiers: SF.TokenList(),
                               type: SF.ParseTypeName(clientName),
                               identifier: SF.Identifier("item"),
                               @default: null) })),
                   constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                   body: SF.Block(GenClientRemoveOverrideBody(clientName, idKey, typeKey, typeId, cache)),
                   semicolonToken: SF.Token(SyntaxKind.SemicolonToken));
                yield return SF.MethodDeclaration(
               attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword)),
                   returnType: SF.ParseTypeName("bool"),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier("Add"),
                   typeParameterList: null,
                   parameterList: SF.ParameterList(
                       SF.SeparatedList(new[] {
                           SF.Parameter(
                               attributeLists: SF.List<AttributeListSyntax>(),
                               modifiers: SF.TokenList(),
                               type: SF.ParseTypeName(clientName),
                               identifier: SF.Identifier("item"),
                               @default: null) })),
                   constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                   body: SF.Block(GenClientAddOverrideBody(clientName, idKey, typeKey, typeId, cache)),
                   semicolonToken: SF.Token(SyntaxKind.SemicolonToken));
            }
        }

        private IEnumerable<StatementSyntax> GenClientRemoveOverrideBody(string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId, HashSet<RefField> cache)
        {
            yield return SF.ParseStatement($"var removed = base.Remove(item);");
            if (cache.Count > 0)
            {
                yield return SF.ParseStatement("if(removed){");
            }
            foreach (var refField in cache)
            {
                yield return SF.ParseStatement($"if(item.{refField.KeyName} != null){{");
                yield return SF.ParseStatement($"var item{refField.ValueName} = (item.{refField.ValueFieldName} ?? (item.{refField.ValueFieldName} = ClientProvider.Default.{refField.ValueType}.Select(item.{refField.KeyName}.Value))) as {refField.Definition};");
                yield return SF.ParseStatement($"item{refField.ValueName}?.{refField.PropertyName}.Remove(item);");
                yield return SF.ParseStatement("}");
            }
            if (cache.Count > 0)
            {
                yield return SF.ParseStatement("}");
            }
            yield return SF.ParseStatement($"return removed;");
        }

        private IEnumerable<StatementSyntax> GenClientAddOverrideBody(string clientName, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, int typeId, HashSet<RefField> cache)
        {
            yield return SF.ParseStatement($"var added = base.Add(item);");
            if (cache.Count > 0)
            {
                yield return SF.ParseStatement("if(added){");
            }
            foreach (var refField in cache)
            {
                yield return SF.ParseStatement($"if(item.{refField.KeyName} != null){{");
                yield return SF.ParseStatement($"var item{refField.ValueName} = (item.{refField.ValueFieldName} ?? (item.{refField.ValueFieldName} = ClientProvider.Default.{refField.ValueType}.Select(item.{refField.KeyName}.Value))) as {refField.Definition};");
                yield return SF.ParseStatement($"item{refField.ValueName}?.{refField.PropertyName}.Add(item);");
                yield return SF.ParseStatement("}");
            }
            if (cache.Count > 0)
            {
                yield return SF.ParseStatement("}");
            }
            yield return SF.ParseStatement($"return added;");
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

        private string GetClientBaseType(string clientName, out JsonSchemaProperty idKey, out JsonSchemaProperty typeKey, out int typeId)
        {
            idKey = null;
            typeKey = null;
            typeId = 0;
            var logged = document.Operations
                .Where(p => p.Operation.Tags.Contains(clientName, StringComparer.OrdinalIgnoreCase))
                .FirstOrDefault(p => p.Path.Contains("/GetItemLogs/", StringComparison.OrdinalIgnoreCase));
            var loggedReturnSchema = logged == null ? null : GetReturningTypeSchema(logged);
            var loggedTypeName = loggedReturnSchema == null ? null : GetArrayElementTypeString(loggedReturnSchema);
            if (document.Definitions.TryGetValue(clientName, out var schema))
            {
                idKey = GetPrimaryKey(schema);
                typeKey = GetTypeKey(schema);
                typeId = GetTypeId(schema);

                return $"{(loggedTypeName != null ? "Logged" : "")}Client<{clientName}, {(idKey == null ? "int" : GetTypeString(idKey, false, "List"))}{(logged != null ? $", {loggedTypeName}" : "")}>";
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
                    if (baseClass.ExtensionData != null && baseClass.ExtensionData.TryGetValue("x-id", out propertyName))
                    {
                        return baseClass.Properties[propertyName.ToString()];
                    }
            }
            return null;
        }

        private JsonSchemaProperty GetReferenceProperty(JsonSchema schema, string name, bool inherit = true)
        {
            var find = schema.Properties.Values.FirstOrDefault(p => p.ExtensionData != null
                 && p.ExtensionData.TryGetValue("x-id", out var propertyName)
                 && propertyName.Equals(name));
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
                return (int)Helper.Parse(id, typeof(int));
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
                            SF.Argument(SF.ParseExpression($"{clientName}.{idName}Invoker<{clientName}>.Default")),
                            SF.Argument(SF.ParseExpression($"{clientName}.{typeName}Invoker<{clientName}>.Default")),
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

        private string GetReturningType(OpenApiOperationDescription descriptor)
        {
            var returnType = "string";
            if (descriptor.Operation.Responses.TryGetValue("200", out var responce) && responce.Schema != null)
            {
                returnType = $"{GetTypeString(responce.Schema, false, "List")}";
            }
            return returnType;
        }

        private string GetReturningTypeCheck(OpenApiOperationDescription descriptor, string operationName)
        {
            var returnType = GetReturningType(descriptor);
            if (operationName == "GenerateId")
                returnType = "object";
            //if (returnType == "AccessValue")
            //    returnType = "IAccessValue";
            //if (returnType == "List<AccessItem>")
            //    returnType = "IEnumerable<IAccessItem>";
            return returnType;
        }

        private IEnumerable<MemberDeclarationSyntax> GenOperation(OpenApiOperationDescription descriptor)
        {
            var operationName = GetOperationName(descriptor, out var clientName);
            var actualName = $"{operationName}Async";
            var baseType = GetClientBaseType(clientName, out var id, out var typeKey, out var typeId);
            var isOverride = baseType != "ClientBase" && VirtualOperations.Contains(actualName);
            var returnType = GetReturningTypeCheck(descriptor, operationName);
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
            //        semicolonToken: SF.Token(SyntaxKind.SemicolonToken));

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
                    parameterList: SF.ParameterList(SF.SeparatedList(GenOperationParameter(descriptor))),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    body: SF.Block(GenOperationBody(actualName, descriptor, isOverride)),
                    semicolonToken: SF.Token(SyntaxKind.SemicolonToken));
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

        private IEnumerable<StatementSyntax> GenOperationBody(string actualName, OpenApiOperationDescription descriptor, bool isOverride)
        {
            var method = descriptor.Method.ToString().ToUpperInvariant();
            var path = descriptor.Path;
            var responceSchema = (JsonSchema)null;
            var mediatype = "application/json";
            if (descriptor.Operation.Responses.TryGetValue("200", out var responce))
            {
                responceSchema = responce.Schema;
                mediatype = responce.Content.Keys.FirstOrDefault() ?? "application/json";
            }

            var returnType = GetReturningType(descriptor);

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
            requestBuilder.Append($"await Request");
            if (responceSchema?.Type == JsonObjectType.Array)
                requestBuilder.Append("Array");
            requestBuilder.Append($"<{returnType}");
            if (responceSchema?.Type == JsonObjectType.Array)
                requestBuilder.Append($", {GetTypeString(responceSchema.Item, false, "List")}");
            requestBuilder.Append($">(progressToken, \"{method}\", \"{path}\", \"{mediatype}\"");
            var bodyParameter = descriptor.Operation.Parameters.FirstOrDefault(p => p.Kind != OpenApiParameterKind.Path);
            if (bodyParameter == null)
            {
                requestBuilder.Append(", null");
            }
            else
            {
                requestBuilder.Append($", {bodyParameter.Name}");
            }
            foreach (var parameter in descriptor.Operation.Parameters.Where(p => p.Kind == OpenApiParameterKind.Path))
            {
                requestBuilder.Append($", {parameter.Name}");
            }
            requestBuilder.Append(").ConfigureAwait(false);");

            yield return SF.ParseStatement($"return {requestBuilder.ToString()}");
        }

        private IEnumerable<ParameterSyntax> GenOperationParameter(OpenApiOperationDescription descriptor)
        {
            foreach (var parameter in descriptor.Operation.Parameters)
            {
                yield return SF.Parameter(attributeLists: SF.List<AttributeListSyntax>(),
                                                         modifiers: SF.TokenList(),
                                                         type: GetTypeDeclaration(parameter, false, "List"),
                                                         identifier: SF.Identifier(parameter.Name),
                                                         @default: null);
            }
            yield return SF.Parameter(attributeLists: SF.List<AttributeListSyntax>(),
                                                        modifiers: SF.TokenList(),
                                                        type: SF.ParseTypeName("ProgressToken"),
                                                        identifier: SF.Identifier("progressToken"),
                                                        @default: null);
        }

        private CompilationUnitSyntax GetOrGenDefinion(string key)
        {
            if (!cacheModels.TryGetValue(key, out var tree))
            {
                cacheModels[key] = null;
                cacheModels[key] = tree = GenDefinition(document.Definitions[key]);
            }
            return tree;
        }

        private CompilationUnitSyntax GenDefinition(JsonSchema schema)
        {
            var @class = schema.IsEnumeration ? GenDefinitionEnum(schema) : GenDefinitionClass(schema);
            return SyntaxHelper.GenUnit(@class, Namespace, usings);
        }

        private MemberDeclarationSyntax GenDefinitionEnum(JsonSchema schema)
        {
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
            if (schema.ExtensionData?.TryGetValue("x-flags", out var flags) ?? false)
            {
                yield return SyntaxHelper.GenAttribute("Flags");
            }
        }

        private IEnumerable<EnumMemberDeclarationSyntax> GenDefinitionEnumMemebers(JsonSchema schema)
        {
            var value = -1;
            if (schema.ExtensionData != null && schema.ExtensionData.TryGetValue("x-flags", out var flags) && flags is long lflags)
            {
                value = (int)lflags;
            }
            int i = 0;
            var definitionName = GetDefinitionName(schema);
            foreach (var item in schema.Enumeration)
            {
                var sitem = item.ToString().Replace(" ", "");
                if (!Char.IsLetter(sitem[0]))
                {
                    sitem = definitionName[0] + sitem;
                }
                yield return SF.EnumMemberDeclaration(attributeLists: SF.List(GenDefinitionEnumMemberAttribute(item)),
                        identifier: SF.Identifier(sitem),
                        equalsValue: value >= 0
                            ? SF.EqualsValueClause(SF.Token(SyntaxKind.EqualsToken), SF.ParseExpression(value.ToString()))
                            : null);

                i++;
                if (value == 0)
                {
                    value = 1;
                }
                else if (value > 0)
                {
                    value *= 2;
                }

                //SF.EqualsValueClause(SF.Token(SyntaxKind.EqualsToken), SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(i++)))
            }
        }

        private IEnumerable<AttributeListSyntax> GenDefinitionEnumMemberAttribute(object item)
        {
            //[System.Runtime.Serialization.EnumMember(Value = "Empty")]
            yield return SyntaxHelper.GenAttribute("EnumMember", $"Value = \"{item.ToString()}\"");
        }

        private MemberDeclarationSyntax GenDefinitionClass(JsonSchema schema)
        {
            var refFields = referenceFields[schema] = new List<RefField>();
            return SF.ClassDeclaration(
                    attributeLists: SF.List(DefinitionAttributeList()),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)),
                    identifier: SF.Identifier(GetDefinitionName(schema)),
                    typeParameterList: null,
                    baseList: SF.BaseList(SF.SeparatedList(GenDefinitionClassBases(schema))),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    members: SF.List(GenDefinitionClassMemebers(schema, refFields)));
        }

        private IEnumerable<BaseTypeSyntax> GenDefinitionClassBases(JsonSchema schema)
        {
            if (schema.InheritedSchema != null)
            {
                GetOrGenDefinion(schema.InheritedSchema.Id);
                yield return SF.SimpleBaseType(SF.ParseTypeName(GetDefinitionName(schema.InheritedSchema)));
            }
            else
            {
                //yield return SF.SimpleBaseType(SF.ParseTypeName(nameof(IContainerNotifyPropertyChanged)));
                if (schema.Id == "DBItem")
                    yield return SF.SimpleBaseType(SF.ParseTypeName(nameof(SynchronizedItem)));
                else
                    yield return SF.SimpleBaseType(SF.ParseTypeName(nameof(DefaultItem)));
            }
            var idKey = GetPrimaryKey(schema, false);
            if (idKey != null)
            {
                yield return SF.SimpleBaseType(SF.ParseTypeName(nameof(IPrimaryKey)));
                yield return SF.SimpleBaseType(SF.ParseTypeName(nameof(IQueryFormatable)));
            }
        }

        private IEnumerable<MemberDeclarationSyntax> GenDefinitionClassMemebers(JsonSchema schema, List<RefField> refFields)
        {
            var idKey = GetPrimaryKey(schema);
            var typeKey = GetTypeKey(schema);
            var typeId = GetTypeId(schema);

            foreach (var property in schema.Properties)
            {
                foreach (var item in GenDefinitionClassField(property.Value, idKey, refFields))
                {
                    yield return item;
                }
            }

            if (typeId != 0 || refFields.Count > 0)
            {
                yield return SF.ConstructorDeclaration(
                          attributeLists: SF.List<AttributeListSyntax>(),
                          modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                          identifier: SF.Identifier(GetDefinitionName(schema)),
                          parameterList: SF.ParameterList(),
                          initializer: null,
                          body: SF.Block(GenDefinitionClassConstructorBody(typeKey, typeId, refFields)));

                //yield return SF.PropertyDeclaration(
                //    attributeLists: SF.List<AttributeListSyntax>(),
                //    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword)),
                //    type: SF.ParseTypeName("SynchronizedStatus"),
                //    explicitInterfaceSpecifier: null,
                //    identifier: SF.Identifier("SyncStatus"),
                //    accessorList: SF.AccessorList(SF.List(new[]
                //    {
                //        SF.AccessorDeclaration(
                //            kind: SyntaxKind.GetAccessorDeclaration,
                //            body: SF.Block(GenDefintionClassPropertySynckGet(typeKey, typeId, refFields))),
                //        SF.AccessorDeclaration(
                //            kind: SyntaxKind.SetAccessorDeclaration,
                //            body: SF.Block(new[]{ SF.ParseStatement($"base.SyncStatus = value;")}))
                //    })),
                //    expressionBody: null,
                //    initializer: null,
                //    semicolonToken: SF.Token(SyntaxKind.None));
            }

            if (refFields.Count > 0 && idKey.ParentSchema != schema)
            {
                yield return GenDefinitionClassProperty(idKey, idKey, typeKey, refFields, true);
            }

            foreach (var property in schema.Properties)
            {
                yield return GenDefinitionClassProperty(property.Value, idKey, typeKey, refFields);
            }

            if (GetPrimaryKey(schema, false) != null)
            {
                yield return SF.PropertyDeclaration(
                    attributeLists: SF.List(new[] {
                        SF.AttributeList(SF.SingletonSeparatedList(SF.Attribute(SF.IdentifierName("JsonIgnore")))) }),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                    type: SF.ParseTypeName("object"),
                    explicitInterfaceSpecifier: null,
                    identifier: SF.Identifier(nameof(IPrimaryKey.PrimaryKey)),
                    accessorList: SF.AccessorList(SF.List(new[]
                    {
                        SF.AccessorDeclaration(
                            kind: SyntaxKind.GetAccessorDeclaration,
                            body: SF.Block(new[]{ SF.ParseStatement($"return {idKey.Name};") })),
                        SF.AccessorDeclaration(
                            kind: SyntaxKind.SetAccessorDeclaration,
                            body: SF.Block(new[]{ SF.ParseStatement($"{idKey.Name} = ({GetTypeString(idKey, true, "List")})value;")}))
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
                    semicolonToken: SF.Token(SyntaxKind.SemicolonToken));
            }
            var definitionName = GetDefinitionName(schema);

            foreach (var property in schema.Properties)
            {
                var name = GetInvokerName(property.Value);
                var refkey = GetPropertyRefKey(property.Value);
                var propertyType = GetTypeString(property.Value, property.Value.IsNullableRaw ?? true, refkey == null ? "SelectableList" : "ReferenceList");
                var propertyName = GetPropertyName(property.Value);

                yield return GenDefinitionClassPropertyInvoker(name, definitionName, propertyName, propertyType);
            }

            if (GetPrimaryKey(schema, false) != null)
            {
                yield return GenDefinitionClassPropertyInvoker("PrimaryKeyInvoker", definitionName, "PrimaryKey", "object");
            }
        }

        private ClassDeclarationSyntax GenDefinitionClassPropertyInvoker(string name, string definitionName, string propertyName, string propertyType)
        {

            return SF.ClassDeclaration(
                     attributeLists: SF.List(GenDefinitionClassPropertyInvokerAttribute(definitionName, propertyName)),
                     modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)),
                     identifier: SF.Identifier(name + "<T>"),
                     typeParameterList: null,
                     baseList: SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(
                            SF.SimpleBaseType(SF.ParseTypeName($"Invoker<T, {propertyType}>")))),
                     constraintClauses: SF.List(new TypeParameterConstraintClauseSyntax[] {
                         SF.TypeParameterConstraintClause(
                             name: SF.IdentifierName("T"),
                             constraints: SF.SeparatedList(new TypeParameterConstraintSyntax[] {
                                 SF.TypeConstraint(SF.ParseTypeName(definitionName))
                             }))
                     }),
                     members: SF.List(GenDefinitionClassPropertyInvokerMemebers(name, propertyName, propertyType, definitionName)));
        }

        private IEnumerable<MemberDeclarationSyntax> GenDefinitionClassPropertyInvokerMemebers(string name, string propertyName, string propertyType, string definitionName)
        {
            yield return SF.FieldDeclaration(attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.StaticKeyword), SF.Token(SyntaxKind.ReadOnlyKeyword)),
                  declaration: SF.VariableDeclaration(
                      type: SF.ParseTypeName(name + "<T>"),
                      variables: SF.SingletonSeparatedList(
                          SF.VariableDeclarator(
                              identifier: SF.Identifier("Default"),
                              argumentList: null,
                              initializer: SF.EqualsValueClause(SF.ParseExpression($"new {name}<T>()"))))));

            //public override string Name { get; }
            yield return SF.PropertyDeclaration(
                   attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword)),
                   type: SF.ParseTypeName("string"),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier(nameof(IInvoker.Name)),
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
                   identifier: SF.Identifier(nameof(IInvoker.CanWrite)),
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
                   identifier: SF.Identifier(nameof(IInvoker.GetValue)),
                   constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                   typeParameterList: null,
                   body: null,
                   parameterList: SF.ParameterList(SF.SeparatedList(new[] {SF.Parameter(
                       attributeLists: SF.List<AttributeListSyntax>(),
                       modifiers: SF.TokenList(),
                       type: SF.ParseTypeName("T"),
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
                   identifier: SF.Identifier(nameof(IInvoker.SetValue)),
                   constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                   typeParameterList: null,
                   body: null,
                   parameterList: SF.ParameterList(SF.SeparatedList(new[] {
                       SF.Parameter(
                           attributeLists: SF.List<AttributeListSyntax>(),
                           modifiers: SF.TokenList(),
                           type: SF.ParseTypeName("T"),
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

        private IEnumerable<AttributeListSyntax> GenDefinitionClassPropertyInvokerAttribute(string definitionName, string propertyName)
        {
            yield return SyntaxHelper.GenAttribute("Invoker", $"typeof({definitionName}), nameof({definitionName}.{propertyName})");
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

        private IEnumerable<StatementSyntax> GenDefinitionClassConstructorBody(JsonSchemaProperty typeKey, int typeId, List<RefField> refFields)
        {
            if (typeId != 0)
            {
                yield return SF.ParseStatement($"{GetPropertyName(typeKey)} = {typeId};");
            }
            foreach (var refField in refFields)
            {
                //        yield return SF.ParseStatement($@"{refField.FieldName} = new {refField.FieldType}(
                //new Query<{refField.TypeName}>(new[]{{{refField.ParameterName}}}),
                //ClientProvider.Default.{refField.TypeName}.Items,
                //false);");
                yield return SF.ParseStatement($@"{refField.FieldName} = new {refField.FieldType} (this, nameof({refField.PropertyName}));");
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

        private PropertyDeclarationSyntax GenDefinitionClassProperty(JsonSchemaProperty property, JsonSchemaProperty idKey, JsonSchemaProperty typeKey, List<RefField> refFields, bool isOverride = false)
        {
            var refkey = GetPropertyRefKey(property);
            var typeDeclaration = GetTypeDeclaration(property, property.IsNullableRaw ?? true, refkey == null ? "SelectableList" : "ReferenceList");

            return SF.PropertyDeclaration(
                attributeLists: SF.List(GenDefinitionClassPropertyAttributes(property, idKey, typeKey)),
                modifiers: SF.TokenList(GenDefinitionClassPropertyModifiers(property, idKey, isOverride)),
                type: typeDeclaration,
                explicitInterfaceSpecifier: null,
                identifier: SF.Identifier(GetPropertyName(property)),
                accessorList: SF.AccessorList(SF.List(GenDefinitionClassPropertyAccessors(property, idKey, refFields, isOverride))),
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

        private IEnumerable<AttributeListSyntax> GenDefinitionClassPropertyAttributes(JsonSchemaProperty property, JsonSchemaProperty idKey, JsonSchemaProperty typeKey)
        {
            if (property.IsReadOnly
                || (property.ExtensionData != null && property.ExtensionData.TryGetValue("readOnly", out var isReadOnly) && (bool)isReadOnly)
                || property.Type == JsonObjectType.Object && !property.IsEnumeration
                || (property.Type == JsonObjectType.None && property.ActualTypeSchema.Type == JsonObjectType.Object && !property.ActualTypeSchema.IsEnumeration))

            {
                yield return SF.AttributeList(
                             SF.SingletonSeparatedList(
                                 SF.Attribute(
                                     SF.IdentifierName("JsonIgnoreSerialization"))));
            }
            else if (property == typeKey)
            {
                yield return SyntaxHelper.GenAttribute("JsonProperty", $"Order = -3");
            }
            else if (property == idKey)
            {
                yield return SyntaxHelper.GenAttribute("JsonProperty", $"Order = -2");
            }
            else //if (!property.IsRequired)
            {
                yield return SyntaxHelper.GenAttribute("JsonProperty", $"NullValueHandling = NullValueHandling.Include");
            }

            foreach (var attribute in GenDefinitionClassPropertyValidationAttributes(property, idKey, typeKey))
            {
                yield return attribute;
            }
        }

        private IEnumerable<AttributeListSyntax> GenDefinitionClassPropertyValidationAttributes(JsonSchemaProperty property, JsonSchemaProperty idKey, JsonSchemaProperty typeKey)
        {
            var propertyName = GetPropertyName(property);
            var objectProperty = GetReferenceProperty((JsonSchema)property.Parent, property.Name);
            if (objectProperty != null)
            {
                propertyName = GetPropertyName(objectProperty);
            }

            if (property.IsRequired && property != idKey && property != typeKey)
            {
                yield return SyntaxHelper.GenAttribute("Required", $"ErrorMessage = \"{propertyName} is required\"");
            }

            if (property.MaxLength != null)
            {
                yield return SyntaxHelper.GenAttribute("MaxLength",
                    $"{property.MaxLength}, ErrorMessage = \"{propertyName} only max {property.MaxLength} letters allowed.\"");
            }
            var idProperty = GetReferenceIdProperty(property);
            if (idProperty != null)
            {
                foreach (var attribute in GenDefinitionClassPropertyValidationAttributes(idProperty, idKey, typeKey))
                {
                    yield return attribute;
                }
            }
        }

        private IEnumerable<AccessorDeclarationSyntax> GenDefinitionClassPropertyAccessors(JsonSchemaProperty property, JsonSchemaProperty idKey, List<RefField> refFields, bool isOverride)
        {
            yield return SF.AccessorDeclaration(
                kind: SyntaxKind.GetAccessorDeclaration,
                body: SF.Block(
                    GenDefintionClassPropertyGet(property, isOverride)
                ));
            yield return SF.AccessorDeclaration(
                kind: SyntaxKind.SetAccessorDeclaration,
                body: SF.Block(
                    GenDefinitionClassPropertySet(property, idKey, refFields, isOverride)
                ));
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
            if (property.ExtensionData != null && property.ExtensionData.TryGetValue("x-id", out var idProperty))
            {
                var idFiledName = GetFieldName((string)idProperty);
                yield return SF.ParseStatement($"if({fieldName} == null && {idFiledName} != null){{");
                //yield return SF.ParseStatement($"var client = ({GetTypeString(property, false, "List")}Client)ClientProvider.Default.GetClient<{GetTypeString(property, false, "List")}>();");
                yield return SF.ParseStatement($"{fieldName} = ClientProvider.Default.{GetTypeString(property, false, "List")}.Get({idFiledName}.Value,(item) =>{{ {fieldName} = item; OnPropertyChanged(\"{ GetPropertyName(property)}\"); }});");
                //yield return SF.ParseStatement($"{fieldName} = ClientProvider.Default.{GetTypeString(property, false, "List")}.Select({idFiledName}.Value);");
                yield return SF.ParseStatement("}");
            }
            yield return SF.ParseStatement($"return {fieldName};");
        }

        private IEnumerable<StatementSyntax> GenDefinitionClassPropertySet(JsonSchemaProperty property, JsonSchemaProperty idKey, List<RefField> refFields, bool isOverride)
        {
            if (!isOverride)
            {
                yield return SF.ParseStatement($"if({GetFieldName(property)} == value) return;");
                yield return SF.ParseStatement($"var temp = {GetFieldName(property)};");
                yield return SF.ParseStatement($"{GetFieldName(property)} = value;");
                var refPropertyName = (object)null;
                if (property.ExtensionData != null && property.ExtensionData.TryGetValue("x-id", out refPropertyName))
                {
                    var idProperty = GetPrimaryKey(property.Reference);
                    yield return SF.ParseStatement($"{refPropertyName} = value?.{GetPropertyName(idProperty)};");
                }
                var objectProperty = GetReferenceProperty((JsonSchema)property.Parent, property.Name);
                if (objectProperty != null)
                {
                    var objectFieldName = GetFieldName(objectProperty);
                    yield return SF.ParseStatement($"if({objectFieldName}?.Id != value)");
                    yield return SF.ParseStatement("{");
                    yield return SF.ParseStatement($"{objectFieldName} = null;");
                    yield return SF.ParseStatement($"OnPropertyChanged(\"{GetPropertyName(objectProperty)}\");");
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

        private IEnumerable<FieldDeclarationSyntax> GenDefinitionClassField(JsonSchemaProperty property, JsonSchemaProperty idKey, List<RefField> refFields)
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
                    TypeName = GetTypeString(property.Item, false),
                };
                refFields.Add(refField);
                //var refTypePrimary = GetPrimaryKey(refTypeSchema);
                //var refTypePrimaryName = GetPropertyName(refTypePrimary);
                //var refTypePrimaryType = GetTypeString(refTypePrimary, true, "SelectableList");
                refField.KeyProperty = GetProperty(refField.TypeSchema, refkey);
                refField.KeyName = GetPropertyName(refField.KeyProperty);
                refField.KeyType = GetTypeString(refField.KeyProperty, true);

                refField.ValueProperty = GetReferenceProperty((JsonSchema)refField.KeyProperty.Parent, refField.KeyName);
                refField.ValueName = GetPropertyName(refField.ValueProperty);
                refField.ValueType = GetTypeString(refField.ValueProperty, false, null);
                refField.ValueFieldName = GetFieldName(refField.ValueProperty);

                refField.InvokerName = $"{refField.TypeName}.{refkey}Invoker<{refField.TypeName}>.Default";

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

                refField.FieldType = GetTypeString(property, property.IsNullableRaw ?? true, "ReferenceList");
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
                var type = GetTypeString(property, property.IsNullableRaw ?? true);
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
                               initializer: property.Default != null
                               ? SF.EqualsValueClause(GenFieldDefault(property, idKey))
                               : property.Type == JsonObjectType.Array
                               ? SF.EqualsValueClause(SF.ParseExpression($"new {type}()")) : null))));
            }
        }

        private ExpressionSyntax GenFieldDefault(JsonSchemaProperty property, JsonSchemaProperty idKey)
        {
            var text = property.Default.ToString();
            var type = GetTypeString(property, false);
            if (type == "bool")
                text = text.ToLowerInvariant();
            else if (type == "string")
                text = $"\"{text}\"";
            else if (property.ActualSchema != null && property.ActualSchema.IsEnumeration)
                text = $"{type}.{text}";
            return SF.ParseExpression(text);
        }

        private string GetArrayElementTypeString(JsonSchema schema)
        {
            return schema.Type == JsonObjectType.Array
                ? GetTypeString(schema.Item, false, "List")
                : null;
        }

        private string GetTypeString(JsonSchema schema, bool nullable, string listType = "SelectableList")
        {
            switch (schema.Type)
            {
                case JsonObjectType.Integer:
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
                        case "binary":
                            return "byte[]";
                        case "date":
                        case "date-time":
                            return "DateTime" + (nullable ? "?" : string.Empty);
                        default:
                            return "string";
                    }
                case JsonObjectType.Array:
                    return $"{listType}<{GetTypeString(schema.Item, false, listType)}>";
                case JsonObjectType.None:
                    if (schema.ActualTypeSchema != null)
                    {
                        if (schema.ActualTypeSchema.Type != JsonObjectType.None)
                            return GetTypeString(schema.ActualTypeSchema, nullable, listType);
                        else
                        { }
                    }
                    break;
                case JsonObjectType.Object:
                    if (schema.Id != null)
                    {
                        GetOrGenDefinion(schema.Id);
                        if (schema.IsEnumeration)
                        {
                            return GetDefinitionName(schema) + (nullable ? "?" : string.Empty);
                        }
                        else
                        {
                            return GetDefinitionName(schema);
                        }
                    }
                    break;
                case JsonObjectType.File:
                    return "Stream";
            }
            return "string";
        }

        private TypeSyntax GetTypeDeclaration(JsonSchema property, bool nullable, string listType)
        {
            return SF.ParseTypeName(GetTypeString(property, nullable, listType));
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
}
