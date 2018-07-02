using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Module.Flow;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataWF.Module.FlowGui
{
    public class DocumentDetailView<T> : ListEditor, IDocument, ISync where T : DocumentDetail, new()
    {        
        protected Document document;
        protected DBTableView<T> view;

        public DocumentDetailView() : base(new DocumentLayoutList())
        {
            view = new DBTableView<T>(Table, new QParam(LogicType.And, Table.ParseProperty(nameof(DocumentDetail.DocumentId)), CompareType.Equal, 0), DBViewKeys.Empty);
            DataSource = view;
            List.EditMode = EditModes.ByF2;
            toolLog.Visible = Table.IsLoging;
            toolLoad.Visible =
                toolRefresh.Visible =
                toolSave.Visible =
                toolStatus.Visible = false;

            Name = nameof(DocumentDetailView<T>);
        }

        DBItem IDocument.Document { get => Document; set => Document = (Document)value; }

        public DBTable Table
        {
            get { return DBTable.GetTable(typeof(T), null, false, true); }
        }

        public virtual Document Document
        {
            get { return document; }
            set
            {
                if (document != value)
                {
                    document = value;
                    view.DefaultParam.Value = document?.Id ?? 0;
                    view.UpdateFilter();
                    view.IsSynchronized = false;
                }
            }
        }

        public T Current
        {
            get { return (T)list.SelectedItem; }
            set { list.SelectedItem = value; }
        }

        public virtual void Sync()
        {
            if (!view.IsSynchronized)
            {
                try
                {
                    view.Load();
                    view.IsSynchronized = true;
                }
                catch (Exception ex) { Helper.OnException(ex); }
            }
        }

        public async Task SyncAsync()
        {
            if (!view.IsSynchronized)
            {
                await Task.Run(() => Sync());
            }
        }

        protected async override void OnToolLoadClick(object sender, EventArgs e)
        {
            await SyncAsync();
        }

        protected override void OnToolInsertClick(object sender, EventArgs e)
        {
            var newItem = new T();
            newItem.Document = Document;
            ShowObject(newItem);
        }

        protected override void OnToolRemoveClick(object sender, EventArgs e)
        {
            var items = list.Selection.GetItems<T>();
            //base.OnToolRemoveClick(sender, e);
            foreach (var data in items)
            {
                data.Delete();
            }
        }

        protected override void OnToolLogClick(object sender, EventArgs e)
        {
            var logs = new DataLogView
            {
                Table = Table,
                Filter = Current,
                Mode = Current == null ? DataLogMode.Table : DataLogMode.Default
            };
            logs.ShowDialog(this);
        }

        protected override void OnToolWindowAcceptClick(object sender, EventArgs e)
        {
            var item = fields.FieldSource as T;
            if (item != null)
            {
                item.Attach();
            }
            if (view.IsStatic)
            {
                base.OnToolWindowAcceptClick(sender, e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            view?.Dispose();
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, nameof(DocumentDetailView<T>), typeof(T).Name);
        }
    }
}
