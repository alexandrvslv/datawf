using System;

namespace DataWF.Web.ClientGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = args.Length > 0 ? args[0] : throw new ArgumentException();
            string destination = args.Length > 1 ? args[1] : string.Empty;

            var generator = new Common.ClientGenerator();
            generator.Generate(url, destination).Wait();
        }
    }
}
