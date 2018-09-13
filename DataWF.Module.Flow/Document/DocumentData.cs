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

using DataWF.Common;
using DataWF.Data;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace DataWF.Module.Flow
{
    public class DocumentDataList : DBTableView<DocumentData>
    {
        Document document;
        public DocumentDataList()
            : this("", DBViewKeys.None)
        {
        }

        public DocumentDataList(Document document)
            : this(DocumentData.DBTable.ParseProperty(nameof(DocumentData.DocumentId)).Name + "=" + document.PrimaryId, DBViewKeys.Static)
        {
            this.document = document;
        }

        public DocumentDataList(string filter, DBViewKeys mode)
            : base(filter, mode)
        {
        }

        public override int AddInternal(DocumentData item)
        {
            if (document != null && item.Document == null)
                item.Document = document;
            return base.AddInternal(item);
        }

        public void FilterByDocument(Document document)
        {
            DefaultParam = new QParam(LogicType.And, DocumentData.DBTable.ParseProperty(nameof(DocumentData.DocumentId)), CompareType.Equal, document.PrimaryId);
        }
    }

    public class ListDocumentData : SelectableList<DocumentData>
    {
        Document document;

        public ListDocumentData(Document document)
            : base(DocumentData.DBTable.Select(
                DocumentData.DBTable.ParseProperty(nameof(DocumentData.DocumentId)), CompareType.Equal, document.PrimaryId))
        {
            this.document = document;
        }

        public override int AddInternal(DocumentData item)
        {
            if (Contains(item))
                return -1;
            if (item.Document == null)
                item.Document = document;
            int index = base.AddInternal(item);
            item.Attach();
            return index;
        }
    }


    [DataContract, Table("ddocument_data", "Document", BlockSize = 400)]
    public class DocumentData : DocumentDetail
    {
        public static DBTable<DocumentData> DBTable
        {
            get { return GetTable<DocumentData>(); }
        }

        protected object templateDocument = null;
        private byte[] buf;

        public DocumentData()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetProperty<long?>(nameof(Id)); }
            set { SetProperty(value, nameof(Id)); }
        }

        [Index("ddocument_data_item_type", false)]
        public override int? ItemType { get => base.ItemType; set => base.ItemType = value; }

        [Index("ddocument_data_document_id")]
        public override long? DocumentId { get => base.DocumentId; set => base.DocumentId = value; }

        [DataMember, Column("template_data_id")]
        public int? TemplateDataId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(TemplateDataId))]
        public TemplateData TemplateData
        {
            get { return GetPropertyReference<TemplateData>(); }
            set
            {
                SetPropertyReference(value);
                FileData = (byte[])value?.File?.Data.Clone();
                RefreshName();
            }
        }

        [DataMember, Column("file_name", 1024, Keys = DBColumnKeys.View | DBColumnKeys.FileName)]
        public string FileName
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("file_url", 1024)]
        public string FileUrl
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("file_data", Keys = DBColumnKeys.File)]
        public virtual byte[] FileData
        {
            get { return buf ?? (buf = GetZip(Table.Columns.GetByProperty(nameof(FileData)))); }
            set
            {
                var column = Table.Columns.GetByProperty(nameof(FileData));
                buf = value;
                SetZip(column, value);

                if (IsTemplate && templateDocument != null)
                {
                    if (templateDocument is IDisposable)
                        ((IDisposable)templateDocument).Dispose();
                    templateDocument = null;
                }
            }
        }

        public Stream GetFile()
        {
            if (FileName == null)
            {
                return null;
            }
            return GetZipMemoryStream(Table.ParseProperty(nameof(FileData)));
        }

        public void SetFile(Stream stream, string fileName)
        {
            FileName = fileName;
            SetStream(stream, Table.ParseProperty(nameof(FileData)));
        }

        public string FileSize
        {
            get
            {
                float len = FileData?.Length ?? 0;
                int i = 0;
                while (len >= 1024 && i < 3)
                {
                    len = len / 1024;
                    i++;
                }
                return string.Format("{0:0.00} {1}", len, i == 0 ? "B" : i == 1 ? "KB" : i == 2 ? "MB" : "GB");
            }
        }

        public bool IsTemplate
        {
            get { return TemplateDataId != null; }
        }

        [XmlIgnore, Browsable(false)]
        public object TemplateDocument
        {
            get { return templateDocument; }
            set { templateDocument = value; }
        }

        public string GetFullPath()
        {
            return Path.Combine(Path.GetTempPath(), "Documents", FileName);
        }

        public string GetData()
        {
            var file = GetFullPath();
            using (var stream = GetData(file))
            {
                return file;
            }
        }

        public Stream GetData(string fileName)
        {
            return GetFileStream(Table.Columns.GetByKey(DBColumnKeys.File), fileName);
        }

        public void SetData(string filePath, bool cache)
        {
            if (cache)
            {
                FileData = File.ReadAllBytes(filePath);
            }
            else
            {
                SetStream(filePath, Table.Columns.GetByKey(DBColumnKeys.File));
            }
        }

        public string Parse()
        {
            return Parse(new DocumentExecuteArgs { Document = Document, ProcedureCategory = TemplateData.Template.Code });
        }

        public string Parse(DocumentExecuteArgs param)
        {
            if (TemplateData == null || TemplateData.File == null)
            {
                return null;
            }

            var filePath = (string)null;
            using (var stream = GetData(GetFullPath()))
            {
                filePath = DocumentParser.Execute(stream, FileName, param);
            }
            SetData(filePath, false);
            return filePath;
        }

        public BackgroundWorker ExecuteAsync(DocumentExecuteArgs param)
        {
            var worker = new BackgroundWorker();
            //worker.WorkerSupportsCancellation = false;
            worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                try
                {
                    Parse(param);
                    e.Result = param;
                }
                catch (Exception ex)
                {
                    e.Result = ex;
                }
            };
            worker.RunWorkerAsync(param);
            return worker;
            //worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }



        public void RefreshName()
        {
            if (IsTemplate && TemplateData?.File != null)
            {
                if (string.IsNullOrEmpty(Document.Number))
                {
                    FileName = $"{Path.GetFileNameWithoutExtension(TemplateData.File.DataName)}{DateTime.Now.ToString("yy-MM-dd_hh-mm-ss")}{TemplateData.File.FileType}";
                }
                else
                {
                    FileName = $"{Path.GetFileNameWithoutExtension(TemplateData.File.DataName)}{Document.Number}{TemplateData.File.FileType}";
                }
            }
        }
    }
}
