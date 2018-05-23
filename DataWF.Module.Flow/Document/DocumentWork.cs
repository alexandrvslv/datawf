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

using DataWF.Data;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using DataWF.Module.Common;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    public class DocumentWorkList : DBTableView<DocumentWork>
    {
        [NonSerialized()]
        Document doc;

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

        public override void OnListChanged(ListChangedType type, int newIndex = -1, int oldIndex = -1, string property = null)
        {
            base.OnListChanged(type, newIndex, oldIndex, property);
            if (document != null && (property == null || property == DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.IsComplete)).Name))
                document.RefreshCache();
        }
    }

    [DataContract, Table("ddocument_work", "Document", BlockSize = 400)]
    public class DocumentWork : DocumentDetail
    {
        public static DBTable<DocumentWork> DBTable
        {
            get { return DBService.GetTable<DocumentWork>(); }
        }

        public static DocumentWork Empty = new DocumentWork();

        public DocumentWork()
        {
            Build(DBTable);
        }

        [Browsable(false)]
        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetProperty<long?>(nameof(Id)); }
            set { SetProperty(value, nameof(Id)); }
        }

        [Browsable(false)]
        [DataMember, Column("stage_id", Keys = DBColumnKeys.View), Index("ddocument_work_stage_id")]
        public int? StageId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(StageId))]
        public Stage Stage
        {
            get { return GetPropertyReference<Stage>(); }
            set
            {
                SetPropertyReference(value);
                Work = value?.Work;
                IsSystem = value?.Keys != null && (value.Keys & StageKey.IsSystem) == StageKey.IsSystem;
                IsStart = value?.Keys != null && (value.Keys & StageKey.IsStart) == StageKey.IsStart;
                IsStop = value?.Keys != null && (value.Keys & StageKey.IsStop) == StageKey.IsStop;
            }
        }

        [Browsable(false)]
        [DataMember, Column("work_id"), Index("ddocument_work_work_id")]
        public int? WorkId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get { return GetPropertyReference<Work>(); }
            set { SetPropertyReference(value); }
        }

        [Browsable(false)]
        [DataMember, Column("user_id", Keys = DBColumnKeys.View), Index("ddocument_work_user_id")]
        public int? UserId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetPropertyReference<User>(); }
            set
            {
                SetPropertyReference(value);
                Position = value?.Position;
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [DataMember, Column("position_id", Keys = DBColumnKeys.View), Index("ddocument_work_position_id")]
        public int? PositionId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get { return GetPropertyReference<Position>(); }
            set
            {
                SetPropertyReference(value);
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [DataMember, Column("department_id", Keys = DBColumnKeys.View), Index("ddocument_work_department_id")]
        public int? DepartmentId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get { return GetPropertyReference<Department>(); }
            set { SetPropertyReference(value); }
        }

        [Browsable(false)]
        [DataMember, Column("from_id")]
        public long? FromId
        {
            get { return GetProperty<long?>(nameof(FromId)); }
            set { SetProperty(value, nameof(FromId)); }
        }

        [Reference(nameof(FromId))]
        public DocumentWork From
        {
            get { return GetPropertyReference<DocumentWork>(); }
            set { SetPropertyReference(value); }
        }

        [Browsable(false)]
        [DataMember, Column("date_read")]
        public DateTime? DateRead
        {
            get { return GetProperty<DateTime?>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("date_complete")]
        public DateTime? DateComplete
        {
            get { return GetProperty<DateTime?>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("date_limit")]
        public DateTime? DateLimit
        {
            get { return GetProperty<DateTime?>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("is_complete", Default = "False")]
        public bool IsComplete
        {
            get { return DateComplete != null; }
        }

        [Browsable(false)]
        [DataMember, Column("is_system")]
        public bool? IsSystem
        {
            get { return GetProperty<bool?>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("is_start")]
        public bool? IsStart
        {
            get { return GetProperty<bool?>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("is_stop")]
        public bool? IsStop
        {
            get { return GetProperty<bool?>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("description", 2048)]
        public string Description
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [Browsable(false)]
        public bool IsCurrent
        {
            get
            {
                return !IsComplete && (User == User.CurrentUser
                  || (User == null && Position == User.CurrentUser?.Position)
                  || (Position == null && Department == User.CurrentUser?.Department));
            }
        }

        [Browsable(false)]
        public bool IsUser
        {
            get { return User == null || User == User.CurrentUser; }
        }

        [Browsable(false)]
        public bool IsCheck
        {
            get { return DateLimit != null; }
        }

        [Browsable(false)]
        public bool IsResend { get; set; }


        public DBItem Staff
        {
            get { return (DBItem)User ?? (DBItem)Position ?? (DBItem)Department; }
            set
            {
                if (value is Department)
                    Department = (Department)value;
                else if (value is Position)
                    Position = (Position)value;
                else if (value is User)
                    User = (User)value;
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
