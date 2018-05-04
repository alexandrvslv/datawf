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
using System.Threading.Tasks;

namespace DataWF.Module.FlowGui
{
    public class DocumentReferenceView : DocumentListView, ISync, IDocument, ILocalizable
    {
        private Document document;
        private ToolItem toolAttach;
        private ToolItem toolDetach;
        private bool synch = false;

        public DocumentReferenceView()
        {
            toolAttach = new ToolItem(ToolAttachClick) { Glyph = GlyphType.PlusSquareO };
            toolDetach = new ToolItem(ToolDetachClick) { Glyph = GlyphType.MinusSquareO };

            AllowPreview = false;
            AutoLoad = false;
            FilterVisible = true;
            LabelText = null;
            MainDock = true;
            ShowPreview = false;

            Bar.Items.Add(toolAttach);
            Bar.Items.Add(toolDetach);

            Name = "DocumentRelations";
        }

        DBItem IDocument.Document { get { return Document; } set { Document = value as Document; } }

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
                        Filter.Referencing = value;                        
                    }

                }
            }
        }

        private void DocumentRefChanged(Document arg1, ListChangedType arg2)
        {
            Documents.UpdateFilter();
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, base.Name, "Relations", GlyphType.Link);
        }

        public override bool ReadOnly
        {
            get { return base.ReadOnly; }
            set
            {
                base.ReadOnly = value;
                toolAttach.Sensitive = !value;
                toolDetach.Sensitive = !value;
            }
        }


        public void Sync()
        {
            if (!synch)
            {
                try
                {
                    document.GetReferencing<DocumentReference>(nameof(DocumentReference.DocumentId), DBLoadParam.Load);
                    document.GetReferencing<DocumentReference>(nameof(DocumentReference.ReferenceId), DBLoadParam.Load);
                    //refs.Documents.UpdateFilter();
                    synch = true;
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            }
        }

        public async Task SyncAsync()
        {
            await Task.Run(() => Sync());
        }

        private void ToolAttachClick(object sender, EventArgs e)
        {
            var window = new DocumentFinder()
            {
                VisibleAccept = true,
                Size = new Size(800, 600)
            };
            window.ButtonAcceptClick += (o, a) =>
            {
                foreach (Document d in window.List.GetSelected())
                {
                    if (document.ContainsReference(d.Id) || document == d)
                        continue;
                    var refer = new DocumentReference();
                    refer.GenerateId();
                    refer.Document = document;
                    refer.Reference = d;
                    refer.Attach();
                }
                window.Dispose();
            };
            window.Show(this, Point.Zero);
        }

        private void ToolDetachClick(object sender, EventArgs e)
        {
            if (List.SelectedItem == null)
                return;
            var dc = List.SelectedItem as Document;
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
