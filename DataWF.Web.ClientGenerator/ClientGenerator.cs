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
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DataWF.Web.ClientGenerator
{
    public class ClientGenerator
    {
        private readonly HashSet<string> VirtualOperations = new HashSet<string> { "GetAsync", "PutAsync", "PostAsync", "FindAsync", "DeleteAsync", "CopyAsync", "GenerateIdAsync" };
        private Dictionary<string, CompilationUnitSyntax> cacheModels = new Dictionary<string, CompilationUnitSyntax>();
        private Dictionary<string, ClassDeclarationSyntax> cacheClients = new Dictionary<string, ClassDeclarationSyntax>();
        private List<UsingDirectiveSyntax> usings = new List<UsingDirectiveSyntax>();
        private SwaggerDocument document;
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
                document = SwaggerDocument.FromUrlAsync(url.OriginalString).GetAwaiter().GetResult();
            else if (url.Scheme == "file")
                document = SwaggerDocument.FromFileAsync(url.LocalPath).GetAwaiter().GetResult();
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
                        SF.SimpleBaseType(SF.ParseTypeName("IClientProvider")))),
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

            yield return SyntaxHelper.GenProperty("string", "BaseUrl", true);
            yield return SyntaxHelper.GenProperty("AuthorizationInfo", "Authorization", true);

            yield return SF.PropertyDeclaration(
                    attributeLists: SF.List<AttributeListSyntax>(),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                    type: SF.ParseTypeName("IEnumerable<IClient>"),
                    explicitInterfaceSpecifier: null,
                    identifier: SF.Identifier("Clients"),
                    accessorList: SF.AccessorList(SF.List(new[] {
                        SF.AccessorDeclaration( SyntaxKind.GetAccessorDeclaration,  SF.Block(GenProviderClientsBody()))
                    })),
                    expressionBody: null,
                    initializer: null,
                    semicolonToken: SF.Token(SyntaxKind.None));

            foreach (var client in cacheClients.Keys)
            {
                yield return SyntaxHelper.GenProperty($"{client}Client", client, false);
            }

            yield return SF.MethodDeclaration(
               attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(new[] { SF.Token(SyntaxKind.PublicKeyword) }),
                   returnType: SF.ParseTypeName("ICRUDClient<T>"),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier("GetClient"),
                   typeParameterList: SF.TypeParameterList(SF.SingletonSeparatedList(SF.TypeParameter("T"))),
                   parameterList: SF.ParameterList(),
                   constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                   body: SF.Block(new[] { SF.ParseStatement("return Clients.OfType<ICRUDClient<T>>().FirstOrDefault();") }),
                   semicolonToken: SF.Token(SyntaxKind.SemicolonToken));

            yield return SF.MethodDeclaration(
               attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(new[] { SF.Token(SyntaxKind.PublicKeyword) }),
                   returnType: SF.ParseTypeName("ICRUDClient"),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier("GetClient"),
                   typeParameterList: null,
                   parameterList: SF.ParameterList(SF.SingletonSeparatedList(SF.Parameter(
                       attributeLists: SF.List<AttributeListSyntax>(),
                       modifiers: SF.TokenList(),
                       type: SF.ParseTypeName("Type"),
                       identifier: SF.Identifier("type"),
                       @default: null))),
                   constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                   body: SF.Block(new[] { SF.ParseStatement("return Clients.OfType<ICRUDClient>().FirstOrDefault(p=>p.ItemType == type);") }),
                   semicolonToken: SF.Token(SyntaxKind.SemicolonToken));

            yield return SF.MethodDeclaration(
               attributeLists: SF.List<AttributeListSyntax>(),
                   modifiers: SF.TokenList(new[] { SF.Token(SyntaxKind.PublicKeyword) }),
                   returnType: SF.ParseTypeName("ICRUDClient"),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier("GetClient"),
                   typeParameterList: null,
                   parameterList: SF.ParameterList(SF.SeparatedList(new[]{
                       SF.Parameter( attributeLists: SF.List<AttributeListSyntax>(), modifiers: SF.TokenList(), type: SF.ParseTypeName("Type"), identifier: SF.Identifier("type"), @default: null),
                       SF.Parameter( attributeLists: SF.List<AttributeListSyntax>(), modifiers: SF.TokenList(), type: SF.ParseTypeName("int"), identifier: SF.Identifier("typeId"), @default: null)
                   })),
                   constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                   body: SF.Block(new[] { SF.ParseStatement("return Clients.OfType<ICRUDClient>().FirstOrDefault(p => TypeHelper.IsBaseType(p.ItemType, type) && p.TypeId == typeId);") }),
                   semicolonToken: SF.Token(SyntaxKind.SemicolonToken));
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
                yield return SF.ParseStatement($"{client} = new {client}Client{{Provider = this}};");
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
                File.WriteAllText(Path.Combine(Output, "Provider.cs"), provider.ToFullString());
            }

            var modelPath = Path.Combine(Output, "Models");
            Directory.CreateDirectory(modelPath);
            foreach (var entry in cacheModels)
            {
                if (save)
                {
                    File.WriteAllText(Path.Combine(modelPath, entry.Key + ".cs"), entry.Value.ToFullString());
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
                    File.WriteAllText(Path.Combine(clientPath, entry.Key + "Client.cs"), unit.ToFullString());
                }
                list.Add(unit.SyntaxTree);
            }

            return list;
        }

        public virtual string GetClientName(SwaggerOperationDescription decriptor)
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

        public virtual string GetOperationName(SwaggerOperationDescription descriptor, out string clientName)
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

        private void AddClientOperation(SwaggerOperationDescription descriptor)
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
                        members: SF.List<MemberDeclarationSyntax>(GenClientConstructor(clientName, idKey, typeKey, typeId))
                        );
        }

        private string GetClientBaseType(string clientName, out JsonProperty idKey, out JsonProperty typeKey, out int typeId)
        {
            idKey = null;
            typeKey = null;
            typeId = 0;
            if (document.Definitions.TryGetValue(clientName, out var schema))
            {
                idKey = GetPrimaryKey(schema);
                typeKey = GetTypeKey(schema);
                typeId = GetTypeId(schema);
                return $"Client<{clientName}, {(idKey == null ? "int" : GetTypeString(idKey, false, "List"))}>";
            }
            return $"ClientBase";
        }

        private JsonProperty GetPrimaryKey(JsonSchema4 schema, bool inherit = true)
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

        private JsonProperty GetExtensionKey(JsonSchema4 schema, string name, bool inherit = true)
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

        private JsonProperty GetTypeKey(JsonSchema4 schema)
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

        private int GetTypeId(JsonSchema4 schema)
        {
            if (schema.ExtensionData != null && schema.ExtensionData.TryGetValue("x-type-id", out var id))
            {
                return (int)Helper.Parse(id, typeof(int));
            }

            return 0;
        }

        private IEnumerable<ConstructorDeclarationSyntax> GenClientConstructor(string clientName, JsonProperty idKey, JsonProperty typeKey, int typeId)
        {
            var idName = idKey == null ? null : GetPropertyName(idKey);
            var typeName = typeKey == null ? null : GetPropertyName(typeKey);
            var initialize = idKey == null ? null : SF.ConstructorInitializer(
                    SyntaxKind.BaseConstructorInitializer,
                    SF.ArgumentList(
                        SF.SeparatedList(new[] {
                            SF.Argument(SF.ParseExpression($"new Invoker<{clientName},{GetTypeString(idKey, true, "List")}>(nameof({clientName}.{idName}), p=>p.{idName}, (p,v)=>p.{idName}=v)")),
                            SF.Argument(SF.ParseExpression($"new Invoker<{clientName},{GetTypeString(typeKey, true, "List")}>(nameof({clientName}.{typeName}), p=>p.{typeName}, (p,v)=>p.{typeName}=v)")),
                            SF.Argument(SF.ParseExpression($"{typeId}")),
                        })));
            yield return SF.ConstructorDeclaration(
                attributeLists: SF.List(ClientAttributeList()),
                modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                identifier: SF.Identifier($"{clientName}Client"),
                parameterList: SF.ParameterList(),
                initializer: initialize,
                body: SF.Block());
        }

        private IEnumerable<AttributeListSyntax> ClientAttributeList()
        {
            yield break;
        }

        private string GetReturningType(SwaggerOperationDescription descriptor)
        {
            var returnType = "string";
            if (descriptor.Operation.Responses.TryGetValue("200", out var responce) && responce.Schema != null)
            {
                returnType = $"{GetTypeString(responce.Schema, false, "List")}";
            }
            return returnType;
        }

        private string GetReturningTypeCheck(SwaggerOperationDescription descriptor, string operationName)
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

        private IEnumerable<MemberDeclarationSyntax> GenOperation(SwaggerOperationDescription descriptor)
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

        //private StatementSyntax GenOperationWrapperBody(string actualName, SwaggerOperationDescription descriptor)
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

        private IEnumerable<StatementSyntax> GenOperationBody(string actualName, SwaggerOperationDescription descriptor, bool isOverride)
        {
            var method = descriptor.Method.ToString().ToUpperInvariant();
            var path = descriptor.Path;
            var responceSchema = (JsonSchema4)null;
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
            var bodyParameter = descriptor.Operation.Parameters.FirstOrDefault(p => p.Kind != SwaggerParameterKind.Path);
            if (bodyParameter == null)
            {
                requestBuilder.Append(", null");
            }
            else
            {
                requestBuilder.Append($", {bodyParameter.Name}");
            }
            foreach (var parameter in descriptor.Operation.Parameters.Where(p => p.Kind == SwaggerParameterKind.Path))
            {
                requestBuilder.Append($", {parameter.Name}");
            }
            requestBuilder.Append(").ConfigureAwait(false);");

            yield return SF.ParseStatement($"return {requestBuilder.ToString()}");
        }

        private IEnumerable<ParameterSyntax> GenOperationParameter(SwaggerOperationDescription descriptor)
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


        private CompilationUnitSyntax GenDefinition(JsonSchema4 schema)
        {
            var @class = schema.IsEnumeration ? GenDefinitionEnum(schema) : GenDefinitionClass(schema);
            return SyntaxHelper.GenUnit(@class, Namespace, usings);
        }

        private MemberDeclarationSyntax GenDefinitionEnum(JsonSchema4 schema)
        {
            return SF.EnumDeclaration(
                    attributeLists: SF.List(GenDefinitionEnumAttributes(schema)),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                    identifier: SF.Identifier(GetDefinitionName(schema)),
                    baseList: null,
                    members: SF.SeparatedList(GenDefinitionEnumMemebers(schema))
                    );
        }

        private IEnumerable<AttributeListSyntax> GenDefinitionEnumAttributes(JsonSchema4 schema)
        {
            if (schema.ExtensionData?.TryGetValue("x-flags", out var flags) ?? false)
            {
                yield return SyntaxHelper.GenAttribute("Flags");
            }
        }

        private IEnumerable<EnumMemberDeclarationSyntax> GenDefinitionEnumMemebers(JsonSchema4 schema)
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
                var sitem = item.ToString();
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

                //SF.EqualsValueClause(
                //    SF.Token(SyntaxKind.EqualsToken),
                //    SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(i++)))


            }
        }

        private IEnumerable<AttributeListSyntax> GenDefinitionEnumMemberAttribute(object item)
        {
            //[System.Runtime.Serialization.EnumMember(Value = "Empty")]
            yield return SyntaxHelper.GenAttribute("EnumMember", $"Value = \"{item.ToString()}\"");
        }

        private MemberDeclarationSyntax GenDefinitionClass(JsonSchema4 schema)
        {
            return SF.ClassDeclaration(
                    attributeLists: SF.List(DefinitionAttributeList()),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)),
                    identifier: SF.Identifier(GetDefinitionName(schema)),
                    typeParameterList: null,
                    baseList: SF.BaseList(SF.SeparatedList(GenDefinitionClassBases(schema))),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    members: SF.List(GenDefinitionClassMemebers(schema)));
        }

        private IEnumerable<BaseTypeSyntax> GenDefinitionClassBases(JsonSchema4 schema)
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

        private IEnumerable<MemberDeclarationSyntax> GenDefinitionClassMemebers(JsonSchema4 schema)
        {
            var idKey = GetPrimaryKey(schema);
            var typeKey = GetTypeKey(schema);
            var typeId = GetTypeId(schema);
            if (typeId != 0)
            {
                yield return SF.ConstructorDeclaration(
                          attributeLists: SF.List<AttributeListSyntax>(),
                          modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                          identifier: SF.Identifier(GetDefinitionName(schema)),
                          parameterList: SF.ParameterList(),
                          initializer: null,
                          body: SF.Block(new[] {
                              SF.ParseStatement($"{GetPropertyName(GetTypeKey(schema))} = {typeId};")
                          }));
            }
            foreach (var property in schema.Properties)
            {
                //    property.Value.Id = property.Key;
                yield return GenDefinitionClassField(property.Value, idKey);
            }

            foreach (var property in schema.Properties)
            {
                yield return GenDefinitionClassProperty(property.Value, idKey, typeKey);
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

            //if (schema.InheritedSchema == null)
            //{
            //    yield return SF.EventFieldDeclaration(
            //        attributeLists: SF.List<AttributeListSyntax>(),
            //        modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
            //        declaration: SF.VariableDeclaration(
            //            type: SF.ParseTypeName(nameof(PropertyChangedEventHandler)),
            //            variables: SF.SeparatedList(new[] { SF.VariableDeclarator(nameof(INotifyPropertyChanged.PropertyChanged)) })));
            //    yield return SyntaxHelper.GenProperty(nameof(INotifyListPropertyChanged), nameof(IContainerNotifyPropertyChanged.Container), true)
            //        .WithAttributeLists(SF.List(new[] { SF.AttributeList(SF.SingletonSeparatedList(SF.Attribute(SF.IdentifierName("JsonIgnore")))) }));
            //    yield return SyntaxHelper.GenProperty("bool?", nameof(ISynchronized.IsSynchronized), true)
            //        .WithAttributeLists(SF.List(new[] { SF.AttributeList(SF.SingletonSeparatedList(SF.Attribute(SF.IdentifierName("JsonIgnore")))) }));
            //    yield return SyntaxHelper.GenProperty("IDictionary<string, object>", nameof(ISynchronized.Changes), false)
            //        .WithAttributeLists(SF.List(new[] { SF.AttributeList(SF.SingletonSeparatedList(SF.Attribute(SF.IdentifierName("JsonIgnore")))) }));
            //    yield return SF.MethodDeclaration(
            //        attributeLists: SF.List<AttributeListSyntax>(),
            //        modifiers: SF.TokenList(
            //            SF.Token(SyntaxKind.ProtectedKeyword),
            //            SF.Token(SyntaxKind.VirtualKeyword)),
            //        returnType: SF.PredefinedType(SF.Token(SyntaxKind.VoidKeyword)),
            //        explicitInterfaceSpecifier: null,
            //        identifier: SF.Identifier("OnPropertyChanged"),
            //        typeParameterList: null,
            //        parameterList: SF.ParameterList(SF.SeparatedList(GenPropertyChangedParameter())),
            //        constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
            //        body: SF.Block(new[] {
            //            SF.ParseStatement($"if({nameof(ISynchronized.IsSynchronized)} != null)"),
            //            SF.ParseStatement("{"),
            //            SF.ParseStatement($"{nameof(ISynchronized.IsSynchronized)} = false;"),

            //            SF.ParseStatement("}"),
            //            SF.ParseStatement($"var arg = new PropertyChangedEventArgs(propertyName);"),
            //            SF.ParseStatement($"Container?.OnItemPropertyChanged(this, arg);"),
            //            SF.ParseStatement($"PropertyChanged?.Invoke(this, arg);") }),
            //        semicolonToken: SF.Token(SyntaxKind.SemicolonToken));
            //}
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

        private PropertyDeclarationSyntax GenDefinitionClassProperty(JsonProperty property, JsonProperty idKey, JsonProperty typeKey)
        {
            var typeDeclaration = GetTypeDeclaration(property, property.IsNullableRaw ?? true, "SelectableList");
            return SF.PropertyDeclaration(
                attributeLists: SF.List(GenDefinitionClassPropertyAttributes(property, idKey, typeKey)),
                modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                type: typeDeclaration,
                explicitInterfaceSpecifier: null,
                identifier: SF.Identifier(GetPropertyName(property)),
                accessorList: SF.AccessorList(SF.List(GenDefinitionClassPropertyAccessors(property))),
                expressionBody: null,
                initializer: null,
                semicolonToken: SF.Token(SyntaxKind.None)
               );
        }

        private IEnumerable<AttributeListSyntax> GenDefinitionClassPropertyAttributes(JsonProperty property, JsonProperty idKey, JsonProperty typeKey)
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


            if (property.IsRequired && property != idKey && property != typeKey)
            {
                yield return SyntaxHelper.GenAttribute("Required", $"ErrorMessage = \"{GetPropertyName(property)} is required\"");
            }

            if (property.MaxLength != null)
            {
                yield return SyntaxHelper.GenAttribute("MaxLength",
                    $"{property.MaxLength}, ErrorMessage = \"{GetPropertyName(property)} only max {property.MaxLength} letters allowed.\"");
            }
        }



        private IEnumerable<AccessorDeclarationSyntax> GenDefinitionClassPropertyAccessors(JsonProperty property)
        {
            yield return SF.AccessorDeclaration(
                kind: SyntaxKind.GetAccessorDeclaration,
                body: SF.Block(
                    GenDefintionClassPropertyGet(property)
                ));
            yield return SF.AccessorDeclaration(
                kind: SyntaxKind.SetAccessorDeclaration,
                body: SF.Block(
                    GenDefinitionClassPropertySet(property)
                ));
        }

        private IEnumerable<StatementSyntax> GenDefintionClassPropertyGet(JsonProperty property)
        {
            var fieldName = GetFieldName(property);
            if (property.ExtensionData != null && property.ExtensionData.TryGetValue("x-id", out var idProperty))
            {
                var idFiledName = GetFieldName((string)idProperty);
                yield return SF.ParseStatement($"if({fieldName} == null && {idFiledName} != null){{");
                //yield return SF.ParseStatement($"var client = ({GetTypeString(property, false, "List")}Client)ClientProvider.Default.GetClient<{GetTypeString(property, false, "List")}>();");
                yield return SF.ParseStatement($"{fieldName} = ClientProvider.Default.{GetTypeString(property, false, "List")}.Get({idFiledName}.Value);");
                //yield return SF.ParseStatement($"{fieldName} = ClientProvider.Default.{GetTypeString(property, false, "List")}.Select({idFiledName}.Value);");
                yield return SF.ParseStatement("}");
            }
            yield return SF.ParseStatement($"return {fieldName};");
        }

        private IEnumerable<StatementSyntax> GenDefinitionClassPropertySet(JsonProperty property)
        {
            yield return SF.ParseStatement($"if({GetFieldName(property)} == value) return;");
            yield return SF.ParseStatement($"OnPropertyChanging();");
            yield return SF.ParseStatement($"{GetFieldName(property)} = value;");
            var refPropertyName = (object)null;
            if (property.ExtensionData != null && property.ExtensionData.TryGetValue("x-id", out refPropertyName))
            {
                var idProperty = GetPrimaryKey(property.Reference);
                yield return SF.ParseStatement($"{refPropertyName} = value?.{GetPropertyName(idProperty)};");
            }
            var objectProperty = GetExtensionKey((JsonSchema4)property.Parent, property.Name);
            if (objectProperty != null)
            {
                var objectFieldName = GetFieldName(objectProperty);
                yield return SF.ParseStatement($"if({objectFieldName} != null && {objectFieldName}.Id != value)");
                yield return SF.ParseStatement("{");
                yield return SF.ParseStatement($"{objectFieldName} = null;");
                yield return SF.ParseStatement("}");
                yield return SF.ParseStatement($"OnPropertyChanged(true, \"{ GetPropertyName(objectProperty)}\");");
            }
            if (refPropertyName == null)
            {
                yield return SF.ParseStatement($"OnPropertyChanged(true);");
            }
        }

        private string GetPropertyName(JsonProperty property)
        {
            return GetDefinitionName(property.Name);
        }

        private string GetDefinitionName(JsonSchema4 schema)
        {
            return GetDefinitionName(schema.Id);
        }

        private string GetDefinitionName(string name)
        {
            return string.Concat(char.ToUpperInvariant(name[0]).ToString(), name.Substring(1));
        }

        private string GetFieldName(JsonProperty property)
        {
            return GetFieldName(property.Name);
        }

        private string GetFieldName(string property)
        {
            return string.Concat("_", char.ToLowerInvariant(property[0]).ToString(), property.Substring(1));
        }

        private FieldDeclarationSyntax GenDefinitionClassField(JsonProperty property, JsonProperty idKey)
        {
            var type = GetTypeString(property, property.IsNullableRaw ?? true, "SelectableList");
            var field = SF.FieldDeclaration(attributeLists: SF.List<AttributeListSyntax>(),
                modifiers: SF.TokenList(SF.Token(SyntaxKind.ProtectedKeyword)),
               declaration: SF.VariableDeclaration(
                   type: SF.ParseTypeName(type),
                   variables: SF.SingletonSeparatedList(
                       SF.VariableDeclarator(
                           identifier: SF.ParseToken(GetFieldName(property)),
                           argumentList: null,
                           initializer: property.Default != null
                           ? SF.EqualsValueClause(GenFieldDefault(property, idKey))
                           : property.Type == JsonObjectType.Array
                           ? SF.EqualsValueClause(SF.ParseExpression($"new {type}()")) : null))));

            return field;
        }

        private ExpressionSyntax GenFieldDefault(JsonProperty property, JsonProperty idKey)
        {
            var text = property.Default.ToString();
            var type = GetTypeString(property, false, "SelectableList");
            if (type == "bool")
                text = text.ToLowerInvariant();
            else if (type == "string")
                text = $"\"{text}\"";
            else if (property.ActualSchema != null && property.ActualSchema.IsEnumeration)
                text = $"{type}.{text}";
            return SF.ParseExpression(text);
        }

        private string GetTypeString(JsonSchema4 value, bool nullable, string listType)
        {
            switch (value.Type)
            {
                case JsonObjectType.Integer:
                    if (value.Format == "int64")
                    {
                        return "long" + (nullable ? "?" : string.Empty);
                    }
                    return "int" + (nullable ? "?" : string.Empty);
                case JsonObjectType.Boolean:
                    return "bool" + (nullable ? "?" : string.Empty);
                case JsonObjectType.Number:
                    if (value.IsEnumeration)
                    {
                        goto case JsonObjectType.Object;
                    }
                    if (string.IsNullOrEmpty(value.Format))
                    {
                        return "decimal" + (nullable ? "?" : string.Empty);
                    }
                    return value.Format + (nullable ? "?" : string.Empty);
                case JsonObjectType.String:
                    if (value.IsEnumeration)
                    {
                        goto case JsonObjectType.Object;
                    }
                    switch (value.Format)
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
                    return $"{listType}<{GetTypeString(value.Item, false, listType)}>";
                case JsonObjectType.None:
                    if (value.ActualTypeSchema != null)
                    {
                        if (value.ActualTypeSchema.Type != JsonObjectType.None)
                            return GetTypeString(value.ActualTypeSchema, nullable, listType);
                        else
                        { }
                    }
                    break;
                case JsonObjectType.Object:
                    if (value.Id != null)
                    {
                        GetOrGenDefinion(value.Id);
                        if (value.IsEnumeration)
                        {
                            return GetDefinitionName(value) + (nullable ? "?" : string.Empty);
                        }
                        else
                        {
                            return GetDefinitionName(value);
                        }
                    }
                    break;
                case JsonObjectType.File:
                    return "Stream";
            }
            return "string";
        }

        private TypeSyntax GetTypeDeclaration(JsonSchema4 value, bool nullable, string listType)
        {
            return SF.ParseTypeName(GetTypeString(value, nullable, listType));
        }

        private IEnumerable<AttributeListSyntax> DefinitionAttributeList()
        {
            yield break;
        }
    }
}