using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;
using System.Threading.Tasks;
using DataWF.Module.CommonGui;
using DataWF.Data.Gui;

namespace DataWF.Module.FlowGui
{
    [Module(true)]
    public class DocumentWorker : DocumentListView, IDockContent
    {
        public static DocumentWorker Worker;

        private DocumentWorkList works;
        private QQuery qWork;
        private QQuery qDocs;
        private List<Document> mdocuemnts = new List<Document>();
        private Stage mstage = null;
        private Template mtemplate = null;
        private ManualResetEvent load = new ManualResetEvent(false);
        private DocumentFilter search = new DocumentFilter();
        private ToolItem toolRefresh;


        private System.Timers.Timer mtimer = new System.Timers.Timer(20000);

        public DocumentWorker()
        {
            toolRefresh = new ToolItem(ToolLoadOnClick) { Name = "Load", GlyphColor = Colors.DarkBlue, Glyph = GlyphType.Download };
            toolPreview.InsertAfter(new[] { toolRefresh });

            //mtimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs asg) => { CheckNewDocument(null); mtimer.Stop(); };

            qWork = new QQuery(string.Empty, DocumentWork.DBTable);
            qWork.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            qWork.BuildPropertyParam(nameof(DocumentWork.UserId), CompareType.Equal, User.CurrentUser.Id);

            var qDocWorks = new QQuery(string.Empty, DocumentWork.DBTable);
            qDocWorks.Columns.Add(new QColumn(DocumentWork.DBTable.ParseProperty(nameof(DocumentWork.DocumentId))));
            qDocWorks.BuildPropertyParam(nameof(DocumentWork.IsComplete), CompareType.Equal, false);
            qDocWorks.BuildPropertyParam(nameof(DocumentWork.UserId), CompareType.Equal, User.CurrentUser.Id);

            qDocs = new QQuery(string.Empty, Document.DBTable);
            qDocs.BuildPropertyParam(nameof(Document.Id), CompareType.In, qDocWorks);

            works = new DocumentWorkList(qWork.ToWhere(), DBViewKeys.Empty);
            works.ListChanged += WorksListChanged;

            AllowPreview = true;
            Filter.IsCurrent = true;
            Name = "Documents";
            Worker = this;
            FilterVisible = true;

            //Task.Run(() =>
            //{
            //    try
            //    {
            //        var items = qWork.Select().Cast<DocumentWork>().ToList();
            //        items.Sort((x, y) =>
            //        {
            //            var result = x.Document.Template.Name.CompareTo(y.Document.Template.Name);
            //            return result == 0 ? x.Stage.Name.CompareTo(y.Stage.Name) : result;
            //        });
            //        foreach (var item in items)
            //        {
            //            works.Add(item);
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Helper.OnException(ex);
            //    }
            //});

            Task.Run(() =>
            {
                while (true)
                {
                    var task = new TaskExecutor();
                    task.Name = "Load Documents";
                    task.Action = () =>
                    {
                        try
                        {
                            Document.DBTable.Load(qDocs, DBLoadParam.Synchronize, null);
                            DocumentWork.DBTable.Load(qWork, DBLoadParam.Synchronize, works);
                            Helper.LogWorkingSet("Documents");
                        }
                        catch (Exception ex)
                        {
                            Helper.OnException(ex);
                        }
                        return null;
                    };
                    GuiService.Main.AddTask(this, task);
                    load.Reset();
                    load.WaitOne(200000);
                }
            });
        }

        private void WorksListChanged(object sender, ListChangedEventArgs e)
        {
            DocumentWork work = e.NewIndex >= 0 ? works[e.NewIndex] : null;
            Document document = work?.Document;
            int di = 0;
            if (e.ListChangedType == ListChangedType.ItemAdded)
                di = 1;
            else if (e.ListChangedType == ListChangedType.ItemDeleted)
                di = -1;
            if (document != null && work.IsUser)
            {
                if (di > 0 && GuiService.Main != null)
                    CheckNewDocument(document);

                if (di != 0)
                {
                    IncrementNode(filterView.Templates.Find(document.Template), di);
                    if (work.User != null)
                        IncrementNode(filterView.Users.Find(work.User), di);
                    if (work.Stage != null)
                        IncrementNode(filterView.Works.Find(work.Stage), di);
                }
            }
        }

        private void IncrementNode(TableItemNode node, int d)
        {
            while (node != null)
            {
                node.Count = (int)node.Count + d;
                node = node.Group as TableItemNode;
            }
        }

        private void CheckNewDocument(object obj)
        {
            Document doc = obj as Document;
            Stage stage = null;
            Template template = null;
            bool add = false;
            if (doc != null)
            {
                var work = doc.WorkCurrent;
                if (work != null && work.DateRead == DateTime.MinValue)
                {
                    stage = work.Stage;
                    template = doc.Template;
                    add = true;
                }
            }
            if (mstage != stage || mtemplate != template)
            {
                if (mdocuemnts.Count > 0)
                {
                    GuiService.Main.SetStatus(new StateInfo("Document",
                        string.Format("{1} ({0})", mdocuemnts.Count, mtemplate, mstage),
                        string.Format("{0}", mstage), StatusType.Information, mtemplate));
                    mdocuemnts.Clear();
                }
                mstage = stage;
                mtemplate = template;
            }
            if (add)
            {
                mdocuemnts.Add(doc);
                mtimer.Start();
            }
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, Name, "Documents", GlyphType.Book);
        }

        internal static ToolMenuItem InitWork(DocumentWork d, EventHandler clickHandler)
        {
            var item = new ToolMenuItem();
            item.Tag = d;
            item.Name = d.Id.ToString();
            item.Text = string.Format("{0}-{1}", d.Stage, d.User);
            if (clickHandler != null)
                item.Click += clickHandler;
            return item;
        }

        private void TemplateItemClick(object sender, EventArgs e)
        {
            var item = sender as TemplateMenuItem;
            if (item.DropDown?.Items.Count > 0)
                return;
            ViewDocuments(CreateDocuments(item.Template, null));
        }

        private void ToolLoadOnClick(object sender, EventArgs e)
        {
            load.Set();
        }

        protected override void Dispose(bool disposing)
        {
            if (works != null)
                works.Dispose();
            base.Dispose(disposing);
        }
    }
}
