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

    [Table("ddocument_data", "Document", BlockSize = 400), InvokerGenerator]
    public partial class DocumentData : DocumentItem
    {
        private byte[] buf;
        private User currentUser;
        private TemplateData template;

        public DocumentData(DBTable table) : base(table)
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get => GetValue<long?>(DocumentDataTable.IdKey);
            set => SetValue(value, DocumentDataTable.IdKey);
        }

        [Index("ddocument_data_document_id")]
        public override long? DocumentId { get => base.DocumentId; set => base.DocumentId = value; }

        [Index("ddocument_data_item_type", false)]
        public override int ItemType
        {
            get => base.ItemType;
            set => base.ItemType = value;
        }

        [Column("template_data_id")]
        public int? TemplateDataId
        {
            get => GetValue<int?>(DocumentDataTable.TemplateDataIdKey);
            set => SetValue(value, DocumentDataTable.TemplateDataIdKey);
        }

        [Reference(nameof(TemplateDataId))]
        public TemplateData TemplateData
        {
            get => GetReference(DocumentDataTable.TemplateDataIdKey, ref template);
            set => SetReference(template = value, DocumentDataTable.TemplateDataIdKey);
        }

        [Column("file_name", 1024, Keys = DBColumnKeys.View | DBColumnKeys.FileName)]
        public virtual string FileName
        {
            get => GetValue<string>(Table.FileNameKey);
            set => SetValue(value, Table.FileNameKey);
        }

        [Column("file_url", 1024)]
        public string FileUrl
        {
            get => GetValue<string>(DocumentDataTable.FileUrlKey);
            set => SetValue(value, DocumentDataTable.FileUrlKey);
        }

        [Column("file_last_write", Keys = DBColumnKeys.FileLastWrite)]
        public DateTime? FileLastWrite
        {
            get => GetValue<DateTime?>(DocumentDataTable.FileLastWriteKey) ?? Stamp;
            set => SetValue(value, DocumentDataTable.FileLastWriteKey);
        }

        [Column("file_data", Keys = DBColumnKeys.File)]
        public virtual byte[] FileData
        {
            get => buf ?? (buf = GetValue(DocumentDataTable.FileDataKey));
            set => SetValue(value, DocumentDataTable.FileDataKey);
        }

        [Column("file_lob", Keys = DBColumnKeys.FileOID)]
        public virtual long? FileLOB
        {
            get => GetValue<long?>(DocumentDataTable.FileLOBKey);
            set => SetValue(value, DocumentDataTable.FileLOBKey);
        }

        [Browsable(false)]
        [Column("current_user_id", ColumnType = DBColumnTypes.Code)]
        public int? CurrentUserId
        {
            get => currentUser?.Id;
            set => CurrentUser = Schema.GetTable<User>().LoadById(value);
        }

        [Browsable(false)]
        [Reference(nameof(CurrentUserId))]
        public User CurrentUser
        {
            get => currentUser;
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

        protected override void RaisePropertyChanged(string property)
        {
            base.RaisePropertyChanged(property);
            if (Attached)
            {
                document?.OnReferenceChanged(this);
            }
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

        public async Task<Stream> GetData(string fileName, DBTransaction transaction)
        {
            if (FileLOB != null)
            {
                var item = await GetBlobFileStream(Table.FileOIDKey, fileName, transaction);
                if (item != null)
                {
                    return item;
                }
            }
            return GetZipFileStream(Table.FileKey, fileName, transaction);
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
            await SetBlob(stream, Table.FileOIDKey, transaction);
            //SetStream(stream, Table.FileKey, user);
        }

        [ControllerMethod]
        public async Task<FileStream> RefreshData(DBTransaction transaction)
        {
            return new FileStream(await Parse(transaction, true), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Task<string> Parse(DBTransaction transaction, bool fromTemplate = false)
        {
            return Parse(new DocumentExecuteArgs(document)
            {
                Transaction = transaction
            }, fromTemplate);
        }

        public async Task ParseAndSave(DocumentExecuteArgs param, bool fromTemplate = false)
        {
            var path = await Parse(param, fromTemplate);
            if (path != null)
            {
                await SetData(path, param.Transaction);
            }
        }

        public async Task<string> Parse(DocumentExecuteArgs param, bool fromTemplate = false)
        {
            if (TemplateData == null || TemplateData.File == null)
            {
                return await GetDataPath(param.Transaction);
            }
            var filePath = (string)null;
            if (FileLOB == null || string.IsNullOrEmpty(FileName) || fromTemplate)
            {
                if (string.IsNullOrEmpty(FileName))
                {
                    FileName = RefreshName();
                }
                var templateFileTable = (TemplateFileTable<TemplateFile>)Schema.GetTable<TemplateFile>();
                filePath = Helper.GetDocumentsFullPath(FileName, "ParserNew" + (Id ?? TemplateData.Id));
                using (var stream = TemplateData.File.GetZipFileStream(templateFileTable.FileKey, filePath, param.Transaction))
                {
                    filePath = Execute(param, stream);
                }
            }
            else
            {
                filePath = Helper.GetDocumentsFullPath(FileName, "Parser" + (Id ?? TemplateData.Id));
                using (var stream = await GetData(filePath, param.Transaction))
                {
                    if (stream.Length == 0)
                    {
                        return null;
                    }
                    filePath = Execute(param, stream);
                }
            }

            return filePath;
        }

        public virtual string Execute(DocumentExecuteArgs param, Stream stream)
        {
            return DocumentFormatter.Execute(stream, FileName, param);
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

        public virtual string RefreshName()
        {
            if (IsTemplate && TemplateData?.File != null)
            {
                if (string.IsNullOrEmpty(Document?.Number)
                    || Document.Datas.Any(p => p.TemplateDataId != null
                    && p.FileName?.IndexOf(Document.Number, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return $"{Path.GetFileNameWithoutExtension(TemplateData.File.DataName)}{DateTime.Now.ToString("yyMMddhhmmss")}{TemplateData.File.FileType}";
                }
                else
                {
                    return $"{Document.Number}{TemplateData.File.FileType}";
                }
            }
            return FileName;
        }
    }
}
