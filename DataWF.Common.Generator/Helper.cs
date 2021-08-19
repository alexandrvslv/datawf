using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataWF.Common.Generator
{
    public static class Helper
    {
        private static readonly Dictionary<Assembly, Dictionary<string, Type>> cacheAssemblyTypes = new Dictionary<Assembly, Dictionary<string, Type>>();
        private static readonly Dictionary<IAssemblySymbol, Dictionary<string, INamedTypeSymbol>> cacheAssemblySymbolTypes = new Dictionary<IAssemblySymbol, Dictionary<string, INamedTypeSymbol>>();
        //https://stackoverflow.com/a/24768641
        public static string ToInitcap(this string str, params char[] separator)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            var charArray = new List<char>(str.Length);
            bool newWord = true;
            foreach (Char currentChar in str)
            {
                var newChar = currentChar;
                if (Char.IsLetter(currentChar))
                {
                    if (newWord)
                    {
                        newWord = false;
                        newChar = Char.ToUpper(currentChar);
                    }
                    else
                    {
                        newChar = Char.ToLower(currentChar);
                    }
                }
                else if (separator.Contains(currentChar))
                {
                    newWord = true;
                    continue;
                }
                charArray.Add(newChar);
            }
            return new string(charArray.ToArray());
        }

        public static INamedTypeSymbol ParseType(Type value, IEnumerable<IAssemblySymbol> assemblies)
        {
            return ParseType(value.FullName, assemblies);
        }

        public static INamedTypeSymbol ParseType(string value, IEnumerable<IAssemblySymbol> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var type = ParseType(value, assembly);
                if (type != null)
                    return type;
            }
            return null;
        }

        public static INamedTypeSymbol ParseType(string value, IAssemblySymbol assembly)
        {
            var byName = value.IndexOf('.') < 0;
            if (byName)
            {
                if (!cacheAssemblySymbolTypes.TryGetValue(assembly, out var cache))
                {
                    var definedTypes = assembly.GetForwardedTypes();
                    cacheAssemblySymbolTypes[assembly] =
                        cache = new Dictionary<string, INamedTypeSymbol>(definedTypes.Length, StringComparer.Ordinal);
                    foreach (var defined in definedTypes)
                    {
                        cache[defined.Name] = defined;
                    }
                }

                return cache.TryGetValue(value, out var type) ? type : null;
            }
            else
            {
                return assembly.GetTypeByMetadataName(value);
            }
        }

        public static Type ParseType(string value, Assembly assembly)
        {
            var byName = value.IndexOf('.') < 0;
            if (byName)
            {
                if (!cacheAssemblyTypes.TryGetValue(assembly, out var cache))
                {
                    var definedTypes = assembly.GetExportedTypes();
                    cacheAssemblyTypes[assembly] =
                        cache = new Dictionary<string, Type>(definedTypes.Length, StringComparer.Ordinal);
                    foreach (var defined in definedTypes)
                    {
                        cache[defined.Name] = defined;
                    }
                }

                return cache.TryGetValue(value, out var type) ? type : null;
            }
            else
            {
                return assembly.GetType(value);
            }
        }
    }
}
