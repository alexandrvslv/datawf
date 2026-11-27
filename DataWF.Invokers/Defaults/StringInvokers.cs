using System;

namespace DataWF.Invokers.Defaults;

[Invoker(ToGenerate = typeof(string), Instance = true)]
public partial class StringInvokers
{
    public class StringConstructor : RuntimeConstructor<string>
    {
        public static readonly StringConstructor Instance = new();

        public override string Types => string.Empty;

        public override string Create() => string.Empty;

        public override string Create(params object[]? args) => string.Empty;
    }

}
