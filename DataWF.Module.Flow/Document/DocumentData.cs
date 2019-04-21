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
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

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
            DefaultParam = new QParam(LogicType.And, DocumentData.DocumentKey, CompareType.Equal, document.PrimaryId);
        }
    }

    public class ListDocumentData : SelectableList<DocumentData>
    {
        readonly Document document;

        public ListDocumentData(Document document)
            : base(DocumentData.DBTable.Select(
                DocumentData.DocumentKey, CompareType.Equal, document.PrimaryId))
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
    public class DocumentData : DocumentDetail<DocumentData>
    {
        private static DBColumn templateDataKey = DBColumn.EmptyKey;        
        private static DBColumn fileUrlKey = DBColumn.EmptyKey;

        public static DBColumn TemplateDataKey => DBTable.ParseProperty(nameof(TemplateDataId), ref templateDataKey);
        public static DBColumn FileUrlKey => DBTable.ParseProperty(nameof(FileUrl), ref fileUrlKey);

        private byte[] buf;
        private User currentUser;
        private TemplateData template;

        public DocumentData()
        { }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Index("ddocument_data_item_type", false)]
        public override int? ItemType { get => base.ItemType; set => base.ItemType = value; }

        [Index("ddocument_data_document_id")]
        public override long? DocumentId { get => base.DocumentId; set => base.DocumentId = value; }

        [DataMember, Column("template_data_id")]
        public int? TemplateDataId
        {
            get { return GetValue<int?>(TemplateDataKey); }
            set { SetValue(value, TemplateDataKey); }
        }

        [Reference(nameof(TemplateDataId))]
        public TemplateData TemplateData
        {
            get { return GetReference(TemplateDataKey, ref template); }
            set { template = SetReference(value, TemplateDataKey); }
        }

        [DataMember, Column("file_name", 1024, Keys = DBColumnKeys.View | DBColumnKeys.FileName)]
        public string FileName
        {
            get { return GetValue<string>(Table.FileNameKey); }
            set { SetValue(value, Table.FileNameKey); }
        }

        [DataMember, Column("file_url", 1024)]
        public string FileUrl
        {
            get { return GetValue<string>(FileUrlKey); }
            set { SetValue(value, FileUrlKey); }
        }

        [DataMember, Column("file_data", Keys = DBColumnKeys.File)]
        public virtual byte[] FileData
        {
            get { return buf ?? (buf = GetZip(Table.FileKey)); }
            set { SetValue(value, Table.FileKey); }
        }

        [DataMember, Column("file_lob", DBDataType = DBDataType.LargeObject, Keys = DBColumnKeys.FileLOB)]
        public virtual uint? FileLOB
        {
            get { return GetValue<uint?>(Table.FileLOBKey); }
            set { SetValue(value, Table.FileLOBKey); }
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

        public async Task<string> GetDataPath(DBTransaction trnasaction)
        {
            using (var stream = await GetData(trnasaction))
            {
                return stream == null ? null : ((FileStream)stream).Name;
            }
        }

        public Task<Stream> GetData(DBTransaction trnasaction)
        {
            var filePath = Helper.GetDocumentsFullPath(FileName, nameof(DocumentData) + Id);
            if (filePath == null)
            {
                return null;
            }
            return GetData(filePath, trnasaction);
        }

        public async Task<Stream> GetData(string fileName, DBTransaction transactio)
        {
            if (FileLOB != null)
            {
                var item = await GetLOBFileStream(Table.FileLOBKey, fileName, transactio);
                if (item != null)
                {
                    return item;
                }
            }
            return GetZipFileStream(Table.FileKey, fileName);
        }

        public async Task SetData(string filePath, DBTransaction transaction)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                await SetData(stream, null, transaction);
            }
        }

        public async Task SetData(Stream stream, string fileName, DBTransaction transaction)
        {
            if (fileName != null)
            {
                FileName = fileName;
            }
            await SetLOB(stream, Table.FileLOBKey, transaction);
            //SetStream(stream, Table.FileKey, user);
        }

        [ControllerMethod]
        public async Task<FileStream> RefreshData(DBTransaction transaction)
        {
            return new FileStream(await Parse(transaction, true), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Task<string> Parse(DBTransaction transaction, bool fromTemplate = false)
        {
            return Parse(new DocumentExecuteArgs
            {
                Document = Document,
                Transaction = transaction
            }, fromTemplate);
        }

        public async Task<string> Parse(DocumentExecuteArgs param, bool fromTemplate = false)
        {
            if (TemplateData == null || TemplateData.File == null)
            {
                return await GetDataPath(param.Transaction);
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
                using (var stream = await GetData(filePath, param.Transaction))
                {
                    filePath = DocumentParser.Execute(stream, FileName, param);
                }
            }

            return filePath;
        }

        public BackgroundWorker ExecuteAsync(DocumentExecuteArgs param)
        {
            var worker = new BackgroundWorker();
            //worker.WorkerSupportsCancellation = false;
            worker.DoWork += async (object sender, DoWorkEventArgs e) =>
            {
                try
                {
                    await Parse(param);
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
                query.BuildParam(Table.LogTable.BaseKey, Id);
                query.BuildParam(Table.LogTable.FileNameKey, CompareType.IsNot, null);
                var parameterData = new QParam();
                parameterData.Parameters.Add(QQuery.CreateParam(LogicType.And, Table.LogTable.FileLOBKey, CompareType.IsNot, null));
                parameterData.Parameters.Add(QQuery.CreateParam(LogicType.Or, Table.LogTable.FileKey, CompareType.IsNot, null));
                query.Parameters.Add(parameterData);
                var lob = (uint?)null;

                foreach (var logItem in Table.LogTable.Load(query).OrderByDescending(p => p[Table.LogTable.PrimaryKey]))
                {
                    var lobLob = logItem.GetValue<uint?>(Table.LogTable.FileLOBKey);
                    if (lobLob == null || lobLob != lob)
                    {
                        lob = lobLob;
                        yield return new DocumentDataLog(logItem);
                    }
                }
            };
        }

        [ControllerMethod]
        public async Task<Stream> GetLogFile(int logId)
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
            if (Table.LogTable.FileLOBKey != null)
            {
                var lob = logItem.GetValue(Table.LogTable.FileLOBKey);
                if (lob != null)
                {
                    return await logItem.GetLOBFileStream(Table.LogTable.FileLOBKey, Helper.GetDocumentsFullPath(fileName, "DataLog" + logItem.LogId));
                }
            }
            return logItem.GetFileStream(Table.LogTable.FileKey, Helper.GetDocumentsFullPath(fileName, "DataLog" + logItem.LogId));
        }

        [ControllerMethod]
        public async Task RemoveLogFile(int logId, DBTransaction transaction)
        {
            if (!Access.GetFlag(AccessType.Admin, transaction.Caller))
            {
                throw new Exception("Access Denied!");
            }

            var logItem = Table.LogTable.LoadById(logId);
            if (logItem == null)
            {
                throw new Exception($"Not Found!");
            }

            if (Table.LogTable.FileLOBKey != null)
            {
                var lob = logItem.GetValue<uint?>(Table.LogTable.FileLOBKey);
                if (lob != null)
                {
                    if (lob == FileLOB)
                    {
                        throw new Exception($"Latest log entry. Deletion Canceled!");
                    }

                    var qquery = new QQuery(Table.LogTable);
                    qquery.BuildParam(Table.LogTable.FileLOBKey, lob);
                    foreach (var item in Table.LogTable.Load(qquery).ToList())
                    {
                        if (item != logItem)
                        {
                            item.Delete();
                            await item.Save(transaction);
                        }
                    }
                }
            }
            logItem.Delete();
            await logItem.Save(transaction);
            Table.LogTable.Trunc();
        }

        public virtual string RefreshName()
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
