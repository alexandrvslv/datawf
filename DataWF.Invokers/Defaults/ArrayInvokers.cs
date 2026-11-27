using System;

namespace DataWF.Invokers.Defaults;

public class ArrayInvokers<T> : IInvokerRegister
{
    public static void RegisterInvokers()
    {
        InvokeProvider.RegisterInvoker<T[]?>(LengthInvoker.Instance);
        InvokeProvider.RegisterInvoker<T[]?>(LongLengthInvoker.Instance);
        InvokeProvider.RegisterInvoker<T[]?>(RankInvoker.Instance);
        InvokeProvider.RegisterConstructor(ArrayConstructor.Instance);
    }

    public class ArrayConstructor : RuntimeConstructor<T[]>, IRuntimeConstructor<T[], int>
    {
        public static readonly ArrayConstructor Instance = new();
        public override string Types => nameof(Int32);

        public override T[] Create() => Array.Empty<T>();

        public override T[] Create(params object[]? args)
        {
            return args?[0] is int length ? new T[length] : throw new ArgumentOutOfRangeException();
        }

        public T[] Create(int length) => new T[length];
    }

    public class LengthInvoker : Invoker<T[]?, int>
    {
        public static readonly LengthInvoker Instance = new();

        public override string Name => nameof(Array.Length);

        public override bool CanWrite => false;

        public override int GetValue(scoped in T[]? target) => target?.Length ?? 0;

        public override void SetValue(scoped ref T[]? target, int value) { }
    }

    public class LongLengthInvoker : Invoker<T[]?, long>
    {
        public static readonly LongLengthInvoker Instance = new();

        public override string Name => nameof(Array.LongLength);

        public override bool CanWrite => false;

        public override long GetValue(scoped in T[]? target) => target?.LongLength ?? 0L;

        public override void SetValue(scoped ref T[]? target, long value) { }
    }

    public class RankInvoker : Invoker<T[]?, int>
    {
        public static readonly RankInvoker Instance = new();

        public override string Name => nameof(Array.Rank);

        public override bool CanWrite => false;

        public override int GetValue(scoped in T[]? target) => target?.Rank ?? 0;

        public override void SetValue(scoped ref T[]? target, int value) { }
    }
}
