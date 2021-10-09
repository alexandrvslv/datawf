namespace DataWF.Common
{
    public class WebTableItemList<T> : ChangeableList<T>, IWebTableItemList
    {
        public WebTableItemList(IWebClient client)
        {
            Client = client;
        }

        public IWebClient Client { get; }
    }
}