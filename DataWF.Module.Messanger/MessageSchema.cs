using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;

namespace DataWF.Module.Messanger
{
    [Schema("message_schema")]
    [SchemaEntry(typeof(Message))]
    [SchemaEntry(typeof(MessageAddress))]
    [SchemaEntry(typeof(MessageData))]
    public partial class MessageSchema : CommonSchema
    {

    }
}

