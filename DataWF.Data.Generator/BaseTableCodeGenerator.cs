using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using DataWF.Common.Generator;

namespace DataWF.Data.Generator
{
    internal abstract class BaseTableCodeGenerator : CodeGenerator
    {
        protected AttributesCache attributes;
        protected List<string> cultures;
        protected List<IPropertySymbol> properties;

        public BaseTableCodeGenerator(ref GeneratorExecutionContext context, Compilation compilation) : base(ref context, compilation)
        {
        }

        public override Compilation Compilation
        {
            get => base.Compilation;
            set
            {
                base.Compilation = value;
                Attributes = new AttributesCache(Compilation);
            }
        }

        public AttributesCache Attributes { get => attributes; set => attributes = value; }
        public List<string> Cultures { get => cultures; set => cultures = value; }
        protected List<IPropertySymbol> Properties { get => properties; set => properties = value; }
    }

}