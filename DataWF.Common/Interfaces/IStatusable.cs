namespace DataWF.Common
{
    public interface IStatusable
    {
        DBStatus Status { get; set; }
    }


    public class StatusableStatusInvoker<T> : Invoker<T, DBStatus> where T : IStatusable
    {
        public static readonly StatusableStatusInvoker<T> Instance = new StatusableStatusInvoker<T>();
        public override string Name => nameof(IStatusable.Status);
        public override bool CanWrite => true;

        public override DBStatus GetValue(T target) => target.Status;

        public override void SetValue(T target, DBStatus value) => target.Status = value;
    }

    public class StatusableStatusInvoker : StatusableStatusInvoker<IStatusable>
    {

    }
}

