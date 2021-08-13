using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DataWF.Common.Generator;
using System.Linq;
using System.Diagnostics;

namespace DataWF.Data.Generator
{
    internal class SchemaCodeGenerator : BaseTableCodeGenerator
    {
        public SchemaCodeGenerator(ref GeneratorExecutionContext context, Compilation compilation) 
            : base(ref context, compilation)
        {
            InvokerCodeGenerator = new InvokerCodeGenerator(ref context, compilation);
        }

        public SchemaLogCodeGenerator SchemaLogCodeGenerator { get; set; }

        public override bool Process(INamedTypeSymbol classSymbol)
        {
            ClassSymbol = classSymbol;
            string classSource = Generate();
            if (classSource != null)
            {
                try
                {
                    Context.AddSource($"{classSymbol.ContainingNamespace.ToDisplayString()}.{classSymbol.Name}SchemaGen.cs", SourceText.From(classSource, Encoding.UTF8));

                    var invokerAttribute = ClassSymbol.GetAttribute(InvokerCodeGenerator.AtributeType);
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
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;

            var schemaAttribute = classSymbol.GetAttribute(Attributes.Schema);
            var schemaName = schemaAttribute?.ConstructorArguments.FirstOrDefault().Value;

            var schemaEntries = classSymbol.GetAttributes(Attributes.SchemaEntry)
                                .Select(p => p.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol)
                                .Where(p => p != null && !p.IsAbstract);
            var baseInterface = "IDBSchema";
            if (classSymbol.BaseType != null && classSymbol.BaseType.Name != "DBSchema")
            {
                baseInterface = $"{classSymbol.BaseType.ContainingNamespace.ToDisplayString()}.I{classSymbol.BaseType.Name}";
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
            source.Append($@"

        public override void Generate(string name)
        {{
            base.Generate(name);");
            if (schemaName != null)
            {                
                source.Append($@"
            if(string.IsNullOrEmpty(name))
                Name = ""{schemaName}"";");
            }
            if (schemaEntries.Any())
            {
                source.Append($@"
            Generate(new Type[]
            {{");
                foreach (var schemaEntry in schemaEntries)
                {
                    source.Append($@"
                typeof({schemaEntry.Name}),");
                }

                source.Append($@"
            }});");
            }
            source.Append($@"
        }}
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
    }

}