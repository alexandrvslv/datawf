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

    [Table("ddocument_data", "Document", BlockSize = 400)]
    public class DocumentData : DBItem, IDocumentDetail
    {
        public static readonly DBTable<DocumentData> DBTable = GetTable<DocumentData>();
        public static readonly DBColumn<int?> TemplateDataKey = DBTable.ParseProperty<int?>(nameof(TemplateDataId));
        public static readonly DBColumn<string> FileUrlKey = DBTable.ParseProperty<string>(nameof(FileUrl));
        public static readonly DBColumn<string> FileNameKey = DBTable.ParseProperty<string>(nameof(FileName));
        public static readonly DBColumn<DateTime?> FileLastWriteKey = DBTable.ParseProperty<DateTime?>(nameof(FileLastWrite));
        public static readonly DBColumn<long?> DocumentKey = DBTable.ParseProperty<long?>(nameof(DocumentId));

        private byte[] buf;
        private User currentUser;
        private TemplateData template;
        private Document document;

        public DocumentData()
        { }

        [Browsable(false)]
        [Column("document_id"), Index("ddocument_data_document_id")]
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

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get => GetValue<long?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Index("ddocument_data_item_type", false)]
        public override int ItemType
        {
            get => base.ItemType;
            set => base.ItemType = value;
        }

        [Column("template_data_id")]
        public int? TemplateDataId
        {
            get => GetValue<int?>(TemplateDataKey);
            set => SetValue(value, TemplateDataKey);
        }

        [Reference(nameof(TemplateDataId))]
        public TemplateData TemplateData
        {
            get => GetReference(TemplateDataKey, ref template);
            set => SetReference(template = value, TemplateDataKey);
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
            get => GetValue<string>(FileUrlKey);
            set => SetValue(value, FileUrlKey);
        }

        [Column("file_last_write", Keys = DBColumnKeys.FileLastWrite)]
        public DateTime? FileLastWrite
        {
            get => GetValue<DateTime?>(FileLastWriteKey) ?? Stamp;
            set => SetValue(value, FileLastWriteKey);
        }

        [Column("file_data", Keys = DBColumnKeys.File)]
        public virtual byte[] FileData
        {
            get => buf ?? (buf = GetValue(Table.FileKey));
            set => SetValue(value, Table.FileKey);
        }

        [Column("file_lob", Keys = DBColumnKeys.FileLOB)]
        public virtual long? FileLOB
        {
            get => GetValue<long?>(Table.FileBLOBKey);
            set => SetValue(value, Table.FileBLOBKey);
        }

        [Browsable(false)]
        [Column("current_user_id", ColumnType = DBColumnTypes.Code)]
        public int? CurrentUserId
        {
            get => currentUser?.Id;
            set => CurrentUser = User.DBTable.LoadById(value);
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
                var item = await GetBlobFileStream(Table.FileBLOBKey, fileName, transaction);
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
            await SetBlob(stream, Table.FileBLOBKey, transaction);
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

                filePath = Helper.GetDocumentsFullPath(FileName, "ParserNew" + (Id ?? TemplateData.Id));
                using (var stream = TemplateData.File.GetZipFileStream(TemplateFile.DBTable.FileKey, filePath, param.Transaction))
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
