namespace DataWF.Common
{
    public interface IBetween
    {
        object MaxValue();
        object MinValue();
    }

    public interface IBetween<T>
    {
        T MaxValue();
        T MinValue();
    }

    public interface IQItem : IInvoker
    { 
    }

    public interface IQBetween
    {
        IQItem MaxValue();
        IQItem MinValue();
    }
}

