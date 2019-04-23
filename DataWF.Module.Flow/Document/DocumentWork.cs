/*
 DocumentWork.cs
 
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

using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    public class DocumentWorkList : DBTableView<DocumentWork>
    {
        readonly Document doc;

        public DocumentWorkList(string filter = "", DBViewKeys mode = DBViewKeys.None)
            : base(filter, mode)
        {
            ApplySortInternal(new DBComparer<DocumentWork>(DocumentWork.DBTable.PrimaryKey, ListSortDirection.Ascending));
        }

        public DocumentWorkList(Document document)
            : this(DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DocumentId)).Name + "=" + document.PrimaryId, DBViewKeys.None)
        {
            this.doc = document;
        }

        public override int AddInternal(DocumentWork item)
        {
            if (doc != null && item.DocumentId == null)
                item.Document = doc;
            return base.AddInternal(item);
        }

        public override string ToString()
        {
            string s = "";
            foreach (DocumentWork tos in this)
                s += tos.ToString() + "\n";
            return s.TrimEnd(new char[] { '\n' });
        }
    }

    public class ListDocumentWork : SelectableList<DocumentWork>
    {
        private Document document;

        public ListDocumentWork(Document document)
            : base(DocumentWork.DBTable.Select(DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DocumentId)), CompareType.Equal, document.PrimaryId),
                   new DBComparer(DocumentWork.DBTable.PrimaryKey))
        {
            this.document = document;
        }

        public override int AddInternal(DocumentWork item)
        {
            if (Contains(item))
                return -1;
            if (item.Document == null)
                item.Document = document;
            var index = base.AddInternal(item);
            item.Attach();
            return index;
        }

        public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemPropertyChanged(sender, e);
            if (document != null
                && (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == nameof(DocumentWork.IsComplete)))
            {
                document.RefreshCache();
            }
        }
    }

    [DataContract, Table("ddocument_work", "Document", BlockSize = 400)]
    public class DocumentWork : DocumentDetail<DocumentWork>
    {
        private static DBColumn stageIdKey = DBColumn.EmptyKey;
        private static DBColumn workKey = DBColumn.EmptyKey;
        private static DBColumn userKey = DBColumn.EmptyKey;
        private static DBColumn positionKey = DBColumn.EmptyKey;
        private static DBColumn departmentKey = DBColumn.EmptyKey;
        private static DBColumn fromKey = DBColumn.EmptyKey;
        private static DBColumn dateReadKey = DBColumn.EmptyKey;
        private static DBColumn dateLimitKey = DBColumn.EmptyKey;
        private static DBColumn dateCompleteKey = DBColumn.EmptyKey;
        private static DBColumn isCompleteKey = DBColumn.EmptyKey;
        private static DBColumn isStartKey = DBColumn.EmptyKey;
        private static DBColumn isStopKey = DBColumn.EmptyKey;
        private static DBColumn isSystemKey = DBColumn.EmptyKey;
        private static DBColumn descriptionKey = DBColumn.EmptyKey;

        public static DBColumn StageIdKey => DBTable.ParseProperty(nameof(StageId), ref stageIdKey);
        public static DBColumn WorkKey => DBTable.ParseProperty(nameof(WorkId), ref workKey);
        public static DBColumn UserKey => DBTable.ParseProperty(nameof(UserId), ref userKey);
        public static DBColumn PositionKey => DBTable.ParseProperty(nameof(PositionId), ref positionKey);
        public static DBColumn DepartmentKey => DBTable.ParseProperty(nameof(DepartmentId), ref departmentKey);
        public static DBColumn FromKey => DBTable.ParseProperty(nameof(FromId), ref fromKey);
        public static DBColumn DateReadKey => DBTable.ParseProperty(nameof(DateRead), ref dateReadKey);
        public static DBColumn DateLimitKey => DBTable.ParseProperty(nameof(DateLimit), ref dateLimitKey);
        public static DBColumn DateCompleteKey => DBTable.ParseProperty(nameof(DateComplete), ref dateCompleteKey);
        public static DBColumn IsCompleteKey => DBTable.ParseProperty(nameof(IsComplete), ref isCompleteKey);
        public static DBColumn IsStartKey => DBTable.ParseProperty(nameof(IsStart), ref isStartKey);
        public static DBColumn IsStopKey => DBTable.ParseProperty(nameof(IsStop), ref isStopKey);
        public static DBColumn IsSystemKey => DBTable.ParseProperty(nameof(IsSystem), ref isSystemKey);
        public static DBColumn DescriptionKey => DBTable.ParseProperty(nameof(Description), ref descriptionKey);

        public static DocumentWork Empty = new DocumentWork();
        private Stage stage;
        private Work work;
        private User user;
        private Department department;
        private DocumentWork from;

        public DocumentWork()
        {
        }

        [Browsable(false)]
        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Index("ddocument_work_document_id")]
        public override long? DocumentId { get => base.DocumentId; set => base.DocumentId = value; }

        [Browsable(false)]
        [DataMember, Column("stage_id", Keys = DBColumnKeys.View), Index("ddocument_work_stage_id")]
        public int? StageId
        {
            get { return GetValue<int?>(StageIdKey); }
            set { SetValue(value, StageIdKey); }
        }

        [Reference(nameof(StageId))]
        public Stage Stage
        {
            get { return GetReference(StageIdKey, ref stage); }
            set
            {
                stage = SetReference(value, StageIdKey);
                Work = value?.Work;
                IsSystem = value?.Keys != null && (value.Keys & StageKey.System) == StageKey.System;
                IsStart = value?.Keys != null && (value.Keys & StageKey.Start) == StageKey.Start;
                IsStop = value?.Keys != null && (value.Keys & StageKey.Stop) == StageKey.Stop;
            }
        }

        [Browsable(false)]
        [DataMember, Column("work_id"), Index("ddocument_work_work_id")]
        public int? WorkId
        {
            get { return GetValue<int?>(WorkKey); }
            set { SetValue(value, WorkKey); }
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get { return GetReference(WorkKey, ref work); }
            set { work = SetReference(value, WorkKey); }
        }

        [Browsable(false)]
        [DataMember, Column("user_id", Keys = DBColumnKeys.View), Index("ddocument_work_user_id")]
        public int? UserId
        {
            get { return GetValue<int?>(UserKey); }
            set { SetValue(value, UserKey); }
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetReference(UserKey, ref user); }
            set
            {
                user = SetReference(value, UserKey);
                Position = value?.Position;
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [DataMember, Column("position_id", Keys = DBColumnKeys.View), Index("ddocument_work_position_id")]
        public int? PositionId
        {
            get { return GetValue<int?>(PositionKey); }
            set { SetValue(value, PositionKey); }
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get { return GetReference(PositionKey, ref position); }
            set
            {
                position = SetReference(value, PositionKey);
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [DataMember, Column("department_id", Keys = DBColumnKeys.View), Index("ddocument_work_department_id")]
        public int? DepartmentId
        {
            get { return GetValue<int?>(DepartmentKey); }
            set { SetValue(value, DepartmentKey); }
        }

        private Position position;

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get { return GetReference(DepartmentKey, ref department); }
            set { department = SetReference(value, DepartmentKey); }
        }

        [Browsable(false)]
        [DataMember, Column("from_id")]
        public long? FromId
        {
            get { return GetValue<long?>(FromKey); }
            set { SetValue(value, FromKey); }
        }

        [Reference(nameof(FromId))]
        public DocumentWork From
        {
            get { return GetReference(FromKey, ref from); }
            set { from = SetReference(value, FromKey); }
        }

        [Browsable(false)]
        [DataMember, Column("date_read")]
        public DateTime? DateRead
        {
            get { return GetValue<DateTime?>(DateReadKey); }
            set { SetValue(value, DateReadKey); }
        }

        [DataMember, Column("date_limit")]
        public DateTime? DateLimit
        {
            get { return GetValue<DateTime?>(DateLimitKey); }
            set { SetValue(value, DateLimitKey); }
        }

        [DataMember, Column("date_complete"), Index("ddocument_work_date_complete")]
        public DateTime? DateComplete
        {
            get { return GetValue<DateTime?>(DateCompleteKey); }
            set
            {
                SetValue(value, DateCompleteKey);
                if (value != null && !Completed)
                {
                    IsComplete = true;
                }
            }
        }

        [DataMember, Column("is_complete"), Index("ddocument_work_is_complete"), DefaultValue(false)]
        public bool? IsComplete
        {
            get { return GetValue<bool?>(IsCompleteKey); }
            set
            {
                SetValue(value, IsCompleteKey);
                if (value ?? false && DateComplete == null)
                {
                    DateComplete = DateComplete = DateTime.Now;
                }
            }
        }

        [Browsable(false)]
        public bool Completed { get { return IsComplete ?? false; } }

        [Browsable(false)]
        [DataMember, Column("is_system")]
        public bool? IsSystem
        {
            get { return GetValue<bool?>(IsSystemKey); }
            set { SetValue(value, IsSystemKey); }
        }

        [DataMember, Column("is_start")]
        public bool? IsStart
        {
            get { return GetValue<bool?>(IsStartKey); }
            set { SetValue(value, IsStartKey); }
        }

        [DataMember, Column("is_stop")]
        public bool? IsStop
        {
            get { return GetValue<bool?>(IsStopKey); }
            set { SetValue(value, IsStopKey); }
        }

        [DataMember, Column("description", 2048)]
        public string Description
        {
            get { return GetValue<string>(DescriptionKey); }
            set { SetValue(value, DescriptionKey); }
        }

        [Browsable(false)]
        public bool IsCurrent(IUserIdentity user)
        {
            var currentUser = (User)user;
            return !Completed && (User == currentUser
              || (User == null && Position == currentUser?.Position)
              || (Position == null && Department == currentUser?.Department));
        }

        [Browsable(false)]
        public bool IsUser(IUserIdentity iser)
        {
            return User == null || User == iser;
        }

        [Browsable(false)]
        public bool IsCheck
        {
            get { return DateLimit != null; }
        }

        [Browsable(false)]
        public bool IsResend { get; set; }

        [Browsable(false)]
        public DBItem Staff
        {
            get { return (DBItem)User ?? (DBItem)Position ?? (DBItem)Department; }
            set
            {
                if (value is Department department)
                    Department = department;
                else if (value is Position position)
                    Position = position;
                else if (value is User user)
                    User = user;
            }
        }

        public override string ToString()
        {
            //if (cacheToString.Length == 0)
            //    cacheToString = string.Format("{0}-{1}",
            //        Stage != null ? Stage.Code : string.Empty,
            //        Stage != null ? Stage.Name : string.Empty);
            return base.ToString();
        }
    }
}

