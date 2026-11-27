using DataWF.Invokers;
using System;

namespace DataWF.Collections.Pulls;

public abstract class GenericPull<T> : Pull, IInvoker<IPullHandler, T>
{

    internal GenericPull(int blockSize) : base(blockSize)
    {
    }

    public override Type DataType => typeof(T);

    public Type TargetType => typeof(IPullHandler);

    public bool CanWrite => true;

    public string Name { get; set; } = string.Empty;

    public override object? Get(int index) => GetValue(PullHandler.FromSeqence(index, blockSize));

    public override object? Get(int block, int blockIndex) => GetValue(new PullHandler(block, blockIndex));

    public override void Set(int index, object? value) => SetValue(PullHandler.FromSeqence(index, blockSize), (T)value);

    public override void Set(int block, int blockIndex, object? value) => SetValue(new PullHandler(block, blockIndex), (T)value);

    public T GetValue(int index) => GetValue(PullHandler.FromSeqence(index, blockSize));

    public void SetValue(int index, T value) => SetValue(PullHandler.FromSeqence(index, blockSize), value);

    public abstract T GetValue(in PullHandler index);

    public abstract void SetValue(in PullHandler index, T value);

    public T GetValue(int block, int blockIndex) => GetValue(new PullHandler(block, blockIndex));

    public void SetValue(int block, int blockIndex, T value) => SetValue(new PullHandler(block, blockIndex), value);

    public T GetValue(scoped in IPullHandler target) => GetValue(target.GetRefHandler());

    public void SetValue(scoped ref IPullHandler target, T value) => SetValue(target.GetRefHandler(), value);

    public T GetValue(IPullHandler target) => GetValue(target.GetRefHandler());

    public void SetValue(IPullHandler target, T value) => SetValue(target.GetRefHandler(), value);

    public T GetValue(object target) => GetValue(((IPullHandler)target).GetRefHandler());

    public void SetValue(object target, T value) => SetValue(((IPullHandler)target).GetRefHandler(), value);

    public void SetValue(object? target, object? value) => SetValue(((IPullHandler)target!).GetRefHandler(), (T)value);

    object? IInvoker.GetValue(object? target) => GetValue(((IPullHandler)target!).GetRefHandler());
}
