using DataWF.Data;
using DataWF.Data.Gui;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Xwt.Drawing;
using System.IO;
using DataWF.Module.Counterpart;
//using System.Windows.Forms;

namespace DataWF.Module.FlowGui
{
    public class PDocument : LayoutDBTable
    {
        public new static ILayoutCellEditor InitCellEditor(object sender, object listItem, ILayoutCell cell)
        {
            ILayoutCell c = listItem is ILayoutCell && cell.Name == "Value" ? (ILayoutCell)listItem : cell;

            object data = ((LayoutList)sender).FieldSource != null ? ((LayoutList)sender).FieldSource : listItem;
            ILayoutCellEditor ed = null;
            if (c.Invoker != null)
            {
                if (c.Invoker.DataType == typeof(Template) || c.Invoker.DataType == typeof(User) ||
                    c.Invoker.DataType == typeof(Work) || c.Invoker.DataType == typeof(Stage) ||
                    c.Invoker.DataType == typeof(UserGroup) || c.Invoker.DataType == typeof(DBProcedure))
                {
                    ed = new CellEditorFlowTree();
                    ((CellEditorFlowTree)ed).DataType = c.Invoker.DataType;
                }
                else if (data is DocumentSearch && c.Invoker.DataType == typeof(DBItem))
                {
                    ed = new CellEditorFlowTree();
                    ((CellEditorFlowTree)ed).DataType = c.Invoker.DataType;
                    ((CellEditorFlowTree)ed).FlowKeys = FlowTreeKeys.Work | FlowTreeKeys.Stage | FlowTreeKeys.Template;
                }
                else if (data is User && c.Name == "Position")
                    ed = new CellEditorList() { DataSource = Position.DBTable.DefaultView };
                else if (data is GroupPermission && c.Name == "Permission")
                    ed = new CellEditorFlowParameters();
                else if (data is StageParam && c.Name == "Param")
                    ed = new CellEditorFlowParameters();
                else if (data is TemplateParam && c.Name == "Param")
                    ed = new CellEditorFlowParameters();
                else
                    ed = LayoutDBTable.InitCellEditor(listItem, cell);

                if (ed.GetType() == typeof(CellEditorTable))
                {
                    if (c.Invoker.DataType == typeof(Customer))
                        ((CellEditorTable)ed).Table = Customer.DBTable;
                    if (c.Invoker.DataType == typeof(Address))
                        ((CellEditorTable)ed).Table = Address.DBTable;
                    if (c.Invoker.DataType == typeof(Location))
                        ((CellEditorTable)ed).Table = Location.DBTable;
                }
            }

            return ed;
        }
        private Template viewmode;
        private CellStyle styleBold;

        public PDocument()
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

        [DefaultValue(null)]
        public override object FieldSource
        {
            get { return base.FieldSource; }
            set
            {
                if (value is Document)
                    viewmode = ((Document)value).Template;
                else
                    viewmode = null;
                //if (value is DocumentSynchParam)
                //    Table = FlowEnvir.Config.Document.Table;
                base.FieldSource = value;
            }
        }

        protected override string GetCacheKey()
        {
            if (viewmode != null)
                return "Template" + viewmode.Id + (_gridMode ? "List" : "");
            else if (ListType == typeof(Document))
                return "TempateAll" + (_gridMode ? "List" : "");
            else
                return base.GetCacheKey();
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
                this.ListType = this.ListType;
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

        public override bool GetCellReadOnly(object listItem, object itemValue, ILayoutCell cell)
        {
            if (cell?.Name == Document.DBTable.ParseColumn(nameof(Document.Important)).Name)
            {
                return false;
            }
            else if (cell != null && cell.Invoker != null && cell.Invoker is TemplateParam)
            {
                TemplateParam param = (TemplateParam)cell.Invoker;
                return param.Access.Edit;
            }
            return base.GetCellReadOnly(listItem, itemValue, cell);
        }

        protected override void OnGetProperties(LayoutListPropertiesArgs arg)
        {
            bool documented = GetIsDocument(arg.Cell, out var filter);
            if (documented)
            {
                arg.Properties = new List<string>();
                if (arg.Cell == null)
                {
                    if (filter == null || filter.IsCompaund)
                        arg.Properties.Add(Document.DBTable.ParseProperty(nameof(Document.Template)).Name);
                    if (Document == null)
                    {
                        arg.Properties.Add(Document.DBTable.ParseProperty(nameof(Document.Important)).Name);
                        arg.Properties.Add(Document.DBTable.ParseProperty(nameof(Document.WorkStage)).Name);
                        arg.Properties.Add(Document.DBTable.ParseProperty(nameof(Document.WorkUser)).Name);
                        arg.Properties.Add(Document.DBTable.ParseProperty(nameof(Document.WorkCurrent)).Name);
                    }
                    arg.Properties.Add(Document.DBTable.ParseProperty(nameof(Document.Id)).Name);
                    arg.Properties.Add(Document.DBTable.ParseProperty(nameof(Document.Date)).Name);
                }
                if (filter == null)
                {
                    foreach (TemplateParam p in TemplateParam.DBTable.DefaultView)
                        if (p.Type == ParamType.Column && p.Access.View && !arg.Properties.Contains(p.PrimaryCode))
                            arg.Properties.Add((arg.Cell == null ? string.Empty : arg.Cell.Name + ".") + p.PrimaryCode);
                }
                else
                {
                    foreach (TemplateParam templateParam in filter.TemplateAllParams)
                        if (templateParam.Type == ParamType.Column && templateParam.Access.View)
                            arg.Properties.Add(templateParam.PrimaryCode);
                }
                //List<DBColumn> cols = FlowEnvir.Config.Document.Table.Columns.GetByGroup(FlowEnvir.Config.Document.Name);
                //foreach (DBColumn col in cols)
                //    arg.Properties.Add(col.Code);
            }
            base.OnGetProperties(arg);
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

        protected override DBColumn ParseDBColumn(string name)
        {
            ILayoutCell cell = null;
            int index = name.LastIndexOf('.');
            if (index >= 0)
            {
                cell = ListInfo.Columns[name.Substring(0, index)] as ILayoutCell;
                name = name.Substring(index);
            }
            bool documented = GetIsDocument(cell, out var filter);
            if (documented)
            {
                TemplateParam tparam = filter != null ? filter.GetAttribute(name) :
                    TemplateParam.DBTable.LoadByCode(name, TemplateParam.DBTable.ParseColumn(nameof(TemplateParam.ParamCode)), DBLoadParam.None);
                if (tparam?.Param is DBColumn)
                    return (DBColumn)tparam.Param;
            }
            return base.ParseDBColumn(name);
        }

        protected override ILayoutCellEditor GetCellEditor(object listItem, object itemValue, ILayoutCell cell)
        {
            ILayoutCell f = listItem is LayoutField && cell.Name == "Value" ? (ILayoutCell)listItem : cell;

            if (f.CellEditor != null)
                return f.CellEditor;

            if (handleGetCellEditor != null)
            {
                f.CellEditor = handleGetCellEditor(this, listItem, cell);
                if (f.CellEditor != null)
                    return f.CellEditor;
            }
            return InitCellEditor(this, listItem, cell);
        }

        public override bool IsComplex(ILayoutCell cell)
        {
            if (cell?.Invoker is TemplateParam)
            {
                TemplateParam param = (TemplateParam)cell.Invoker;
                return param.GetColumn().IsReference;
            }
            return base.IsComplex(cell);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
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
