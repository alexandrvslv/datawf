using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [Table("ddocument_work", "Document", BlockSize = 400), InvokerGenerator]
    public sealed partial class DocumentWork : DocumentItem
    {
        public static DocumentWork Empty = new DocumentWork(null);
        private Stage stage;
        private Work work;
        private User user;
        private Department department;
        private DocumentWork from;

        public DocumentWork(DBTable table) : base(table)
        {
        }

        [Index("ddocument_work_document_id")]
        public override long? DocumentId
        {
            get => base.DocumentId;
            set => base.DocumentId = value;
        }

        [Browsable(false)]
        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long Id
        {
            get => GetValue<long>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false)]
        [Column("stage_id", Keys = DBColumnKeys.View), Index("ddocument_work_stage_id")]
        public int? StageId
        {
            get => GetValue<int?>(Table.StageIdKey);
            set => SetValue(value, Table.StageIdKey);
        }

        [Reference(nameof(StageId))]
        public Stage Stage
        {
            get => GetReference(Table.StageIdKey, ref stage);
            set
            {
                SetReference(stage = value, Table.StageIdKey);
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
            get => GetValue<int?>(Table.WorkIdKey);
            set => SetValue(value, Table.WorkIdKey);
        }

        [Reference(nameof(WorkId))]
        public Work Work
        {
            get => GetReference(Table.WorkIdKey, ref work);
            set => SetReference(work = value, Table.WorkIdKey);
        }

        [Browsable(false)]
        [Column("user_id", Keys = DBColumnKeys.View), Index("ddocument_work_user_id")]
        public int? UserId
        {
            get => GetValue<int?>(Table.UserIdKey);
            set => SetValue(value, Table.UserIdKey);
        }

        [Reference(nameof(UserId))]
        public User User
        {
            get => GetReference(Table.UserIdKey, ref user);
            set
            {
                SetReference(user = value, Table.UserIdKey);
                Position = value?.Position;
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [Column("position_id", Keys = DBColumnKeys.View), Index("ddocument_work_position_id")]
        public int? PositionId
        {
            get => GetValue<int?>(Table.PositionIdKey);
            set => SetValue(value, Table.PositionIdKey);
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get => GetReference(Table.PositionIdKey, ref position);
            set
            {
                SetReference(position = value, Table.PositionIdKey);
                Department = value?.Department;
            }
        }

        [Browsable(false)]
        [Column("department_id", Keys = DBColumnKeys.View), Index("ddocument_work_department_id")]
        public int? DepartmentId
        {
            get => GetValue<int?>(Table.DepartmentIdKey);
            set => SetValue(value, Table.DepartmentIdKey);
        }

        private Position position;

        [Reference(nameof(DepartmentId))]
        public Department Department
        {
            get => GetReference(Table.DepartmentIdKey, ref department);
            set => SetReference(department = value, Table.DepartmentIdKey);
        }

        [Browsable(false)]
        [Column("from_id"), Index("ddocument_work_from_id")]
        public long? FromId
        {
            get => GetValue<long?>(Table.FromIdKey);
            set => SetValue(value, Table.FromIdKey);
        }

        [Reference(nameof(FromId))]
        public DocumentWork From
        {
            get => GetReference(Table.FromIdKey, ref from);
            set => SetReference(from = value, Table.FromIdKey);
        }

        [Browsable(false)]
        [Column("date_read")]
        public DateTime? DateRead
        {
            get => GetValue<DateTime?>(Table.DateReadKey);
            set => SetValue(value, Table.DateReadKey);
        }

        [Column("date_limit")]
        public DateTime? DateLimit
        {
            get => GetValue<DateTime?>(Table.DateLimitKey);
            set => SetValue(value, Table.DateLimitKey);
        }

        [Column("date_complete"), Index("ddocument_work_date_complete")]
        public DateTime? DateComplete
        {
            get => GetValue<DateTime?>(Table.DateCompleteKey);
            set
            {
                SetValue(value, Table.DateCompleteKey);
                if (value != null && !Completed)
                {
                    IsComplete = true;
                }
            }
        }

        [Column("is_complete"), Index("ddocument_work_is_complete"), DefaultValue(false)]
        public bool? IsComplete
        {
            get => GetValue<bool?>(Table.IsCompleteKey);
            set
            {
                SetValue(value, Table.IsCompleteKey);
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
            get => GetValue<bool?>(Table.IsSystemKey);
            set => SetValue(value, Table.IsSystemKey);
        }

        [Column("is_start")]
        public bool? IsStart
        {
            get => GetValue<bool?>(Table.IsStartKey);
            set => SetValue(value, Table.IsStartKey);
        }

        [Column("is_stop")]
        public bool? IsStop
        {
            get => GetValue<bool?>(Table.IsStopKey);
            set => SetValue(value, Table.IsStopKey);
        }

        [Column("description", 2048)]
        public string Description
        {
            get => GetValue<string>(Table.DescriptionKey);
            set => SetValue(value, Table.DescriptionKey);
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

        protected override void RaisePropertyChanged(string property)
        {
            base.RaisePropertyChanged(property);
            if (Attached)
            {
                document?.OnReferenceChanged(this);
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

