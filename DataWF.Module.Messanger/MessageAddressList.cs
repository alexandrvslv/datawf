using DataWF.Data;

namespace DataWF.Module.Messanger
{
    public class MessageAddressList : DBTableView<MessageAddress>
    {
        public MessageAddressList(MessageAddressTable<MessageAddress> table, string filter = "")
            : base(table, filter)
        {
            //_ApplySort(new DBRowComparer(FlowEnvir.Config.StageParameter.Table, FlowEnvir.Config.StageParameter.Table.PrimaryKey.Code, ListSortDirection.Ascending));
        }

        public MessageAddressList(MessageAddressTable<MessageAddress> table, Message message)
            : this(table, $"({table.MessageIdKey.Name} = {message.PrimaryId}")
        { }
    }

}