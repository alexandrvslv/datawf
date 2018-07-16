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

namespace DataWF.Web.CodeGenerator
{
    public class ClientGenerator
    {
        private readonly List<string> AbstractOperations = new List<string> { "GetAsync", "PutAsync", "PostAsync", "FindAsync", "DeleteAsync" };
        private string Namespace = "DataWF.Web.Client";
        private Dictionary<string, CompilationUnitSyntax> cacheModels = new Dictionary<string, CompilationUnitSyntax>();
        private Dictionary<string, ClassDeclarationSyntax> cacheClients = new Dictionary<string, ClassDeclarationSyntax>();
        private List<UsingDirectiveSyntax> usings = new List<UsingDirectiveSyntax>();
        private SwaggerDocument document;
        private Uri uri;
        private CompilationUnitSyntax provider;

        public ClientGenerator(string url, string output)
        {
            Output = string.IsNullOrEmpty(output) ? null : Path.GetFullPath(output);
            uri = new Uri(url);
        }

        public string Output { get; }


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


            document = SwaggerDocument.FromUrlAsync(uri.OriginalString).GetAwaiter().GetResult();
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
            return SyntaxFactory.ClassDeclaration(
                    attributeLists: SyntaxFactory.List(ClientAttributeList()),
                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                    identifier: SyntaxFactory.Identifier($"ClientProvider"),
                    typeParameterList: null,
                    baseList: SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IClientProvider")))),
                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    members: SyntaxFactory.List(GenProviderMemebers())
                    );
        }

        private IEnumerable<MemberDeclarationSyntax> GenProviderMemebers()
        {
            yield return SyntaxFactory.ConstructorDeclaration(
                           attributeLists: SyntaxFactory.List(ClientAttributeList()),
                           modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                           identifier: SyntaxFactory.Identifier($"ClientProvider"),
                           parameterList: SyntaxFactory.ParameterList(),
                           initializer: null,
                           body: SyntaxFactory.Block(GenProviderConstructorBody()));

            yield return SyntaxHelper.GenProperty("string", "BaseUrl", true);
            yield return SyntaxHelper.GenProperty("AuthorizationInfo", "Authorization", true);

            yield return SyntaxFactory.PropertyDeclaration(
                    attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                    type: SyntaxFactory.ParseTypeName("IEnumerable<IClient>"),
                    explicitInterfaceSpecifier: null,
                    identifier: SyntaxFactory.Identifier("Clients"),
                    accessorList: SyntaxFactory.AccessorList(SyntaxFactory.List(new[] {
                        SyntaxFactory.AccessorDeclaration( SyntaxKind.GetAccessorDeclaration,  SyntaxFactory.Block(GenProviderClientsBody()))
                    })),
                    expressionBody: null,
                    initializer: null,
                    semicolonToken: SyntaxFactory.Token(SyntaxKind.None));

            foreach (var client in cacheClients.Keys)
            {
                yield return SyntaxHelper.GenProperty($"{client}Client", client, false);
            }
        }

        private IEnumerable<StatementSyntax> GenProviderClientsBody()
        {
            foreach (var client in cacheClients.Keys)
            {
                yield return SyntaxFactory.ParseStatement($"yield return {client};");
            }
        }

        private IEnumerable<StatementSyntax> GenProviderConstructorBody()
        {
            //SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression())
            yield return SyntaxFactory.ParseStatement($"BaseUrl = \"{uri.Scheme}://{uri.Authority}\";");
            foreach (var client in cacheClients.Keys)
            {
                yield return SyntaxFactory.ParseStatement($"{client} = new {client}Client{{Provider = this}};");
            }
        }

        public List<SyntaxTree> GetUnits(bool save)
        {
            var list = new List<SyntaxTree>();
            var assembly = typeof(ClientGenerator).Assembly;
            var baseName = assembly.GetName().Name + ".ClientTemplate.";
            list.AddRange(SyntaxHelper.LoadResources(assembly, baseName, save ? Output : null).Select(p => p.SyntaxTree));

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

        private string GetClientBaseType(string clientName, out JsonProperty id)
        {
            if (document.Definitions.TryGetValue(clientName, out var schema))
            {
                id = GetPrimaryKey(schema);
                return $"Client<{clientName}, {(id == null ? "int" : GetTypeString(id, false))}>";
            }
            else
            {
                id = null;
                return $"ClientBase";
            }
        }

        private ClassDeclarationSyntax GenClient(string clientName)
        {
            var baseType = SyntaxFactory.ParseTypeName(GetClientBaseType(clientName, out var id));

            return SyntaxFactory.ClassDeclaration(
                        attributeLists: SyntaxFactory.List(ClientAttributeList()),
                        modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                        identifier: SyntaxFactory.Identifier($"{clientName}Client"),
                        typeParameterList: null,
                        baseList: SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                            SyntaxFactory.SimpleBaseType(baseType))),
                        constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                        members: SyntaxFactory.List<MemberDeclarationSyntax>(GenClientConstructor(clientName, id))
                        );
        }

        private JsonProperty GetPrimaryKey(JsonSchema4 schema)
        {
            if (schema.ExtensionData != null && schema.ExtensionData.TryGetValue("x-id", out var propertyName))
            {
                return schema.Properties[propertyName.ToString()];
            }
            else if (schema.AllOf != null)
            {
                return GetPrimaryKey(schema.AllOf.FirstOrDefault());
            }
            return null;
        }

        private IEnumerable<ConstructorDeclarationSyntax> GenClientConstructor(string clientName, JsonProperty id)
        {
            var initialize = id == null ? null : SyntaxFactory.ConstructorInitializer(
                    SyntaxKind.BaseConstructorInitializer,
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[] {
                            SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"\"{id.Name}\"")) })));
            yield return SyntaxFactory.ConstructorDeclaration(
                attributeLists: SyntaxFactory.List(ClientAttributeList()),
                modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                identifier: SyntaxFactory.Identifier($"{clientName}Client"),
                parameterList: SyntaxFactory.ParameterList(),
                initializer: initialize,
                body: SyntaxFactory.Block());
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
                returnType = $"{GetTypeString(responce.Schema, false)}";
            }
            return returnType;
        }

        private IEnumerable<MemberDeclarationSyntax> GenOperation(SwaggerOperationDescription descriptor)
        {
            var operationName = GetOperationName(descriptor, out var clientName);
            var actualName = $"{operationName}Async";
            var baseType = GetClientBaseType(clientName, out var id);
            var returnType = GetReturningType(descriptor);
            returnType = returnType.Length > 0 ? $"Task<{returnType}>" : "Task";

            //yield return SyntaxFactory.MethodDeclaration(
            //    attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
            //        modifiers: SyntaxFactory.TokenList(
            //            baseType != "ClientBase" && AbstractOperations.Contains(actualName)
            //            ? new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword) }
            //            : new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }),
            //        returnType: SyntaxFactory.ParseTypeName(returnType),
            //        explicitInterfaceSpecifier: null,
            //        identifier: SyntaxFactory.Identifier(actualName),
            //        typeParameterList: null,
            //        parameterList: SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(GenOperationParameter(descriptor, false))),
            //        constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
            //        body: SyntaxFactory.Block(GenOperationWrapperBody(actualName, descriptor)),
            //        semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            yield return SyntaxFactory.MethodDeclaration(
                attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(
                        baseType != "ClientBase" && AbstractOperations.Contains(actualName)
                        ? new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword) }
                        : new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword) }),
                    returnType: SyntaxFactory.ParseTypeName(returnType),
                    explicitInterfaceSpecifier: null,
                    identifier: SyntaxFactory.Identifier(actualName),
                    typeParameterList: null,
                    parameterList: SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(GenOperationParameter(descriptor, true))),
                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    body: SyntaxFactory.Block(GenOperationBody(descriptor)),
                    semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
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
        //    return SyntaxFactory.ParseStatement(builder.ToString());
        //}

        private IEnumerable<StatementSyntax> GenOperationBody(SwaggerOperationDescription descriptor)
        {
            var method = descriptor.Method.ToString().ToUpperInvariant();
            var path = descriptor.Path;
            var mediatype = "application/json";
            var returnType = GetReturningType(descriptor);
            var builder = new StringBuilder();

            builder.Append($"return await Request<{returnType}>(cancellationToken, \"{method}\", \"{path}\", \"{mediatype}\"");
            var bodyParameter = descriptor.Operation.Parameters.FirstOrDefault(p => p.Kind == SwaggerParameterKind.Body);
            if (bodyParameter == null)
            {
                builder.Append(", null");
            }
            foreach (var parameter in descriptor.Operation.Parameters)
            {
                builder.Append($", {parameter.Name}");
            }
            builder.Append(");");
            yield return SyntaxFactory.ParseStatement(builder.ToString());
        }

        private IEnumerable<ParameterSyntax> GenOperationParameter(SwaggerOperationDescription descriptor, bool cancelationToken)
        {
            foreach (var parameter in descriptor.Operation.Parameters)
            {
                yield return SyntaxFactory.Parameter(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                                                         modifiers: SyntaxFactory.TokenList(),
                                                         type: GetTypeDeclaration(parameter, false),
                                                         identifier: SyntaxFactory.Identifier(parameter.Name),
                                                         @default: null);
            }
            if (cancelationToken)
            {
                yield return SyntaxFactory.Parameter(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                                                            modifiers: SyntaxFactory.TokenList(),
                                                            type: SyntaxFactory.ParseTypeName("CancellationToken"),
                                                            identifier: SyntaxFactory.Identifier("cancellationToken"),
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
            return SyntaxFactory.EnumDeclaration(
                    attributeLists: SyntaxFactory.List(DefinitionAttributeList()),
                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                    identifier: SyntaxFactory.Identifier(schema.Id),
                    baseList: null,
                    members: SyntaxFactory.SeparatedList(GetDefinitionEnumMemebers(schema))
                    );
        }

        private IEnumerable<EnumMemberDeclarationSyntax> GetDefinitionEnumMemebers(JsonSchema4 schema)
        {
            int i = 0;
            foreach (var item in schema.Enumeration)
            {
                yield return SyntaxFactory.EnumMemberDeclaration(attributeLists: SyntaxFactory.List(GenDefinitionEnumMemberAttribute(item)),
                        identifier: SyntaxFactory.Identifier(item.ToString()),
                        equalsValue: SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.Token(SyntaxKind.EqualsToken),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i++))));

            }
        }

        private IEnumerable<AttributeListSyntax> GenDefinitionEnumMemberAttribute(object item)
        {
            //[System.Runtime.Serialization.EnumMember(Value = "Empty")]
            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList(
                         SyntaxFactory.Attribute(
                         SyntaxFactory.IdentifierName("EnumMember")).WithArgumentList(
                             SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(
                                 SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression($"Value = \"{item.ToString()}\"")))))));
        }

        private MemberDeclarationSyntax GenDefinitionClass(JsonSchema4 schema)
        {
            var baseType = SyntaxFactory.ParseTypeName(nameof(IContainerNotifyPropertyChanged));

            if (schema.InheritedSchema != null)
            {
                GetOrGenerateDefinion(schema.InheritedSchema.Id);
                baseType = SyntaxFactory.ParseTypeName(schema.InheritedSchema.Id);
            }
            return SyntaxFactory.ClassDeclaration(
                    attributeLists: SyntaxFactory.List(DefinitionAttributeList()),
                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                    identifier: SyntaxFactory.Identifier(schema.Id),
                    typeParameterList: null,
                    baseList: SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                        SyntaxFactory.SimpleBaseType(baseType))),
                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    members: SyntaxFactory.List(GenDefinitionClassMemebers(schema)));
        }

        private IEnumerable<MemberDeclarationSyntax> GenDefinitionClassMemebers(JsonSchema4 schema)
        {
            foreach (var property in schema.Properties)
            {
                property.Value.Id = property.Key;
                yield return GenDefinitionClassField(property.Value);
            }

            foreach (var property in schema.Properties)
            {
                yield return GenDefinitionClassProperty(property.Value);
            }

            if (schema.InheritedSchema == null)
            {
                yield return SyntaxFactory.EventFieldDeclaration(
                    attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                    declaration: SyntaxFactory.VariableDeclaration(
                        type: SyntaxFactory.ParseTypeName(nameof(PropertyChangedEventHandler)),
                        variables: SyntaxFactory.SeparatedList(new[] { SyntaxFactory.VariableDeclarator(nameof(INotifyPropertyChanged.PropertyChanged)) })));
                yield return SyntaxHelper.GenProperty(nameof(INotifyListPropertyChanged), nameof(IContainerNotifyPropertyChanged.Container), true)
                    .WithAttributeLists(SyntaxFactory.List(new[]{
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(
                                SyntaxFactory.IdentifierName("JsonIgnore")))) }));
                yield return SyntaxFactory.MethodDeclaration(
                    attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                        SyntaxFactory.Token(SyntaxKind.VirtualKeyword)),
                    returnType: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    explicitInterfaceSpecifier: null,
                    identifier: SyntaxFactory.Identifier("OnPropertyChanged"),
                    typeParameterList: null,
                    parameterList: SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(GenPropertyChangedParameter())),
                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    body: SyntaxFactory.Block(new[] {
                        SyntaxFactory.ParseStatement($"Container?.OnPropertyChanged(this, propertyName);"),
                        SyntaxFactory.ParseStatement($"PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));") }),
                    semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }
        }

        private IEnumerable<ParameterSyntax> GenPropertyChangedParameter()
        {
            var @default = SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.Token(SyntaxKind.EqualsToken),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
            yield return SyntaxFactory.Parameter(
                attributeLists: SyntaxFactory.List(new[]{
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(
                                SyntaxFactory.IdentifierName("CallerMemberName")))) }),
                modifiers: SyntaxFactory.TokenList(),
                type: SyntaxFactory.ParseTypeName("string"),
                identifier: SyntaxFactory.Identifier("propertyName"),
                @default: @default);
        }

        private PropertyDeclarationSyntax GenDefinitionClassProperty(JsonProperty property)
        {
            var typeDeclaration = GetTypeDeclaration(property, true);
            return SyntaxFactory.PropertyDeclaration(
                attributeLists: SyntaxFactory.List(GenClassPropertyAttributes(property)),
                modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                type: typeDeclaration,
                explicitInterfaceSpecifier: null,
                identifier: SyntaxFactory.Identifier(property.Id),
                accessorList: SyntaxFactory.AccessorList(SyntaxFactory.List(GenPropertyAccessors(property))),
                expressionBody: null,
                initializer: null,
                semicolonToken: SyntaxFactory.Token(SyntaxKind.None)
               );
        }

        private IEnumerable<AttributeListSyntax> GenClassPropertyAttributes(JsonProperty property)
        {
            if (property.Type == JsonObjectType.None ||
                property.IsReadOnly)
            {
                yield return SyntaxFactory.AttributeList(
                             SyntaxFactory.SingletonSeparatedList(
                             SyntaxFactory.Attribute(
                             SyntaxFactory.IdentifierName("JsonProperty")).WithArgumentList(
                                 SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(
                                     SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression($"NullValueHandling = NullValueHandling.Ignore")))))));
            }
        }

        private IEnumerable<AccessorDeclarationSyntax> GenPropertyAccessors(JsonProperty property)
        {
            yield return SyntaxFactory.AccessorDeclaration(
                kind: SyntaxKind.GetAccessorDeclaration,
                body: SyntaxFactory.Block(
                    GenPropertyGet(property)
                ));
            yield return SyntaxFactory.AccessorDeclaration(
                kind: SyntaxKind.SetAccessorDeclaration,
                body: SyntaxFactory.Block(
                    GenPropertySet(property)
                ));
        }

        private static IEnumerable<StatementSyntax> GenPropertySet(JsonProperty property)
        {
            yield return SyntaxFactory.ParseStatement($"if({GetFieldName(property)} == value) return;");
            yield return SyntaxFactory.ParseStatement($"{GetFieldName(property)} = value;");
            yield return SyntaxFactory.ParseStatement($"OnPropertyChanged();");
        }

        private static string GetFieldName(JsonProperty property)
        {
            return string.Concat("_", char.ToLowerInvariant(property.Name[0]).ToString(), property.Name.Substring(1));
        }

        private static IEnumerable<StatementSyntax> GenPropertyGet(JsonProperty property)
        {
            yield return SyntaxFactory.ParseStatement($"return {GetFieldName(property)};");
        }

        private FieldDeclarationSyntax GenDefinitionClassField(JsonProperty property)
        {
            return SyntaxFactory.FieldDeclaration(SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)),
                SyntaxFactory.VariableDeclaration(GetTypeDeclaration(property, true))
                                 .AddVariables(SyntaxFactory.VariableDeclarator(GetFieldName(property))));
        }

        private string GetTypeString(JsonSchema4 value, bool nullable)
        {
            switch (value.Type)
            {
                case JsonObjectType.Integer:
                    return Helper.ToInitcap(value.Format) + (nullable ? "?" : string.Empty);
                case JsonObjectType.Boolean:
                    return "bool" + (nullable ? "?" : string.Empty);
                case JsonObjectType.Number:
                    return value.Format + (nullable ? "?" : string.Empty);
                case JsonObjectType.String:
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
                    return $"List<{GetTypeString(value.Item, false)}>";
                case JsonObjectType.None:
                    if (value.ActualTypeSchema != null)
                    {
                        if (value.ActualTypeSchema.Type != JsonObjectType.None)
                            return GetTypeString(value.ActualTypeSchema, nullable);
                        else
                        { }
                    }
                    break;
                case JsonObjectType.Object:
                    if (value.Id != null)
                    {
                        GetOrGenerateDefinion(value.Id);
                        if (value.IsEnumeration)
                            return value.Id + (nullable ? "?" : string.Empty);
                        else
                            return value.Id;
                    }
                    break;
            }
            return "string";
        }

        private TypeSyntax GetTypeDeclaration(JsonSchema4 value, bool nullable)
        {
            return SyntaxFactory.ParseTypeName(GetTypeString(value, nullable));
        }

        private IEnumerable<AttributeListSyntax> DefinitionAttributeList()
        {
            yield break;
        }
    }
}