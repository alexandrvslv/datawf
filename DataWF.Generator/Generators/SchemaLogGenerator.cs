using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Text;

namespace DataWF.Generator
{
    internal class SchemaLogGenerator : SchemaGenerator
    {
        private object? baseSchemaName;
        private string schemaName;

        public SchemaLogGenerator(TypeRecordProvider provider, INamedTypeSymbol typeSymbol)
            : base(provider, typeSymbol)
        {
            var schemaAttribute = typeSymbol.GetAttribute(Attributes.Schema);
            baseSchemaName = schemaAttribute?.ConstructorArguments.FirstOrDefault().Value;
            schemaName = baseSchemaName + "_log";
        }

        public override bool ProcessInternal(ref SourceProductionContext context)
        {
            Generate(ref context);
            return true;
        }

        public string Generate(ref SourceProductionContext context)
        {
            var namespaceName = Namespace;
            var className = $"{TypeName}Log";


            var baseInterface = Helper.cIModelLogSchema;
            if (Type.BaseType != null
                && !string.Equals(Type.BaseType.Name, Helper.cModelSchema, StringComparison.Ordinal))
            {
                baseInterface = $"{Type.BaseType.Namespace}.I{Type.BaseType.Name}Log";
            }

            var baseClass = Helper.cModelLogSchema;
            if (Type.BaseType != null
                && !string.Equals(Type.BaseType.Name, Helper.cModelSchema, StringComparison.Ordinal))
            {
                baseClass = $"{Type.BaseType.Namespace}.{Type.BaseType.Name}Log";
            }
            var namespaces = schemaEntries.Select(p => p.Type.Namespace)
                .Union(new[] { Helper.cSystem, "System.Text.Json.Serialization", "DataWF", "DataWF.Data" })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(p => p, StringComparer.Ordinal);

            source = new StringBuilder();

            foreach (var @namespace in namespaces)
            {
                if (@namespace == "<global namespace>")
                    continue;
                source.Append($"""

                    using {@namespace};
                    """);
            }
            source.Append($$"""

                namespace {{namespaceName}}
                {
                    [Schema("{{schemaName}}")]
                """);
            source.Append($$"""

                    public partial class {{className}}: {{baseClass}}
                    {
                        public {{className}}()
                        {
                            TargetSchemaName = "{{baseSchemaName}}";
                        }

                        [JsonIgnore]
                        public new I{{TypeName}} TargetSchema
                        {
                            get => (I{{TypeName}})base.TargetSchema;
                            set => base.TargetSchema = value;
                        }
                """);
            foreach (var schemaEntry in schemaEntries)
            {
                var typeName = $"{schemaEntry.Type.Name}Log";
                var fullTypeName = $"{schemaEntry.Type.Namespace}.{typeName}";//{(schemaEntry.Is)}
                var typeValue = schemaEntry.Type.GenericParameters?.FirstOrDefault()?.Type
                    ?? schemaEntry.Type.BaseType?.GenericParameters?.FirstOrDefault()?.Type;
                if (typeValue == null)
                    continue;
                source.Append($"""

                        {typeName}Log? _{schemaEntry.Name}Log;
                        {typeName}Log? {schemaEntry.Name}Log => _{schemaEntry.Name}Log ??= ({typeName}Log)TargetSchema.{schemaEntry.Name}.LogSchema;
                    """);
            }

            source.Append($$"""

                        public override DBSchemaLog NewLogSchema()
                        {
                            return new {{className}}
                                {
                                    Name = Name + "_log",
                                    Connection = connection,
                                    TargetSchema = this
                                };
                        }    
                    }

                    public partial class {{TypeName}}
                    { 
                        public {{TypeName}}()
                        {
                            logSchemaName="{{schemaName}}";
                        }

                        [JsonIgnore]
                        public new I{{className}} LogSchema
                        {
                            get => (I{{className}})base.LogSchema;
                            set => base.LogSchema = value;
                        }

                        public override DBSchemaLog NewLogSchema()
                        {
                            return new {{className}}
                                {
                                    Name = Name + "_log",
                                    Connection = connection,
                                    TargetSchema = this
                                };
                        }
                    }
                }
                """);
            return source.ToString();
        }
    }

}