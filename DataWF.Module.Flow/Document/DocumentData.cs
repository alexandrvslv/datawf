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
            : base(DocumentData.DBTable, filter, mode)
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
            DefaultFilter = DocumentData.DBTable.ParseProperty(nameof(DocumentData.DocumentId)).Name + "=" + document.PrimaryId;
        }
    }

    public class ListDocumentData : SelectableList<DocumentData>
    {
        Document document;

        public ListDocumentData(Document document)
            : base(DocumentData.DBTable.Select(
                DocumentData.DBTable.ParseProperty(nameof(DocumentData.DocumentId)), document.PrimaryId, CompareType.Equal))
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

    [DataContract, Table("wf_flow", "ddocument_data", "Document", BlockSize = 2000)]
    public class DocumentData : DBItem
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

        [Browsable(false)]
        [DataMember, Column("document_id")]
        public long? DocumentId
        {
            get { return GetProperty<long?>(); }
            set { SetProperty(value); }
        }

        [Reference("fk_ddocument_data_document_id", nameof(DocumentId))]
        public Document Document
        {
            get { return GetPropertyReference<Document>(nameof(DocumentId)); }
            set { SetPropertyReference(value, nameof(DocumentId)); }
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
        public byte[] FileData
        {
            get { return buf ?? (buf = DBService.GetZip(this, ParseProperty(nameof(FileData)))); }
            set
            {
                var column = ParseProperty(nameof(FileData));
                buf = value;
                DBService.SetZip(this, column, value);

                if (IsTemplate.GetValueOrDefault() && templateDocument != null)
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


        [DataMember, Column("is_template")]
        public bool? IsTemplate
        {
            get { return GetProperty<bool?>(); }
            set { SetProperty(value); }
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

        public byte[] Parse(ExecuteArgs param)
        {
            if (IsTemplate.GetValueOrDefault())
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

        public void RefreshByTemplate()
        {
            IsTemplate = true;
            FileData = (byte[])Document.Template.Data.Clone();
            FileName = Document.Template.DataName.Replace(".", DateTime.Now.ToString("yyyyMMddHHmmss") + ".");
        }

        public void Load(string p)
        {
            FileData = File.ReadAllBytes(p);
            FileName = Path.GetFileName(p);
        }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
            if (Document != null)
            {
                Document.OnReferenceChanged(this);
            }
        }

        public static BackgroundWorker ExecuteAsync(DocumentData data, ExecuteArgs param)
        {
            BackgroundWorker worker = new BackgroundWorker();
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

        public static void Execute(DocumentData data, ExecuteArgs param)
        {
            if (data.FileData == null || data.Document.Template.Data == null)
                return;
            data.FileData = null;//TODO Template.Parser.Execute(data.Data, data.DataName, param);
        }
    }
}
