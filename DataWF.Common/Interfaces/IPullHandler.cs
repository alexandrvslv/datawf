namespace DataWF.Common
{
    public interface IPullHandler
    {
        int Handler { get; set; }

        short Block { get; set; }

        short BlockIndex { get; set; }
    }
}

