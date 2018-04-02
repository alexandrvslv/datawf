using DataWF.Data.Gui;
using DataWF.Common;
using System;
using System.ComponentModel;
using System.Threading;
using DataWF.Gui;
using DataWF.Module.Flow;
using Xwt;
using Xwt.Drawing;
using DataWF.Data;

namespace DataWF.Module.FlowGui
{
    public class DocumentRelations : VPanel, ISynch, IDocument, IReadOnly, ILocalizable
    {
        private Document document;
        private DocumentSearch search = new DocumentSearch();
        private DocumentListView refs = new DocumentListView();
        private ToolItem toolAttach = new ToolItem();
        private ToolItem toolDetach = new ToolItem();
        private bool synch = false;

        public DocumentRelations()
        {
            refs.LabelText = null;
            refs.MainDock = false;
            refs.Name = "documentListView1";
            refs.TemplateFilter = null;

            this.Name = "DocumentRelations";

            PackStart(refs, true, true);

            toolAttach.Click += ToolAttachClick;
            toolAttach.Glyph = GlyphType.PlusCircle;

            toolDetach.Click += ToolDetachClick;
            toolDetach.Glyph = GlyphType.MinusCircle;

            refs.Tools.Items.Add(toolAttach);
            refs.Tools.Items.Add(toolDetach);
            refs.AutoLoad = false;
            //refs.AllowPreview = false;
            Localize();
        }

        public DocumentListView Documents
        {
            get { return refs; }
        }

        public Document Document
        {
            get { return document; }
            set
            {
                if (document != value)
                {
                    synch = false;
                    if (document != null)
                        document.RefChanged -= DocumentRefChanged;
                    document = value;
                    if (document != null)
                    {
                        document.RefChanged += DocumentRefChanged;
                        if (refs.Documents == null)
                            refs.Documents = new DocumentList();
                        refs.Search = null;
                        search.Clear();
                        search.Attributes.Add(document.CreateRefsFilter());
                        refs.Search = search;
                    }
                    refs.Preview = false;
                }
            }
        }

        private void DocumentRefChanged(Document arg1, ListChangedType arg2)
        {
            refs.Documents.UpdateFilter();
        }

        public void Localize()
        {
            GuiService.Localize(this, base.Name, "Relations");
            GuiService.Localize(toolAttach, base.Name, "Attach");
            GuiService.Localize(toolDetach, base.Name, "Detach");
            refs.Localize();
        }

        public bool ReadOnly
        {
            get { return !toolAttach.Sensitive; }
            set
            {
                toolAttach.Sensitive = !value;
                toolDetach.Sensitive = !value;
            }
        }

        DBItem IDocument.Document
        {
            get { return Document; }
            set { Document = value as Document; }
        }

        public void Synch()
        {
            if (!synch)
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    try
                    {
                        document.Initialize(DocInitType.Refed | DocInitType.Refing);
                        refs.Documents.UpdateFilter();
                        synch = true;
                    }
                    catch (Exception ex)
                    {
                        Helper.OnException(ex);
                    }
                });
        }

        private void ToolAttachClick(object sender, EventArgs e)
        {
            var ds = new DocumentFinder();
            ds.VisibleAccept = true;
            ds.Size = new Size(800, 600);
            ds.ButtonAcceptClick += (o, a) =>
            {
                foreach (Document d in ds.List.GetSelected())
                {
                    if (document.ContainsReference(d.Id) || document == d)
                        continue;
                    var refer = new DocumentReference();
                    refer.GenerateId();
                    refer.Document = document;
                    refer.Reference = d;
                    refer.Attach();
                }
                ds.Dispose();
            };
            ds.Show(this, Point.Zero);
        }

        private void ToolDetachClick(object sender, EventArgs e)
        {
            if (refs.List.SelectedItem == null)
                return;
            var dc = refs.List.SelectedItem as Document;
            var refer = document.FindReference(dc.Id);
            if (refer != null)
            {
                refer.Delete();
                //document.Refed.Remove(refer);
                //document.Refing.Remove(refer);
            }
            //document.Refs.Remove(dc);
            //document.Refs.UpdateFilter();
        }

        protected override void Dispose(bool disp)
        {
            base.Dispose(disp);
        }

    }
}
