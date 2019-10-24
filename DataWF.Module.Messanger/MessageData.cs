/*
 Message.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>  

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Messanger
{
    public class MessageDataList : DBTableView<MessageAddress>
    {
        public MessageDataList(string filter)
            : base(MessageAddress.DBTable, filter)
        {
            //_ApplySort(new DBRowComparer(FlowEnvir.Config.StageParameter.Table, FlowEnvir.Config.StageParameter.Table.PrimaryKey.Code, ListSortDirection.Ascending));
        }

        public MessageDataList()
            : this(string.Empty)
        { }

        public MessageDataList(Message message)
            : this($"({MessageData.DBTable.ParseProperty(nameof(MessageData.MessageId)).Name} = {message.PrimaryId})")
        { }
    }

    [DataContract, Table("dmessage_data", "Message", IsLoging = false)]
    public class MessageData : MessageDetail
    {
        public static readonly DBTable<MessageData> DBTable = GetTable<MessageData>();
        public static readonly DBColumn DataNameKey = DBTable.ParseProperty(nameof(DataName));
        public static readonly DBColumn DataKey = DBTable.ParseProperty(nameof(Data));

        public MessageData()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [DataMember, Column("mdata_name")]
        public string DataName
        {
            get => GetValue<string>(DataNameKey);
            set => SetValue(value, DataNameKey);
        }

        [DataMember, Column("mdata")]
        public byte[] Data
        {
            get => GetValue<byte[]>(DataKey);
            set => SetValue(value, DataKey);
        }
    }

}