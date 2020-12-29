using DataWF.Data;
using DataWF.Module.Common;

namespace DataWF.Module.Messanger
{
    public class MessageList : DBTableView<Message>
    {
        public MessageList(MessageTable table, string filter = "")
            : base(table, filter)
        {
            //_ApplySort(new DBRowComparer(FlowEnvir.Config.StageParameter.Table, FlowEnvir.Config.StageParameter.Table.PrimaryKey.Code, ListSortDirection.Ascending));
        }

        public MessageList(MessageTable table, User fromUser, User user)
            : this(table, $"({table.UserIdKey.Name} = {user.Id} and {table.IdKey.Name} in (select {table.IdKey.Name} from {table.Name} where {table.UserIdKey.Name} = {fromUser.Id})) or ({table.UserIdKey.Name} = {fromUser.Id} and {table.IdKey.Name} in(select {table.IdKey.Name} from {table.Name} where {table.UserIdKey.Name} = {user.Id}))")
        { }
    }
}

