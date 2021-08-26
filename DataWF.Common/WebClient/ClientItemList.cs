namespace DataWF.Common
{
    public class ClientItemList<T> : ChangeableList<T>, IClientItemList
    {
        public ClientItemList(IClient client)
        {
            Client = client;
        }

        public IClient Client { get; }
    }
}