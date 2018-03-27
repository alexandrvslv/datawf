using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Threading;
using DataWF.Module.Flow;
using Xwt;


namespace DataWF.Module.FlowGui
{
    public class DocumentHeader : VPanel, IDocument, ISynch, ILocalizable, IReadOnly
    {
        private Document document;
        private GroupBoxItem gAttribute;
        private GroupBoxItem gWork;
        private GroupBoxItem gFiles;
        private GroupBox groupBox;
        private LayoutList works;
        private DocumentFiles files;
        private ListEditor fields;
        private DBTableView<DocumentWork> view;
        private bool synch = false;
        //private GroupBoxMap groupBoxMap2;
        //private VScrollBar vScroll;

        public DocumentHeader()
        {
            view = new DBTableView<DocumentWork>(DocumentWork.DBTable, "", DBViewKeys.Empty);
            view.ApplySortInternal(DocumentWork.DBTable.DefaultComparer);
            view.ListChanged += ContentListChanged;

            works = new LayoutList()
            {
                AllowSort = false,
                AutoToStringFill = true,
                GenerateColumns = false,
                Name = "works",
                Text = "Works",

                ListInfo = new LayoutListInfo(
                    new LayoutColumn() { Name = "ToString", FillWidth = true },
                    new LayoutColumn() { Name = "Date", Width = 115 },
                    new LayoutColumn() { Name = "IsComplete", Width = 20 })
                {
                    ColumnsVisible = false,
                    HeaderVisible = false
                },
                ListSource = view
            };

            files = new DocumentFiles()
            {
                Current = null,
                Name = "files",
                ReadOnly = false,
                AutoSize = true
            };
            files.view.ListChanged += ContentListChanged;


            fields = new ListEditor()
            {
                Name = "fields",
                Text = "Document"
            };
            fields.Bar.Visible = false;
            fields.List.AllowCellSize = true;
            fields.List.EditMode = EditModes.ByClick;
            fields.List.EditState = EditListState.Edit;
            fields.List.GenerateColumns = false;
            fields.List.GenerateToString = false;
            fields.List.Grouping = false;
            fields.List.GridMode = true;

            gWork = new GroupBoxItem()
            {
                Row = 1,
                Widget = works,
                FillHeight = true,
                Name = "Works",
                Width = 380
            };

            gFiles = new GroupBoxItem()
            {
                Widget = files,
                Name = "Files",
                Width = 380
            };

            gAttribute = new GroupBoxItem()
            {
                Widget = fields,
                FillWidth = true,
                FillHeight = true,
                Name = "Attribute"
            };

            groupBox = new GroupBox(
                gAttribute, 
                new GroupBoxMap(gFiles, gWork) { Col = 1, FillWidth = true })
            { Name = "panel1" };

            PackStart(groupBox, true, true);
            Name = "DocumentHeader";

            //SizeChanged += DocumentHeader_SizeChanged;
            Localize();
        }

        public void Synch()
        {
            if (!synch)
                ThreadPool.QueueUserWorkItem((o) =>
                    {
                        try
                        {
                            document.Initialize(DocInitType.Data | DocInitType.Workflow);
                            synch = true;
                        }
                        catch (Exception ex) { Helper.OnException(ex); }
                    });
        }

        public ListEditor Fields { get { return fields; } }

        private void CheckWidth()
        {
            //int h = (int)groupBoxMap.CalucaleSize().Height;
            ////h += 50;
            //vScroll.Minimum = 0;
            //vScroll.Maximum = h > this.Height ? h - Height : 0;
            //if (vScroll.Value < h && vScroll.Maximum >= h)
            //    vScroll.Value = 0;

            //vScroll.Visible = vScroll.Maximum > 0;
            //vScroll.SmallChange = (this.Height) / 8;
            //vScroll.LargeChange = (this.Height) / 2;
            //while (vScroll.LargeChange > vScroll.Maximum)
            //{
            //    vScroll.SmallChange = (int)(vScroll.SmallChange / 1.1);
            //    vScroll.LargeChange = (int)(vScroll.LargeChange / 1.1);
            //}
            //vScroll.Maximum += this.vScroll.LargeChange;

            //int width = (vScroll.Visible ? this.Width - vScroll.Width : this.Width) - 2;
            //if (groupBoxMap.Width != width)
            //    groupBoxMap.Width = width;
            //else
            groupBox.ResizeLayout();
        }

        public Document Document
        {
            get { return document; }
            set
            {
                if (document != value)
                {
                    synch = false;
                    document = value;
                    fields.DataSource = document;
                    view.DefaultFilter = DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DocumentId)).Name + "=" + (document == null ? "0" : document.Id.ToString());
                    files.Document = document;

                    //works.ListInfo.Columns["Stamp"].Visible = true;
                    CheckWidth();
                }
            }
        }

        private void ContentListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            Application.Invoke(() => CheckWidth());
        }

        void DocumentHeader_SizeChanged(object sender, EventArgs e)
        {
            CheckWidth();
        }

        public bool ReadOnly
        {
            get { return fields.ReadOnly; }
            set
            {
                fields.ReadOnly = value;
                files.ReadOnly = value;
            }
        }

        DBItem IDocument.Document { get => Document; set => Document = (Document)value; }

        public void Localize()
        {
            GuiService.Localize(this, "DocumentHeader", "Header");
            GuiService.Localize(gWork, "DocumentHeader", "Works");
            GuiService.Localize(gFiles, "DocumentHeader", "Files");
            GuiService.Localize(gAttribute, "DocumentHeader", "Attributes");
            fields.Localize();
            works.Localize();
            files.Localize();
        }

        protected override void Dispose(bool disp)
        {
            gWork.Dispose();
            gFiles.Dispose();
            gAttribute.Dispose();
            view.ListChanged -= ContentListChanged;
            view.Dispose();
            base.Dispose(disp);
        }
    }
}
