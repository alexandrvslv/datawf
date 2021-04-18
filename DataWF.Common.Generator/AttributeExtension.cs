using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DataWF.Common.Generator
{
    public static class AttributeExtension
    {
        public static TypedConstant GetNamedValue(this AttributeData attributeData, string name)
        {
            return attributeData.NamedArguments.FirstOrDefault(kvp => string.Equals(kvp.Key, name, StringComparison.Ordinal)).Value;
        }

        public static AttributeData GetAttribute(this ISymbol symbol, INamedTypeSymbol type)
        {
            return symbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass.Equals(type, SymbolEqualityComparer.Default));
        }

        public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, INamedTypeSymbol type)
        {
            return symbol.GetAttributes().Where(ad => ad.AttributeClass.Equals(type, SymbolEqualityComparer.Default));
        }

    }

}