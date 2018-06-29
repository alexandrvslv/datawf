using DataWF.Web.Common;
using System;

namespace DataWF.Web.CodeGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string type = args.Length > 0 ? args[0] : throw new ArgumentException();
            string path = args.Length > 1 ? args[1] : throw new ArgumentException();
            string output = args.Length > 2 ? args[2] : string.Empty;
            string nameSpace = args.Length > 3 ? args[3] : null;

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
