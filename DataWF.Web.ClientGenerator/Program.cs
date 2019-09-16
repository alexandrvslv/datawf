using System;
using System.Collections.Generic;

namespace DataWF.Web.ClientGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var key = (string)null;
            foreach (var item in args)
            {
                if (item.Length == 0)
                    continue;
                if (item.StartsWith('-'))
                    key = item;
                else if (key != null)
                    map[key] = $"{(map.TryGetValue(key, out var value) ? value + " " : string.Empty)}{item.Trim(' ', '\'', '\"')}";
            }
            if (!map.TryGetValue("-p", out var path) && !map.TryGetValue("--path", out path))
            {
                throw new ArgumentException("Path missing, expect -p|--path path1 path2");
            }
            if (!map.TryGetValue("-o", out var output) && !map.TryGetValue("--out", out output))
            {
                throw new ArgumentException("Out missing, expect -o|--out path1 path2");
            }
            if (!map.TryGetValue("-n", out var nameSpace) && !map.TryGetValue("--namespace", out nameSpace))
            {
                //throw new ArgumentException("Nmaespace missing, expect -n|--namespace Name.Space");
            }
            var generator = new ClientGenerator(path, output, nameSpace);
            generator.Generate();
            generator.GetUnits(true);

        }
    }
}
