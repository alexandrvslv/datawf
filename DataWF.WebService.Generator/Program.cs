using System;
using System.Collections.Generic;

namespace DataWF.WebService.Generator
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
                if (item.StartsWith("-", StringComparison.Ordinal))
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
            CodeGeneratorMode mode = CodeGeneratorMode.Controllers;
            if (map.TryGetValue("-m", out var modeText) || map.TryGetValue("--mode", out modeText))
            {
                if (!Enum.TryParse<CodeGeneratorMode>(modeText, out mode))
                    throw new ArgumentException("Mode missing, expect -m|--mode Controllers|,|Logs|,|Invokers");
            }

            var generator = new ServiceGenerator(path, output, nameSpace);
            generator.Mode = mode;
            generator.Generate();
            generator.GetUnits(true);

        }
    }
}
