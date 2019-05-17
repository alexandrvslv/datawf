/*
 DocumentData.cs
 
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

namespace DataWF.Module.Flow
{
    public abstract class DocumentDetail<T> : DBItem, IDocumentDetail where T : DBItem, new()
    {
        private static DBTable<T> dbTable;
        private static DBColumn documentKey = DBColumn.EmptyKey;
        public static DBColumn DocumentKey => DBTable.ParseProperty(nameof(DocumentId), ref documentKey);

        public static DBTable<T> DBTable => dbTable ?? (dbTable = GetTable<T>());

        private Document document;

        //public

        [Browsable(false)]
        [DataMember, Column("document_id")]
        public virtual long? DocumentId
        {
            get { return GetValue<long?>(DocumentKey); }
            set { SetValue(value, DocumentKey); }
        }

        [Reference(nameof(DocumentId))]
        public Document Document
        {
            get { return GetReference(DocumentKey, ref document); }
            set { SetReference(document = value, DocumentKey); }
        }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
            var reference = GetReference<Document>(Table.ParseProperty(nameof(Document)), ref document, DBLoadParam.None);
            if (Attached && reference != null)
            {
                reference.OnReferenceChanged(this);
            }
        }
    }
}
