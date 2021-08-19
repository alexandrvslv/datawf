using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DataWF.Common.Generator;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace DataWF.Data.Generator
{
    internal class SchemaCodeGenerator : BaseTableCodeGenerator
    {
        private string namespaceName;
        private string className;
        private AttributeData schemaAttribute;
        private object schemaName;
        private IEnumerable<ITypeSymbol> schemaEntries;
        private List<ITypeSymbol> allSchemaEntries;
        private string baseInterface;

        public SchemaCodeGenerator(ref GeneratorExecutionContext context, Compilation compilation)
            : base(ref context, compilation)
        {
            InvokerCodeGenerator = new InvokerCodeGenerator(ref context, compilation);
        }

        public SchemaLogCodeGenerator SchemaLogCodeGenerator { get; set; }

        public override bool Process(INamedTypeSymbol classSymbol)
        {
            TypeSymbol = classSymbol;
            string classSource = Generate();
            if (classSource != null)
            {
                try
                {
                    Context.AddSource($"{classSymbol.ContainingNamespace.ToDisplayString()}.{classSymbol.Name}SchemaGen.cs", SourceText.From(classSource, Encoding.UTF8));

                    var invokerAttribute = TypeSymbol.GetAttribute(InvokerCodeGenerator.AtributeType);
                    if (invokerAttribute == null)
                    {
                        //InvokerCodeGenerator.Process(classSymbol);
                    }

                    if (SchemaLogCodeGenerator?.Process(classSymbol) == true)
                        Compilation = SchemaLogCodeGenerator.Compilation;
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Generator Fail: {ex.Message} at {ex.StackTrace}");
#if DEBUG
                    if (!System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debugger.Launch();
                    }
#endif
                }
            }
            return false;
        }

        public override string Generate()
        {
            namespaceName = typeSymbol.ContainingNamespace.ToDisplayString();
            className = typeSymbol.Name;

            schemaAttribute = typeSymbol.GetAttribute(Attributes.Schema);
            schemaName = schemaAttribute?.ConstructorArguments.FirstOrDefault().Value;

            schemaEntries = typeSymbol.GetAttributes(Attributes.SchemaEntry)
                                .Select(p => p.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol)
                                .Where(p => p != null && !p.IsAbstract);
            allSchemaEntries = GetAllSchemaEntries();
            baseInterface = "IDBSchema";
            if (typeSymbol.BaseType != null && typeSymbol.BaseType.Name != "DBSchema")
            {
                baseInterface = $"{typeSymbol.BaseType.ContainingNamespace.ToDisplayString()}.I{typeSymbol.BaseType.Name}";
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

        private List<ITypeSymbol> GetAllSchemaEntries()
        {
            var list = new List<ITypeSymbol>();
            var symbol = typeSymbol;
            while (symbol != null)
            {
                list.AddRange(symbol.GetAttributes(Attributes.SchemaEntry)
                .Select(p => p.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol)
                .Where(p => p != null && !p.IsAbstract));
                symbol = symbol.BaseType;
            }
            return list;
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