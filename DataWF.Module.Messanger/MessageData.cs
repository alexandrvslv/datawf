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

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("mdata_name", Keys = DBColumnKeys.FileName)]
        public string DataName
        {
            get => GetValue<string>(Table.DataNameKey);
            set => SetValue(value, Table.DataNameKey);
        }

        [Column("mdata", Keys = DBColumnKeys.File)]
        public byte[] Data
        {
            get => GetValue<byte[]>(Table.DataKey);
            set => SetValue(value, Table.DataKey);
        }
    }

}