using DataWF.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using NJsonSchema;
using NSwag;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Web.Common
{
    public class ClientGenerator
    {
        private string Namespace = "DataWF.Web.Client";
        private Dictionary<string, CompilationUnitSyntax> cacheModels = new Dictionary<string, CompilationUnitSyntax>();
        private Dictionary<string, ClassDeclarationSyntax> cacheClients = new Dictionary<string, ClassDeclarationSyntax>();
        private List<UsingDirectiveSyntax> usings = new List<UsingDirectiveSyntax>();
        private SwaggerDocument document;
        private Uri uri;

        public async Task Generate(string url, string output)
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
            output = Path.GetFullPath(output);
            Directory.CreateDirectory(output);

            var assembly = typeof(ClientGenerator).Assembly;
            var baseName = assembly.GetName().Name + ".ClientTemplate.";
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (!name.StartsWith(baseName))
                    continue;
                var path = Path.Combine(output, name.Substring(baseName.Length));
                using (var stream = new FileStream(path, FileMode.OpenOrCreate))
                {
                    assembly.GetManifestResourceStream(name).CopyTo(stream);
                }
            }

            document = await SwaggerDocument.FromUrlAsync(url);
            uri = new Uri(url);
            foreach (var definition in document.Definitions)
            {
                definition.Value.Id = definition.Key;
            }

            var modelPath = Path.Combine(output, "Models");
            Directory.CreateDirectory(modelPath);
            foreach (var definition in document.Definitions)
            {
                var realisation = GetOrGenerateDefinion(definition.Key);
                File.WriteAllText(Path.Combine(modelPath, definition.Key + ".cs"), realisation.ToFullString());
            }

            foreach (var operation in document.Operations)
            {
                AddClientOperation(operation);
            }

            var clientPath = Path.Combine(output, "Clients");
            Directory.CreateDirectory(clientPath);
            foreach (var entry in cacheClients)
            {
                File.WriteAllText(Path.Combine(clientPath, entry.Key + "Client.cs"), GenUnit(entry.Value).ToFullString());
            }
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

            cacheClients[clientName] = clientSyntax.AddMembers(GenOperation(operationName, descriptor).ToArray());
        }

        private ClassDeclarationSyntax GenClient(string clientName)
        {
            if (!cacheModels.ContainsKey(clientName))
            {
                throw new InvalidOperationException($"Client <{clientName}> not found on Definitions");
            }
            var baseType = SyntaxFactory.ParseTypeName($"BaseClient<{clientName}>");

            return SyntaxFactory.ClassDeclaration(
                    attributeLists: SyntaxFactory.List(ClientAttributeList()),
                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                    identifier: SyntaxFactory.Identifier($"{clientName}Client"),
                    typeParameterList: null,
                    baseList: SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                        SyntaxFactory.SimpleBaseType(baseType))),
                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    members: SyntaxFactory.List<MemberDeclarationSyntax>(GenClientConstructor(clientName))
                    );
        }

        private IEnumerable<ConstructorDeclarationSyntax> GenClientConstructor(string clientName)
        {
            yield return SyntaxFactory.ConstructorDeclaration(
                attributeLists: SyntaxFactory.List(ClientAttributeList()),
                modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                identifier: SyntaxFactory.Identifier($"{clientName}Client"),
                parameterList: SyntaxFactory.ParameterList(),
                initializer: null,
                body: SyntaxFactory.Block(SyntaxFactory.ParseStatement($"BaseUrl = \"{uri.Scheme}://{uri.Authority}\";")));
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
                returnType = $"{GetTypeString(responce.Schema)}";
            }
            return returnType;
        }

        private IEnumerable<MemberDeclarationSyntax> GenOperation(string operationName, SwaggerOperationDescription descriptor)
        {
            var actualName = $"{operationName}Async";

            var returnType = GetReturningType(descriptor);
            returnType = returnType.Length > 0 ? $"Task<{returnType}>" : "Task";

            yield return SyntaxFactory.MethodDeclaration(
                attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                    returnType: SyntaxFactory.ParseTypeName(returnType),
                    explicitInterfaceSpecifier: null,
                    identifier: SyntaxFactory.Identifier(actualName),
                    typeParameterList: null,
                    parameterList: SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(GenOperationParameter(descriptor, false))),
                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    body: SyntaxFactory.Block(GenOperationWrapperBody(actualName, descriptor)),
                    semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            yield return SyntaxFactory.MethodDeclaration(
                attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.AsyncKeyword)),
                    returnType: SyntaxFactory.ParseTypeName(returnType),
                    explicitInterfaceSpecifier: null,
                    identifier: SyntaxFactory.Identifier(actualName),
                    typeParameterList: null,
                    parameterList: SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(GenOperationParameter(descriptor, true))),
                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    body: SyntaxFactory.Block(GenOperationBody(descriptor)),
                    semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private StatementSyntax GenOperationWrapperBody(string actualName, SwaggerOperationDescription descriptor)
        {
            var builder = new StringBuilder();
            builder.Append($"return {actualName}(");

            if (descriptor.Operation.Parameters.Any(p => p.Kind == SwaggerParameterKind.Body))
            {
                builder.Append("value, ");
            }
            foreach (var parameter in descriptor.Operation.Parameters)
            {
                if (parameter.Kind != SwaggerParameterKind.Body)
                {
                    builder.Append($"{parameter.Name}, ");
                }
            }
            builder.Append("CancellationToken.None);");
            return SyntaxFactory.ParseStatement(builder.ToString());
        }

        private StatementSyntax GenOperationBody(SwaggerOperationDescription descriptor)
        {
            var method = descriptor.Method.ToString().ToUpperInvariant();
            var path = descriptor.Path;
            var mediatype = "application/json";
            var returnType = GetReturningType(descriptor);
            var builder = new StringBuilder();
            builder.Append($"return await Request<{returnType}>(cancellationToken, \"{method}\", \"{path}\", \"{mediatype}\"");
            if (descriptor.Operation.Parameters.Any(p => p.Kind == SwaggerParameterKind.Body))
            {
                builder.Append(", value");
            }
            else
            {
                builder.Append(", null");
            }
            foreach (var parameter in descriptor.Operation.Parameters)
            {
                if (parameter.Kind != SwaggerParameterKind.Body)
                {
                    builder.Append($", {parameter.Name}");
                }
            }
            builder.Append(");");
            return SyntaxFactory.ParseStatement(builder.ToString());
        }

        private IEnumerable<ParameterSyntax> GenOperationParameter(SwaggerOperationDescription descriptor, bool cancelationToken)
        {
            foreach (var parameter in descriptor.Operation.Parameters)
            {
                yield return SyntaxFactory.Parameter(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                                                         modifiers: SyntaxFactory.TokenList(),
                                                         type: GetTypeDeclaration(parameter.ActualSchema ?? parameter),
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

        private CompilationUnitSyntax GenUnit(MemberDeclarationSyntax @class)
        {
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(Namespace))
                                             .AddMembers(@class);
            return SyntaxFactory.CompilationUnit(
                externs: SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                usings: SyntaxFactory.List(usings),
                attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                members: SyntaxFactory.List<MemberDeclarationSyntax>(new[] { @namespace }))
                                        .NormalizeWhitespace("    ", true);
        }

        private CompilationUnitSyntax GenDefinition(JsonSchema4 schema)
        {
            var @class = schema.IsEnumeration ? GenDefinitionEnum(schema) : GenDefinitionClass(schema);
            return GenUnit(@class);
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
            var baseType = SyntaxFactory.ParseTypeName($"INotifyPropertyChanged");

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
                        type: SyntaxFactory.ParseTypeName("PropertyChangedEventHandler"),
                        variables: SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>(new[] { SyntaxFactory.VariableDeclarator("PropertyChanged") })));

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
                    body: SyntaxFactory.Block(SyntaxFactory.ParseStatement($"PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));")),
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
            var typeDeclaration = GetTypeDeclaration(property);
            return SyntaxFactory.PropertyDeclaration(
                attributeLists: SyntaxFactory.List<AttributeListSyntax>(GenClassPropertyAttributes(property)),
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
            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList(
                         SyntaxFactory.Attribute(
                         SyntaxFactory.IdentifierName("JsonProperty")).WithArgumentList(
                             SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(
                                 SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression($"NullValueHandling = NullValueHandling.Ignore")))))));
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
            yield return SyntaxFactory.ParseStatement($"if(_{property.Name} == value) return;");
            yield return SyntaxFactory.ParseStatement($"_{property.Name} = value;");
            yield return SyntaxFactory.ParseStatement($"OnPropertyChanged();");
        }

        private static IEnumerable<StatementSyntax> GenPropertyGet(JsonProperty property)
        {
            yield return SyntaxFactory.ParseStatement($"return _{property.Name};");
        }

        private FieldDeclarationSyntax GenDefinitionClassField(JsonProperty property)
        {
            return SyntaxFactory.FieldDeclaration(SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)),
                SyntaxFactory.VariableDeclaration(GetTypeDeclaration(property))
                                 .AddVariables(SyntaxFactory.VariableDeclarator("_" + property.Name)));
        }

        private string GetTypeString(JsonSchema4 value)
        {
            switch (value.Type)
            {
                case JsonObjectType.Integer:
                    return Helper.ToInitcap(value.Format) + "?";
                case JsonObjectType.Boolean:
                    return "bool" + "?";
                case JsonObjectType.Number:
                    return value.Format + "?";
                case JsonObjectType.String:
                    switch (value.Format)
                    {
                        case "byte":
                        case "binary":
                            return "byte[]";
                        case "date":
                        case "date-time":
                            return "DateTime?";
                        default:
                            return "string";
                    }
                case JsonObjectType.Array:
                    return $"List<{GetTypeString(value.Item)}>";
                case JsonObjectType.None:
                    if (value.ActualTypeSchema != null)
                    {
                        if (value.ActualTypeSchema.Type != JsonObjectType.None)
                            return GetTypeString(value.ActualTypeSchema);
                        else
                        { }
                    }
                    break;
                case JsonObjectType.Object:
                    if (value.Id != null)
                    {
                        GetOrGenerateDefinion(value.Id);
                        if (value.IsEnumeration)
                            return value.Id + "?";
                        else
                            return value.Id;
                    }
                    break;
            }
            return "string";
        }

        private TypeSyntax GetTypeDeclaration(JsonSchema4 value)
        {
            return SyntaxFactory.ParseTypeName(GetTypeString(value));
        }

        private IEnumerable<AttributeListSyntax> DefinitionAttributeList()
        {
            yield break;
        }
    }
}