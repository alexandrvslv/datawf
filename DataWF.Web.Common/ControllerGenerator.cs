using DataWF.Common;
using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DataWF.Web.Common
{
    public class ControllerGenerator
    {
        [Obsolete()]
        public static void Generate(DBSchema schema)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(schema.Name), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            var typeAttributes = TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout;
            foreach (var table in schema.Tables)
            {
                var itemType = table.GetType().GetGenericArguments().FirstOrDefault();
                var tableAttribute = DBTable.GetTableAttribute(itemType);
                var controllerType = typeof(DBController<>).MakeGenericType(itemType);
                var typeBuilder = moduleBuilder.DefineType(tableAttribute.ItemType.Name + "Controller", typeAttributes, controllerType);
                var apiControllerAttribute = new CustomAttributeBuilder(typeof(ApiControllerAttribute).GetConstructor(Type.EmptyTypes), new object[] { });
                typeBuilder.SetCustomAttribute(apiControllerAttribute);
                var routeAttribute = new CustomAttributeBuilder(typeof(RouteAttribute).GetConstructor(new[] { typeof(string) }), new object[] { "api/[controller]" });
                typeBuilder.SetCustomAttribute(routeAttribute);

                var constructor = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            }
        }

        public static void GenerateRoslyn(DBSchema schema)
        {
            var name = schema.Name.ToInitcap('_');
            using (var file = new FileStream("DBController.cs", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var sourceText = SourceText.From(file, Encoding.UTF8);
                var tree = CSharpSyntaxTree.ParseText(sourceText);
                var node = tree.GetRoot();
                var classNode = node.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                var usingNodes = node.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();
                if (classNode != null)
                {
                    string baseClassName = classNode.Identifier.Text;

                    foreach (var table in schema.Tables)
                    {
                        var itemType = table.GetType().GetGenericArguments().FirstOrDefault();
                        var tableAttribute = DBTable.GetTableAttribute(itemType);
                        var controllerType = typeof(DBController<>).MakeGenericType(itemType);

                        string controllerClassName = $"{tableAttribute.ItemType.Name}Controller";
                        // Only for demo purposes, pluralizing an object is done by
                        // simply adding the "s" letter. Consider proper algorithms
                        string newImplementation =
                          $@"public namespace DataWF.Web.{name} 
{{
[Route(""api /[controller]"")]
 [ApiController]
public class {controllerClassName} : {baseClassName}<{itemType.FullName}>
{{
public {controllerClassName}() {{
// default ctor
}}
//TODO Methods from itemType
}}
}}
";
                        var newTree = CSharpSyntaxTree.ParseText(newImplementation);
                        var newNamespace = (NamespaceDeclarationSyntax)newTree.GetRoot();
                        newNamespace.AddUsings(usingNodes);
                        
                        
                    }
                }
                else
                {
                    return ;
                }
            }
        }
    }
}
