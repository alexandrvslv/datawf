/*
 DBTable.cs
 
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

using System;

namespace DataWF.Data
{
    public class DBLogView
    {
        public DBLogView()
        { }
        public DBLogView(DBLogItem logItem)
        {
            Id = (long)logItem.LogId;
            Date = (DateTime)logItem.DateCreate;
            Type = (DBLogType)logItem.LogType;
            UserId = logItem.UserLog?.UserId;
            User = logItem.UserLog?.DBUser?.Name;
        }
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public DBLogType Type { get; set; }

        public int? UserId { get; set; }
        public string User { get; set; }
    }
}
