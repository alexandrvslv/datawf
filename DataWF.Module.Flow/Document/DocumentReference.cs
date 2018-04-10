/*
 DocumentReference.cs
 
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
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    public enum DocumentReferenceMode
    {
        None,
        Refed,
        Refing
    }

    public class DocumentReferenceList : DBTableView<DocumentReference>
    {
        private Document doc;
        private DocumentReferenceMode mode = DocumentReferenceMode.None;

        public DocumentReferenceList()
            : this("", DBViewKeys.None)
        { }

        public DocumentReferenceList(string filter, DBViewKeys mode)
            : base(DocumentReference.DBTable, filter, mode)
        {
        }

        public DocumentReferenceList(Document document, DocumentReferenceMode Mode)
            : this(Mode == DocumentReferenceMode.Refed
                   ? DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.DocumentId)).Name + "=" + document.PrimaryId
                : Mode == DocumentReferenceMode.Refing
                   ? DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)).Name + "=" + document.PrimaryId
                   : DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.DocumentId)).Name + "=" + document.PrimaryId +
                   " or " + DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)).Name + "=" + document.PrimaryId, DBViewKeys.Static)
        {
            this.doc = document;
            this.mode = Mode;
        }

        public Document Document
        {
            get { return doc; }
        }

        public DocumentReferenceMode Mode
        {
            get { return mode; }
        }

        public override int AddInternal(DocumentReference item)
        {
            if (doc != null)
            {
                if (mode == DocumentReferenceMode.Refed && item.Document == null)
                    item.Document = doc;
                else if (mode == DocumentReferenceMode.Refing && item.Reference == null)
                    item.Reference = doc;
            }
            return base.AddInternal(item);
            //if (mode == DocumentReferenceListMode.Refed && item.Reference != null && !item.Reference.Refing.Contains(item))
            //{
            //    item.Reference.Refing.Add(item);
            //}
            //if (mode == DocumentReferenceListMode.Refing && item.Document != null && !item.Document.Refed.Contains(item))
            //{
            //    item.Document.Refed.Add(item);
            //}
        }

        public override bool Remove(DocumentReference item)
        {
            bool flag = base.Remove(item);
            //if (mode == DocumentReferenceListMode.Refed && item.Reference != null && item.Reference.Refing.Contains(item))
            //{
            //    item.Reference.Refing.Remove(item);
            //}
            //if (mode == DocumentReferenceListMode.Refing && item.Document != null && item.Document.Refed.Contains(item))
            //{
            //    item.Document.Refed.Remove(item);
            //}

            return flag;
        }
    }

    public class ListDocumentReference : SelectableList<DocumentReference>
    {
        private Document document;
        private DocumentReferenceMode mode;

        public ListDocumentReference(Document document, DocumentReferenceMode mode)
            : base(mode == DocumentReferenceMode.Refed
                   ? DocumentReference.DBTable.Select(DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.DocumentId)),
                                                                                                         document.PrimaryId, CompareType.Equal)
                   : DocumentReference.DBTable.Select(DocumentReference.DBTable.ParseProperty(nameof(DocumentReference.ReferenceId)),
                                                                                                           document.PrimaryId, CompareType.Equal))
        {
            this.document = document;
            this.mode = mode;
        }

        public override int AddInternal(DocumentReference item)
        {
            if (Contains(item))
                return -1;
            if (mode == DocumentReferenceMode.Refed && item.Document == null)
                item.Document = document;
            else if (mode == DocumentReferenceMode.Refing && item.Reference == null)
                item.Reference = document;
            var index = base.AddInternal(item);
            item.Attach();
            return index;
        }

        public override void OnListChanged(ListChangedType type, int newIndex = -1, int oldIndex = -1, string property = null)
        {
            base.OnListChanged(type, newIndex, oldIndex, property);
            if (newIndex >= 0 && document != null)
            {
                if (document._refChanged != null)
                {
                    document._refChanged(document, type);
                }
            }
        }

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
        }
    }

    [DataContract, Table("wf_flow", "ddocument_reference", "Document", BlockSize = 2000)]
    public class DocumentReference : DBItem
    {
        public static DBTable<DocumentReference> DBTable
        {
            get { return DBService.GetTable<DocumentReference>(); }
        }

        public DocumentReference()
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
        [DataMember, Column("document_id")]
        public long? DocumentId
        {
            get { return GetProperty<long?>(); }
            set { SetProperty(value); }
        }

        [Reference("fk_ddocument_reference_documentid", nameof(DocumentId))]
        public Document Document
        {
            get { return GetPropertyReference<Document>(nameof(DocumentId)); }
            set { SetPropertyReference(value, nameof(DocumentId)); }
        }

        [Browsable(false)]
        [DataMember, Column("reference_id")]
        public long? ReferenceId
        {
            get { return GetProperty<long?>(); }
            set { SetProperty(value); }
        }

        [Reference("fk_ddocument_reference_reference_id", nameof(ReferenceId))]
        public Document Reference
        {
            get { return GetPropertyReference<Document>(nameof(ReferenceId)); }
            set { SetPropertyReference(value, nameof(ReferenceId)); }
        }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
            if (Document != null)
            {
                Document.OnReferenceChanged(this);
            }
            if (Reference != null)
            {
                Reference.OnReferenceChanged(this);
            }
        }

    }
}
