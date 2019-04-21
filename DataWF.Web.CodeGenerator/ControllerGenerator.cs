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
using System.Threading.Tasks;

namespace DataWF.Web.CodeGenerator
{

    public partial class ControllerGenerator
    {
        private const string prStream = "uploaded";
        private const string prUser = "CurrentUser";
        private const string prTransaction = "transaction";
        private Dictionary<string, MetadataReference> references;
        private Dictionary<string, ClassDeclarationSyntax> trees = new Dictionary<string, ClassDeclarationSyntax>();
        private Dictionary<string, Dictionary<string, UsingDirectiveSyntax>> treeUsings = new Dictionary<string, Dictionary<string, UsingDirectiveSyntax>>();
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

            var usings = new Dictionary<string, UsingDirectiveSyntax>(StringComparer.Ordinal) {
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
                        var usings = new Dictionary<string, UsingDirectiveSyntax>(StringComparer.Ordinal) {
                            { "DataWF.Common", SyntaxHelper.CreateUsingDirective("DataWF.Common") },
                            { "DataWF.Data", SyntaxHelper.CreateUsingDirective("DataWF.Data") },
                            { "DataWF.Web.Common", SyntaxHelper.CreateUsingDirective("DataWF.Web.Common") },
                            { "Microsoft.AspNetCore.Mvc", SyntaxHelper.CreateUsingDirective("Microsoft.AspNetCore.Mvc") },
                            { "Microsoft.AspNetCore.Authentication.JwtBearer", SyntaxHelper.CreateUsingDirective("Microsoft.AspNetCore.Authentication.JwtBearer") },
                            { "Microsoft.AspNetCore.Authorization", SyntaxHelper.CreateUsingDirective("Microsoft.AspNetCore.Authorization") },
                            { "System", SyntaxHelper.CreateUsingDirective("System") },
                            { "System.Collections.Generic", SyntaxHelper.CreateUsingDirective("System.Collections.Generic") }
                        };
                        var controller = GetOrGenerateController(tableAttribute, itemType, usings);
                        //if (tableAttribute.ItemType != itemType)
                        //{
                        //    trees[tableAttribute.ItemType.Name] = controller.AddMembers(GetControllerMemebers(tableAttribute, itemType).ToArray());
                        //}
                    }
                }
            }
        }

        private ClassDeclarationSyntax GetOrGenerateBaseController(TableAttributeCache tableAttribute, Type itemType, Dictionary<string, UsingDirectiveSyntax> usings, out string controllerClassName)
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
                    members: SyntaxFactory.List(GetControllerMemebers(tableAttribute, itemType, usings))
                    );

                trees[name] = baseController;
                treeUsings[name] = usings;
            }
            return baseController;
        }

        public bool IsPrimaryType(Type itemType)
        {
            return itemType == typeof(DBItem) || itemType == typeof(DBGroupItem);
        }

        //https://carlos.mendible.com/2017/03/02/create-a-class-with-net-core-and-roslyn/
        private ClassDeclarationSyntax GetOrGenerateController(TableAttributeCache tableAttribute, Type itemType, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            var primaryKeyType = tableAttribute.PrimaryKey?.GetDataType() ?? typeof(int);
            var baseName = $"Base{(tableAttribute.FileKey != null ? "File" : "")}Controller<{itemType.Name}, {primaryKeyType.Name}>";
            var baseType = itemType;

            //while (baseType != null && !IsPrimaryType(baseType))
            //{
            //    if (baseType == tableAttribute.ItemType || baseType != itemType)
            //    {
            //        GetOrGenerateBaseController(tableAttribute, baseType, usings, out var baseClassName);
            //        if (baseName == null)
            //            baseName = $"{baseClassName}<{itemType.Name}, {primaryKeyType.Name}>";
            //    }
            //    baseType = baseType.BaseType;
            //}

            if (!trees.TryGetValue(itemType.Name, out var controller))
            {
                var controllerClassName = $"{itemType.Name}Controller";
                controller = SyntaxFactory.ClassDeclaration(
                    attributeLists: SyntaxFactory.List(GetControllerAttributeList()),
                    modifiers: SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword)),
                    identifier: SyntaxFactory.Identifier(controllerClassName),
                    typeParameterList: null,
                    baseList: SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(baseName)))),
                    constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                    members: SyntaxFactory.List(GetControllerMemebers(tableAttribute, itemType, usings))
                    );

                trees[itemType.Name] = controller;
                treeUsings[itemType.Name] = usings;
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
                var usings = treeUsings[entry.Key];
                var unit = SyntaxHelper.GenUnit(entry.Value, Namespace, usings.Values);
                if (save)
                {
                    File.WriteAllText(Path.Combine(Output, $"{entry.Key}Controller.cs"), unit.ToFullString());
                }
                list.Add(unit.SyntaxTree);
            }
            return list;
        }

        public IEnumerable<MemberDeclarationSyntax> GetControllerMemebers(TableAttributeCache table, Type type, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            AddUsing(type, usings);
            //if (table.ItemType == type && !baseClass)
            //    yield break;
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))//BindingFlags.DeclaredOnly
            {
                if (method.GetCustomAttribute<ControllerMethodAttribute>() != null
                    && (!method.IsVirtual || method.GetBaseDefinition() == null))
                {
                    yield return GetControllerMethod(method, table, usings);
                }
            }
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))//BindingFlags.DeclaredOnly
            {
                if (method.GetCustomAttribute<ControllerMethodAttribute>() != null)
                {
                    yield return GetControllerMethod(method, table, usings);
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
        private MethodDeclarationSyntax GetControllerMethod(MethodInfo method, TableAttributeCache table, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            AddUsing(method.DeclaringType, usings);
            AddUsing(method.ReturnType, usings);

            var returning = method.ReturnType == typeof(void) ? "void" : $"ActionResult<{TypeHelper.FormatCode(method.ReturnType)}>";
            var modifiers = new List<SyntaxToken> { SyntaxFactory.Token(SyntaxKind.PublicKeyword) };
            var isAsync = TypeHelper.IsBaseType(method.ReturnType, typeof(Task));
            if (isAsync)
            {
                modifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
                if (method.ReturnType.IsGenericType)
                {
                    var returnType = method.ReturnType.GetGenericArguments().FirstOrDefault();
                    AddUsing(returnType, usings);
                    returning = $"Task<ActionResult<{TypeHelper.FormatCode(returnType)}>>";
                }
                else
                {
                    returning = "Task<ActionResult>";
                }
            }

            var parametersInfo = GetParametersInfo(method, table, usings);
            return SyntaxFactory.MethodDeclaration(attributeLists: SyntaxFactory.List(GetControllerMethodAttributes(method, parametersInfo)),
                          modifiers: SyntaxFactory.TokenList(modifiers.ToArray()),
                          returnType: returning == "void"
                          ? SyntaxFactory.ParseTypeName("IActionResult")
                          : SyntaxFactory.ParseTypeName(returning),
                          explicitInterfaceSpecifier: null,
                          identifier: SyntaxFactory.Identifier(method.Name),
                          typeParameterList: null,
                          parameterList: SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(GetControllerMethodParameters(method, table, parametersInfo))),
                          constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                          body: SyntaxFactory.Block(GetControllerMethodBody(method, parametersInfo, returning)),
                          semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            // Annotate that this node should be formatted
            //.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private IEnumerable<StatementSyntax> GetControllerMethodBody(MethodInfo method, List<MethodParametrInfo> parametersInfo, string returning)
        {
            var isTransact = parametersInfo.Any(p => p.Info.ParameterType == typeof(DBTransaction));
            var isVoid = method.ReturnType == typeof(void);
            var returnType = method.ReturnType;
            var isAsync = TypeHelper.IsBaseType(method.ReturnType, typeof(Task));
            if (isAsync)
            {
                returning = returning.Substring(5, returning.Length - 6);
                if (method.ReturnType.IsGenericType)
                {
                    returnType = method.ReturnType.GetGenericArguments().FirstOrDefault();
                }
                isVoid = method.ReturnType == typeof(Task);
            }

            if (isTransact)
            {
                yield return SyntaxFactory.ParseStatement($"using(var {prTransaction} = new DBTransaction(table.Connection, {prUser})) {{");
            }

            yield return SyntaxFactory.ParseStatement("try {");

            if (!method.IsStatic)
            {
                yield return SyntaxFactory.ParseStatement($"var idValue = table.LoadById(id, DBLoadParam.Load | DBLoadParam.Referencing);");
                yield return SyntaxFactory.ParseStatement("if (idValue == null)");
                yield return SyntaxFactory.ParseStatement("{ return NotFound(); }");
            }
            var parametersBuilder = new StringBuilder();
            foreach (var parameter in parametersInfo)
            {
                if (parameter.Table != null)
                {
                    yield return SyntaxFactory.ParseStatement($"var {parameter.ValueName} = DBItem.GetTable<{TypeHelper.FormatCode(parameter.Info.ParameterType)}>().LoadById({parameter.Info.Name}, DBLoadParam.Load | DBLoadParam.Referencing);");
                }
                else if (parameter.ValueName == prStream)
                {
                    if (isAsync)
                    {
                        yield return SyntaxFactory.ParseStatement($"var {parameter.ValueName} = (await Upload(true))?.Stream;");
                    }
                    else
                    {
                        yield return SyntaxFactory.ParseStatement($"var {parameter.ValueName} = Upload(true).GetAwaiter().GetResult()?.Stream;");
                    }
                }
                parametersBuilder.Append($"{parameter.ValueName}, ");
            }
            if (parametersInfo.Count > 0)
            {
                parametersBuilder.Length -= 2;
            }
            var builder = new StringBuilder();
            if (TypeHelper.IsBaseType(returnType, typeof(Stream)))
            {
                yield return SyntaxFactory.ParseStatement($"var exportStream = {(isAsync ? "(await " : "")}{(method.IsStatic ? method.DeclaringType.Name : " idValue")}.{method.Name}({parametersBuilder}){(isAsync ? ")" : "")} as FileStream;");
                if (isTransact)
                {
                    yield return SyntaxFactory.ParseStatement($"{prTransaction}.Commit();");
                }
                yield return SyntaxFactory.ParseStatement($"return new FileStreamResult(exportStream, System.Net.Mime.MediaTypeNames.Application.Octet){{ FileDownloadName = Path.GetFileName(exportStream.Name) }};");
            }
            else
            {
                if (!isVoid)
                {
                    builder.Append("var result = ");
                }
                if (isAsync)
                {
                    builder.Append("await ");
                }

                builder.Append($"{(method.IsStatic ? method.DeclaringType.Name : "idValue")}.{method.Name}({parametersBuilder}");
                builder.AppendLine(");");

                yield return SyntaxFactory.ParseStatement(builder.ToString());
                if (isTransact)
                {
                    yield return SyntaxFactory.ParseStatement($"{prTransaction}.Commit();");
                }

                if (!isVoid)
                {
                    yield return SyntaxFactory.ParseStatement($"return new {returning}(result);");
                }
                else
                {
                    yield return SyntaxFactory.ParseStatement($"return Ok();");
                }
            }

            yield return SyntaxFactory.ParseStatement("}");
            yield return SyntaxFactory.ParseStatement("catch (Exception ex) {");
            if (isTransact)
            {
                yield return SyntaxFactory.ParseStatement($"{prTransaction}.Rollback();");
            }
            yield return SyntaxFactory.ParseStatement("return BadRequest(ex);");
            yield return SyntaxFactory.ParseStatement("}");
            if (isTransact)
            {
                yield return SyntaxFactory.ParseStatement("}");
            }
        }

        private IEnumerable<AttributeListSyntax> GetControllerMethodAttributes(MethodInfo method, List<MethodParametrInfo> parametersList)
        {
            var parameters = method.Name + (method.IsStatic ? "" : "/{id}");
            var post = false;
            foreach (var parameter in parametersList)
            {
                if (parameter.Info.ParameterType == typeof(DBTransaction))
                    continue;
                if (parameter.ValueName == prStream)
                {
                    yield return SyntaxFactory.AttributeList(
                         SyntaxFactory.SingletonSeparatedList(
                             SyntaxFactory.Attribute(
                                 SyntaxFactory.IdentifierName("DisableFormValueModelBinding"))));
                    post = true;
                    continue;
                }
                parameters += $"/{{{parameter.Info.Name}}}";
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
                                 SyntaxFactory.IdentifierName(post ? "HttpPost" : "HttpGet"))));
        }

        private List<MethodParametrInfo> GetParametersInfo(MethodInfo method, TableAttributeCache table, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            var parametersInfo = new List<MethodParametrInfo>();
            foreach (var parameter in method.GetParameters())
            {
                var methodParameter = new MethodParametrInfo { Info = parameter };
                parametersInfo.Add(methodParameter);
                AddUsing(methodParameter.Info.ParameterType, usings);
                if (methodParameter.Info.ParameterType == typeof(DBTransaction))
                {
                    methodParameter.ValueName = prTransaction;
                }
                else if (TypeHelper.IsBaseType(methodParameter.Info.ParameterType, typeof(Stream)))
                {
                    methodParameter.ValueName = prStream;
                }
            }
            return parametersInfo;
        }

        private IEnumerable<ParameterSyntax> GetControllerMethodParameters(MethodInfo method, TableAttributeCache table, List<MethodParametrInfo> parametersInfo)
        {
            if (!method.IsStatic)
            {
                yield return SyntaxFactory.Parameter(attributeLists: SyntaxFactory.List(GetParameterAttributes()),
                                                             modifiers: SyntaxFactory.TokenList(),
                                                             type: SyntaxFactory.ParseTypeName((table.PrimaryKey?.GetDataType() ?? typeof(int)).Name),
                                                             identifier: SyntaxFactory.Identifier("id"),
                                                             @default: null);
            }

            foreach (var methodParameter in parametersInfo)
            {
                if (methodParameter.Info.ParameterType == typeof(DBTransaction)
                    || methodParameter.ValueName == prStream)
                {
                    continue;
                }
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

        private void AddUsing(Type type, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            AddUsing(type.Namespace, usings);
        }

        private void AddUsing(string usingName, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            if (!usings.TryGetValue(usingName, out var syntax))
            {
                usings.Add(usingName, SyntaxHelper.CreateUsingDirective(usingName));
            }
        }

        //private void GetMethod(StringBuilder builder, MethodInfo method, DBTable table)
        //{
        //    AddUsing(method.ReturnType);
        //    var mparams = method.GetParameters();
        //    var parameters = method.IsStatic ? "" : "/{id:int}";
        //    var returning = method.ReturnType == typeof(void) ? "void" : $"ActionResult<{TypeHelper.FormatCode(method.ReturnType)}>";
        //    parametersInfo = new List<MethodParametrInfo>();

        //    foreach (var parameter in mparams)
        //    {
        //        parametersInfo.Add(new MethodParametrInfo { Info = parameter });
        //        parameters += $"/{{{parameter.Name}}}";
        //    }
        //    //var methodsyntax = GetMethod(method);
        //    builder.AppendLine($"[Route(\"{method.Name}{parameters}\"), HttpGet()]");
        //    builder.Append($"public {(method.IsVirtual ? "virtual" : "")} {returning} {method.Name} (");
        //    if (!method.IsStatic)
        //    {
        //        builder.Append($"int id{(mparams.Length > 0 ? ", " : "")}");
        //    }
        //    if (mparams.Length > 0)
        //    {
        //        foreach (var parameter in parametersInfo)
        //        {
        //            AddUsing(parameter.Type);
        //            builder.Append($"{TypeHelper.FormatCode(parameter.Type)} {parameter.Info.Name}, ");
        //        }
        //        builder.Length -= 2;
        //    }
        //    builder.AppendLine(") {");
        //    if (!method.IsStatic)
        //    {
        //        AddUsing(method.DeclaringType);
        //        builder.AppendLine($"var idValue = table.LoadById<{TypeHelper.FormatCode(method.DeclaringType)}>(id);");

        //        foreach (var parameter in parametersInfo)
        //        {
        //            if (parameter.Table != null)
        //            {
        //                AddUsing(parameter.Info.ParameterType);
        //                builder.AppendLine($"var {parameter.ValueName} = DBItem.GetTable<{TypeHelper.FormatCode(parameter.Info.ParameterType)}>().LoadById({parameter.Info.Name});");
        //            }
        //        }
        //        if (method.ReturnType != typeof(void))
        //            builder.Append($@"return new {returning}(");
        //        builder.Append($" idValue.{method.Name}(");

        //        if (mparams.Length > 0)
        //        {
        //            foreach (var parameter in parametersInfo)
        //            {
        //                builder.Append($"{parameter.ValueName}, ");
        //            }
        //            builder.Length -= 2;
        //        }
        //        if (method.ReturnType != typeof(void))
        //            builder.Append(")");
        //        builder.AppendLine(");");

        //    }
        //    builder.AppendLine("}");
        //}
    }

    public class MethodParametrInfo
    {
        private ParameterInfo info;

        public Type Type { get; internal set; }
        public TableAttributeCache Table { get; internal set; }
        public string ValueName { get; internal set; }
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