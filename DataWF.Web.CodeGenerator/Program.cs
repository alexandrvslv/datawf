using DataWF.Web.Common;
using System;
using System.Collections.Generic;

namespace DataWF.Web.CodeGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var map = new Dictionary<string, string>();
            var key = (string)null;
            foreach (var item in args)
            {
                if (item.Length == 0)
                    continue;
                if (item.StartsWith('-'))
                    key = item;
                else if (key != null)
                    map[key] = $"{(map.TryGetValue(key, out var value) ? value : string.Empty)} {item}";
            }
            if (!map.TryGetValue("-t", out var type) && !map.TryGetValue("--type", out type))
                throw new ArgumentException("Type missing, expect -t|--type client|controller");
            if (!map.TryGetValue("-p", out var path) && !map.TryGetValue("--path", out path))
                throw new ArgumentException("Path missing, expect -p|--path path1 path2");
            if (!map.TryGetValue("-o", out var output) && !map.TryGetValue("--out", out output))
                throw new ArgumentException("Out missing, expect -o|--out path1 path2");
            if (map.TryGetValue("-n", out var nameSpace) || map.TryGetValue("--namespace", out nameSpace))
            { }

            if (type.Equals("client", StringComparison.OrdinalIgnoreCase))
            {
                var generator = new ClientGenerator(path, output);
                generator.Generate();
                generator.GetUnits(true);
            }

            if (type.Equals("controller", StringComparison.OrdinalIgnoreCase))
            {
                var generator = new ControllerGenerator(path, output, nameSpace);
                generator.Generate();
                generator.GetUnits(true);
            }
        }
    }
}
