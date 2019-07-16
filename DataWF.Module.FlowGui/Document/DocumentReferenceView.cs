using DataWF.Common;
using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Gui;
using DataWF.Module.Flow;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Xwt;
using Xwt.Drawing;

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
            toolAttach = new ToolItem(ToolAttachClick) { Name = "Attach", Glyph = GlyphType.PlusSquareO, GlyphColor = Colors.Green };
            toolDetach = new ToolItem(ToolDetachClick) { Name = "Deattach", Glyph = GlyphType.MinusSquareO, GlyphColor = Colors.Red };

            Bar.Items.Add(toolAttach);
            Bar.Items.Add(toolDetach);

            filterCustomer.Visible = false;
            toolPreview.Checked = false;
            AutoLoad = false;
            LabelText = null;
            HideOnClose = true;
            Name = nameof(DocumentReferenceView);
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
                    //if (document != null)
                    //    document.RefChanged -= DocumentRefChanged;
                    document = value;
                    if (document != null)
                    {
                        //document.RefChanged += DocumentRefChanged;
                        Filter.Referencing = value;
                    }

                }
            }
        }

        private void DocumentRefChanged(Document arg1, ListChangedType arg2)
        {
            //Documents.UpdateFilter();
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, base.Name, "Documents", GlyphType.Link);
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
                    Documents.Load(DBLoadParam.Referencing);
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
            await Task.Run(() => Sync()).ConfigureAwait(false);
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
                foreach (Document selected in window.List.GetSelected())
                {
                    if (document.ContainsReference(selected) || document == selected)
                        continue;
                    document.CreateReference(selected);
                }
                window.Dispose();
            };
            window.Show(this, Point.Zero);
        }

        private void ToolDetachClick(object sender, EventArgs e)
        {
            if (List.SelectedItem == null)
                return;
            var selected = List.SelectedItem as Document;
            var refer = document.FindReference(selected);
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
            Document = null;
            base.Dispose(disp);
        }

    }
}
