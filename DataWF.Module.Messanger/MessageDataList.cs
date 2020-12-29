using DataWF.Data;

namespace DataWF.Module.Messanger
{
    public class MessageDataList : DBTableView<MessageData>
    {
        public MessageDataList(MessageDataTable<MessageData> table, string filter = "")
            : base(table, filter)
        {
            //_ApplySort(new DBRowComparer(FlowEnvir.Config.StageParameter.Table, FlowEnvir.Config.StageParameter.Table.PrimaryKey.Code, ListSortDirection.Ascending));
        }

        public MessageDataList(MessageDataTable<MessageData> table, Message message)
            : this(table, $"({table.MessageIdKey.Name} = {message.PrimaryId})")
        { }
    }

}