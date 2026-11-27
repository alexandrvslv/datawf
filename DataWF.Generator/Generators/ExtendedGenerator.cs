using System;
using System.Collections.Generic;
using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;

namespace DataWF.Generator
{
    internal abstract class ExtendedGenerator : PropertiesGenerator
    {
        protected List<string>? cultures;

        public ExtendedGenerator(TypeRecordProvider provider, INamedTypeSymbol typeSymbol, TakeBaseMode takeBase, Func<IPropertySymbol, bool>? validateProperty)
            : base(provider, typeSymbol, takeBase, validateProperty ?? (static p => !p.IsStatic))
        {
        }

        public List<string>? Cultures
        {
            get => cultures;
            set => cultures = value;
        }
    }
}