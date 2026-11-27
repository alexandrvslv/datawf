using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataWF.Generator
{
    internal class ProviderGenerator : ExtendedGenerator
    {
        private IEnumerable<PropertyRecord> schemaEntries;
        private string baseInterface;

        public ProviderGenerator(TypeRecordProvider provider, INamedTypeSymbol typeSymbol)
            : base(provider, 
                typeSymbol,
                TakeBaseMode.Recursive | TakeBaseMode.Attribute | TakeBaseMode.Property | TakeBaseMode.PropertyInherited | TakeBaseMode.NestedType,
                static p => !p.IsStatic)
        {
            schemaEntries = Type.Properties!
                               .Where(p => p.Type.IsSubclassOf("ModelSchema"))
                               .ToList();


            baseInterface = Helper.cIWebProvider;
            if (typeSymbol.BaseType != null
                && !string.Equals(typeSymbol.BaseType.Name, Helper.cModelProvider, StringComparison.Ordinal))
            {
                baseInterface = $"{typeSymbol.BaseType.ContainingNamespace.ToDisplayString()}.I{typeSymbol.BaseType.Name}";
            }
        }

        public override bool ProcessInternal(ref SourceProductionContext context)
        {
            var source = Generate();
            context.AddSource(GetFileName(), SourceText.From(source, Encoding.UTF8));
            return true;
        }

        public string Generate()
        {
            var namespaces = schemaEntries.Select(p => p.Type.Namespace)
                .Union(new[] { Helper.cSystem, "System.Text.Json.Serialization", "DataWF" })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(p => p, StringComparer.Ordinal);

            source.Clear();

            foreach (var @namespace in namespaces)
            {
                if (@namespace == "<global namespace>")
                    continue;
                source.Append($@"
using {@namespace};");
            }
            source.Append($@"
namespace {Namespace};
public partial class {TypeName}: I{TypeName}
{{");
            ProcessConstructor();
            foreach (var schemaEntry in schemaEntries)
            {
                if (schemaEntry.IsPartial)
                    continue;
                var schemaTypeName = schemaEntry.Type.Name;
                source.Append($@"
    private {schemaTypeName} _{schemaEntry.Name};");

                source.Append($@"
    [JsonIgnore]
    public {schemaTypeName} {schemaEntry.Name} => _{schemaEntry.Name} ??= ({schemaTypeName})GetSchema<{schemaEntry.Name}>();");
            }

            source.Append($@"
}}");
            source.Append($@"
public partial interface I{TypeName}: {TypeName}
{{");
            foreach (var schemaEntry in schemaEntries)
            {
                var schemaTypeName = schemaEntry.Type.Name;
                source.Append($@"
    {schemaTypeName} {schemaEntry.Name} {{ get; }}");
            }
            source.Append($@"
}}");
            return source.ToString();
        }

        private void ProcessConstructor()
        {
            source.Append($@"

        public {TypeName}()
    {{");
            foreach (var schemaEntry in schemaEntries)
            {
                var schemaName = schemaEntry.Name;
                source.Append($@"
        Add(new {schemaEntry.Type}(){{ Name = ""{schemaName}""}});");
            }
            source.Append($@"
    }}");
        }
    }
}