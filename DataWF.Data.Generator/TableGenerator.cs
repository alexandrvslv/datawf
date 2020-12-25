using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DataWF.Data.Generator
{
    [Generator]
    public class TableGenerator : ISourceGenerator
    {

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the attribute text
            //context.AddSource("AutoNotifyAttribute", SourceText.From(attributeText, Encoding.UTF8));

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            //Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));

            // get the newly bound attributes
            var attributes = new AttributesCache();
            attributes.Column = context.Compilation.GetTypeByMetadataName("DataWF.Data.ColumnAttribute");
            attributes.Table = context.Compilation.GetTypeByMetadataName("DataWF.Data.TableAttribute");
            attributes.ItemType = context.Compilation.GetTypeByMetadataName("DataWF.Data.ItemTypeAttribute");
            attributes.Culture = context.Compilation.GetTypeByMetadataName("DataWF.Data.CultureKeyAttribute");
            attributes.Reference = context.Compilation.GetTypeByMetadataName("DataWF.Data.ReferenceAttribute");


            // loop over the candidate fields, and keep the ones that are actually annotated
            List<IPropertySymbol> propertySymbols = new List<IPropertySymbol>();
            foreach (PropertyDeclarationSyntax property in receiver.CandidateFields)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(property.SyntaxTree);
                IPropertySymbol propertySymbol = model.GetDeclaredSymbol(property);
                propertySymbols.Add(propertySymbol);
            }

            // group the fields by class, and generate the source
            foreach (IGrouping<INamedTypeSymbol, IPropertySymbol> group in propertySymbols.GroupBy(f => f.ContainingType))
            {
                string classSource = ProcessTable(group.Key, group.ToList(), attributes, context);
                if (classSource != null)
                {
                    context.AddSource($"{group.Key.Name}Table.cs", SourceText.From(classSource, Encoding.UTF8));
                }
            }
        }

        private string ProcessTable(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, AttributesCache attributes, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            var tableAttribyte = classSymbol.GetAttributes().SingleOrDefault(p => p.AttributeClass.Equals(attributes.Table, SymbolEqualityComparer.Default));
            if (tableAttribyte != null)
            {

                var className = classSymbol.Name + "Table";
                var typeName = tableAttribyte.NamedArguments.SingleOrDefault(p => p.Key == "Type").Value;
                if (!typeName.IsNull)
                {
                    //typeName = 
                }
                // begin building the generated source
                StringBuilder source = new StringBuilder($@"
using DataWF.Common;
using DataWF.Data;
using {namespaceName};
");

                source.Append($@"
namespace {namespaceName}
{{
    public partial class {className} : DBTable<{classSymbol.Name}>
    {{
");

                // create properties for each field 
                foreach (IPropertySymbol propertySymbol in properties)
                {
                    ProcessColumnField(source, propertySymbol, attributes);
                }
                source.Append("\n");
                foreach (IPropertySymbol propertySymbol in properties)
                {
                    ProcessColumnProperty(source, propertySymbol, attributes);
                }
                source.Append(@"
    } 
}");
                return source.ToString();
            }
            return null;
        }

//        private void ProcessInvokerClass(StringBuilder source, IPropertySymbol propertySymbol, string className)
//        {
//            // get the name and type of the field
//            string propertyName = propertySymbol.Name;
//            ITypeSymbol propertyType = propertySymbol.Type;
//            source.Append($@"
//        public class {propertyName}Invoker: Invoker<{propertySymbol.ContainingType.Name}, {propertyType}>
//        {{
//            public override string Name => nameof({propertySymbol.ContainingType.Name}.{propertyName});
//            public override bool CanWrite => {(!propertySymbol.IsReadOnly ? "true" : "false")};
//            public override {propertyType} GetValue({propertySymbol.ContainingType.Name} target) => target.{propertyName};
//            public override void SetValue({propertySymbol.ContainingType.Name} target, {propertyType} value){(propertySymbol.IsReadOnly? "{}":$" => target.{propertyName} = value;")}
//        }}
//");

//        }

//        private void ProcessInvokerAttributes(StringBuilder source, IPropertySymbol propertySymbol, string className)
//        {
//            // get the name and type of the field
//            string propertyName = propertySymbol.Name;
//            ITypeSymbol propertyType = propertySymbol.Type;
//            source.Append($@"
//[assembly: Invoker(typeof({propertySymbol.ContainingType.Name}), nameof({propertySymbol.ContainingType.Name}.{propertyName}), typeof({className}.{propertyName}Invoker))]");
//        }

        private void ProcessColumnField(StringBuilder source, IPropertySymbol propertySymbol, AttributesCache attributes)
        {
            // get the name and type of the field
            string propertyName = propertySymbol.Name;
            ITypeSymbol propertyType = propertySymbol.Type;
            string keyFieldName = $"_{propertyName}Key";

            // get the AutoNotify attribute from the field, and any associated data
            var columnAttribute = propertySymbol.GetAttributes().SingleOrDefault(ad => ad.AttributeClass.Equals(attributes.Column, SymbolEqualityComparer.Default));
            if (columnAttribute != null)
            {
                TypedConstant overridenPropertyType = columnAttribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == "DataType").Value;
                if (!overridenPropertyType.IsNull)
                    propertyType = (ITypeSymbol)overridenPropertyType.Value;

                source.Append($@"
        private DBColumn<{propertyType}> {keyFieldName};");
            }

            var cultureAttribute = propertySymbol.GetAttributes().SingleOrDefault(ad => ad.AttributeClass.Equals(attributes.Culture, SymbolEqualityComparer.Default));
            if (cultureAttribute != null)
            {
                source.Append($@"
        private DBColumn<{propertyType}> {keyFieldName};");
            }
        }

        private void ProcessColumnProperty(StringBuilder source, IPropertySymbol propertySymbol, AttributesCache attributes)
        {
            // get the name and type of the field
            string propertyName = propertySymbol.Name;
            ITypeSymbol propertyType = propertySymbol.Type;
            string keyFieldName = $"_{propertyName}Key";

            // get the AutoNotify attribute from the field, and any associated data
            AttributeData columnAttribute = propertySymbol.GetAttributes().SingleOrDefault(ad => ad.AttributeClass.Equals(attributes.Column, SymbolEqualityComparer.Default));
            if (columnAttribute != null)
            {
                TypedConstant overridenPropertyType = columnAttribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == "DataType").Value;
                if (!overridenPropertyType.IsNull)
                    propertyType = (ITypeSymbol)overridenPropertyType.Value;

                string keyProeprtyName = $"{(propertyName.EndsWith("Id") && propertyName.Length > 4 ? propertyName.Substring(0, propertyName.Length - 2) : propertyName) }Key";
                source.Append($@"
        public DBColumn<{propertyType}> {keyProeprtyName} => ParseProperty(nameof({propertySymbol.ContainingType.Name}.{propertyName}), ref {keyFieldName});
");
            }

            var cultureAttribute = propertySymbol.GetAttributes().SingleOrDefault(ad => ad.AttributeClass.Equals(attributes.Culture, SymbolEqualityComparer.Default));
            if (cultureAttribute != null)
            {
                string keyProeprtyName = $"{propertyName}Key";
                source.Append($@"
        public DBColumn<{propertyType}> {keyProeprtyName} => ParseProperty(nameof({propertySymbol.ContainingType.Name}.{propertyName}), ref {keyFieldName});
");
            }
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<PropertyDeclarationSyntax> CandidateFields { get; } = new List<PropertyDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is PropertyDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    CandidateFields.Add(fieldDeclarationSyntax);
                }
            }
        }

        class AttributesCache
        {
            public INamedTypeSymbol Column;
            public INamedTypeSymbol Table;
            public INamedTypeSymbol ItemType;
            public INamedTypeSymbol Culture;
            public INamedTypeSymbol Reference;

        }
    }


}