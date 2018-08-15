using DataWF.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema;
using NSwag;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DataWF.Web.CodeGenerator
{
    public class ClientGenerator
    {
        private readonly List<string> AbstractOperations = new List<string> { "GetAsync", "PutAsync", "PostAsync", "FindAsync", "DeleteAsync" };
        private Dictionary<string, CompilationUnitSyntax> cacheModels = new Dictionary<string, CompilationUnitSyntax>();
        private Dictionary<string, ClassDeclarationSyntax> cacheClients = new Dictionary<string, ClassDeclarationSyntax>();
        private List<UsingDirectiveSyntax> usings = new List<UsingDirectiveSyntax>();
        private SwaggerDocument document;
        private CompilationUnitSyntax provider;

        public ClientGenerator(string url, string output, string nameSpace = "DataWF.Web.Client")
        {
            Namespace = nameSpace;
            Output = string.IsNullOrEmpty(output) ? null : Path.GetFullPath(output);
            Url = new Uri(url);
        }

        private Uri Url { get; }

        public string Output { get; }

        private string Namespace { get; }

        public void Generate()
        {
            usings = new List<UsingDirectiveSyntax>() {
                SyntaxHelper.CreateUsingDirective("DataWF.Common") ,
                SyntaxHelper.CreateUsingDirective("System") ,
                SyntaxHelper.CreateUsingDirective("System.Collections.Generic") ,
                SyntaxHelper.CreateUsingDirective("System.ComponentModel") ,
                SyntaxHelper.CreateUsingDirective("System.Linq") ,
                SyntaxHelper.CreateUsingDirective("System.Runtime.Serialization") ,
                SyntaxHelper.CreateUsingDirective("System.Runtime.CompilerServices") ,
                SyntaxHelper.CreateUsingDirective("System.Threading") ,
                SyntaxHelper.CreateUsingDirective("System.Threading.Tasks") ,
                SyntaxHelper.CreateUsingDirective("System.Net.Http") ,
                SyntaxHelper.CreateUsingDirective("System.Net.Http.Headers") ,
                SyntaxHelper.CreateUsingDirective("Newtonsoft.Json")
            };

            document = SwaggerDocument.FromUrlAsync(Url.OriginalString).GetAwaiter().GetResult();
            foreach (var definition in document.Definitions)
            {
                definition.Value.Id = definition.Key;
            }
            foreach (var definition in document.Definitions)
            {
                GetOrGenerateDefinion(definition.Key);
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
            yield return SF.ParseStatement($"BaseUrl = \"{Url.Scheme}://{Url.Authority}\";");
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
            return name.Length == 0 ? descriptor.Method.ToString() : name.ToString();
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
                        modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
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

        private JsonProperty GetPrimaryKey(JsonSchema4 schema)
        {
            if (schema.ExtensionData != null && schema.ExtensionData.TryGetValue("x-id", out var propertyName))
            {
                return schema.Properties[propertyName.ToString()];
            }

            foreach (var baseClass in schema.AllInheritedSchemas)
                if (baseClass.ExtensionData != null && baseClass.ExtensionData.TryGetValue("x-id", out propertyName))
                {
                    return baseClass.Properties[propertyName.ToString()];
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
                            SF.Argument(SF.ParseExpression($"new Invoker<{clientName},{GetTypeString(idKey, true, "List")}>(nameof({clientName}.{idName}), p=>p.{idName})")),
                            SF.Argument(SF.ParseExpression($"new Invoker<{clientName},{GetTypeString(typeKey, true, "List")}>(nameof({clientName}.{typeName}), p=>p.{typeName})")),
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

        private IEnumerable<MemberDeclarationSyntax> GenOperation(SwaggerOperationDescription descriptor)
        {
            var operationName = GetOperationName(descriptor, out var clientName);
            var actualName = $"{operationName}Async";
            var baseType = GetClientBaseType(clientName, out var id, out var typeKey, out var typeId);
            var returnType = GetReturningType(descriptor);
            returnType = returnType.Length > 0 ? $"Task<{returnType}>" : "Task";

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
                        baseType != "ClientBase" && AbstractOperations.Contains(actualName)
                        ? new[] { SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.OverrideKeyword), SF.Token(SyntaxKind.AsyncKeyword) }
                        : new[] { SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.AsyncKeyword) }),
                    returnType: SF.ParseTypeName(returnType),
                    explicitInterfaceSpecifier: null,
                    identifier: SF.Identifier(actualName),
                    typeParameterList: null,
                    parameterList: SF.ParameterList(SF.SeparatedList(GenOperationParameter(descriptor, true))),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    body: SF.Block(GenOperationBody(descriptor)),
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
        //    builder.Append("CancellationToken.None);");
        //    return SF.ParseStatement(builder.ToString());
        //}

        private IEnumerable<StatementSyntax> GenOperationBody(SwaggerOperationDescription descriptor)
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

            var builder = new StringBuilder();

            builder.Append($"return await Request");
            if (responceSchema?.Type == JsonObjectType.Array)
                builder.Append("Array");
            builder.Append($"<{returnType}");
            if (responceSchema?.Type == JsonObjectType.Array)
                builder.Append($", {GetTypeString(responceSchema.Item, false, "List")}");
            builder.Append($">(cancellationToken, \"{method}\", \"{path}\", \"{mediatype}\"");
            var bodyParameter = descriptor.Operation.Parameters.FirstOrDefault(p => p.Kind != SwaggerParameterKind.Path);
            if (bodyParameter == null)
            {
                builder.Append(", null");
            }
            else
            {
                builder.Append($", {bodyParameter.Name}");
            }
            foreach (var parameter in descriptor.Operation.Parameters.Where(p => p.Kind == SwaggerParameterKind.Path))
            {
                builder.Append($", {parameter.Name}");
            }
            builder.Append(").ConfigureAwait(false);");
            yield return SF.ParseStatement(builder.ToString());
        }

        private IEnumerable<ParameterSyntax> GenOperationParameter(SwaggerOperationDescription descriptor, bool cancelationToken)
        {
            foreach (var parameter in descriptor.Operation.Parameters)
            {
                yield return SF.Parameter(attributeLists: SF.List<AttributeListSyntax>(),
                                                         modifiers: SF.TokenList(),
                                                         type: GetTypeDeclaration(parameter, false, "List"),
                                                         identifier: SF.Identifier(parameter.Name),
                                                         @default: null);
            }
            if (cancelationToken)
            {
                yield return SF.Parameter(attributeLists: SF.List<AttributeListSyntax>(),
                                                            modifiers: SF.TokenList(),
                                                            type: SF.ParseTypeName("CancellationToken"),
                                                            identifier: SF.Identifier("cancellationToken"),
                                                            @default: null);
            }
        }

        private CompilationUnitSyntax GetOrGenerateDefinion(string key)
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
                    attributeLists: SF.List(DefinitionAttributeList()),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                    identifier: SF.Identifier(GetDefinitionName(schema)),
                    baseList: null,
                    members: SF.SeparatedList(GetDefinitionEnumMemebers(schema))
                    );
        }

        private IEnumerable<EnumMemberDeclarationSyntax> GetDefinitionEnumMemebers(JsonSchema4 schema)
        {
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
                        equalsValue: null);
                //SF.EqualsValueClause(
                //    SF.Token(SyntaxKind.EqualsToken),
                //    SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(i++)))


            }
        }

        private IEnumerable<AttributeListSyntax> GenDefinitionEnumMemberAttribute(object item)
        {
            //[System.Runtime.Serialization.EnumMember(Value = "Empty")]
            yield return SF.AttributeList(
                         SF.SingletonSeparatedList(
                         SF.Attribute(
                         SF.IdentifierName("EnumMember")).WithArgumentList(
                             SF.AttributeArgumentList(SF.SingletonSeparatedList(
                                 SF.AttributeArgument(SF.ParseExpression($"Value = \"{item.ToString()}\"")))))));
        }

        private MemberDeclarationSyntax GenDefinitionClass(JsonSchema4 schema)
        {
            var baseType = SF.ParseTypeName(nameof(IContainerNotifyPropertyChanged));

            if (schema.InheritedSchema != null)
            {
                GetOrGenerateDefinion(schema.InheritedSchema.Id);
                baseType = SF.ParseTypeName(GetDefinitionName(schema.InheritedSchema));
            }
            return SF.ClassDeclaration(
                    attributeLists: SF.List(DefinitionAttributeList()),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)),
                    identifier: SF.Identifier(GetDefinitionName(schema)),
                    typeParameterList: null,
                    baseList: SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(
                        SF.SimpleBaseType(baseType))),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    members: SF.List(GenDefinitionClassMemebers(schema)));
        }

        private IEnumerable<MemberDeclarationSyntax> GenDefinitionClassMemebers(JsonSchema4 schema)
        {
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
                yield return GenDefinitionClassField(property.Value);
            }

            foreach (var property in schema.Properties)
            {
                yield return GenDefinitionClassProperty(property.Value);
            }

            if (schema.InheritedSchema == null)
            {
                yield return SF.EventFieldDeclaration(
                    attributeLists: SF.List<AttributeListSyntax>(),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                    declaration: SF.VariableDeclaration(
                        type: SF.ParseTypeName(nameof(PropertyChangedEventHandler)),
                        variables: SF.SeparatedList(new[] { SF.VariableDeclarator(nameof(INotifyPropertyChanged.PropertyChanged)) })));
                yield return SyntaxHelper.GenProperty(nameof(INotifyListPropertyChanged), nameof(IContainerNotifyPropertyChanged.Container), true)
                    .WithAttributeLists(SF.List(new[]{
                    SF.AttributeList(
                        SF.SingletonSeparatedList(
                            SF.Attribute(
                                SF.IdentifierName("JsonIgnore")))) }));
                yield return SF.MethodDeclaration(
                    attributeLists: SF.List<AttributeListSyntax>(),
                    modifiers: SF.TokenList(
                        SF.Token(SyntaxKind.ProtectedKeyword),
                        SF.Token(SyntaxKind.VirtualKeyword)),
                    returnType: SF.PredefinedType(SF.Token(SyntaxKind.VoidKeyword)),
                    explicitInterfaceSpecifier: null,
                    identifier: SF.Identifier("OnPropertyChanged"),
                    typeParameterList: null,
                    parameterList: SF.ParameterList(SF.SeparatedList(GenPropertyChangedParameter())),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    body: SF.Block(new[] {
                        SF.ParseStatement($"Container?.OnPropertyChanged(this, propertyName);"),
                        SF.ParseStatement($"PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));") }),
                    semicolonToken: SF.Token(SyntaxKind.SemicolonToken));
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

        private PropertyDeclarationSyntax GenDefinitionClassProperty(JsonProperty property)
        {
            var typeDeclaration = GetTypeDeclaration(property, true, "SelectableList");
            return SF.PropertyDeclaration(
                attributeLists: SF.List(GenClassPropertyAttributes(property)),
                modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                type: typeDeclaration,
                explicitInterfaceSpecifier: null,
                identifier: SF.Identifier(GetPropertyName(property)),
                accessorList: SF.AccessorList(SF.List(GenPropertyAccessors(property))),
                expressionBody: null,
                initializer: null,
                semicolonToken: SF.Token(SyntaxKind.None)
               );
        }

        private IEnumerable<AttributeListSyntax> GenClassPropertyAttributes(JsonProperty property)
        {
            if (property.IsReadOnly
                || (property.ExtensionData != null && property.ExtensionData.TryGetValue("readOnly", out var isReadOnly) && (bool)isReadOnly)
                || property.Type == JsonObjectType.Object && !property.IsEnumeration
                || (property.Type == JsonObjectType.None && property.ActualTypeSchema.Type == JsonObjectType.Object && !property.ActualTypeSchema.IsEnumeration))

            {
                yield return SF.AttributeList(
                             SF.SingletonSeparatedList(
                                 SF.Attribute(
                                     SF.IdentifierName("JsonIgnore"))));
            }

            if (property.IsArray || property.Default != null)
            {
                yield return SF.AttributeList(
                             SF.SingletonSeparatedList(
                                 SF.Attribute(
                                     SF.IdentifierName("JsonProperty")).WithArgumentList(
                                     SF.AttributeArgumentList(SF.SingletonSeparatedList(
                                         SF.AttributeArgument(SF.ParseExpression($"NullValueHandling = NullValueHandling.Ignore")))))));
            }

        }

        private IEnumerable<AccessorDeclarationSyntax> GenPropertyAccessors(JsonProperty property)
        {
            yield return SF.AccessorDeclaration(
                kind: SyntaxKind.GetAccessorDeclaration,
                body: SF.Block(
                    GenPropertyGet(property)
                ));
            yield return SF.AccessorDeclaration(
                kind: SyntaxKind.SetAccessorDeclaration,
                body: SF.Block(
                    GenPropertySet(property)
                ));
        }

        private IEnumerable<StatementSyntax> GenPropertySet(JsonProperty property)
        {
            yield return SF.ParseStatement($"if({GetFieldName(property)} == value) return;");
            yield return SF.ParseStatement($"{GetFieldName(property)} = value;");
            yield return SF.ParseStatement($"OnPropertyChanged();");

            if (property.ExtensionData != null && property.ExtensionData.TryGetValue("x-id", out var idProperty))
            {
                yield return SF.ParseStatement($"{idProperty} = value?.Id;");
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

        private IEnumerable<StatementSyntax> GenPropertyGet(JsonProperty property)
        {
            var fieldName = GetFieldName(property);
            if (property.ExtensionData != null && property.ExtensionData.TryGetValue("x-id", out var idProperty))
            {
                var idFiledName = GetFieldName((string)idProperty);
                yield return SF.ParseStatement($"if({fieldName} == null && {idFiledName} != null){{");
                yield return SF.ParseStatement($"var client = ({GetTypeString(property, false, "List")}Client)ClientProvider.Default.GetClient<{GetTypeString(property, false, "List")}>();");
                yield return SF.ParseStatement($"{fieldName} = client.Get({idFiledName}.Value);");
                yield return SF.ParseStatement("}");
            }
            yield return SF.ParseStatement($"return {fieldName};");
        }

        private FieldDeclarationSyntax GenDefinitionClassField(JsonProperty property)
        {
            return SF.FieldDeclaration(attributeLists: SF.List<AttributeListSyntax>(),
                modifiers: SF.TokenList(SF.Token(SyntaxKind.PrivateKeyword)),
               declaration: SF.VariableDeclaration(
                   type: GetTypeDeclaration(property, true, "SelectableList"),
                   variables: SF.SingletonSeparatedList(
                       SF.VariableDeclarator(
                           identifier: SF.ParseToken(GetFieldName(property)),
                           argumentList: null,
                           initializer: property.Default != null
                           ? SF.EqualsValueClause(GenFieldDefault(property))
                           : null))));
        }

        private ExpressionSyntax GenFieldDefault(JsonProperty property)
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
                        GetOrGenerateDefinion(value.Id);
                        if (value.IsEnumeration)
                            return GetDefinitionName(value) + (nullable ? "?" : string.Empty);
                        else
                            return GetDefinitionName(value);
                    }
                    break;
                case JsonObjectType.File:
                    return "string";
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