using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DataWF.Common.Generator
{
    internal class InvokerGenerator : BaseGenerator
    {
        protected IEnumerable<IPropertySymbol> properties;
        private HashSet<string> baseInvokers;

        public InvokerGenerator(CompilationContext compilationContext) : base(compilationContext)
        {
        }

        public IEnumerable<IPropertySymbol> Properties { get => properties; set => properties = value; }

        public bool ForceInstance { get; set; }

        public override bool Process()
        {
            Properties = TypeSymbol.GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(symbol => !symbol.IsOverride
                                && !symbol.IsIndexer
                                && !symbol.IsStatic
                                && !symbol.Name.Contains('.'))
                        .ToList();

            if (Properties == null || !Properties.Any())
            {
                return false;
            }

            var source = Generate();
            CompilationContext.Context.AddSource($"{TypeSymbol.ContainingNamespace.ToDisplayString()}.{TypeSymbol.Name}{(TypeSymbol.TypeParameters.Count())}InvokersGen.cs",
                SourceText.From(source, Encoding.UTF8));
            return true;
        }

        public string Generate()
        {
            string namespaceName = TypeSymbol.ContainingNamespace.ToDisplayString();
            var className = TypeSymbol.Name;

            var attributeData = TypeSymbol.GetAttribute(Attributes.Invoker);
            var argInstance = attributeData?.GetNamedValue("Instance") ?? default(TypedConstant);
            var isInstance = ForceInstance || !argInstance.IsNull && (bool)argInstance.Value;
            var genericArgs = TypeSymbol.IsGenericType ? $"<{string.Join(", ", TypeSymbol.TypeParameters.Select(p => p.Name))}>" : string.Empty;
            // begin building the generated source
            source = new StringBuilder($@"{(namespaceName != "DataWF.Common" ? "using DataWF.Common;" : "")}
using {namespaceName};
");

            baseInvokers = new HashSet<string>(GetBaseInvokers(TypeSymbol).Select(p => p.Name));

            source.Append($@"
namespace {namespaceName}
{{
    public partial class {className}{genericArgs}
    {{");

            foreach (IPropertySymbol propertySymbol in properties)
            {
                attributeData = propertySymbol.GetAttribute(Attributes.Invoker);
                var argIgnore = attributeData?.GetNamedValue("Ignore") ?? default(TypedConstant);
                var isIgnore = !argIgnore.IsNull && (bool)argIgnore.Value;
                if (!isIgnore)
                {
                    ProcessInvokerClass(propertySymbol, genericArgs, isInstance);
                }
            }
            source.Append(@"}
}");
            return source.ToString();
        }

        private void ProcessInvokerClass(IPropertySymbol propertySymbol, string genericArgs, bool instance)
        {
            // get the name and type of the field
            string propertyName = propertySymbol.Name;
            ITypeSymbol propertyType = propertySymbol.Type;

            var invokerName = $"{ propertyName }Invoker";
            int nameIndex = 1;
            while (baseInvokers.Contains(invokerName))
            {
                invokerName = $"{ propertyName }New{ nameIndex++ }Invoker";
            }

            ProcessInvokerAttributes(propertySymbol);
            var instanceCache = instance ? $@"
            public static readonly {invokerName} Instance = new {invokerName}();" : "";
            source.Append($@"
        public class {invokerName}: Invoker<{TypeSymbol.Name}{genericArgs}, {propertyType}>
        {{{instanceCache}
            public override string Name => nameof({TypeSymbol.Name}{genericArgs}.{propertyName});
            public override bool CanWrite => {(!propertySymbol.IsReadOnly ? "true" : "false")};
            public override {propertyType} GetValue({TypeSymbol.Name}{genericArgs} target) => target.{propertyName};
            public override void SetValue({TypeSymbol.Name}{genericArgs} target, {propertyType} value){(propertySymbol.IsReadOnly ? "{}" : $" => target.{propertyName} = value;")}
        }}
");
        }

        private IEnumerable<INamedTypeSymbol> GetBaseInvokers(INamedTypeSymbol containingType)
        {
            while (containingType != null)
            {
                foreach (var type in containingType.GetMembers().OfType<INamedTypeSymbol>())
                {
                    yield return type;
                }
                containingType = containingType.BaseType;
            }
        }

        private void ProcessInvokerAttributes(IPropertySymbol propertySymbol)
        {
            // get the name and type of the field
            string propertyName = propertySymbol.Name;
            ITypeSymbol propertyType = propertySymbol.Type;
            string genericArgs = TypeSymbol.IsGenericType ? $"<{string.Join("", Enumerable.Repeat(",", TypeSymbol.TypeParameters.Length - 1))}>" : "";
            string nameGenericArgs = TypeSymbol.IsGenericType ? "<object>" : "";
            source.Append($@"
        [Invoker(nameof({propertyName}))]");//typeof({TypeSymbol.Name}{genericArgs}), 
        }
    }

}