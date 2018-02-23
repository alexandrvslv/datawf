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

    [Table("flow", "ddocdata", BlockSize = 2000)]
    public class DocumentData : DBItem
    {
        public static DBTable<DocumentData> DBTable
        {
            get { return DBService.GetTable<DocumentData>(); }
        }

        [NonSerialized()]
        protected object templateDocument = null;

        public DocumentData()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetProperty<long?>(nameof(Id)); }
            set { SetProperty(value, nameof(Id)); }
        }

        [Browsable(false)]
        [Column("documentid")]
        public long? DocumentId
        {
            get { return GetProperty<long?>(nameof(DocumentId)); }
            set { SetProperty(value, nameof(DocumentId)); }
        }

        [Reference("fk_ddocdata_documentid", nameof(DocumentId))]
        public Document Document
        {
            get { return GetPropertyReference<Document>(nameof(DocumentId)); }
            set { SetPropertyReference(value, nameof(DocumentId)); }
        }

        [Column("docdata")]
        public byte[] Data
        {
            get
            {
                var column = ParseProperty(nameof(Data));
                byte[] buf = GetCache(column) as byte[];
                if (buf == null)
                {
                    buf = DBService.GetZip(this, column);
                    SetCache(column, buf);
                }
                return buf;
            }
            set
            {
                var column = ParseProperty(nameof(Data));
                SetCache(column, value);
                DBService.SetZip(this, column, value);

                if (IsTemplate.GetValueOrDefault() && templateDocument != null)
                {
                    if (templateDocument is IDisposable)
                        ((IDisposable)templateDocument).Dispose();
                    templateDocument = null;
                }
            }
        }

        public string Size
        {
            get
            {
                float len = Data == null ? 0 : Data.Length;
                int i = 0;
                while (len >= 1024 && i < 3)
                {
                    len = len / 1024;
                    i++;
                }
                return string.Format("{0:0.00} {1}", len, i == 0 ? "B" : i == 1 ? "KB" : i == 2 ? "MB" : "GB");
            }
        }
        [Column("dataname", 1024)]
        public string DataName
        {
            get { return GetProperty<string>(nameof(DataName)); }
            set { SetProperty(value, nameof(DataName)); }
        }

        [Column("istemplate")]
        public bool? IsTemplate
        {
            get { return GetProperty<bool?>(nameof(IsTemplate)); }
            set { SetProperty(value, nameof(IsTemplate)); }
        }

        [XmlIgnore, Browsable(false)]
        public object TemplateDocument
        {
            get { return templateDocument; }
            set { templateDocument = value; }
        }

        public string GetFullPath()
        {
            return Path.Combine(Helper.GetDirectory(Environment.SpecialFolder.LocalApplicationData, true), "Temp", "Documents", DataName);
        }

        public string SaveData()
        {
            return SaveData(GetFullPath());
        }

        public string SaveData(string fileName)
        {
            return SaveData(fileName, Data);
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
            return Data;
        }

        public string Execute()
        {
            return Execute(GetFullPath());
        }

        public string Execute(string file)
        {
            file = SaveData(file, Data);
            System.Diagnostics.Process.Start(file);
            return file;
        }

        //http://stackoverflow.com/questions/210650/validate-image-from-file-in-c-sharp
        public bool IsImage()
        {
            byte[] buf = Data;
            if (buf != null)
            {
                if (Encoding.ASCII.GetString(buf, 0, 2) == "BM" ||
                    Encoding.ASCII.GetString(buf, 0, 3) == "GIF" ||
                    (buf[0] == (byte)137 && buf[1] == (byte)80 && buf[2] == (byte)78 && buf[3] == (byte)71) || //png
                    (buf[0] == (byte)73 && buf[1] == (byte)73 && buf[2] == (byte)42) || // TIFF
                    (buf[0] == (byte)77 && buf[1] == (byte)77 && buf[2] == (byte)42) || // TIFF2
                    (buf[0] == (byte)255 && buf[1] == (byte)216 && buf[2] == (byte)255 && buf[3] == (byte)224) || //jpeg
                    (buf[0] == (byte)255 && buf[1] == (byte)216 && buf[2] == (byte)255 && buf[3] == (byte)225)) //jpeg canon
                    return true;
            }
            return false;
        }

        public bool IsText()
        {
            byte[] buf = Data;
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
            Data = (byte[])Document.Template.Data.Clone();
            DataName = Document.Template.DataName.Replace(".", DateTime.Now.ToString("yyyyMMddHHmmss") + ".");
        }

        public void Load(string p)
        {
            Data = File.ReadAllBytes(p);
            DataName = Path.GetFileName(p);
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
            if (data.Data == null || data.Document.Template.Data == null)
                return;
            data.Data = null;//TODO Template.Parser.Execute(data.Data, data.DataName, param);
        }
    }
}
