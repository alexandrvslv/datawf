using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using System;

namespace DataWF.Generator
{
    internal abstract class PropertiesGenerator : BaseGenerator
    {
        public PropertiesGenerator(
            TypeRecordProvider provider,
            INamedTypeSymbol typeSymbol,
            TakeBaseMode takeBase,
            Func<IPropertySymbol, bool> validateProperty)
            : base(provider, typeSymbol, takeBase, validateProperty)
        { }

        public PropertiesGenerator(TypeRecordProvider provider, TypeRecord type)
            : base(provider, type)
        { }

        protected EquatableList<PropertyRecord>? Properties { get => Type.Properties; }

    }

}