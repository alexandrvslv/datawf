using System;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Collections.Generic;
using DataWF.Generator.Records;

namespace DataWF.Generator
{
    internal class SchemaGenerator : ExtendedGenerator
    {
        private object? schemaName;
        protected IEnumerable<PropertyRecord> schemaEntries;
        private string baseInterface;

        public SchemaGenerator(TypeRecordProvider provider, INamedTypeSymbol typeSymbol)
            : base(provider, 
                typeSymbol,
                takeBase: TakeBaseMode.Recursive | TakeBaseMode.Attribute | TakeBaseMode.Property | TakeBaseMode.PropertyInherited | TakeBaseMode.NestedType,
                InvokerGenerator.ValidateProperty)
        {
            var schemaAttribute = typeSymbol.GetAttribute(Attributes.Schema);
            schemaName = schemaAttribute?.ConstructorArguments.FirstOrDefault().Value;

            schemaEntries = Type.Properties!.Where(p => p.Type.IsSubclassOf("ModelTable")).ToList();
            baseInterface = Helper.cIModelSchema;
            if (typeSymbol.BaseType != null
                && !string.Equals(typeSymbol.BaseType.Name, Helper.cModelSchema, StringComparison.Ordinal))
            {
                baseInterface = $"{typeSymbol.BaseType.ContainingNamespace.ToDisplayString()}.I{typeSymbol.BaseType.Name}";
            }
        }

        public override bool ProcessInternal(ref SourceProductionContext context)
        {
            var source = Generate();
            context.AddSource(GetFileName(), source);
            if(Type.GetAttribute(Attributes.Invoker) == null)
            {
                var invoker = new InvokerGenerator(provider, Type);
                invoker.ProcessInternal(ref context);
            }
            return true;
        }

        public string Generate()
        {
            var namespaces = schemaEntries.Select(p => p.Type.Namespace)
                .Union(new[] { Helper.cSystem, "System.Text.Json.Serialization", "DataWF.Core" })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(p => p, StringComparer.Ordinal);

            source.Clear();

            foreach (var @namespace in namespaces)
            {
                if (@namespace == "<global namespace>")
                    continue;
                source.Append($"""

                    using {@namespace};
                    """);
            }
            source.Append($$"""

                namespace {{Namespace}};
                public partial class {{TypeName}}: I{{TypeName}}
                {
                """);
            foreach (var schemaProperty in schemaEntries)
            {
                var itemType = schemaProperty.Type.GenericParameters?.FirstOrDefault()?.Type
                    ?? schemaProperty.Type.BaseType?.GenericParameters?.FirstOrDefault()?.Type;
                if (itemType != null && schemaProperty.IsPartial)
                {
                    source.Append($"""
                            private {schemaProperty.Type.Name}? _{schemaProperty.Name};
                        """);

                    source.Append($"""

                            [JsonIgnore]
                            public partial {schemaProperty.Type} {schemaProperty.Name} => _{schemaProperty.Name} ??= ({schemaProperty.Type})GetTable<{itemType}>();
                        """);
                }
            }

            ProcessGenerateMethod();

            source.Append($$"""
                    }
                """);
            source.Append($$"""

                    public partial interface I{{TypeName}}: {{baseInterface}}
                    {
                """);
            foreach (var schemaProperty in schemaEntries)
            {
                source.Append($$"""
                            {{schemaProperty.Type.Name}} {{schemaProperty.Name}} { get; }
                    """);
            }
            source.Append($$"""
                    }
                }
                """);
            return source.ToString();
        }

        private void ProcessGenerateMethod()
        {
            source.Append($$"""

                        public override void Generate(string name = null)
                        {
                            Name = name ?? "{{schemaName}}";
                """);
            if (schemaEntries.Any())
            {
                source.Append($$"""
                                Generate(new Type[]
                                {
                    """);
                foreach (var schemaEntry in schemaEntries)
                {
                    source.Append($"""
                                        typeof({schemaEntry.Type.Namespace}.{schemaEntry.Type.Name}),
                        """);
                }

                source.Append($$"""
                                });
                    """);
            }

            source.Append($$"""
                        }
                """);
        }
    }
}