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
using DataWF.Common;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

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
            get { return DBService.GetTable<DocumentData>(); }
        }

        protected object templateDocument = null;
        private byte[] buf;

        public DocumentData()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetProperty<long?>(nameof(Id)); }
            set { SetProperty(value, nameof(Id)); }
        }

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
                FileData = (byte[])value?.Data.Clone();
                RefreshName();
            }
        }

        [DataMember, Column("file_name", 1024, Keys = DBColumnKeys.View)]
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

        [DataMember, Column("file_data")]
        public virtual byte[] FileData
        {
            get { return buf ?? (buf = DBService.GetZip(this, Table.Columns.GetByProperty(nameof(FileData)))); }
            set
            {
                var column = Table.Columns.GetByProperty(nameof(FileData));
                buf = value;
                DBService.SetZip(this, column, value);

                if (IsTemplate && templateDocument != null)
                {
                    if (templateDocument is IDisposable)
                        ((IDisposable)templateDocument).Dispose();
                    templateDocument = null;
                }
            }
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
            return Path.Combine(Helper.GetDirectory(Environment.SpecialFolder.LocalApplicationData, true), "Temp", "Documents", FileName);
        }

        public string SaveData()
        {
            return SaveData(GetFullPath());
        }

        public string SaveData(string fileName)
        {
            return SaveData(fileName, FileData);
        }

        public string SaveData(string fileName, byte[] Data)
        {
            string directory = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllBytes(fileName, Data);
            return fileName;
        }

        public byte[] Parse()
        {
            return Parse(new DocumentExecuteArgs() { Document = Document, ProcedureCategory = TemplateData.Template.Code });
        }

        public byte[] Parse(DocumentExecuteArgs param)
        {
            if (IsTemplate)
            {
                Execute(this, param);
            }
            return FileData;
        }

        public string Execute()
        {
            return Execute(GetFullPath());
        }

        public string Execute(string file)
        {
            file = SaveData(file, FileData);
            System.Diagnostics.Process.Start(file);
            return file;
        }

        public bool IsImage()
        {
            return Helper.IsImage(FileData);
        }

        public bool IsText()
        {
            byte[] buf = FileData;
            int c = 0;
            if (buf != null)
            {
                string first = Encoding.ASCII.GetString(buf, 0, 4);
                if (first == "%PDF" || IsImage())
                    return false;
                for (int i = 0; i < 5000 && i < buf.Length; i++)
                {
                    c = (buf[i] == 0x0 || buf[i] == 181) ? c + 1 : 0;
                    if (c > 4)
                        return false;
                }
            }
            return true;
        }

        public void Load(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    FileData = memory.ToArray();// File.ReadAllBytes(path);
                }
            }
            FileName = Path.GetFileName(path);
        }

        public static BackgroundWorker ExecuteAsync(DocumentData data, DocumentExecuteArgs param)
        {
            var worker = new BackgroundWorker();
            //worker.WorkerSupportsCancellation = false;
            worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                try
                {
                    Execute(data, param);
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

        public static void Execute(DocumentData data, DocumentExecuteArgs param)
        {
            if (data.FileData == null || data.TemplateData == null)
                return;
            data.FileData = DocumentParser.Execute(data.TemplateData.Data, data.FileName, param);
        }

        public void RefreshName()
        {
            if (IsTemplate && TemplateData.DataName != null)
            {
                if (string.IsNullOrEmpty(Document.Number))
                {
                    FileName = $"{Path.GetFileNameWithoutExtension(TemplateData.DataName)}{DateTime.Now.ToString("yy-MM-dd_hh-mm-ss")}{Path.GetExtension(TemplateData.DataName)}";
                }
                else
                {
                    FileName = $"{Path.GetFileNameWithoutExtension(TemplateData.DataName)}{Document.Number}.{Path.GetExtension(TemplateData.DataName)}";
                }
            }
        }
    }
}
