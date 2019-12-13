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
            ApplySortInternal(new DBComparer<DocumentWork, long?>(DocumentWork.DBTable.PrimaryKey, ListSortDirection.Ascending));
        }

        public DocumentWorkList(Document document)
            : this(DocumentWork.DocumentKey.Name + "=" + document.PrimaryId, DBViewKeys.None)
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
            : base(DocumentWork.DBTable.Select(DocumentWork.DocumentKey, CompareType.Equal, document.PrimaryId),
                   new DBComparer<DocumentWork, long?>(DocumentWork.DBTable.PrimaryKey))
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
                || string.Equals(e.PropertyName, nameof(DocumentWork.IsComplete), StringComparison.Ordinal)))
            {
                document.RefreshCache();
            }
        }
    }

    [Table("ddocument_work", "Document", BlockSize = 400)]
    public class DocumentWork : DBItem, IDocumentDetail
    {
        public static readonly DBTable<DocumentWork> DBTable = GetTable<DocumentWork>();
        public static readonly DBColumn StageIdKey = DBTable.ParseProperty(nameof(StageId));
        public static readonly DBColumn WorkKey = DBTable.ParseProperty(nameof(WorkId));
        public static readonly DBColumn UserKey = DBTable.ParseProperty(nameof(UserId));
        public static readonly DBColumn PositionKey = DBTable.ParseProperty(nameof(PositionId));
        public static readonly DBColumn DepartmentKey = DBTable.ParseProperty(nameof(DepartmentId));
        public static readonly DBColumn FromKey = DBTable.ParseProperty(nameof(FromId));
        public static readonly DBColumn DateReadKey = DBTable.ParseProperty(nameof(DateRead));
        public static readonly DBColumn DateLimitKey = DBTable.ParseProperty(nameof(DateLimit));
        public static readonly DBColumn DateCompleteKey = DBTable.ParseProperty(nameof(DateComplete));
        public static readonly DBColumn IsCompleteKey = DBTable.ParseProperty(nameof(IsComplete));
        public static readonly DBColumn IsStartKey = DBTable.ParseProperty(nameof(IsStart));
        public static readonly DBColumn IsStopKey = DBTable.ParseProperty(nameof(IsStop));
        public static readonly DBColumn IsSystemKey = DBTable.ParseProperty(nameof(IsSystem));
        public static readonly DBColumn DescriptionKey = DBTable.ParseProperty(nameof(Description));
        public static readonly DBColumn DocumentKey = DBTable.ParseProperty(nameof(DocumentId));

        public static DocumentWork Empty = new DocumentWork();
        private Stage stage;
        private Work work;
        private User user;
        private Department department;
        private DocumentWork from;
        private Document document;

        public DocumentWork()
        {
        }

        [Browsable(false)]
        [Column("document_id"), Index("ddocument_work_document_id")]
        public virtual long? DocumentId
        {
            get => GetValue<long?>(DocumentKey);
            set => SetValue(value, DocumentKey);
        }

        [Reference(nameof(DocumentId))]
        public Document Document
        {
            get => GetReference(DocumentKey, ref document);
            set => SetReference(document = value, DocumentKey);
        }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
            if (Attached)
            {
                GetReference<Document>(DocumentKey, ref document, DBLoadParam.None)?.OnReferenceChanged(this);
            }
        }

        [Browsable(false)]
        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get => GetValue<long?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Browsable(false)]
        [Column("stage_id", Keys = DBColumnKeys.View), Index("ddocument_work_stage_id")]
        public int? StageId
        {
            get => GetValue<int?>(StageIdKey);
            set => SetValue(value, StageIdKey);
        }

        [Reference(nameof(StageId))]
        public Stage Stage
        {
            get => GetReference(StageIdKey, ref stage);
            set
            {
                SetReference(stage = value, StageIdKey);
                Work = value?.Work;
                IsSystem = value?.Keys != null && (value.Keys & StageKey.System) == StageKey.System;
                IsStart = value?.Keys != null && (value.Keys & StageKey.Start) == StageKey.Start;
                IsStop = value?.Keys != null && (value.Keys & StageKey.Stop) == StageKey.Stop;
            }
        }

        [Browsable(false)]
        [Column("work_id"), Index("ddocument_work_work_id")]
        public int? WorkId
        {
            get => GetValue<int?>(WorkKey);
            set => SetValue(value, WorkKey);
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get => GetReference(WorkKey, ref work);
            set => SetReference(work = value, WorkKey);
        }

        [Browsable(false)]
        [Column("user_id", Keys = DBColumnKeys.View), Index("ddocument_work_user_id")]
        public int? UserId
        {
            get => GetValue<int?>(UserKey);
            set => SetValue(value, UserKey);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetReference(UserKey, ref user);
            set
            {
                SetReference(user = value, UserKey);
                Position = value?.Position;
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [Column("position_id", Keys = DBColumnKeys.View), Index("ddocument_work_position_id")]
        public int? PositionId
        {
            get => GetValue<int?>(PositionKey);
            set => SetValue(value, PositionKey);
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get => GetReference(PositionKey, ref position);
            set
            {
                SetReference(position = value, PositionKey);
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [Column("department_id", Keys = DBColumnKeys.View), Index("ddocument_work_department_id")]
        public int? DepartmentId
        {
            get => GetValue<int?>(DepartmentKey);
            set => SetValue(value, DepartmentKey);
        }

        private Position position;

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get => GetReference(DepartmentKey, ref department);
            set => SetReference(department = value, DepartmentKey);
        }

        [Browsable(false)]
        [Column("from_id"), Index("ddocument_work_from_id")]
        public long? FromId
        {
            get => GetValue<long?>(FromKey);
            set => SetValue(value, FromKey);
        }

        [Reference(nameof(FromId))]
        public DocumentWork From
        {
            get => GetReference(FromKey, ref from);
            set => SetReference(from = value, FromKey);
        }

        [Browsable(false)]
        [Column("date_read")]
        public DateTime? DateRead
        {
            get => GetValue<DateTime?>(DateReadKey);
            set => SetValue(value, DateReadKey);
        }

        [Column("date_limit")]
        public DateTime? DateLimit
        {
            get => GetValue<DateTime?>(DateLimitKey);
            set => SetValue(value, DateLimitKey);
        }

        [Column("date_complete"), Index("ddocument_work_date_complete")]
        public DateTime? DateComplete
        {
            get => GetValue<DateTime?>(DateCompleteKey);
            set
            {
                SetValue(value, DateCompleteKey);
                if (value != null && !Completed)
                {
                    IsComplete = true;
                }
            }
        }

        [Column("is_complete"), Index("ddocument_work_is_complete"), DefaultValue(false)]
        public bool? IsComplete
        {
            get => GetValue<bool?>(IsCompleteKey);
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
        [Column("is_system")]
        public bool? IsSystem
        {
            get => GetValue<bool?>(IsSystemKey);
            set => SetValue(value, IsSystemKey);
        }

        [Column("is_start")]
        public bool? IsStart
        {
            get => GetValue<bool?>(IsStartKey);
            set => SetValue(value, IsStartKey);
        }

        [Column("is_stop")]
        public bool? IsStop
        {
            get => GetValue<bool?>(IsStopKey);
            set => SetValue(value, IsStopKey);
        }

        [Column("description", 2048)]
        public string Description
        {
            get => GetValue<string>(DescriptionKey);
            set => SetValue(value, DescriptionKey);
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
            get => (DBItem)User ?? (DBItem)Position ?? (DBItem)Department;
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

