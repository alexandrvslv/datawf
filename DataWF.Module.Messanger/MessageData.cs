using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Messanger
{

    [Table("dmessage_data", "Message", Keys = DBTableKeys.NoLogs), InvokerGenerator]
    public partial class MessageData : MessageDetail
    {
        public MessageData(DBTable table) : base(table)
        { }

        public IMessageDataTable MessageDataTable => (IMessageDataTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("mdata_name", Keys = DBColumnKeys.FileName)]
        public string DataName
        {
            get => GetValue<string>(MessageDataTable.DataNameKey);
            set => SetValue(value, MessageDataTable.DataNameKey);
        }

        [Column("mdata", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get => GetValue<byte[]>(MessageDataTable.DataKey);
            set => SetValue(value, MessageDataTable.DataKey);
        }
    }

}