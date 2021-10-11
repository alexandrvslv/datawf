using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Collections.Generic;

namespace DataWF.Common.Generator
{
    internal class ProviderGenerator : ExtendedGenerator
    {
        private string namespaceName;
        private string className;
        private AttributeData providerAttribute;
        private IEnumerable<ITypeSymbol> schemaEntries;
        private IEnumerable<ITypeSymbol> allSchemaEntries;
        private string baseInterface;

        public ProviderGenerator(CompilationContext compilationContext, InvokerGenerator invokerGenerator)
            : base(compilationContext, invokerGenerator)
        {
        }

        public override bool Process()
        {
            string classSource = Generate();
            if (classSource != null)
            {
                CompilationContext.Context.AddSource($"{TypeSymbol.ContainingNamespace.ToDisplayString()}.{TypeSymbol.Name}ProviderGen.cs", SourceText.From(classSource, Encoding.UTF8));

                var invokerAttribute = TypeSymbol.GetAttribute(Attributes.Invoker);
                if (invokerAttribute == null)
                {
                    //InvokerCodeGenerator.Process(classSymbol);
                }

                return true;
            }
            return false;
        }

        public string Generate()
        {
            namespaceName = TypeSymbol.ContainingNamespace.ToDisplayString();
            className = TypeSymbol.Name;

            providerAttribute = TypeSymbol.GetAttribute(Attributes.Schema);

            schemaEntries = TypeSymbol.GetAttributes(Attributes.SchemaEntry)
                                .Select(p => p.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol)
                                .Where(p => p != null && !p.IsAbstract);
            allSchemaEntries = GetAllSchemaEntries();
            baseInterface = Helper.cIWebProvider;
            if (TypeSymbol.BaseType != null
                && !string.Equals(TypeSymbol.BaseType.Name, Helper.cModelProvider, StringComparison.Ordinal))
            {
                baseInterface = $"{TypeSymbol.BaseType.ContainingNamespace.ToDisplayString()}.I{TypeSymbol.BaseType.Name}";
            }
            var namespaces = schemaEntries.Select(p => p.ContainingNamespace.ToDisplayString())
                .Union(new[] { Helper.cSystem, "System.Text.Json.Serialization", "DataWF.Common" })
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
            ProcessConstructor();
            foreach (var schemaEntry in schemaEntries)
            {
                var schemaTypeName = schemaEntry.Name;
                source.Append($@"
        private {schemaTypeName} _{schemaEntry.Name};");

                source.Append($@"
        [JsonIgnore]
        public {schemaTypeName} {schemaEntry.Name} => _{schemaEntry.Name} ?? (_{schemaEntry.Name} = ({schemaTypeName})GetSchema<{schemaEntry.Name}>());");
            }

            source.Append($@"
    }}");
            source.Append($@"
    public partial interface I{className}: {baseInterface}
    {{");
            foreach (var schemaEntry in schemaEntries)
            {
                var schemaTypeName = schemaEntry.Name;
                source.Append($@"
        {schemaTypeName} {schemaEntry.Name} {{ get; }}");
            }
            source.Append($@"
    }}
}}");
            return source.ToString();
        }

        private IEnumerable<ITypeSymbol> GetAllSchemaEntries()
        {
            return TypeSymbol.GetAllAttributes(Attributes.SchemaEntry)
                .Select(p => p.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol)
                .Where(p => p != null && !p.IsAbstract);
        }

        private void ProcessConstructor()
        {
            source.Append($@"

        public {className}()
        {{");
            foreach (var schemaEntry in allSchemaEntries)
            {
                var schemaName = schemaEntry.GetAttribute(Attributes.Schema)?
                    .ConstructorArguments.FirstOrDefault().Value?.ToString()
                    ?? schemaEntry.Name;
                source.Append($@"
            Add(new {schemaEntry.ContainingNamespace.ToDisplayString()}.{schemaEntry.Name}(){{ Name = ""{schemaName}""}});");
            }
            source.Append($@"
        }}");
        }
    }
}