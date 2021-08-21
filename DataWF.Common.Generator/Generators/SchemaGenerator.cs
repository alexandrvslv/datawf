using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DataWF.Common.Generator;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace DataWF.Common.Generator
{
    internal class SchemaGenerator : BaseTableGenerator
    {
        private string namespaceName;
        private string className;
        private AttributeData schemaAttribute;
        private object schemaName;
        private IEnumerable<ITypeSymbol> schemaEntries;
        private IEnumerable<ITypeSymbol> allSchemaEntries;
        private string baseInterface;

        public SchemaGenerator(ref GeneratorExecutionContext context, InvokerGenerator invokerGenerator)
            : base(ref context, invokerGenerator)
        {
        }

        public SchemaLogGenerator SchemaLogCodeGenerator { get; set; }

        public override bool Process()
        {
            string classSource = Generate();
            if (classSource != null)
            {
                Context.AddSource($"{TypeSymbol.ContainingNamespace.ToDisplayString()}.{TypeSymbol.Name}SchemaGen.cs", SourceText.From(classSource, Encoding.UTF8));

                var invokerAttribute = TypeSymbol.GetAttribute(Attributes.Invoker);
                if (invokerAttribute == null)
                {
                    //InvokerCodeGenerator.Process(classSymbol);
                }

                SchemaLogCodeGenerator?.Process(TypeSymbol);

                return true;
            }
            return false;
        }

        public string Generate()
        {
            namespaceName = TypeSymbol.ContainingNamespace.ToDisplayString();
            className = TypeSymbol.Name;

            schemaAttribute = TypeSymbol.GetAttribute(Attributes.Schema);
            schemaName = schemaAttribute?.ConstructorArguments.FirstOrDefault().Value;

            schemaEntries = TypeSymbol.GetAttributes(Attributes.SchemaEntry)
                                .Select(p => p.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol)
                                .Where(p => p != null && !p.IsAbstract);
            allSchemaEntries = GetAllSchemaEntries();
            baseInterface = "IDBSchema";
            if (TypeSymbol.BaseType != null && TypeSymbol.BaseType.Name != "DBSchema")
            {
                baseInterface = $"{TypeSymbol.BaseType.ContainingNamespace.ToDisplayString()}.I{TypeSymbol.BaseType.Name}";
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
namespace { namespaceName}
{{
    public partial class {className}: I{className}
    {{");
            foreach (var schemaEntry in schemaEntries)
            {
                var tableTypeName = schemaEntry.IsSealed ? $"{schemaEntry.Name}Table" : $"{schemaEntry.Name}Table<{schemaEntry.Name}>";
                source.Append($@"
        private {tableTypeName} _{schemaEntry.Name};");

                source.Append($@"
        [JsonIgnore]
        public {tableTypeName} {schemaEntry.Name} => _{schemaEntry.Name} ??= ({tableTypeName})GetTable<{schemaEntry.Name}>();");
            }

            ProcessGenerateMethod();

            source.Append($@"
    }}");
            source.Append($@"
    public partial interface I{className}: {baseInterface}
    {{");
            foreach (var schemaEntry in schemaEntries)
            {
                var tableTypeName = schemaEntry.IsSealed ? $"{schemaEntry.Name}Table" : $"{schemaEntry.Name}Table<{schemaEntry.Name}>";
                source.Append($@"
        {tableTypeName} {schemaEntry.Name} {{ get; }}");
            }
            source.Append($@"
    }}
}}");
            return source.ToString();
        }

        private IEnumerable<INamedTypeSymbol> GetAllSchemaEntries()
        {
            return TypeSymbol.GetAllAttributes(Attributes.SchemaEntry)
                .Select(p => p.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol)
                .Where(p => p != null && !p.IsAbstract);
        }

        private void ProcessGenerateMethod()
        {
            source.Append($@"

        public override void Generate(string name)
        {{
            Name = name ?? ""{schemaName}"";");
            if (allSchemaEntries.Any())
            {
                source.Append($@"
            Generate(new Type[]
            {{");
                foreach (var schemaEntry in allSchemaEntries)
                {
                    source.Append($@"
                typeof({schemaEntry.ContainingNamespace.ToDisplayString()}.{schemaEntry.Name}),");
                }

                source.Append($@"
            }});");
            }

            source.Append($@"
        }}");
        }
    }
}