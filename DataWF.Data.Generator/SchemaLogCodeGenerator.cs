using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DataWF.Common.Generator;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace DataWF.Data.Generator
{
    internal class SchemaLogCodeGenerator : BaseTableCodeGenerator
    {
        public SchemaLogCodeGenerator(ref GeneratorExecutionContext context, Compilation compilation)
            : base(ref context, compilation)
        {
            SchemaCodeGenerator = new SchemaCodeGenerator(ref context, compilation);
        }
        public SchemaCodeGenerator SchemaCodeGenerator { get; }

        public override bool Process(INamedTypeSymbol classSymbol)
        {
            ClassSymbol = classSymbol;
            string classSource = Generate();
            if (classSource != null)
            {
                try
                {
                    var logSchemaSource = SourceText.From(classSource, Encoding.UTF8);
                    Context.AddSource($"{classSymbol.ContainingNamespace.ToDisplayString()}.{classSymbol.Name}SchemaLogGen.cs", logSchemaSource);

                    var logSchemaSyntax = CSharpSyntaxTree.ParseText(logSchemaSource, (CSharpParseOptions)Options);

                    SchemaCodeGenerator.Cultures = Cultures;
                    Compilation =
                        SchemaCodeGenerator.InvokerCodeGenerator.Compilation =
                        SchemaCodeGenerator.Compilation = SchemaCodeGenerator.Compilation.AddSyntaxTrees(logSchemaSyntax);

                    var unitSyntax = (CompilationUnitSyntax)logSchemaSyntax.GetRoot();
                    var logClassSyntax = unitSyntax.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    if (logClassSyntax != null)
                    {
                        SchemaCodeGenerator.Process(logClassSyntax);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Generator Fail: {ex.Message} at {ex.StackTrace}");
                }
            }
            return false;
        }

        public override string Generate()
        {
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = $"{classSymbol.Name}Log";

            var schemaAttribute = classSymbol.GetAttribute(Attributes.Schema);
            var schemaName = schemaAttribute?.ConstructorArguments.FirstOrDefault().Value + "_log";

            var schemaEntries = classSymbol.GetAttributes(Attributes.SchemaEntry)
                                .Select(p => p.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol)
                                .Where(p => p != null && !p.IsAbstract);
            var baseInterface = "IDBSchemaLog";
            if (classSymbol.BaseType != null && classSymbol.BaseType.Name != "DBSchema")
            {
                baseInterface = $"{classSymbol.BaseType.ContainingNamespace.ToDisplayString()}.I{classSymbol.BaseType.Name}Log";
            }

            var baseClass = "DBSchemaLog";
            if (classSymbol.BaseType != null && classSymbol.BaseType.Name != "DBSchema")
            {
                baseClass = $"{classSymbol.BaseType.ContainingNamespace.ToDisplayString()}.{classSymbol.BaseType.Name}Log";
            }
            var namespaces = schemaEntries.Select(p => p.ContainingNamespace.ToDisplayString())
                .Union(new[] { "System", "System.Text.Json.Serialization", "DataWF.Data" })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(p => p, StringComparer.Ordinal);

            source = new StringBuilder();

            foreach (var @namespace in namespaces)
            {
                if (@namespace == "<global namespace>")
                    continue;
                source.Append($@"
using { @namespace };");
            }
            source.Append($@"

namespace { namespaceName }
{{
    [Schema(""{schemaName}"")]");
            foreach (var schemaEntry in schemaEntries)
            {
                var typeName = $"{schemaEntry.Name}Log";
                var fullTypeName = $"{schemaEntry.ContainingNamespace.ToDisplayString()}.{typeName}";//{(schemaEntry.Is)}
                var typeValue = Compilation.GetTypeByMetadataName(fullTypeName);
                if (typeValue == null)
                    continue;
                source.Append($@"
    [SchemaEntry(typeof({typeName}))]");
            }
            source.Append($@"    
    public partial class {className}: {baseClass}
    {{ 
        [JsonIgnore]
        public new I{classSymbol.Name} TargetSchema
        {{
            get => (I{classSymbol.Name})base.TargetSchema;
            set => base.TargetSchema = value;
        }}
    }}

    public partial class {classSymbol.Name}
    {{ 
        [JsonIgnore]
        public new I{className} LogSchema
        {{
            get => (I{className})base.LogSchema;
            set => base.LogSchema = value;
        }}

        public override DBSchemaLog NewLogSchema()
        {{
            return new {className}
                {{
                    Name = Name + ""_log"",
                    Connection = connection,
                    TargetSchema = this
                }};
        }}
    }}
}}");
            return source.ToString();
        }
    }

}