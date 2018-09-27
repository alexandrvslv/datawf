using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DataWF.Web.CodeGenerator
{

    public partial class ControllerGenerator
    {
        private Dictionary<string, MetadataReference> references;
        private Dictionary<string, UsingDirectiveSyntax> usings;
        private List<MethodParametrInfo> parametersInfo;
        private Dictionary<string, ClassDeclarationSyntax> trees = new Dictionary<string, ClassDeclarationSyntax>();
        public List<Assembly> Assemblies { get; private set; }
        public string Output { get; }
        public string Namespace { get; private set; }

        public ControllerGenerator(string paths, string output, string nameSpace)
            : this(paths.Split(new char[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries), output, nameSpace)
        { }

        public ControllerGenerator(IEnumerable<string> assemblies, string output, string nameSpace)
            : this(LoadAssemblies(assemblies), output, nameSpace)
        { }

        public ControllerGenerator(IEnumerable<Assembly> assemblies, string output, string nameSpace)
        {
            Assemblies = new List<Assembly>(assemblies);
            Output = string.IsNullOrEmpty(output) ? null : Path.GetFullPath(output);
            Namespace = nameSpace ?? "DataWF.Web.Controller";
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
            };
            //foreach (var module in Helper.ModuleInitializer)
            //{
            //    var assembly = module.GetType().Assembly;
            //    references.Add(assembly.GetName().Name, MetadataReference.CreateFromFile(assembly.Location));
            //}
            usings = new Dictionary<string, UsingDirectiveSyntax>(StringComparer.Ordinal) {
                { "DataWF.Common", SyntaxHelper.CreateUsingDirective("DataWF.Common") },
                { "DataWF.Data", SyntaxHelper.CreateUsingDirective("DataWF.Data") },
                { "DataWF.Web.Common", SyntaxHelper.CreateUsingDirective("DataWF.Web.Common") },
                { "Microsoft.AspNetCore.Mvc", SyntaxHelper.CreateUsingDirective("Microsoft.AspNetCore.Mvc") },
                { "Microsoft.AspNetCore.Authentication.JwtBearer", SyntaxHelper.CreateUsingDirective("Microsoft.AspNetCore.Authentication.JwtBearer") },
                { "Microsoft.AspNetCore.Authorization", SyntaxHelper.CreateUsingDirective("Microsoft.AspNetCore.Authorization") },
                { "System", SyntaxHelper.CreateUsingDirective("System") },
                { "System.Collections.Generic", SyntaxHelper.CreateUsingDirective("System.Collections.Generic") }
            };
        }

        public static IEnumerable<Assembly> LoadAssemblies(IEnumerable<string> assemblies)
        {
            foreach (var name in assemblies)
            {
                yield return Assembly.LoadFrom(Path.GetFullPath(name));
            }
        }

        public void Generate()
        {
            foreach (var assembly in Assemblies)
            {
                var assemblyName = assembly.GetName().Name;
                if (!references.ContainsKey(assemblyName))
                    references[assemblyName] = MetadataReference.CreateFromFile(assembly.Location);

                foreach (var itemType in assembly.GetExportedTypes())
                {
                    var tableAttribute = DBTable.GetTableAttribute(itemType);
                    if (tableAttribute != null)
                    {
                        var controller = GetOrGenerateController(tableAttribute, itemType);
                        //if (tableAttribute.ItemType != itemType)
                        //{
                        //    trees[tableAttribute.ItemType.Name] = controller.AddMembers(GetControllerMemebers(tableAttribute, itemType).ToArray());
                        //}
                    }
                }
            }
        }

        private ClassDeclarationSyntax GetOrGenerateBaseController(TableAttributeCache tableAttribute, Type itemType, out string controllerClassName)
        {
            string name = $"Base{(tableAttribute.FileKey != null ? "File" : "")}{itemType.Name}";
            controllerClassName = $"{name}Controller";

            if (!trees.TryGetValue(name, out var baseController))
            {
                var fileColumn = tableAttribute.Columns.FirstOrDefault(p => (p.Attribute.Keys & DBColumnKeys.File) == DBColumnKeys.File);
                var primaryKeyType = tableAttribute.PrimaryKey?.GetDataType() ?? typeof(int);
                var baseType = $"Base{(tableAttribute.FileKey != null ? "File" : "")}{(IsPrimaryType(itemType.BaseType) ? "" : itemType.BaseType.Name)}Controller<T, K>";
                baseController = SyntaxFactory.ClassDeclaration(
                    attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AbstractKeyword)),
                    identifier: SyntaxFactory.Identifier(controllerClassName),
                    typeParameterList: SyntaxFactory.TypeParameterList(
                        SyntaxFactory.SeparatedList(new[] {
                            SyntaxFactory.TypeParameter("T"),
                            SyntaxFactory.TypeParameter("K") })),
                    baseList: SyntaxFactory.BaseList(
                        SyntaxFactory.SeparatedList<BaseTypeSyntax>(new[] {
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(baseType)) })),
                    constraintClauses: SyntaxFactory.List(new[] { SyntaxFactory.TypeParameterConstraintClause(
                        name: SyntaxFactory.IdentifierName("T"),
                        constraints: SyntaxFactory.SeparatedList<TypeParameterConstraintSyntax>(new []{
                            SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName(itemType.Name)),
                             SyntaxFactory.TypeConstraint(SyntaxFactory.ParseTypeName("new()"))
                        }))
                    }),
                    members: SyntaxFactory.List(GetControllerMemebers(tableAttribute, itemType, true))
                    );

                trees[name] = baseController;
            }
            return baseController;
        }

        public bool IsPrimaryType(Type itemType)
        {
            return itemType == typeof(DBItem) || itemType == typeof(DBGroupItem);
        }

        //https://carlos.mendible.com/2017/03/02/create-a-class-with-net-core-and-roslyn/
        private ClassDeclarationSyntax GetOrGenerateController(TableAttributeCache tableAttribute, Type itemType)
        {
            var baseName = (string)null;
            var baseType = itemType;
            var primaryKeyType = tableAttribute.PrimaryKey?.GetDataType() ?? typeof(int);

            while (baseType != null && !IsPrimaryType(baseType))
            {
                if (baseType == tableAttribute.ItemType || baseType != itemType)
                {
                    GetOrGenerateBaseController(tableAttribute, baseType, out var baseClassName);
                    if (baseName == null)
                        baseName = $"{baseClassName}<{itemType.Name}, {primaryKeyType.Name}>";
                }
                baseType = baseType.BaseType;
            }

            if (!trees.TryGetValue(itemType.Name, out var controller))
            {
                var controllerClassName = $"{itemType.Name}Controller";
                controller = SyntaxFactory.ClassDeclaration(
                    attributeLists: SyntaxFactory.List(GetControllerAttributeList()),
                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                    identifier: SyntaxFactory.Identifier(controllerClassName),
                    typeParameterList: null,
                    baseList: SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(baseName)))),
                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    members: SyntaxFactory.List(GetControllerMemebers(tableAttribute, itemType, false))
                    );

                trees[itemType.Name] = controller;
            }
            return controller;
        }

        public Assembly Compile()
        {
            var compilation = CSharpCompilation.Create($"{Namespace}.dll", GetUnits(false), references.Values,
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

        public List<SyntaxTree> GetUnits(bool save)
        {
            var list = new List<SyntaxTree>();
            if (save)
            {
                Directory.CreateDirectory(Output);
            }
            var assembly = typeof(ControllerGenerator).Assembly;
            var baseName = assembly.GetName().Name + ".ControllerTemplate.";
            list.AddRange(SyntaxHelper.LoadResources(assembly, baseName, Namespace, save ? Output : null).Select(P => P.SyntaxTree));

            foreach (var entry in trees)
            {
                var unit = SyntaxHelper.GenUnit(entry.Value, Namespace, usings.Values);
                if (save)
                {
                    File.WriteAllText(Path.Combine(Output, $"{entry.Key}Controller.cs"), unit.ToFullString());
                }
                list.Add(unit.SyntaxTree);
            }
            return list;
        }

        public IEnumerable<MemberDeclarationSyntax> GetControllerMemebers(TableAttributeCache table, Type type, bool baseClass)
        {
            AddUsing(type);
            if (table.ItemType == type && !baseClass)
                yield break;
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttribute<ControllerMethodAttribute>() != null
                    && (!method.IsVirtual || method.GetBaseDefinition() == null))
                {
                    yield return GetControllerMethod(method, table, baseClass);
                }
            }
        }

        private IEnumerable<AttributeListSyntax> GetControllerAttributeList()
        {
            //[Authorize(JwtBearerDefaults.AuthenticationScheme)]
            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList(
                             SyntaxFactory.Attribute(
                                 SyntaxFactory.IdentifierName("Authorize")).WithArgumentList(
                                 SyntaxFactory.AttributeArgumentList(
                                     SyntaxFactory.SingletonSeparatedList(
                                         SyntaxFactory.AttributeArgument(
                                             SyntaxFactory.ParseExpression("AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme")))))));

            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList(
                             SyntaxFactory.Attribute(
                                 SyntaxFactory.IdentifierName("Route")).WithArgumentList(
                                 SyntaxFactory.AttributeArgumentList(
                                     SyntaxFactory.SingletonSeparatedList(
                                         SyntaxFactory.AttributeArgument(
                                             SyntaxFactory.LiteralExpression(
                                                 SyntaxKind.StringLiteralExpression,
                                                 SyntaxFactory.Literal("api/[controller]"))))))));
            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList(
                             SyntaxFactory.Attribute(
                                 SyntaxFactory.IdentifierName("ApiController"))));
        }

        //https://stackoverflow.com/questions/37710714/roslyn-add-new-method-to-an-existing-class
        private MethodDeclarationSyntax GetControllerMethod(MethodInfo method, TableAttributeCache table, bool baseClass)
        {
            AddUsing(method.DeclaringType);
            AddUsing(method.ReturnType);
            var returning = method.ReturnType == typeof(void) ? "void" : $"ActionResult<{TypeHelper.FormatCode(method.ReturnType)}>";

            return SyntaxFactory.MethodDeclaration(attributeLists: SyntaxFactory.List(GetControllerMethodAttributes(method)),
                          modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                          returnType: returning == "void"
                          ? SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))
                          : SyntaxFactory.ParseTypeName(returning),
                          explicitInterfaceSpecifier: null,
                          identifier: SyntaxFactory.Identifier(method.Name),
                          typeParameterList: null,
                          parameterList: SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(GetControllerMethodParameters(method, table))),
                          constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                          body: SyntaxFactory.Block(GetControllerMethodBody(method, baseClass)),
                          semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            // Annotate that this node should be formatted
            //.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private IEnumerable<StatementSyntax> GetControllerMethodBody(MethodInfo method, bool baseClass)
        {
            var returning = method.ReturnType == typeof(void) ? "void" : $"ActionResult<{TypeHelper.FormatCode(method.ReturnType)}>";
            if (!method.IsStatic)
            {
                yield return SyntaxFactory.ParseStatement($"var idValue = table.LoadById<{(baseClass ? "T" : TypeHelper.FormatCode(method.DeclaringType))}>(id);");
                yield return SyntaxFactory.ParseStatement("if (idValue == null)");
                if (method.ReturnType != typeof(void))
                {
                    yield return SyntaxFactory.ParseStatement("{ return NotFound(); }");
                }
                else
                {
                    yield return SyntaxFactory.ParseStatement("{ NotFound(); }");
                }

                var parametersBuilder = new StringBuilder();
                foreach (var parameter in parametersInfo)
                {
                    if (parameter.Table != null)
                    {
                        yield return SyntaxFactory.ParseStatement($"var {parameter.ValueName} = DBItem.GetTable<{TypeHelper.FormatCode(parameter.Info.ParameterType)}>().LoadById({parameter.Info.Name});");
                    }
                    parametersBuilder.Append($"{parameter.ValueName}, ");
                }
                if (parametersInfo.Count > 0)
                {
                    parametersBuilder.Length -= 2;
                }
                var builder = new StringBuilder();
                if (TypeHelper.IsBaseType(method.ReturnType, typeof(Stream)))
                {
                    yield return SyntaxFactory.ParseStatement("var fileName = idValue.GetValue<string>(fileNameColumn);");
                    yield return SyntaxFactory.ParseStatement("if (string.IsNullOrEmpty(fileName))");
                    yield return SyntaxFactory.ParseStatement("{ return new EmptyResult(); }");
                    builder.Append($"return File(idValue.{method.Name}({parametersBuilder}), System.Net.Mime.MediaTypeNames.Application.Octet, fileName);");
                }
                else
                {
                    if (method.ReturnType != typeof(void))
                    {
                        builder.Append($"return new {returning}(");
                    }
                    builder.Append($" idValue.{method.Name}({parametersBuilder}");
                    if (method.ReturnType != typeof(void))
                    {
                        builder.Append(")");
                    }

                    builder.AppendLine(");");
                }
                yield return SyntaxFactory.ParseStatement(builder.ToString());

            }
        }

        private IEnumerable<AttributeListSyntax> GetControllerMethodAttributes(MethodInfo method)
        {
            var parameters = method.Name + (method.IsStatic ? "" : "/{id}");
            foreach (var parameter in method.GetParameters())
            {
                parameters += $"/{{{parameter.Name}}}";
            }
            var attributeArgument = SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(parameters)));
            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList(
                             SyntaxFactory.Attribute(
                                 SyntaxFactory.IdentifierName("Route")).WithArgumentList(
                                 SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(attributeArgument)))));
            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList(
                             SyntaxFactory.Attribute(
                                 SyntaxFactory.IdentifierName("HttpGet"))));
        }

        private IEnumerable<ParameterSyntax> GetControllerMethodParameters(MethodInfo method, TableAttributeCache table)
        {
            yield return SyntaxFactory.Parameter(attributeLists: SyntaxFactory.List(GetParameterAttributes()),
                                                             modifiers: SyntaxFactory.TokenList(),
                                                             type: SyntaxFactory.ParseTypeName((table.PrimaryKey?.GetDataType() ?? typeof(int)).Name),
                                                             identifier: SyntaxFactory.Identifier("id"),
                                                             @default: null);
            parametersInfo = new List<MethodParametrInfo>();

            foreach (var parameter in method.GetParameters())
            {
                var methodParameter = new MethodParametrInfo { Info = parameter };
                parametersInfo.Add(methodParameter);
                AddUsing(methodParameter.Info.ParameterType);
                yield return SyntaxFactory.Parameter(attributeLists: SyntaxFactory.List(GetParameterAttributes()),
                                                         modifiers: SyntaxFactory.TokenList(),
                                                         type: SyntaxFactory.ParseTypeName(methodParameter.Type.Name),
                                                         identifier: SyntaxFactory.Identifier(methodParameter.Info.Name),
                                                         @default: null);
            }
        }

        private IEnumerable<AttributeListSyntax> GetParameterAttributes()
        {
            //yield break;
            yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList(
                         SyntaxFactory.Attribute(
                         SyntaxFactory.IdentifierName("FromRoute"))));
        }

        private void AddUsing(Type type)
        {
            AddUsing(type.Namespace);
        }

        private void AddUsing(string usingName)
        {
            if (!usings.TryGetValue(usingName, out var syntax))
            {
                usings.Add(usingName, SyntaxHelper.CreateUsingDirective(usingName));
            }
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
        public TableAttributeCache Table { get; private set; }
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
                    Table = DBTable.GetTableAttribute(Type);
                    var primaryKey = Table?.PrimaryKey;
                    if (primaryKey != null)
                    {
                        Type = primaryKey.GetDataType();
                        ValueName += "Value";
                    }
                }
            }
        }
    }

}