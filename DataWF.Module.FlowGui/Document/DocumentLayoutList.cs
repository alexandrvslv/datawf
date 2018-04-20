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
//using System.Windows.Forms;

namespace DataWF.Module.FlowGui
{

    public class DocumentLayoutList : LayoutList
    {
        private Template viewmode;
        private CellStyle styleBold;

        public DocumentLayoutList()
            : base()
        {
            //this.Size = new Size(872, 454);
        }

        public Document Document
        {
            get { return fieldSource as Document; }
        }

        public DocumentList Documents
        {
            get { return listSource as DocumentList; }
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Template ViewMode
        {
            get { return viewmode; }
            set
            {
                if (viewmode == value)
                    return;
                viewmode = value;
                if (Mode == LayoutListMode.List && TypeHelper.IsBaseType(ListType, typeof(Document)))
                {
                    ListType = viewmode?.DocumentTypeInfo?.Type ?? typeof(Document);
                    //TreeMode = ListInfo.Tree;
                    RefreshBounds(true);
                }
            }
        }

        public override CellStyle OnGetCellStyle(object listItem, object value, ILayoutCell col)
        {
            var style = base.OnGetCellStyle(listItem, value, col);

            if (ListSource is DocumentList && listItem is Document)
            {
                Document document = (Document)listItem;

                if (document.WorkCurrent != null && document.WorkCurrent.DateRead == DateTime.MinValue)
                {
                    if (styleBold == null)
                    {
                        styleBold = GuiEnvironment.StylesInfo["DocumentBold"];
                        if (styleBold == null)
                        {
                            styleBold = style.Clone();
                            styleBold.Name = "DocumentBold";
                            styleBold.Font = style.Font.WithStyle(FontStyle.Oblique);
                            GuiEnvironment.StylesInfo.Add(styleBold);
                        }
                    }
                    style = styleBold;
                }
            }
            return style;
        }

        protected bool GetIsDocument(ILayoutCell cell, out Template filter)
        {
            filter = null;
            var documented = false;
            if (cell == null)
            {
                if (ListSource is DocumentList)
                {
                    documented = true;
                    filter = viewmode;
                }
                else if (Document != null)
                {
                    documented = true;
                    filter = Document.Template;
                }
            }
            else if (cell.Invoker?.DataType == typeof(Document)
                     || (cell.Invoker is DBColumn && ((DBColumn)cell.Invoker).ReferenceTable == Document.DBTable))
            {
                documented = true;
            }
            return documented;
        }

        /* public void PrintDoc(Template prm)
        {
            //if (FlowControls.Properties.Resources.docList == null) return;
            var td = new Doc.Odf.TextDocument(prm.Data);
            TemplateParser op = new TemplateParser(td);

            Dictionary<string, object> elements = new Dictionary<string, object>();
            DateTime dt = DateTime.Now;

            elements.Add("Дата", dt.ToString("D", Common.Localize.Data.Culture));
            elements.Add("ВсегоДокументов", base.ListSource.Count);
            elements.Add("Пользователь", User.CurrentUser);
            elements.Add("Параметры", prm.LabelText);

            Dictionary<string, object> subparam = new Dictionary<string, object>();
            List<Dictionary<string, object>> param = new List<Dictionary<string, object>>();
            foreach (DocumentListColumn colum in prm.PrintColumns)
                subparam.Add(colum.ColumnName, colum.DisplayName);
            param.Add(subparam);
            IEnumerable rows = listSource;
            // if (toolPrintSelected.Checked)
            //     rows = SelectedValues;
            foreach (DBItem row in rows)
            {
                subparam = new Dictionary<string, object>();
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
