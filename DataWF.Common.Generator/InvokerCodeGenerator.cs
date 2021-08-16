using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DataWF.Common.Generator
{
    public class InvokerCodeGenerator : CodeGenerator
    {
        private static readonly DiagnosticDescriptor diagnosticDescriptor = new DiagnosticDescriptor("DWFCG001", "Couldn't generate Invokers", "Couldn't generate Invokers {0} {1}", nameof(InvokerCodeGenerator), DiagnosticSeverity.Warning, true);

        protected IEnumerable<IPropertySymbol> properties;
        protected INamedTypeSymbol atributeType;

        public InvokerCodeGenerator(ref GeneratorExecutionContext context, Compilation compilation) : base(ref context, compilation)
        {
        }

        public override Compilation Compilation
        {
            get => base.Compilation;
            set
            {
                base.Compilation = value;
                AtributeType = Compilation.GetTypeByMetadataName("DataWF.Common.InvokerGeneratorAttribute");
            }
        }

        public IEnumerable<IPropertySymbol> Properties { get => properties; set => properties = value; }
        public INamedTypeSymbol AtributeType { get => atributeType; set => atributeType = value; }

        public override bool Process(INamedTypeSymbol classSymbol)
        {
            try
            {
                ClassSymbol = classSymbol;
                if (classSymbol == null)
                    return false;
                Properties = ClassSymbol.GetMembers()
                            .OfType<IPropertySymbol>()
                            .Where(symbol => !symbol.IsOverride
                                    && !symbol.IsIndexer
                                    && !symbol.IsStatic
                                    && !symbol.Name.Contains('.'))
                            .ToList();

                if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
                {
                    return false; //TODO: issue a diagnostic that it must be top level
                }
                if (Properties == null || !Properties.Any())
                {
                    return false;
                }

                var source = Generate();//
                Context.AddSource($"{classSymbol.ContainingNamespace.ToDisplayString()}.{classSymbol.Name}{(classSymbol.TypeParameters.Count())}InvokersGen.cs", SourceText.From(source.ToString(), Encoding.UTF8));
                return true;

            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debugger.Launch();
                context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, Location.None, classSymbol.Name, ex.Message));
            }
            return false;
        }

        public override string Generate()
        {
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;

            var attributeData = classSymbol.GetAttribute(atributeType);
            var argInstance = attributeData?.GetNamedValue("Instance") ?? default(TypedConstant);
            var isInstance = !argInstance.IsNull && (bool)argInstance.Value;
            var genericArgs = classSymbol.IsGenericType ? $"<{string.Join(", ", classSymbol.TypeParameters.Select(p => p.Name))}>" : string.Empty;
            // begin building the generated source
            source = new StringBuilder($@"{(namespaceName != "DataWF.Common" ? "using DataWF.Common;" : "")}
using {namespaceName};
");

            foreach (IPropertySymbol propertySymbol in properties)
            {
                ProcessInvokerAttributes(propertySymbol);
            }

            source.Append($@"
namespace {namespaceName}
{{
    public partial class {className}{genericArgs}
    {{");

            foreach (IPropertySymbol propertySymbol in properties)
            {
                attributeData = propertySymbol.GetAttribute(atributeType);
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

            var invokerName = $"{propertyName}Invoker";
            var instanceCache = instance ? $@"
            public static readonly {invokerName} Instance = new {invokerName}();" : "";
            source.Append($@"
        public class {invokerName}: Invoker<{classSymbol.Name}{genericArgs}, {propertyType}>
        {{{instanceCache}
            public override string Name => nameof({classSymbol.Name}{genericArgs}.{propertyName});
            public override bool CanWrite => {(!propertySymbol.IsReadOnly ? "true" : "false")};
            public override {propertyType} GetValue({classSymbol.Name}{genericArgs} target) => target.{propertyName};
            public override void SetValue({classSymbol.Name}{genericArgs} target, {propertyType} value){(propertySymbol.IsReadOnly ? "{}" : $" => target.{propertyName} = value;")}
        }}
");
        }

        private void ProcessInvokerAttributes(IPropertySymbol propertySymbol)
        {
            // get the name and type of the field
            string propertyName = propertySymbol.Name;
            ITypeSymbol propertyType = propertySymbol.Type;
            string genericArgs = classSymbol.IsGenericType ? $"<{string.Join("", Enumerable.Repeat(",", classSymbol.TypeParameters.Length - 1))}>" : "";
            string nameGenericArgs = classSymbol.IsGenericType ? "<object>" : "";
            source.Append($@"[assembly: Invoker(typeof({classSymbol.Name}{genericArgs}), ""{propertyName}"", typeof({classSymbol.Name}{genericArgs}.{propertyName}Invoker))]
");
        }
    }

}