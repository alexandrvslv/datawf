using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using DataWF.Common.Generator;

namespace DataWF.Common.Generator
{
    internal abstract class ExtendedGenerator : BaseGenerator
    {
        protected List<string> cultures;
        protected List<IPropertySymbol> properties;

        public ExtendedGenerator(CompilationContext compilationContext, InvokerGenerator invokerGenerator) : base(compilationContext)
        {
            InvokerGenerator = invokerGenerator;
        }

        public List<string> Cultures { get => cultures; set => cultures = value; }

        protected List<IPropertySymbol> Properties { get => properties; set => properties = value; }

        public InvokerGenerator InvokerGenerator { get; set; }

    }

}