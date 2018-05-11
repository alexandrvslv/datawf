namespace DataWF.Common
{
    public interface IDictionaryItem
    {
        object Key { get; set; }
        object Value { get; set; }
        void Reset();
        void Fill(object value);
    }

}
