using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DataWF.Common.Generator;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace DataWF.Common.Generator
{
    internal class SchemaLogGenerator : ExtendedGenerator
    {
        public SchemaLogGenerator(CompilationContext compilationContext, InvokerGenerator invokerGenerator)
            : base(compilationContext, invokerGenerator)
        {
            SchemaGenerator = new SchemaGenerator(compilationContext, invokerGenerator);
        }
        public SchemaGenerator SchemaGenerator { get; }

        public override bool Process()
        {
            string classSource = Generate();
            if (classSource != null)
            {
                var logSchemaSource = SourceText.From(classSource, Encoding.UTF8);
                CompilationContext.Context.AddSource($"{TypeSymbol.ContainingNamespace.ToDisplayString()}.{TypeSymbol.Name}SchemaLogGen.cs", logSchemaSource);

                var logSchemaSyntax = CSharpSyntaxTree.ParseText(logSchemaSource, (CSharpParseOptions)Options);

                SchemaGenerator.Cultures = Cultures;
                CompilationContext.Compilation = SchemaGenerator.Compilation.AddSyntaxTrees(logSchemaSyntax);

                var unitSyntax = (CompilationUnitSyntax)logSchemaSyntax.GetRoot();
                var logClassSyntax = unitSyntax.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Members.OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (logClassSyntax != null)
                {
                    SchemaGenerator.Process(logClassSyntax);
                }
                return true;
            }
            return false;
        }

        public string Generate()
        {
            var namespaceName = TypeSymbol.ContainingNamespace.ToDisplayString();
            var className = $"{TypeSymbol.Name}Log";

            var schemaAttribute = TypeSymbol.GetAttribute(Attributes.Schema);
            var baseSchemaName = schemaAttribute?.ConstructorArguments.FirstOrDefault().Value;
            var schemaName = baseSchemaName + "_log";

            var schemaEntries = TypeSymbol.GetAttributes(Attributes.SchemaEntry)
                                .Select(p => p.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol)
                                .Where(p => p != null && !p.IsAbstract);
            var baseInterface = Helper.cIDBSchemaLog;
            if (TypeSymbol.BaseType != null
                && !string.Equals(TypeSymbol.BaseType.Name, Helper.cDBSchema, StringComparison.Ordinal))
            {
                baseInterface = $"{TypeSymbol.BaseType.ContainingNamespace.ToDisplayString()}.I{TypeSymbol.BaseType.Name}Log";
            }

            var baseClass = Helper.cDBSchemaLog;
            if (TypeSymbol.BaseType != null
                && !string.Equals(TypeSymbol.BaseType.Name, Helper.cDBSchema, StringComparison.Ordinal))
            {
                baseClass = $"{TypeSymbol.BaseType.ContainingNamespace.ToDisplayString()}.{TypeSymbol.BaseType.Name}Log";
            }
            var namespaces = schemaEntries.Select(p => p.ContainingNamespace.ToDisplayString())
                .Union(new[] { Helper.cSystem, "System.Text.Json.Serialization", "DataWF.Common", "DataWF.Data" })
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
        public {className}()
        {{
            TargetSchemaName = ""{baseSchemaName}"";
        }}

        [JsonIgnore]
        public new I{TypeSymbol.Name} TargetSchema
        {{
            get => (I{TypeSymbol.Name})base.TargetSchema;
            set => base.TargetSchema = value;
        }}
    }}

    public partial class {TypeSymbol.Name}
    {{ 
        public {TypeSymbol.Name}()
        {{
            logSchemaName=""{schemaName}"";
        }}

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