namespace DataWF.Common
{
    public interface IPullHandler
    {
        PullHandler Handler { get; }

        ref readonly PullHandler GetRefHandler();
    }
}

