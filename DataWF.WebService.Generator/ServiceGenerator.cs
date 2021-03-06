﻿using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DataWF.WebService.Generator
{
    public partial class ServiceGenerator
    {
        private const string prStream = "uploaded";
        private const string prUser = "CurrentUser";
        private const string prTransaction = "transaction";
        private readonly Dictionary<string, MetadataReference> references;
        private readonly Dictionary<string, ClassDeclarationSyntax> controllers = new Dictionary<string, ClassDeclarationSyntax>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, UsingDirectiveSyntax>> controllersUsings = new Dictionary<string, Dictionary<string, UsingDirectiveSyntax>>(StringComparer.Ordinal);
        private readonly Dictionary<string, ClassDeclarationSyntax> logs = new Dictionary<string, ClassDeclarationSyntax>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, UsingDirectiveSyntax>> logsUsings = new Dictionary<string, Dictionary<string, UsingDirectiveSyntax>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<AttributeListSyntax>> logsAttributes = new Dictionary<string, List<AttributeListSyntax>>(StringComparer.Ordinal);
        private readonly Dictionary<string, ClassDeclarationSyntax> invokers = new Dictionary<string, ClassDeclarationSyntax>(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, UsingDirectiveSyntax>> invokerUsings = new Dictionary<string, Dictionary<string, UsingDirectiveSyntax>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<AttributeListSyntax>> invokerAttributes = new Dictionary<string, List<AttributeListSyntax>>(StringComparer.Ordinal);
        public List<Assembly> Assemblies { get; private set; }
        public string Output { get; }
        public string Namespace { get; private set; }
        public CodeGeneratorMode Mode { get; set; } = CodeGeneratorMode.None;

        public ServiceGenerator(string paths, string output, string nameSpace)
            : this(paths?.Split(new char[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries), output, nameSpace)
        { }

        public ServiceGenerator(IEnumerable<string> assemblies, string output, string nameSpace)
            : this(LoadAssemblies(assemblies), output, nameSpace)
        { }

        public ServiceGenerator(IEnumerable<Assembly> assemblies, string output, string nameSpace)
        {
            Assemblies = new List<Assembly>(assemblies);
            Output = string.IsNullOrEmpty(output) ? null : Path.GetFullPath(output);
            Namespace = nameSpace ?? "DataWF.Web.Controller";
            references = new Dictionary<string, MetadataReference>(StringComparer.Ordinal) {
                {"netstandard", MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location) },
                {"System", MetadataReference.CreateFromFile(typeof(Object).Assembly.Location) },
                {"System.Runtime", MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=0.0.0.0").Location) },
                {"System.Collections", MetadataReference.CreateFromFile(Assembly.Load("System.Collections, Version=0.0.0.0").Location) },
                {"System.List", MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location) },
                {"System.Linq", MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location) },
                {"Microsoft.AspNetCore.Mvc", MetadataReference.CreateFromFile(typeof(ControllerBase).Assembly.Location) },
                {"DataWF.Common", MetadataReference.CreateFromFile(typeof(Helper).Assembly.Location) },
                {"DataWF.Data", MetadataReference.CreateFromFile(typeof(TableGenerator).Assembly.Location) },
            };
        }

        public static IEnumerable<Assembly> LoadAssemblies(IEnumerable<string> assemblies)
        {
            foreach (var name in assemblies)
            {
                var fullPath = Path.GetFullPath(name);
                if ((File.GetAttributes(fullPath) & FileAttributes.Directory) != 0)
                {
                    var files = Directory.GetFiles(fullPath, "*.dll");
                    foreach (var file in files)
                    {
                        var assembly = ResolveAssembly(file);
                        if (assembly != null)
                            yield return assembly;
                    }
                }
                else
                {
                    var assembly = ResolveAssembly(fullPath);
                    if (assembly != null)
                        yield return assembly;
                }
            }
        }

        private static Assembly ResolveAssembly(string file)
        {
            var assembly = (Assembly)null;
            try
            {
                using (var resolver = new AssemblyResolver(file))
                {
                    assembly = resolver.Assembly;
                    SyntaxHelper.ConsoleInfo($"Load Assembly {assembly} from {file}");
                    _ = assembly.GetExportedTypes();
                }
            }
            catch (Exception ex)
            {
                SyntaxHelper.ConsoleWarning($"Can't Load Assembly {file}. {ex.Message}");
            }

            return assembly;
        }

        public void Generate()
        {
            foreach (var assembly in Assemblies)
            {
                var assemblyName = assembly.GetName().Name;
                if (!references.ContainsKey(assemblyName))
                    references[assemblyName] = MetadataReference.CreateFromFile(assembly.Location);
                var types = (Type[])null;
                try
                {
                    types = assembly.GetExportedTypes();
                }
                catch (Exception ex)
                {
                    SyntaxHelper.ConsoleWarning($"Can't Get ExportedTypes of {assembly}. {ex.Message}");
                    continue;
                }

                foreach (var itemType in types)
                {
                    var table = DBTable.GetTableAttribute(itemType);
                    if (table != null && !(table is LogTableGenerator))
                    {
                        if ((Mode & CodeGeneratorMode.Controllers) != 0)
                        {
                            var usings = new Dictionary<string, UsingDirectiveSyntax>(StringComparer.Ordinal) {
                                { "DataWF.Common", SyntaxHelper.CreateUsingDirective("DataWF.Common") },
                                { "DataWF.Data", SyntaxHelper.CreateUsingDirective("DataWF.Data") },
                                { "DataWF.WebService.Common", SyntaxHelper.CreateUsingDirective("DataWF.WebService.Common") },
                                { "Microsoft.AspNetCore.Mvc", SyntaxHelper.CreateUsingDirective("Microsoft.AspNetCore.Mvc") },
                                { "Microsoft.AspNetCore.Authentication.JwtBearer", SyntaxHelper.CreateUsingDirective("Microsoft.AspNetCore.Authentication.JwtBearer") },
                                { "Microsoft.AspNetCore.Authorization", SyntaxHelper.CreateUsingDirective("Microsoft.AspNetCore.Authorization") },
                                { "System", SyntaxHelper.CreateUsingDirective("System") },
                                { "System.Collections.Generic", SyntaxHelper.CreateUsingDirective("System.Collections.Generic") }
                            };
                            var controller = GetOrGenController(table, itemType, usings);
                        }
                        if ((Mode & CodeGeneratorMode.Logs) != 0)
                        {
                            var logUsings = new Dictionary<string, UsingDirectiveSyntax>(StringComparer.Ordinal) {
                                { "DataWF.Common", SyntaxHelper.CreateUsingDirective("DataWF.Common") },
                                { "DataWF.Data", SyntaxHelper.CreateUsingDirective("DataWF.Data") },
                                { "System", SyntaxHelper.CreateUsingDirective("System") },
                                { "System.Collections.Generic", SyntaxHelper.CreateUsingDirective("System.Collections.Generic") }
                            };
                            var logAttributes = new List<AttributeListSyntax>();
                            var log = GetOrGenLog(table, itemType, logUsings);
                        }
                        if ((Mode & CodeGeneratorMode.Invokers) != 0)
                        {
                            var invokerUsings = new Dictionary<string, UsingDirectiveSyntax>(StringComparer.Ordinal) {
                                { "DataWF.Common", SyntaxHelper.CreateUsingDirective("DataWF.Common") },
                                { "DataWF.Data", SyntaxHelper.CreateUsingDirective("DataWF.Data") },
                                { "System", SyntaxHelper.CreateUsingDirective("System") },
                                { "System.Collections.Generic", SyntaxHelper.CreateUsingDirective("System.Collections.Generic") }
                            };
                            GetOrGenInvokers(table, itemType, invokerUsings);
                        }
                    }
                }
            }

        }

        private ClassDeclarationSyntax GetOrGenInvokers(TableGenerator table, Type itemType, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            var baseType = itemType.BaseType;
            while (baseType.IsGenericType)
                baseType = baseType.BaseType;
            if (baseType != typeof(object))
            {
                GetOrGenInvokers(table, baseType, usings);
            }
            if (!invokers.TryGetValue(itemType.Name, out var invokerClass))
            {
                var attributes = new List<AttributeListSyntax>();
                SyntaxHelper.AddUsing(itemType, usings);
                invokerClass = GenInvokersClass(table, itemType, usings, attributes);

                invokers[itemType.Name] = invokerClass;
                invokerUsings[itemType.Name] = usings;
                invokerAttributes[itemType.Name] = attributes;
            }
            return invokerClass;
        }

        private ClassDeclarationSyntax GenInvokersClass(TableGenerator table, Type itemType, Dictionary<string, UsingDirectiveSyntax> usings, List<AttributeListSyntax> invokerAttributes)
        {
            SyntaxHelper.AddUsing(Namespace, usings);
            return SF.ClassDeclaration(
                attributeLists: SF.List<AttributeListSyntax>(),
                modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)),
                identifier: SF.Identifier(itemType.Name + "Invokers"),
                typeParameterList: null,
                baseList: null,
                constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                members: SF.List(GenInvokers(table, itemType, usings, invokerAttributes))
                );
        }

        private IEnumerable<MemberDeclarationSyntax> GenInvokers(TableGenerator table, Type itemType, Dictionary<string, UsingDirectiveSyntax> usings, List<AttributeListSyntax> invokerAttributes)
        {
            var columns = GetColumns(table, itemType);
            foreach (var property in itemType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (TypeHelper.IsIndex(property))
                {
                    continue;
                }

                SyntaxHelper.AddUsing(property.PropertyType, usings);
                yield return GenPropertyInvoker(property.Name + "Invoker",
                    itemType.Name,
                    property.Name,
                    TypeHelper.FormatCode(property.PropertyType),
                    itemType.Name + "Invokers",
                    property.GetSetMethod() != null,
                    invokerAttributes);
            }
        }

        private static List<ColumnGenerator> GetColumns(TableGenerator table, Type itemType)
        {
            var columns = new List<ColumnGenerator>();
            var itemBaseType = itemType.BaseType;
            while (itemBaseType.IsGenericType)
            {
                columns.AddRange(table.Columns
                .Where(p => itemBaseType == p.PropertyInfo?.DeclaringType));
                itemBaseType = itemBaseType.BaseType;
            }
            columns.AddRange(table.Columns
                .Where(p => itemType == p.PropertyInfo?.DeclaringType));
            return columns;
        }

        private static List<ColumnGenerator> GetLogColumns(TableGenerator table, Type itemType)
        {
            var columns = new List<ColumnGenerator>();
            var itemBaseType = itemType.BaseType;
            while (itemBaseType.IsGenericType)
            {
                columns.AddRange(table.Columns
                .Where(p => itemBaseType == p.PropertyInfo?.DeclaringType
                          && p.Attribute.ColumnType == DBColumnTypes.Default
                          && (p.Attribute.Keys & DBColumnKeys.NoLog) == 0));
                itemBaseType = itemBaseType.BaseType;
            }
            columns.AddRange(table.Columns
                .Where(p => itemType == p.PropertyInfo?.DeclaringType
                          && p.Attribute.ColumnType == DBColumnTypes.Default
                          && (p.Attribute.Keys & DBColumnKeys.NoLog) == 0));
            return columns;
        }

        private IEnumerable<ClassDeclarationSyntax> GenPropertyInvokers(ColumnGenerator column, TableGenerator table, Type itemType, Dictionary<string, UsingDirectiveSyntax> usings, List<AttributeListSyntax> attributes)
        {
            SyntaxHelper.AddUsing(column.PropertyInfo.PropertyType, usings);
            yield return GenPropertyInvoker(column.PropertyName + "Invoker",
                itemType.Name,
                column.PropertyName,
                TypeHelper.FormatCode(column.PropertyInfo.PropertyType),
                 itemType.Name,
                column.PropertyInfo.GetSetMethod() != null,
                attributes);
            var reference = table.References.FirstOrDefault(p => p.Column == column);
            if (reference != null && TypeHelper.IsBaseType(itemType, reference.PropertyInfo.DeclaringType))
            {
                SyntaxHelper.AddUsing(reference.PropertyInfo.PropertyType, usings);
                yield return GenPropertyInvoker(reference.PropertyInfo.Name + "Invoker",
                      itemType.Name,
                      reference.PropertyInfo.Name,
                      TypeHelper.FormatCode(reference.PropertyInfo.PropertyType),
                       itemType.Name,
                      reference.PropertyInfo.GetSetMethod() != null,
                      attributes);
            }
        }

        private ClassDeclarationSyntax GetOrGenLog(TableGenerator table, Type itemType, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            if (!table.Attribute.IsLoging)
                return null;
            var logType = TypeHelper.ParseType("LogItem") ?? TypeHelper.ParseType("DBLogItem");
            SyntaxHelper.AddUsing(logType, usings);
            var baseName = logType.Name;

            var baseType = itemType.BaseType;
            while (baseType.IsGenericType)
                baseType = baseType.BaseType;

            if (baseType != typeof(DBItem)
                && baseType != typeof(DBGroupItem))
            {
                GetOrGenLog(table, baseType, usings);
                baseName = string.Concat(baseType.Name, "Log");
            }
            if (!logs.TryGetValue(itemType.Name, out var logClass))
            {
                var invokerAttributes = new List<AttributeListSyntax>();
                SyntaxHelper.AddUsing(itemType, usings);
                logClass = SF.ClassDeclaration(
                    attributeLists: SF.List(GenLogAttributeList(table, itemType)),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)),
                    identifier: SF.Identifier(GetLogClassName(itemType)),
                    typeParameterList: null,
                    baseList: SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(
                        SF.SimpleBaseType(SF.ParseTypeName(baseName)))),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    members: SF.List(GenLogMemebers(table, itemType, baseType, usings, invokerAttributes))
                    );

                logs[itemType.Name] = logClass;
                logsUsings[itemType.Name] = usings;
                logsAttributes[itemType.Name] = invokerAttributes;
            }
            return logClass;
        }

        private IEnumerable<AttributeListSyntax> GenLogAttributeList(TableGenerator table, Type itemType)
        {
            if (itemType.IsAbstract)
                yield break;
            if (itemType.GetCustomAttribute<TableAttribute>(false) != null)
            {
                yield return SF.AttributeList(
                             SF.SingletonSeparatedList(
                                 SF.Attribute(
                                     SF.IdentifierName("LogTable")).WithArgumentList(
                                     SF.AttributeArgumentList(
                                         SF.SingletonSeparatedList(
                                             SF.AttributeArgument(
                                                 SF.ParseExpression($"typeof({itemType.Name}), \"{table.Attribute.TableName}_log\"")))))));
            }
            else if (DBTable.GetItemTypeAttribute(itemType) is ItemTypeGenerator itemTypeAttr)
            {
                yield return SF.AttributeList(
                             SF.SingletonSeparatedList(
                                 SF.Attribute(
                                     SF.IdentifierName("LogItemType")).WithArgumentList(
                                     SF.AttributeArgumentList(
                                         SF.SingletonSeparatedList(
                                             SF.AttributeArgument(
                                                 SF.ParseExpression($"{itemTypeAttr.Attribute.Id}")))))));
            }
        }

        private IEnumerable<MemberDeclarationSyntax> GenLogMemebers(TableGenerator table, Type itemType, Type baseType, Dictionary<string, UsingDirectiveSyntax> usings, List<AttributeListSyntax> logAttributes)
        {
            if ((itemType.GetCustomAttribute<TableAttribute>(false) != null)
                || !TypeHelper.IsBaseType(itemType, table.ItemType))
            {
                SyntaxHelper.AddUsing(table.ItemType, usings);
                yield return SF.FieldDeclaration(
               attributeLists: SF.List<AttributeListSyntax>(),
               modifiers: SF.TokenList(new[] { SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.StaticKeyword), SF.Token(SyntaxKind.ReadOnlyKeyword) }),
               declaration: SF.VariableDeclaration(
                   type: SF.ParseTypeName(nameof(IDBLogTable)),
                   variables: SF.SingletonSeparatedList(
                       SF.VariableDeclarator(
                           identifier: SF.Identifier("DBLogTable"),
                           argumentList: null,
                           initializer: SF.EqualsValueClause(SF.ParseExpression($"GetTable<{table.ItemType.Name}>().LogTable"))))));
            }
            var columns = GetLogColumns(table, itemType);

            foreach (var column in columns)
            {
                yield return GenKeyField(column, table, itemType);
            }
            foreach (var column in columns)
            {
                foreach (var property in GenLogProperty(column, table, itemType, usings))
                    yield return property;
            }
            foreach (var column in columns)
            {
                foreach (var invoker in GenLogPropertyInvokers(column, table, itemType, usings, logAttributes))
                    yield return invoker;
            }
        }

        private IEnumerable<ClassDeclarationSyntax> GenLogPropertyInvokers(ColumnGenerator column, TableGenerator table, Type itemType, Dictionary<string, UsingDirectiveSyntax> usings, List<AttributeListSyntax> logAttributes)
        {
            var typeName = GetLogClassName(itemType);
            SyntaxHelper.AddUsing(Namespace, usings);
            yield return GenPropertyInvoker(column.PropertyName + "Invoker",
                typeName,
                GetLogPropertyName(column),
                TypeHelper.FormatCode(column.PropertyInfo.PropertyType),
                typeName,
                true,
                logAttributes);
            var reference = table.References.FirstOrDefault(p => p.Column == column);
            if (reference != null)
            {
                yield return GenPropertyInvoker(reference.PropertyInfo.Name + "Invoker",
                      typeName,
                      reference.PropertyInfo.Name,
                      TypeHelper.FormatCode(reference.PropertyInfo.PropertyType),
                      typeName,
                      true,
                      logAttributes);
            }
        }

        private IEnumerable<MemberDeclarationSyntax> GenLogProperty(ColumnGenerator column, TableGenerator table, Type itemType, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            SyntaxHelper.AddUsing(TypeHelper.CheckNullable(column.PropertyInfo.PropertyType), usings);
            var typeText = TypeHelper.FormatCode(column.PropertyInfo.PropertyType);
            var nullable = typeText.IndexOf('?') >= 0 ? "Nullable" : string.Empty;
            var nullableType = nullable.Length > 0 ? typeText.Replace("?", "") : typeText;
            yield return SF.PropertyDeclaration(
                   attributeLists: SF.List(GenLogPropertyAttributes(column, table, itemType)),
                   modifiers: SF.TokenList(new[] { SF.Token(SyntaxKind.PublicKeyword) }),
                   type: SF.ParseTypeName(typeText),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier(GetLogPropertyName(column)),
                   accessorList: SF.AccessorList(SF.List(new[]{
                    SF.AccessorDeclaration(
                        kind: SyntaxKind.GetAccessorDeclaration,
                        attributeLists: SF.List<AttributeListSyntax>(),
                        modifiers: SF.TokenList(SF.Token(SyntaxKind.None)),
                        expressionBody:SF.ArrowExpressionClause(SF.ParseExpression($"GetValue{nullable}<{nullableType}>({GetKeyName(column)});"))),
                    SF.AccessorDeclaration(
                        kind: SyntaxKind.SetAccessorDeclaration,
                        attributeLists: SF.List<AttributeListSyntax>(),
                        modifiers: SF.TokenList(SF.Token(SyntaxKind.None)),
                        expressionBody:SF.ArrowExpressionClause(SF.ParseExpression($"SetValue{nullable}(value, {GetKeyName(column)});"))),
                       })),
                   expressionBody: null,
                   initializer: null,
                   semicolonToken: SF.Token(SyntaxKind.None)
                  );
            var reference = table.References.FirstOrDefault(p => p.Column == column);
            if (reference != null)
            {
                SyntaxHelper.AddUsing(reference.PropertyInfo.PropertyType, usings);
                var refTypeText = TypeHelper.FormatCode(reference.PropertyInfo.PropertyType);
                yield return SF.PropertyDeclaration(
                   attributeLists: SF.List(GenLogReferencePropertyAttributes(column, table, itemType)),
                   modifiers: SF.TokenList(new[] { SF.Token(SyntaxKind.PublicKeyword) }),
                   type: SF.ParseTypeName(refTypeText),
                   explicitInterfaceSpecifier: null,
                   identifier: SF.Identifier(reference.PropertyInfo.Name),
                   accessorList: SF.AccessorList(SF.List(new[]{
                    SF.AccessorDeclaration(
                        kind: SyntaxKind.GetAccessorDeclaration,
                        attributeLists: SF.List<AttributeListSyntax>(),
                        modifiers: SF.TokenList(SF.Token(SyntaxKind.None)),
                        expressionBody:SF.ArrowExpressionClause(SF.ParseExpression($"GetReference<{refTypeText}>({GetKeyName(column)});"))),
                    SF.AccessorDeclaration(
                        kind: SyntaxKind.SetAccessorDeclaration,
                        attributeLists: SF.List<AttributeListSyntax>(),
                        modifiers: SF.TokenList(SF.Token(SyntaxKind.None)),
                        expressionBody:SF.ArrowExpressionClause(SF.ParseExpression($"SetReference(value, {GetKeyName(column)});"))),
                       })),
                   expressionBody: null,
                   initializer: null,
                   semicolonToken: SF.Token(SyntaxKind.None)
                  );
            }
        }

        private IEnumerable<AttributeListSyntax> GenLogPropertyAttributes(ColumnGenerator column, TableGenerator table, Type itemType)
        {
            yield return SF.AttributeList(
                         SF.SingletonSeparatedList(
                             SF.Attribute(
                                 SF.IdentifierName("LogColumn")).WithArgumentList(
                                 SF.AttributeArgumentList(
                                     SF.SingletonSeparatedList(
                                         SF.AttributeArgument(
                                             SF.ParseExpression($"\"{column.ColumnName}\", \"{column.ColumnName}_log\"")))))));
        }

        private IEnumerable<AttributeListSyntax> GenLogReferencePropertyAttributes(ColumnGenerator column, TableGenerator table, Type itemType)
        {
            yield return SF.AttributeList(
                         SF.SingletonSeparatedList(
                             SF.Attribute(
                                 SF.IdentifierName("LogReference")).WithArgumentList(
                                 SF.AttributeArgumentList(
                                     SF.SingletonSeparatedList(
                                         SF.AttributeArgument(
                                             SF.ParseExpression($"nameof({GetLogPropertyName(column)})")))))));
        }

        private string GetLogPropertyName(ColumnGenerator column)
        {
            if (column.PropertyInfo.DeclaringType == typeof(DBItem))
            {
                return $"Base{column.PropertyName}";
            }
            return column.PropertyName;
        }

        private MemberDeclarationSyntax GenKeyProperty(ColumnGenerator column, TableGenerator tableAttribute, Type itemType)
        {
            return SF.PropertyDeclaration(
                attributeLists: SF.List<AttributeListSyntax>(),
                modifiers: SF.TokenList(new[] { SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.StaticKeyword) }),
                type: SF.ParseTypeName(nameof(DBColumn)),
                explicitInterfaceSpecifier: null,
                identifier: SF.Identifier(GetKeyName(column)),
                accessorList: null,
                expressionBody: SF.ArrowExpressionClause(
                    SF.ParseExpression($"DBLogTable.ParseLogProperty(\"{column.PropertyName}\", ref {GetKeyFieldName(column)})")),
                initializer: null,
                semicolonToken: SF.Token(SyntaxKind.SemicolonToken)
               );
        }

        private MemberDeclarationSyntax GenKeyField(ColumnGenerator column, TableGenerator table, Type itemType)
        {
            return SF.FieldDeclaration(
                attributeLists: SF.List<AttributeListSyntax>(),
                modifiers: SF.TokenList(new[] { SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.StaticKeyword), SF.Token(SyntaxKind.ReadOnlyKeyword) }),
                declaration: SF.VariableDeclaration(
                    type: SF.ParseTypeName(nameof(DBColumn)),
                    variables: SF.SingletonSeparatedList(
                        SF.VariableDeclarator(
                            identifier: SF.Identifier(GetKeyName(column)),
                            argumentList: null,
                            initializer: SF.EqualsValueClause(SF.ParseExpression($"DBLogTable.ParseLogProperty(nameof({column.PropertyName}))"))))));
        }

        private string GetKeyFieldName(ColumnGenerator column)
        {
            var property = column.PropertyName;
            return string.Concat(char.ToLowerInvariant(property[0]).ToString(CultureInfo.InvariantCulture), property.Substring(1), "Key");
        }

        private string GetKeyName(ColumnGenerator column)
        {
            var property = column.PropertyName;
            return string.Concat(property, "Key");
        }

        private static string GetLogClassName(Type itemType)
        {
            return $"{itemType.Name}Log";
        }

        private ClassDeclarationSyntax GetOrGenBaseController(TableGenerator table, Type itemType, Dictionary<string, UsingDirectiveSyntax> usings, out string controllerClassName)
        {
            string name = $"Base{(table.FileKey != null ? "File" : "")}{itemType.Name}";
            controllerClassName = $"{name}Controller";

            if (!controllers.TryGetValue(name, out var baseController))
            {
                var fileColumn = table.Columns.FirstOrDefault(p => (p.Attribute.Keys & DBColumnKeys.File) == DBColumnKeys.File);
                var primaryKeyType = table.PrimaryKey?.GetDataType() ?? typeof(int);
                var baseType = $"Base{(table.FileKey != null ? "File" : "")}{(IsPrimaryType(itemType.BaseType) ? "" : itemType.BaseType.Name)}Controller<T, K>";
                baseController = SF.ClassDeclaration(
                    attributeLists: SF.List<AttributeListSyntax>(),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.AbstractKeyword)),
                    identifier: SF.Identifier(controllerClassName),
                    typeParameterList: SF.TypeParameterList(
                        SF.SeparatedList(new[] {
                            SF.TypeParameter("T"),
                            SF.TypeParameter("K") })),
                    baseList: SF.BaseList(
                        SF.SeparatedList<BaseTypeSyntax>(new[] {
                        SF.SimpleBaseType(SF.ParseTypeName(baseType)) })),
                    constraintClauses: SF.List(new[] { SF.TypeParameterConstraintClause(
                        name: SF.IdentifierName("T"),
                        constraints: SF.SeparatedList<TypeParameterConstraintSyntax>(new []{
                            SF.TypeConstraint(SF.ParseTypeName(itemType.Name)),
                             SF.TypeConstraint(SF.ParseTypeName("new()"))
                        }))
                    }),
                    members: SF.List(GenControllerMemebers(table, itemType, usings))
                    );

                controllers[name] = baseController;
                controllersUsings[name] = usings;
            }
            return baseController;
        }

        public bool IsPrimaryType(Type itemType)
        {
            return itemType == typeof(DBItem) || itemType == typeof(DBGroupItem);
        }

        //https://carlos.mendible.com/2017/03/02/create-a-class-with-net-core-and-roslyn/
        private ClassDeclarationSyntax GetOrGenController(TableGenerator table, Type itemType, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            var primaryKeyType = table.PrimaryKey?.GetDataType() ?? typeof(int);
            var baseName = $"Base{(table.FileKey != null ? "File" : "")}Controller<{itemType.Name}, {primaryKeyType.Name}>";

            if (table.Attribute.IsLoging)
            {
                var logItemName = itemType.Name + "Log";
                var logItemType = (Mode & CodeGeneratorMode.Logs) != 0 ? logItemName : TypeHelper.ParseType(logItemName)?.FullName;
                //AddUsing(logItemType, usings);
                baseName = $"Base{(table.FileKey != null ? "File" : "Logged")}Controller<{itemType.Name}, {primaryKeyType.Name}, {logItemType}>";
            }
            var baseType = itemType;

            if (!controllers.TryGetValue(itemType.Name, out var controller))
            {
                SyntaxHelper.AddUsing(itemType, usings);
                var controllerClassName = $"{itemType.Name}Controller";
                controller = SF.ClassDeclaration(
                    attributeLists: SF.List(GenControllerAttributeList()),
                    modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)),
                    identifier: SF.Identifier(controllerClassName),
                    typeParameterList: null,
                    baseList: SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(
                        SF.SimpleBaseType(SF.ParseTypeName(baseName)))),
                    constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                    members: SF.List(GenControllerMemebers(table, itemType, usings))
                    );

                controllers[itemType.Name] = controller;
                controllersUsings[itemType.Name] = usings;
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
            var assembly = typeof(ServiceGenerator).Assembly;
            var baseName = assembly.GetName().Name + ".ControllerTemplate.";
            var list = new List<SyntaxTree>();
            list.AddRange(SyntaxHelper.LoadResources(assembly, baseName, Namespace, save ? Output : null).Select(P => P.SyntaxTree));

            if (save)
            {
                Directory.CreateDirectory(Output);
            }

            foreach (var entry in controllers)
            {
                var unit = SyntaxHelper.GenUnit(entry.Value, Namespace, controllersUsings[entry.Key].Values, Enumerable.Empty<AttributeListSyntax>());
                if (save)
                {
                    WriteFile(Path.Combine(Output, $"{entry.Key}Controller.cs"), unit);
                }
                list.Add(unit.SyntaxTree);
            }
            if ((Mode & CodeGeneratorMode.Logs) > 0)
            {
                var logOutput = (Mode & CodeGeneratorMode.Controllers) > 0 ? Path.Combine(Output, "Logs") : "Logs";
                if (save)
                {
                    Directory.CreateDirectory(logOutput);
                }
                foreach (var entry in logs)
                {
                    var unit = SyntaxHelper.GenUnit(entry.Value, Namespace, logsUsings[entry.Key].Values, logsAttributes[entry.Key]);
                    if (save)
                    {
                        WriteFile(Path.Combine(logOutput, $"{entry.Key}Log.cs"), unit);
                    }
                    list.Add(unit.SyntaxTree);
                }
            }
            if ((Mode & CodeGeneratorMode.Invokers) > 0)
            {
                var invokerOutput = (Mode & CodeGeneratorMode.Controllers) > 0 ? Path.Combine(Output, "Invokers") : "Invokers";
                if (save)
                {
                    Directory.CreateDirectory(invokerOutput);
                }
                foreach (var entry in invokers)
                {
                    var unit = SyntaxHelper.GenUnit(entry.Value, Namespace, invokerUsings[entry.Key].Values, invokerAttributes[entry.Key]);
                    if (save)
                    {
                        WriteFile(Path.Combine(invokerOutput, $"{entry.Key}Invokers.cs"), unit);
                    }
                    list.Add(unit.SyntaxTree);
                }
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

        public IEnumerable<MemberDeclarationSyntax> GenControllerMemebers(TableGenerator table, Type type, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            //if (table.ItemType == type && !baseClass)
            //    yield break;
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))//BindingFlags.DeclaredOnly
            {
                var attribute = method.GetCustomAttribute<ControllerMethodAttribute>();
                if (attribute != null && (!method.IsVirtual || method.GetBaseDefinition() == null))
                {
                    yield return GenControllerMethod(method, table, usings, attribute);
                }
            }
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))//BindingFlags.DeclaredOnly
            {
                var attribute = method.GetCustomAttribute<ControllerMethodAttribute>();
                if (attribute != null)
                {
                    yield return GenControllerMethod(method, table, usings, attribute);
                }
            }
        }

        private IEnumerable<AttributeListSyntax> GenControllerAttributeList()
        {
            //[Authorize(JwtBearerDefaults.AuthenticationScheme)]
            yield return SF.AttributeList(
                         SF.SingletonSeparatedList(
                             SF.Attribute(
                                 SF.IdentifierName("Authorize")).WithArgumentList(
                                 SF.AttributeArgumentList(
                                     SF.SingletonSeparatedList(
                                         SF.AttributeArgument(
                                             SF.ParseExpression("AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme")))))));

            yield return SF.AttributeList(
                         SF.SingletonSeparatedList(
                             SF.Attribute(
                                 SF.IdentifierName("Route")).WithArgumentList(
                                 SF.AttributeArgumentList(
                                     SF.SingletonSeparatedList(
                                         SF.AttributeArgument(
                                             SF.LiteralExpression(
                                                 SyntaxKind.StringLiteralExpression,
                                                 SF.Literal("api/[controller]"))))))));

            yield return SF.AttributeList(
                         SF.SingletonSeparatedList(
                             SF.Attribute(
                                 SF.IdentifierName("ApiController"))));
        }

        //https://stackoverflow.com/questions/37710714/roslyn-add-new-method-to-an-existing-class
        private MethodDeclarationSyntax GenControllerMethod(MethodInfo method, TableGenerator table, Dictionary<string, UsingDirectiveSyntax> usings, ControllerMethodAttribute attribute)
        {
            SyntaxHelper.AddUsing(method.DeclaringType, usings);
            SyntaxHelper.AddUsing(method.ReturnType, usings);

            var returning = method.ReturnType == typeof(void) ? "void"
                : attribute.ReturnHtml ? "IActionResult"
                : $"ActionResult<{TypeHelper.FormatCode(method.ReturnType)}>";
            var modifiers = new List<SyntaxToken> { SF.Token(SyntaxKind.PublicKeyword) };
            var isAsync = TypeHelper.IsBaseType(method.ReturnType, typeof(Task));
            if (isAsync)
            {
                modifiers.Add(SF.Token(SyntaxKind.AsyncKeyword));
                if (method.ReturnType.IsGenericType)
                {
                    var returnType = method.ReturnType.GetGenericArguments().FirstOrDefault();
                    SyntaxHelper.AddUsing(returnType, usings);
                    if (attribute.ReturnHtml)
                        returning = $"Task<IActionResult>";
                    else
                        returning = $"Task<ActionResult<{TypeHelper.FormatCode(returnType)}>>";
                }
                else
                {
                    returning = "Task<ActionResult>";
                }
            }

            var parametersInfo = GetParametersInfo(method, table, usings);
            return SF.MethodDeclaration(attributeLists: SF.List(GenControllerMethodAttributes(method, parametersInfo, attribute)),
                          modifiers: SF.TokenList(modifiers.ToArray()),
                          returnType: returning == "void"
                          ? SF.ParseTypeName(nameof(IActionResult))
                          : SF.ParseTypeName(returning),
                          explicitInterfaceSpecifier: null,
                          identifier: SF.Identifier(method.Name),
                          typeParameterList: null,
                          parameterList: SF.ParameterList(SF.SeparatedList(GenControllerMethodParameters(method, table, parametersInfo))),
                          constraintClauses: SF.List<TypeParameterConstraintClauseSyntax>(),
                          body: SF.Block(GenControllerMethodBody(method, parametersInfo, returning, attribute)),
                          semicolonToken: SF.Token(SyntaxKind.None));
            // Annotate that this node should be formatted
            //.WithAdditionalAnnotations(Formatter.Annotation);
        }

        private IEnumerable<StatementSyntax> GenControllerMethodBody(MethodInfo method, List<MethodParametrInfo> parametersInfo, string returning, ControllerMethodAttribute attribute)
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
                yield return SF.ParseStatement($"using(var {prTransaction} = new DBTransaction(table.Connection, {prUser})) {{");
            }

            yield return SF.ParseStatement("try {");

            if (!method.IsStatic)
            {
                yield return SF.ParseStatement($"var idValue = table.LoadById(id, DBLoadParam.Load | DBLoadParam.Referencing);");
                yield return SF.ParseStatement("if (idValue == null)");
                yield return SF.ParseStatement("{ return NotFound(); }");
            }
            var parametersBuilder = new StringBuilder();
            foreach (var parameter in parametersInfo)
            {
                if (parameter.Table != null)
                {
                    yield return SF.ParseStatement($"var {parameter.ValueName} = DBItem.GetTable<{TypeHelper.FormatCode(parameter.Info.ParameterType)}>().LoadById({parameter.Info.Name}, DBLoadParam.Load | DBLoadParam.Referencing);");
                }
                else if (parameter.ValueName == prStream)
                {
                    if (isAsync)
                    {
                        yield return SF.ParseStatement($"var {parameter.ValueName} = (await Upload(true))?.Stream;");
                    }
                    else
                    {
                        yield return SF.ParseStatement($"var {parameter.ValueName} = Upload(true).GetAwaiter().GetResult()?.Stream;");
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
                yield return SF.ParseStatement($"var exportStream = {(isAsync ? "(await " : "")}{(method.IsStatic ? method.DeclaringType.FullName : " idValue")}.{method.Name}({parametersBuilder}){(isAsync ? ")" : "")} as FileStream;");
                if (isTransact)
                {
                    yield return SF.ParseStatement($"{prTransaction}.Commit();");
                }
                yield return SF.ParseStatement($"return new FileStreamResult(exportStream, System.Net.Mime.MediaTypeNames.Application.Octet){{ FileDownloadName = Path.GetFileName(exportStream.Name) }};");
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

                builder.Append($"{(method.IsStatic ? method.DeclaringType.FullName : "idValue")}.{method.Name}({parametersBuilder}");
                builder.AppendLine(");");

                yield return SF.ParseStatement(builder.ToString());
                if (isTransact)
                {
                    yield return SF.ParseStatement($"{prTransaction}.Commit();");
                }
                if (TypeHelper.IsEnumerable(returnType))
                {
                    yield return SF.ParseStatement($"result = Pagination(result);");
                }
                if (!isVoid)
                {
                    if (attribute.ReturnHtml)
                    {
                        yield return SF.ParseStatement(@"return new ContentResult { ContentType = ""text/html"", Content = result };");
                    }
                    else
                    {
                        yield return SF.ParseStatement($"return new {returning}(result);");
                    }
                }
                else
                {
                    yield return SF.ParseStatement($"return Ok();");
                }
            }

            yield return SF.ParseStatement("}");
            yield return SF.ParseStatement("catch (Exception ex) {");
            if (isTransact)
            {
                yield return SF.ParseStatement($"{prTransaction}.Rollback();");
            }
            yield return SF.ParseStatement("return BadRequest(ex);");
            yield return SF.ParseStatement("}");
            if (isTransact)
            {
                yield return SF.ParseStatement("}");
            }
        }

        private IEnumerable<AttributeListSyntax> GenControllerMethodAttributes(MethodInfo method, List<MethodParametrInfo> parametersList, ControllerMethodAttribute attribute)
        {
            if (attribute.Anonymous)
            {
                yield return SF.AttributeList(
                         SF.SingletonSeparatedList(
                             SF.Attribute(
                                 SF.IdentifierName("AllowAnonymous"))));
            }
            var parameters = method.Name + (method.IsStatic ? "" : "/{id}");
            var post = false;
            foreach (var parameter in parametersList)
            {
                if (parameter.Info.ParameterType == typeof(DBTransaction))
                    continue;
                if (parameter.ValueName == prStream)
                {
                    yield return SF.AttributeList(
                         SF.SingletonSeparatedList(
                             SF.Attribute(
                                 SF.IdentifierName("DisableFormValueModelBinding"))));
                    post = true;
                    continue;
                }
                if (parameter.Type != typeof(string)
                    && !parameter.Type.IsValueType)
                {
                    post = true;
                    continue;
                }
                parameters += $"/{{{parameter.Info.Name}}}";
            }
            var attributeArgument = SF.AttributeArgument(
                SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(parameters)));
            yield return SF.AttributeList(
                         SF.SingletonSeparatedList(
                             SF.Attribute(
                                 SF.IdentifierName("Route")).WithArgumentList(
                                 SF.AttributeArgumentList(SF.SingletonSeparatedList(attributeArgument)))));
            yield return SF.AttributeList(
                         SF.SingletonSeparatedList(
                             SF.Attribute(
                                 SF.IdentifierName(post ? "HttpPost" : "HttpGet"))));
        }

        private ClassDeclarationSyntax GenPropertyInvoker(string name, string definitionName, string propertyName, string propertyType, string holder, bool canWrite, List<AttributeListSyntax> invokerAttributes)
        {
            invokerAttributes.AddRange(GenPropertyInvokerAttribute(definitionName, propertyName, $"{holder}.{name}<>"));
            var nullable = propertyType.IndexOf("?") > -1;
            return SF.ClassDeclaration(
                     attributeLists: SF.List<AttributeListSyntax>(),
                     modifiers: SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)),
                     identifier: SF.Identifier(name + "<T>"),
                     typeParameterList: null,
                     baseList: SF.BaseList(SF.SingletonSeparatedList<BaseTypeSyntax>(
                            SF.SimpleBaseType(SF.ParseTypeName($"{(nullable ? "Nullable" : "")}Invoker<T, {(nullable ? propertyType.Replace("?", "") : propertyType)}>")))),
                     constraintClauses: SF.List(new TypeParameterConstraintClauseSyntax[] {
                         SF.TypeParameterConstraintClause(
                             name: SF.IdentifierName("T"),
                             constraints: SF.SeparatedList(new TypeParameterConstraintSyntax[] {
                                 SF.TypeConstraint(SF.ParseTypeName(definitionName))
                             }))
                     }),
                     members: SF.List(GenPropertyInvokerMemebers(name, propertyName, propertyType, definitionName, canWrite)));
        }

        private IEnumerable<MemberDeclarationSyntax> GenPropertyInvokerMemebers(string name, string propertyName, string propertyType, string definitionName, bool canWrite)
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
                   expressionBody: SF.ArrowExpressionClause(SF.ParseExpression(canWrite ? "true" : "false")),
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
                   body: canWrite ? null : SF.Block(SF.ParseStatement("")),
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
                   expressionBody: canWrite ? SF.ArrowExpressionClause(SF.ParseExpression($"target.{propertyName} = value")) : null,
                   semicolonToken: canWrite ? SF.Token(SyntaxKind.SemicolonToken) : SF.Token(SyntaxKind.None));

        }

        private IEnumerable<AttributeListSyntax> GenPropertyInvokerAttribute(string definitionName, string propertyName, string invokerName)
        {
            yield return SF.AttributeList(
                SF.SingletonSeparatedList(
                    SF.Attribute(
                        SF.IdentifierName("assembly: Invoker"))
                    .WithArgumentList(
                        SF.AttributeArgumentList(
                            SF.SingletonSeparatedList(
                                SF.AttributeArgument(
                                    SF.ParseExpression($"typeof({definitionName}), nameof({definitionName}.{propertyName}), typeof({invokerName})")))))));
        }

        private List<MethodParametrInfo> GetParametersInfo(MethodInfo method, TableGenerator table, Dictionary<string, UsingDirectiveSyntax> usings)
        {
            var parametersInfo = new List<MethodParametrInfo>();
            foreach (var parameter in method.GetParameters())
            {
                var methodParameter = new MethodParametrInfo { Info = parameter };
                parametersInfo.Add(methodParameter);
                SyntaxHelper.AddUsing(methodParameter.Info.ParameterType, usings);
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

        private IEnumerable<ParameterSyntax> GenControllerMethodParameters(MethodInfo method, TableGenerator table, List<MethodParametrInfo> parametersInfo)
        {
            if (!method.IsStatic)
            {
                yield return SF.Parameter(attributeLists: SF.List(GenParameterAttributes()),
                                                             modifiers: SF.TokenList(),
                                                             type: SF.ParseTypeName(TypeHelper.FormatCode(table.PrimaryKey?.GetDataType() ?? typeof(int))),
                                                             identifier: SF.Identifier("id"),
                                                             @default: null);
            }

            foreach (var methodParameter in parametersInfo)
            {
                if (methodParameter.Info.ParameterType == typeof(DBTransaction)
                    || methodParameter.ValueName == prStream)
                {
                    continue;
                }
                yield return SF.Parameter(attributeLists: SF.List(GenParameterAttributes(methodParameter)),
                                                         modifiers: SF.TokenList(),
                                                         type: SF.ParseTypeName(TypeHelper.FormatCode(methodParameter.Type)),
                                                         identifier: SF.Identifier(methodParameter.Info.Name),
                                                         @default: null);
            }
        }

        private IEnumerable<AttributeListSyntax> GenParameterAttributes(MethodParametrInfo methodParameter = null)
        {
            var type = methodParameter == null
                || methodParameter.Type.IsValueType
                || methodParameter.Type == typeof(string)
                ? ControllerParameterType.Route
                : ControllerParameterType.Body;
            if (methodParameter?.Attribute != null)
                type = methodParameter.Attribute.Type;
            if (type == ControllerParameterType.Route)
                yield return SF.AttributeList(
                             SF.SingletonSeparatedList(
                             SF.Attribute(
                             SF.IdentifierName("FromRoute"))));
            else if (type == ControllerParameterType.Body)
                yield return SF.AttributeList(
                             SF.SingletonSeparatedList(
                             SF.Attribute(
                             SF.IdentifierName("FromBody"))));
            else if (type == ControllerParameterType.Query)
                yield return SF.AttributeList(
                             SF.SingletonSeparatedList(
                             SF.Attribute(
                             SF.IdentifierName("FromQuery"))));
        }



        //private void GetMethod(StringBuilder builder, MethodInfo method, TableAttributeCache table)
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
        public TableGenerator Table { get; internal set; }
        public string ValueName { get; internal set; }
        public ControllerParameterAttribute Attribute { get; private set; }

        public ParameterInfo Info
        {
            get => info;
            set
            {
                info = value;
                Type = info?.ParameterType;
                ValueName = info?.Name;
                Attribute = Info.GetCustomAttribute<ControllerParameterAttribute>();
                if (TypeHelper.IsBaseType(Type, typeof(DBItem))
                    && (Attribute == null || Attribute.Type != ControllerParameterType.Body))
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