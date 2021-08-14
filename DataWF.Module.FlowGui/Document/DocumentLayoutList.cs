using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using DataWF.Gui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Xwt.Drawing;
using System.IO;
using DataWF.Module.Counterpart;
using DataWF.Common;

namespace DataWF.Module.FlowGui
{
    public class DocumentLayoutList : LayoutList
    {
        private Template template;
        private CellStyle styleBold;

        public DocumentLayoutList()
            : base()
        {
            HideCollections = true;
            //AutoToStringFill = true;
            //this.Size = new Size(872, 454);
        }

        public virtual Document Document
        {
            get { return fieldSource as Document; }
            set { FieldSource = value; }
        }

        //protected override void OnHeaderMouseUp(PListHitTestEventArgs e)
        //{
        //    base.OnHeaderMouseUp(e);
        //    Document row = List[e.HitTest.Index] as Document;
        //    if (row != null)
        //    {
        //        float size = 15 * ListInfo.Scale;
        //        RectangleF checkRect = new RectangleF(e.HitTest.ItemBound.X + 1, e.HitTest.ItemBound.Y + 1, size, size);
        //        if (checkRect.Contains(e.HitTest.Point))
        //        {
        //            row.Important = !row.Important;
        //            row.Save();
        //        }
        //    }
        //}

        //protected override void OnPaintHeader(GraphContext context, int index, object dataSource, RectangleF bound, CellDisplayState state)
        //{
        //    Document row = dataSource as Document;
        //    if (row != null)
        //    {
        //        float size = 15 * ListInfo.Scale;
        //        RectangleF checkRect = new RectangleF(bound.X + 1, bound.Y + 1, size, size);
        //        GuiService.PaintCheckBox(context, checkRect, row.Important ? CheckedState.Checked : CheckedState.Unchecked);
        //        bound.X += checkRect.Width + 2;
        //        bound.Width -= checkRect.Width;
        //    }
        //    base.OnPaintHeader(context, index, dataSource, bound, state);
        //}

        public override LayoutColumn CreateColumn(string name)
        {
            var column = base.CreateColumn(name);
            if (column.Name != nameof(Object.ToString))
            {
                column.Visible = false;
            }
            else
            {
                column.Width += 200;
            }

            return column;
        }

        public DBTableView<Document> Documents
        {
            get => ListSource as DBTableView<Document>;
            set => ListSource = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Template Template
        {
            get { return template; }
            set
            {
                if (template == value && template != null)
                    return;
                template = value;
                if (Mode == LayoutListMode.List && Documents != null)
                {
                    var type = FlowExplorer.Schema.Document.GetItemType(template?.DocumentType ?? 0).Type;
                    if (ListType != type)
                    {
                        Documents.ItemType = type;
                        ListType = type;
                    }
                    //TreeMode = ListInfo.Tree;
                }
            }
        }

        public override CellStyle OnGetCellStyle(object listItem, object value, ILayoutCell col)
        {
            var style = base.OnGetCellStyle(listItem, value, col);

            if (listItem is Document document)
            {
                if (document.CurrentWork != null && document.CurrentWork.DateRead == DateTime.MinValue)
                {
                    if (styleBold == null)
                    {
                        styleBold = GuiEnvironment.Theme["DocumentBold"];
                        if (styleBold == null)
                        {
                            styleBold = style.Clone();
                            styleBold.Name = "DocumentBold";
                            styleBold.Font = style.Font.WithWeight(FontWeight.Bold);
                            GuiEnvironment.Theme.Add(styleBold);
                        }
                    }
                    style = styleBold;
                }
            }
            return style;
        }

        /* public void PrintDoc(Template prm)
        {
            //if (FlowControls.Properties.Resources.docList == null) return;
            var td = new Doc.Odf.TextDocument(prm.Data);
            TemplateParser op = new TemplateParser(td);

            Dictionary<string, object> elements = new Dictionary<string, object>(StringComparer.Ordinal);
            DateTime dt = DateTime.Now;

            elements.Add("Дата", dt.ToString("D", Common.Localize.Data.Culture));
            elements.Add("ВсегоДокументов", base.ListSource.Count);
            elements.Add("Пользователь", User.CurrentUser);
            elements.Add("Параметры", prm.LabelText);

            Dictionary<string, object> subparam = new Dictionary<string, object>(StringComparer.Ordinal);
            List<Dictionary<string, object>> param = new List<Dictionary<string, object>>();
            foreach (DocumentListColumn colum in prm.PrintColumns)
                subparam.Add(colum.ColumnName, colum.DisplayName);
            param.Add(subparam);
            IEnumerable rows = listSource;
            // if (toolPrintSelected.Checked)
            //     rows = SelectedValues;
            foreach (DBItem row in rows)
            {
                subparam = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (DocumentListColumn colum in prm.PrintColumns)
                {
                    if (this.ListInfo.Columns.Contains(colum.ColumnName))
                    {
                        object cell = row[colum.ColumnName];
                        subparam.Add(colum.ColumnName, cell);
                    }
                    else
                        subparam.Add(colum.ColumnName, "");
                }
                param.Add(subparam);
            }
            elements.Add("Документы", param);

            try
            {
                op.PerformReplace(elements);
            }
            catch
            {
            }

            string filename = System.IO.Path.Combine(Helper.GetDirectory(Environment.SpecialFolder.MyDocuments), "Реестр" + dt.ToString("yyyy MMDHHmss") + ".odt ");
            File.WriteAllBytes(filename, td.UnLoad());
            Process.Start(filename);

         }*/
    }
}
