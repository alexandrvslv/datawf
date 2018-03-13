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

namespace DataWF.Module.Flow
{
    public class DocumentWorkList : DBTableView<DocumentWork>
    {
        [NonSerialized()]
        Document doc;

        public DocumentWorkList(string filter = "", DBViewKeys mode = DBViewKeys.None)
            : base(DocumentWork.DBTable, filter, mode)
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

        public void FilterByDocument(Document document)
        {
            Filter = DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DocumentId)).Name + "=" + document.PrimaryId;
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
            : base(DocumentWork.DBTable.Select(DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DocumentId)), document.PrimaryId, CompareType.Equal),
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

    [Table("wf_flow", "dwork", BlockSize = 2000)]
    public class DocumentWork : DBItem
    {
        public static DBTable<DocumentWork> DBTable
        {
            get { return DBService.GetTable<DocumentWork>(); }
        }

        public static DocumentWork Empty = new DocumentWork();

        public DocumentWork()
        {
            Build(DBTable);
            IsComplete = false;
        }

        public override string ToString()
        {
            //if (cacheToString.Length == 0)
            //    cacheToString = string.Format("{0}-{1}",
            //        Stage != null ? Stage.Code : string.Empty,
            //        Stage != null ? Stage.Name : string.Empty);
            return base.ToString();
        }

        [Browsable(false)]
        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetProperty<long?>(nameof(Id)); }
            set { SetProperty(value, nameof(Id)); }
        }

        [Browsable(false)]
        [Column("documentid"), Index("dwork_documentid")]
        public long? DocumentId
        {
            get { return GetProperty<long?>(nameof(DocumentId)); }
            set { SetProperty(value, nameof(DocumentId)); }
        }

        [Reference("fk_dwork_documentid", nameof(DocumentId))]
        public Document Document
        {
            get { return GetPropertyReference<Document>(nameof(DocumentId)); }
            set { SetPropertyReference(value, nameof(DocumentId)); }
        }

        [Browsable(false)]
        [Column("stageid"), Index("dwork_stageid")]
        public int? StageId
        {
            get { return GetProperty<int?>(nameof(StageId)); }
            set { SetProperty(value, nameof(StageId)); }
        }

        [Reference("fk_dwork_stageid", nameof(StageId))]
        public Stage Stage
        {
            get { return GetPropertyReference<Stage>(nameof(StageId)); }
            set
            {
                SetPropertyReference(value, nameof(StageId));
                IsSystem = (value?.Keys & StageKey.IsSystem) == StageKey.IsSystem;
                IsStart = (value?.Keys & StageKey.IsStart) == StageKey.IsStart;
                IsStop = (value?.Keys & StageKey.IsStop) == StageKey.IsStop;
            }
        }

        [Browsable(false)]
        [Column("userid"), Index("dwork_userid")]
        public int? UserId
        {
            get { return GetProperty<int?>(nameof(UserId)); }
            set { SetProperty(value, nameof(UserId)); }
        }

        [Reference("fk_dwork_userid", nameof(UserId))]
        public User User
        {
            get { return GetPropertyReference<User>(nameof(UserId)); }
            set { SetPropertyReference(value, nameof(UserId)); }
        }

        [Browsable(false)]
        [Column("fromid")]
        public long? FromId
        {
            get { return GetProperty<long?>(nameof(FromId)); }
            set { SetProperty(value, nameof(FromId)); }
        }

        [Reference("fk_dwork_fromid", nameof(FromId))]
        public DocumentWork From
        {
            get { return GetPropertyReference<DocumentWork>(nameof(FromId)); }
            set { SetPropertyReference(value, nameof(FromId)); }
        }

        [Browsable(false)]
        [Column("dateread")]
        public DateTime? DateRead
        {
            get { return GetProperty<DateTime?>(nameof(DateRead)); }
            set { SetProperty(value, nameof(DateRead)); }
        }

        [Column("iscomplete")]
        public bool? IsComplete
        {
            get { return GetProperty<bool?>(nameof(IsComplete)); }
            set
            {
                SetProperty(value, nameof(IsComplete));
                if (value.GetValueOrDefault() && DateComplete == null)
                    DateComplete = DateTime.Now;
            }
        }

        [Column("issystem")]
        public bool? IsSystem
        {
            get { return GetProperty<bool?>(nameof(IsSystem)); }
            set { SetProperty(value, nameof(IsSystem)); }
        }

        [Column("isstart")]
        public bool? IsStart
        {
            get { return GetProperty<bool?>(nameof(IsStart)); }
            set { SetProperty(value, nameof(IsStart)); }
        }

        [Column("isstop")]
        public bool? IsStop
        {
            get { return GetProperty<bool?>(nameof(IsStop)); }
            set { SetProperty(value, nameof(IsStop)); }
        }

        [Column("datecomplete")]
        public DateTime? DateComplete
        {
            get { return GetProperty<DateTime?>(nameof(DateComplete)); }
            set { SetProperty(value, nameof(DateComplete)); }
        }

        [Column("datelimit")]
        public DateTime? DateLimit
        {
            get { return GetProperty<DateTime?>(nameof(DateLimit)); }
            set { SetProperty(value, nameof(DateLimit)); }
        }

        [Column("description", 2048)]
        public string Description
        {
            get { return GetProperty<string>(nameof(Description)); }
            set { SetProperty(value, nameof(Description)); }
        }

        public bool IsCurrent
        {
            get { return !IsComplete.Value && (User == User.CurrentUser || GroupHelper.IsParent(User.CurrentUser, User)); }
        }

        public bool IsUser
        {
            get { return User == null || (User == User.CurrentUser || GroupHelper.IsParent(User.CurrentUser, User)); }
        }

        public bool IsCheck
        {
            get { return DateLimit != null; }
        }

        public bool IsResend { get; set; }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
            if (Document != null)
            {
                Document.OnReferenceChanged(this);
            }
        }

    }
}
