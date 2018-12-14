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
using DataWF.Module.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    public class DocumentDataList : DBTableView<DocumentData>
    {
        readonly Document document;

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
        readonly Document document;

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

    public class DocumentDataLog
    {
        public DocumentDataLog()
        { }

        public DocumentDataLog(DBLogItem logItem)
        {
            Id = (int)logItem.LogId;
            BaseId = (long)logItem.BaseId;
            Date = (DateTime)logItem.DateCreate;
            Type = (DBLogType)logItem.LogType;
            User = ((UserLog)logItem.UserLog)?.User?.Name;
            FileName = logItem.GetValue<string>(logItem.LogTable.GetLogColumn(logItem.BaseTable.FileNameKey));
        }

        public int Id { get; set; }

        public long BaseId { get; set; }

        public DateTime Date { get; set; }

        public DBLogType Type { get; set; }

        public string User { get; set; }

        public string FileName { get; set; }

    }

    [DataContract, Table("ddocument_data", "Document", BlockSize = 400)]
    public class DocumentData : DocumentDetail
    {
        public static DBTable<DocumentData> DBTable
        {
            get { return GetTable<DocumentData>(); }
        }

        private byte[] buf;
        private User currentUser;

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
            set { SetPropertyReference(value); }
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
            get { return buf ?? (buf = GetZip(Table.FileKey)); }
            set { SetValue(value, Table.FileKey); }
        }

        [Browsable(false)]
        [DataMember, Column("current_user_id", ColumnType = DBColumnTypes.Code)]
        public int? CurrentUserId
        {
            get { return currentUser?.Id; }
            set { CurrentUser = User.DBTable.LoadById(value); }
        }

        [Browsable(false)]
        [Reference(nameof(CurrentUserId))]
        public User CurrentUser
        {
            get { return currentUser; }
            set
            {
                currentUser = value;
                Accept(value);
            }
        }

        //public string FileSize
        //{
        //    get
        //    {
        //        float len = FileData?.Length ?? 0;
        //        int i = 0;
        //        while (len >= 1024 && i < 3)
        //        {
        //            len = len / 1024;
        //            i++;
        //        }
        //        return string.Format("{0:0.00} {1}", len, i == 0 ? "B" : i == 1 ? "KB" : i == 2 ? "MB" : "GB");
        //    }
        //}

        public bool IsTemplate
        {
            get { return TemplateDataId != null; }
        }

        public string GetDataPath()
        {
            using (var stream = GetData())
            {
                return stream == null ? null : ((FileStream)stream).Name;
            }
        }

        public Stream GetData()
        {
            var filePath = Helper.GetDocumentsFullPath(FileName, nameof(DocumentData) + Id);
            if (filePath == null)
            {
                return null;
            }
            return GetData(filePath);
        }

        public Stream GetData(string fileName)
        {
            return GetZipFileStream(Table.FileKey, fileName);
        }

        public void SetData(string filePath, IUserIdentity user)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                SetData(stream, null, user);
            }
        }

        public void SetData(Stream stream, string fileName, IUserIdentity user)
        {
            if (fileName != null)
            {
                FileName = fileName;
            }

            SetStream(stream, Table.FileKey, user);
        }

        [ControllerMethod]
        public FileStream RefreshData(IUserIdentity user)
        {
            return new FileStream(Parse(user, true), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public string Parse(IUserIdentity user, bool fromTemplate = false)
        {
            return Parse(new DocumentExecuteArgs
            {
                Document = Document,
                ProcedureCategory = TemplateData.Template.Code,
                User = user
            }, fromTemplate);
        }

        public string Parse(DocumentExecuteArgs param, bool fromTemplate = false)
        {
            if (TemplateData == null || TemplateData.File == null)
            {
                return GetDataPath();
            }

            var filePath = Helper.GetDocumentsFullPath(FileName, "Parser" + (Id ?? TemplateData.Id));
            if (filePath == null || fromTemplate)
            {
                using (var stream = TemplateData.File.GetFileStream())
                {
                    FileName = RefreshName();
                    filePath = DocumentParser.Execute(stream, FileName, param);
                }
            }
            else
            {
                using (var stream = GetData(filePath))
                {
                    filePath = DocumentParser.Execute(stream, FileName, param);
                }
            }
            SetData(filePath, param.User);
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

        [ControllerMethod]
        public IEnumerable<DocumentDataLog> GetLogs()
        {
            using (var query = new QQuery(Table.LogTable))
            {
                query.BuildParam(Table.LogTable.BaseKey, this.Id);
                foreach (var logItem in Table.LogTable.Load(query))
                {
                    var fileName = logItem.GetValue<string>(logItem.LogTable.FileNameKey);
                    if (fileName != null)
                    {
                        yield return new DocumentDataLog(logItem);
                    }
                }
            };
        }

        [ControllerMethod]
        public FileStream GetLogFile(int logId)
        {
            var logItem = Table.LogTable.LoadById(logId);
            if (logItem == null)
            {
                throw new Exception($"DataLog with id {logId} not found!");
            }
            var fileName = logItem.GetValue<string>(logItem.LogTable.FileNameKey);
            if (fileName == null)
            {
                throw new Exception($"DataLog with id {logId} no file defined!");
            }

            return logItem.GetFileStream(Table.LogTable.FileKey, Helper.GetDocumentsFullPath(fileName, "DataLog" + logItem.LogId));
        }

        public override void OnPropertyChanged([CallerMemberName]string property = null, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
            if (property == nameof(TemplateDataId))
            {
                //var data = TemplateData;
                //if (data != null)
                //{
                //    using (var template = data.File.GetMemoryStream())
                //    {
                //        FileData = template.ToArray();
                //        FileName = RefreshName();
                //    }
                //}
            }
        }

        public string RefreshName()
        {
            if (IsTemplate && TemplateData?.File != null)
            {
                if (string.IsNullOrEmpty(Document?.Number))
                {
                    return $"{Path.GetFileNameWithoutExtension(TemplateData.File.DataName)}{DateTime.Now.ToString("yy-MM-dd_hh-mm-ss")}{TemplateData.File.FileType}";
                }
                else
                {
                    return $"{Path.GetFileNameWithoutExtension(TemplateData.File.DataName)}{Document.Number}{TemplateData.File.FileType}";
                }
            }
            return FileName;
        }
    }
}
