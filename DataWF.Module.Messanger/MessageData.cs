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
			: this(string.Format("({0} = {1}",
								 MessageData.DBTable.ParseProperty(nameof(MessageData.MessageId)).Name, message.PrimaryId))
		{ }
	}

	[Table("wf_message", "dmessage_data", "Message")]
	public class MessageData : DBItem
	{
		public static DBTable<MessageData> DBTable
		{
			get { return DBService.GetTable<MessageData>(); }
		}

		public MessageData()
		{
			Build(DBTable);
		}

		[Column("unid", Keys = DBColumnKeys.Primary)]
		public int? Id
		{
			get { return GetProperty<int?>(nameof(Id)); }
			set { SetProperty(value, nameof(Id)); }
		}

		[Browsable(false)]
		[Column("messageid")]
		public int? MessageId
		{
			get { return GetProperty<int?>(nameof(MessageId)); }
			set { SetProperty(value, nameof(MessageId)); }
		}

		[Reference("fk_mdata_messageid", nameof(MessageId))]
		public Message Message
		{
			get { return GetPropertyReference<Message>(nameof(MessageId)); }
			set { SetPropertyReference(value, nameof(MessageId)); }
		}

		[Column("mdata_name")]
		public string DataName
		{
			get { return GetProperty<string>(nameof(DataName)); }
			set { SetProperty(value, nameof(DataName)); }
		}

		[Column("mdata")]
		public byte[] Data
		{
			get { return GetProperty<byte[]>(nameof(DataName)); }
			set { SetProperty(value, nameof(DataName)); }
		}
	}

}