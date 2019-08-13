namespace DataWF.Common
{
    public interface IStatusable
    {
        DBStatus Status { get; set; }
    }


    public class StatusableStatusInvoker<T> : Invoker<T, DBStatus>
    {
        public static readonly StatusableStatusInvoker<T> Default = new StatusableStatusInvoker<T>();
        public override string Name => nameof(IStatusable.Status);
        public override bool CanWrite => true;

        public override DBStatus GetValue(T target) => target is IStatusable statusable ? statusable.Status : DBStatus.Empty;

        public override void SetValue(T target, DBStatus value)
        {
            if (target is IStatusable statusable)
                statusable.Status = value;
        }
    }
}

