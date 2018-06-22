using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DataWF.Web.Common
{

    public class DBControllerGenerator
    {
        private Dictionary<string, MetadataReference> references;
        private Dictionary<string, UsingDirectiveSyntax> usings;
        private List<MethodParametrInfo> parametersInfo;

        public DBControllerGenerator()
        {
            references = new Dictionary<string, MetadataReference>() {
                {"netstandard", MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location) },
                {"System", MetadataReference.CreateFromFile(typeof(Object).Assembly.Location) },
                {"System.Runtime", MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=0.0.0.0").Location) },
                {"System.Collections", MetadataReference.CreateFromFile(Assembly.Load("System.Collections, Version=0.0.0.0").Location) },
                {"System.List", MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location) },
                {"System.Linq", MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location) },
                {"Microsoft.AspNetCore.Mvc", MetadataReference.CreateFromFile(typeof(ControllerBase).Assembly.Location) },
                {"DataWF.Common", MetadataReference.CreateFromFile(typeof(Helper).Assembly.Location) },
                {"DataWF.Data", MetadataReference.CreateFromFile(typeof(DBTable).Assembly.Location) },
                {"DataWF.Web.Common", MetadataReference.CreateFromFile(typeof(DBController<>).Assembly.Location) }
            };
            //foreach (var module in Helper.ModuleInitializer)
            //{
            //    var assembly = module.GetType().Assembly;
            //    references.Add(assembly.GetName().Name, MetadataReference.CreateFromFile(assembly.Location));
            //}
            usings = new Dictionary<string, UsingDirectiveSyntax>(StringComparer.Ordinal) {
                { "DataWF.Common", CreateUsingDirective("DataWF.Common") },
                { "DataWF.Data", CreateUsingDirective("DataWF.Data") },
                { "DataWF.Web.Common", CreateUsingDirective("DataWF.Web.Common") },
                { "Microsoft.AspNetCore.Mvc", CreateUsingDirective("Microsoft.AspNetCore.Mvc") },
                { "System", CreateUsingDirective("System") },
                { "System.Collections.Generic", CreateUsingDirective("System.Collections.Generic") }
            };
        }

        //https://carlos.mendible.com/2017/03/02/create-a-class-with-net-core-and-roslyn/
        public Assembly GenerateRoslyn(DBSchema schema)
        {
            var name = schema.Name.ToInitcap('_');
            var files = new List<SyntaxTree>();

            foreach (var table in schema.Tables)
            {
                var itemType = table.GetType().GetGenericArguments().FirstOrDefault();
                var tableAttribute = DBTable.GetTableAttribute(itemType);
                var controllerType = typeof(DBController<>).MakeGenericType(itemType);
                string controllerClassName = $"{tableAttribute.ItemType.Name}Controller";
                var @class = SyntaxFactory.ClassDeclaration(
                    attributeLists: SyntaxFactory.List<AttributeListSyntax>(GetCalssAttributeList()),
                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                    identifier: SyntaxFactory.Identifier(controllerClassName),
                    typeParameterList: null,
                    baseList: SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"DBController<{itemType.Name}>")))),
                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    members: SyntaxFactory.List<MemberDeclarationSyntax>(GetClassMemebers(table))
                    );

                var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName($"DataWF.Web.{name}"))
                    .AddMembers(@class);
                var @unit = SyntaxFactory.CompilationUnit()
                                        .AddUsings(usings.Values.ToArray())
                                        .AddMembers(@namespace).NormalizeWhitespace();

                files.Add(@unit.SyntaxTree);
                var assemblyName = itemType.Assembly.GetName().Name;
                if (!references.ContainsKey(assemblyName))
                    references[assemblyName] = MetadataReference.CreateFromFile(itemType.Assembly.Location);
            }
            CSharpCompilation compilation = CSharpCompilation.Create($"{name}.dll", files, references.Values,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);
                if (!emitResult.Success)
                {
                    IEnumerable<Diagnostic> failures = emitResult.Diagnostics.Where(diagnostic =>
             diagnostic.IsWarningAsError ||
             diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    return Assembly.Load(dllStream.ToArray(), pdbStream.ToArray());
                }
            }
            return null;
        }

        public IEnumerable<MemberDeclarationSyntax> GetClassMemebers(DBTable table)
        {
            foreach (var typeInfo in table.ItemTypes.Values)
            {
                AddUsing(typeInfo.Type);
                foreach (var method in typeInfo.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (method.GetCustomAttribute<ControllerMethodAttribute>() != null
                        && (!method.IsVirtual || method.GetBaseDefinition() == null))
                    {
                        yield return GetMethod(method, table);
                    }
                }
            }
        }


        private IEnumerable<AttributeListSyntax> GetCalssAttributeList()
        {
            var attributeArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal("api/[controller]")));
            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                         SyntaxFactory.Attribute(
                         SyntaxFactory.IdentifierName("Route")).WithArgumentList(
                             SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(attributeArgument)))));
            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                         SyntaxFactory.Attribute(
                         SyntaxFactory.IdentifierName("ApiController"))));
        }

        //https://stackoverflow.com/questions/37710714/roslyn-add-new-method-to-an-existing-class
        private MethodDeclarationSyntax GetMethod(MethodInfo method, DBTable table)
        {
            AddUsing(method.DeclaringType);
            AddUsing(method.ReturnType);
            var returning = method.ReturnType == typeof(void) ? "void" : $"ActionResult<{TypeHelper.FormatCode(method.ReturnType)}>";

            return SyntaxFactory.MethodDeclaration(attributeLists: SyntaxFactory.List<AttributeListSyntax>(GetMethodAttributeList(method)),
                          modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                          returnType: returning == "void"
                          ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))
                          : SyntaxFactory.ParseTypeName(returning),
                          explicitInterfaceSpecifier: null,
                          identifier: SyntaxFactory.Identifier(method.Name),
                          typeParameterList: null,
                          parameterList: SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(GetParametersList(method))),
                          constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                          body: SyntaxFactory.Block(GetMethodBody(method)),
                          semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            // Annotate that this node should be formatted
            //.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private IEnumerable<StatementSyntax> GetMethodBody(MethodInfo method)
        {
            var returning = method.ReturnType == typeof(void) ? "void" : $"ActionResult<{TypeHelper.FormatCode(method.ReturnType)}>";
            if (!method.IsStatic)
            {
                yield return SyntaxFactory.ParseStatement($"var idValue = table.LoadById<{TypeHelper.FormatCode(method.DeclaringType)}>(id);");

                foreach (var parameter in parametersInfo)
                {
                    if (parameter.Table != null)
                    {
                        yield return SyntaxFactory.ParseStatement($"var {parameter.ValueName} = DBItem.GetTable<{TypeHelper.FormatCode(parameter.Info.ParameterType)}>().LoadById({parameter.Info.Name});");
                    }
                }
                var builder = new StringBuilder();
                if (method.ReturnType != typeof(void))
                {
                    builder.Append($"return new {returning}(");
                }
                builder.Append($" idValue.{method.Name}(");

                if (parametersInfo.Count > 0)
                {
                    foreach (var parameter in parametersInfo)
                    {
                        builder.Append($"{parameter.ValueName}, ");
                    }
                    builder.Length -= 2;
                }
                if (method.ReturnType != typeof(void))
                    builder.Append(")");
                builder.AppendLine(");");

                yield return SyntaxFactory.ParseStatement(builder.ToString());

            }
        }

        private IEnumerable<AttributeListSyntax> GetMethodAttributeList(MethodInfo method)
        {
            var parameters = method.Name + (method.IsStatic ? "" : "/{id:int}");
            foreach (var parameter in method.GetParameters())
            {
                parameters += $"/{{{parameter.Name}}}";
            }
            var attributeArgument = SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(parameters)));
            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                         SyntaxFactory.Attribute(
                         SyntaxFactory.IdentifierName("Route")).WithArgumentList(
                             SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(attributeArgument)))));
            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                         SyntaxFactory.Attribute(
                         SyntaxFactory.IdentifierName("HttpGet"))));
        }

        private IEnumerable<ParameterSyntax> GetParametersList(MethodInfo method)
        {
            yield return SyntaxFactory.Parameter(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                                                         modifiers: SyntaxFactory.TokenList(),
                                                         type: SyntaxFactory.ParseTypeName(typeof(int).Name),
                                                         identifier: SyntaxFactory.Identifier("id"),
                                                         @default: null);
            parametersInfo = new List<MethodParametrInfo>();

            foreach (var parameter in method.GetParameters())
            {
                var methodParameter = new MethodParametrInfo { Info = parameter };
                parametersInfo.Add(methodParameter);
                AddUsing(methodParameter.Info.ParameterType);
                yield return SyntaxFactory.Parameter(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                                                         modifiers: SyntaxFactory.TokenList(),
                                                         type: SyntaxFactory.ParseTypeName(methodParameter.Type.Name),
                                                         identifier: SyntaxFactory.Identifier(methodParameter.Info.Name),
                                                         @default: null);
            }
        }

        private void AddUsing(Type type)
        {
            AddUsing(type.Namespace);
        }

        private void AddUsing(string usingName)
        {
            if (!usings.TryGetValue(usingName, out var syntax))
            {
                usings.Add(usingName, CreateUsingDirective(usingName));
            }
        }

        //https://stackoverflow.com/a/36845547
        private UsingDirectiveSyntax CreateUsingDirective(string usingName)
        {
            NameSyntax qualifiedName = null;

            foreach (var identifier in usingName.Split('.'))
            {
                var name = SyntaxFactory.IdentifierName(identifier);

                if (qualifiedName != null)
                {
                    qualifiedName = SyntaxFactory.QualifiedName(qualifiedName, name);
                }
                else
                {
                    qualifiedName = name;
                }
            }

            return SyntaxFactory.UsingDirective(qualifiedName);
        }

        private void GetMethod(StringBuilder builder, MethodInfo method, DBTable table)
        {
            AddUsing(method.ReturnType);
            var mparams = method.GetParameters();
            var parameters = method.IsStatic ? "" : "/{id:int}";
            var returning = method.ReturnType == typeof(void) ? "void" : $"ActionResult<{TypeHelper.FormatCode(method.ReturnType)}>";
            parametersInfo = new List<MethodParametrInfo>();

            foreach (var parameter in mparams)
            {
                parametersInfo.Add(new MethodParametrInfo { Info = parameter });
                parameters += $"/{{{parameter.Name}}}";
            }
            //var methodsyntax = GetMethod(method);
            builder.AppendLine($"[Route(\"{method.Name}{parameters}\"), HttpGet()]");
            builder.Append($"public {(method.IsVirtual ? "virtual" : "")} {returning} {method.Name} (");
            if (!method.IsStatic)
            {
                builder.Append($"int id{(mparams.Length > 0 ? ", " : "")}");
            }
            if (mparams.Length > 0)
            {
                foreach (var parameter in parametersInfo)
                {
                    AddUsing(parameter.Type);
                    builder.Append($"{TypeHelper.FormatCode(parameter.Type)} {parameter.Info.Name}, ");
                }
                builder.Length -= 2;
            }
            builder.AppendLine(") {");
            if (!method.IsStatic)
            {
                AddUsing(method.DeclaringType);
                builder.AppendLine($"var idValue = table.LoadById<{TypeHelper.FormatCode(method.DeclaringType)}>(id);");

                foreach (var parameter in parametersInfo)
                {
                    if (parameter.Table != null)
                    {
                        AddUsing(parameter.Info.ParameterType);
                        builder.AppendLine($"var {parameter.ValueName} = DBItem.GetTable<{TypeHelper.FormatCode(parameter.Info.ParameterType)}>().LoadById({parameter.Info.Name});");
                    }
                }
                if (method.ReturnType != typeof(void))
                    builder.Append($@"return new {returning}(");
                builder.Append($" idValue.{method.Name}(");

                if (mparams.Length > 0)
                {
                    foreach (var parameter in parametersInfo)
                    {
                        builder.Append($"{parameter.ValueName}, ");
                    }
                    builder.Length -= 2;
                }
                if (method.ReturnType != typeof(void))
                    builder.Append(")");
                builder.AppendLine(");");

            }
            builder.AppendLine("}");
        }
    }

    public class MethodParametrInfo
    {
        private ParameterInfo info;

        public Type Type { get; private set; }
        public DBTable Table { get; private set; }
        public string ValueName { get; private set; }
        public ParameterInfo Info
        {
            get => info;
            set
            {
                info = value;
                Type = info.ParameterType;
                ValueName = info.Name;
                if (TypeHelper.IsBaseType(Type, typeof(DBItem)))
                {
                    Table = DBTable.GetTable(Type, null, false, true);
                    if (Table != null && Table.PrimaryKey != null)
                    {
                        Type = Table.PrimaryKey.DataType;
                        ValueName += "Value";
                    }
                }
            }
        }
    }

}