using DataWF.Common;
using DataWF.Gui;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Xwt;
using Xwt.Drawing;
using Xwt.Formats;

namespace DataWF.Data.Gui
{
    public enum SearchState
    {
        Edit,
        Reference
    }

    [Project(typeof(QQuery), ".dbq")]
    public class QueryEditor : VPanel, IProjectEditor, ILocalizable
    {
        private DBTable table;
        private QQuery query;
        private QParam owner;
        private SearchState state;
        private ProjectHandler project;

        private LayoutList listParams = new LayoutList();
        private LayoutList listColumns = new LayoutList();
        private LayoutList listTables = new LayoutList();
        private QueryResultView viewResult = new QueryResultView();
        private Toolsbar tools = new Toolsbar();
        private ToolDropDown toolAdd = new ToolDropDown();
        private ToolItem toolAddGroup = new ToolItem();
        private ToolDropDown toolAddColumn = new ToolDropDown();
        private ToolItem toolDelete = new ToolItem();
        private ToolItem toolUp = new ToolItem();
        private ToolItem toolDown = new ToolItem();
        private ToolItem toolParce = new ToolItem();
        private ToolItem tooSearch = new ToolItem();
        //private ToolStripButton tooSave;
        private ToolLabel toolCount = new ToolLabel();
        private ToolFieldEditor toolTable = new ToolFieldEditor();
        private GroupBox map = new GroupBox();
        private GroupBoxItem gparam = new GroupBoxItem();
        private GroupBoxItem gcolumn = new GroupBoxItem();
        private GroupBoxItem gtable = new GroupBoxItem();
        private GroupBoxItem gtext = new GroupBoxItem();
        private GroupBoxItem gresult = new GroupBoxItem();


        private RichTextView textQuery = new RichTextView();

        public QueryEditor()
        {
            gparam.Widget = listParams;
            gparam.FillHeight = true;
            gparam.FillWidth = true;
            gparam.Expand = true;
            gparam.Glyph = GlyphType.Paragraph;
            gparam.Row = 0;
            //
            gcolumn.Widget = listColumns;
            gcolumn.FillHeight = true;
            gcolumn.FillWidth = true;
            gcolumn.Expand = false;
            gcolumn.Glyph = GlyphType.At;
            gcolumn.Row = 1;
            //
            gtable.Widget = listTables;
            gtable.FillHeight = false;
            gtable.FillWidth = true;
            gtable.DefaultHeight = 100;
            gtable.Expand = false;
            gtable.Glyph = GlyphType.Table;
            gtable.Row = 2;
            //
            gtext.Widget = textQuery;
            gtext.FillHeight = false;
            gtext.FillWidth = true;
            gtext.DefaultHeight = 100;
            gtext.Expand = false;
            gtable.Glyph = GlyphType.Question;
            gtext.Row = 3;
            //
            gresult.Widget = viewResult;
            gresult.FillHeight = true;
            gresult.FillWidth = true;
            gresult.Expand = false;
            gresult.Row = 4;

            map.Add(gparam);
            map.Add(gcolumn);
            map.Add(gtable);
            map.Add(gtext);
            map.Add(gresult);

            tools.Items.Add(toolTable);
            tools.Items.Add(toolAdd);
            tools.Items.Add(toolAddGroup);
            tools.Items.Add(toolDelete);
            tools.Items.Add(toolUp);
            tools.Items.Add(toolDown);
            tools.Items.Add(new ToolSeparator() { Visible = true });
            tools.Items.Add(tooSearch);
            tools.Items.Add(toolCount);
            tools.Items.Add(toolAddColumn);
            tools.Name = "tools";

            toolTable.Name = "toolTable";
            toolTable.Text = "";

            toolAdd.Name = "toolAdd";
            toolAdd.Tag = "";
            toolAdd.Text = "Add";
            toolAdd.DropDown = new Menubar { Name = "Add" };

            toolAddGroup.Name = "toolAddGroup";
            toolAddGroup.Text = "Group";
            toolAddGroup.Click += ToolAddGroupClick;

            toolDelete.Name = "toolDelete";
            toolDelete.Text = "Delete";
            toolDelete.Click += ToolDeleteClick;

            toolUp.Name = "toolUp";
            toolUp.Glyph = GlyphType.ArrowUp;
            toolUp.Click += ToolUpClick;

            toolDown.Name = "toolDown";
            toolDown.Glyph = GlyphType.ArrowDown;
            toolDown.Click += ToolDownClick;

            tooSearch.Name = "tooSearch";
            tooSearch.Click += TooSearchClick;

            toolCount.Name = "toolCount";
            toolCount.Text = "<результаты>";

            toolAddColumn.Name = "toolAddColumn";
            toolAddColumn.Tag = "";

            toolParce.Name = "toolParce";
            toolParce.Tag = "";
            toolParce.Click += ToolParseClick;

            listParams.AllowColumnMove = false;
            listParams.AllowHeaderSize = false;
            listParams.AllowSort = false;
            listParams.EditMode = EditModes.ByClick;
            listParams.EditState = EditListState.Edit;
            listParams.FieldSource = null;
            listParams.Mode = LayoutListMode.List;
            listParams.Name = "listParams";

            textQuery.Name = "textBox1";

            listColumns.AllowColumnMove = false;
            listColumns.AllowHeaderSize = false;
            listColumns.AllowSort = false;
            listColumns.EditMode = EditModes.ByClick;
            listColumns.EditState = EditListState.Edit;
            listColumns.FieldSource = null;
            listColumns.GenerateColumns = false;
            listColumns.Mode = LayoutListMode.List;
            listColumns.Name = "listColumns";

            this.Name = "QueryEditor";
            this.Text = "Search";
            PackStart(tools, false, false);
            PackStart(map, true, true);

            Localize();

            listParams.GenerateColumns = false;
            listParams.ListInfo.Columns.Add(new LayoutColumn()
            {
                Name = nameof(ToString),
                Width = 120,
                Invoker = ToStringInvoker<object>.Instance
            });
            listParams.ListInfo.Columns.Add(new LayoutColumn()
            {
                Name = nameof(QParam.Group),
                Width = 80,
                Visible = false,
                Invoker = new ActionInvoker<QParam, QParam>(nameof(QParam.Group),
                                                      (item) => item.Group,
                                                      (item, value) => item.Group = value),
                CellEditor = new CellEditorQueryGroup() { DataType = typeof(QParam) }
            });
            listParams.ListInfo.Columns.Add(new LayoutColumn()
            {
                Name = nameof(QParam.Order),
                Visible = false,
                Invoker = new ActionInvoker<QParam, int>(nameof(QParam.Order),
                                                   (item) => item.Order,
                                                   (item, value) => item.Order = value)
            });
            listParams.ListInfo.Columns.Add(new LayoutColumn()
            {
                Name = nameof(QParam.Query),
                Visible = false,
                Invoker = new ActionInvoker<QParam, IQuery>(nameof(QParam.Query),
                                                   (item) => item.Query)
                //(item, value) => item.Query = value
            });
            listParams.ListInfo.Columns.Add(new LayoutColumn()
            {
                Name = nameof(QParam.Logic),
                Width = 70,
                Invoker = new ActionInvoker<QParam, LogicType>(nameof(QParam.Logic),
                                                          (item) => item.Logic,
                                                          (item, value) => item.Logic = value),
                CellEditor = new CellEditorList()
                {
                    DataType = typeof(LogicType),
                    DataSource = new SelectableList<LogicType>(new[] {
                        LogicType.And,
                        LogicType.Or,
                        LogicType.AndNot,
                        LogicType.OrNot })
                }
            });
            listParams.ListInfo.Columns.Add(new LayoutColumn()
            {
                Name = nameof(QParam.LeftColumn),
                Width = 80,
                Visible = false,
                Invoker = new ActionInvoker<QParam, DBColumn>(nameof(QParam.LeftColumn),
                                                         (item) => item.LeftColumn,
                                                         (item, value) => item.LeftColumn = value),
                CellEditor = new CellEditorList() { DataType = typeof(DBColumn) }
            });
            listParams.ListInfo.Columns.Add(new LayoutColumn()
            {
                Name = nameof(QParam.Comparer),
                Width = 80,
                Invoker = new ActionInvoker<QParam, CompareType>(nameof(QParam.Comparer),
                                                        (item) => item.Comparer,
                                                        (item, value) => item.Comparer = value),
                CellEditor = new CellEditorList()
                {
                    DataType = typeof(CompareType),
                    DataSource = new SelectableList<CompareType>(new[]{
                        CompareType.Equal,
                        CompareType.NotEqual,
                        CompareType.Like,
                        CompareType.NotLike,
                        CompareType.In,
                        CompareType.NotIn,
                        CompareType.Greater,
                        CompareType.GreaterOrEqual,
                        CompareType.Less,
                        CompareType.LessOrEqual,
                        CompareType.Between,
                        CompareType.NotBetween})
                }
            });
            listParams.ListInfo.Columns.Add(new LayoutColumn()
            {
                Name = nameof(QParam.RightValue),
                Width = 250,
                Invoker = new ActionInvoker<QParam, object>(nameof(QParam.RightValue),
                                                           (item) => item.RightValue,
                                                           (item, value) => item.RightValue = value)
            });
            listParams.ListInfo.Sorters.Add(new LayoutSort(nameof(QParam.Query), ListSortDirection.Ascending, true));
            listParams.ListInfo.Sorters.Add(new LayoutSort(nameof(QParam.Order), ListSortDirection.Ascending, false));
            listParams.ListInfo.Tree = true;
            listParams.ListInfo.HeaderVisible = false;
            listParams.RetriveCellEditor += ListParamsRetriveCellEditor;


            toolTable.Editor = new CellEditorDataTree
            {
                DataKeys = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table,
                DataType = typeof(DBTable)
            };
            toolTable.Field.BindData(this, nameof(Table));
        }

        ILayoutCellEditor ListParamsRetriveCellEditor(object listItem, object value, ILayoutCell cell)
        {
            if (cell.Name == nameof(QParam.RightValue))
            {
                QParam param = (QParam)listItem;
                ILayoutCellEditor editor = null;
                if (param != null && param.LeftColumn != null && !editors.TryGetValue(param, out editor))
                {
                    if ((param.LeftColumn.IsPrimaryKey || param.LeftColumn.IsReference) && param.Comparer.Type == CompareTypes.In)
                    {
                        if (!(param.RightValue is QQuery) && param.LeftColumn.IsReference && param.RightValue == null)
                        {
                            var sub = new QQuery(string.Empty, param.LeftColumn.ReferenceTable);
                            sub.BuildColumn(param.LeftColumn.ReferenceTable.PrimaryKey);
                            param.RightItem = sub;
                        }
                        editor = new CellEditorQuery();
                    }
                    else
                    {
                        editor = TableLayoutList.InitCellEditor(param.LeftColumn);
                        if (param.LeftColumn.DataType == typeof(DateTime) && param.Comparer.Equals(CompareType.Between))
                            ((CellEditorDate)editor).TwoDate = true;
                    }
                    editors[param] = editor;
                }
                return editor;
            }
            return null;
        }

        private Dictionary<QParam, ILayoutCellEditor> editors = new Dictionary<QParam, ILayoutCellEditor>();



        #region IProjectEditor implementation

        public ProjectHandler Project
        {
            get { return project; }
            set
            {
                if (project == value)
                    return;
                project = value;
                if (project != null)
                    Query = project.Project as QQuery;
            }
        }

        public void Reload()
        {
            if (project != null)
                Query = project.Project as QQuery;
        }

        #endregion

        public QQuery Query
        {
            get { return query; }
            set
            {
                query = value;

                if (value != null)
                {
                    Table = value.Table;

                    value.Parameters.CollectionChanged += ParametersListChanged;
                    var parameterList = new SelectableList<QParam>(value.GetAllParameters());
                    listParams.ListSource = parameterList.DefaultView;
                    listColumns.ListSource = value.Columns;
                    listTables.ListSource = value.Tables;
                }
            }
        }

        private void ParametersListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //TODO if (e.Action == NotifyCollectionChangedAction.Replace && ((NotifyListItemChangedEventArgs)e).Property == "Comparer")
            //{
            //    var parameter = e.NewItems.Cast<QParam>().FirstOrDefault();
            //    editors.Remove(parameter);
            //    parameter.Value = null;
            //}

            textQuery.LoadText(Query.Format(), TextFormat.Plain);
        }

        public DBTable Table
        {
            get { return table as DBTable; }
            set
            {
                if (table != value)
                {
                    if (Query == null)
                        Query = new QQuery(string.Empty, value);

                    toolAdd.DropDownItems.Clear();

                    table = value;
                    //if (toolTable.DropDownItems.Count == 0)
                    //    toolTable.DropDownItems.Add(InitTableTool(value));
                    if (state == SearchState.Edit)
                        if (Query != null)
                            Query.Table = value;

                    if (table != null)
                    {
                        this.Text = "Query (" + table.ToString() + ")";
                        foreach (DBColumn item in value.Columns)
                        {
                            var itemL = new ToolMenuItem
                            {
                                Tag = item,
                                Text = item.ToString()
                            };
                            itemL.Click += ColumnItemClick;
                            toolAdd.DropDownItems.Add(itemL);
                        }
                    }
                    var column = (LayoutColumn)listParams.ListInfo.Columns[nameof(QParam.LeftColumn)];
                    ((CellEditorList)column.CellEditor).DataSource = value?.Columns;

                    OnPropertyChanged(nameof(Table));
                }
            }
        }

        //public void IntDBSchema()
        //{
        //    toolTable.DropDownItems.Clear();
        //    if (state == SearchState.Edit)
        //        foreach (DBSchema sc in DBService.Schems)
        //            toolTable.DropDownItems.Add(InitSchemaTool(sc));
        //    else if (state == SearchState.Reference && owner != null)
        //        if (owner.Column.IsPrimaryKey)
        //        {
        //            List<DBRelation> relations = owner.Column.Table.GetChildRelations();
        //            foreach (DBRelation relation in relations)
        //                toolTable.DropDownItems.Add(InitTableTool(relation.Table));

        //        }
        //        else if (owner.Column.ReferenceTable != null)
        //        {
        //            toolTable.DropDownItems.Add(InitTableTool(owner.Column.ReferenceTable));
        //            Table = owner.Column.ReferenceTable;
        //            toolTable.Sensitive = false;
        //        }
        //}

        protected ToolMenuItem InitSchemaTool(DBSchema schema)
        {
            var item = new ToolMenuItem { Tag = schema, Name = schema.Name, Text = schema.ToString(), DropDown = new Menubar { Name = schema.Name } };
            var list = new SelectableList<DBTable>(schema.Tables);
            list.ApplySortInternal("Name", ListSortDirection.Ascending);
            foreach (DBTable ts in list)
            {
                if (ts.Access.GetFlag(AccessType.Admin, GuiEnvironment.User))
                {
                    item.DropDown.Items.Add(InitTableTool(ts));
                }
            }
            return item;
        }

        private ToolMenuItem InitColumnTool(DBColumn column)
        {
            return new ToolMenuItem()
            {
                Tag = column,
                Name = column.Name,
                Text = column.ToString()
            };
        }

        private ToolMenuItem InitTableTool(DBTable table)
        {
            return new ToolMenuItem(TableClick)
            {
                Tag = table,
                Name = table.Name,
                Text = table.ToString()
            };
        }

        private void ColumnItemClick(object sender, EventArgs e)
        {
            var column = ((ToolMenuItem)sender).Tag as DBColumn;
            Query.Parameters.Add(new QParam(column) { Logic = LogicType.And });
        }

        private void TableClick(object sender, EventArgs e)
        {
            Table = ((ToolMenuItem)sender).Tag as DBTable;
        }

        private ToolMenuItem InitQueryTool(QQuery query)
        {
            return new ToolMenuItem
            {
                Tag = query,
                Name = query.GetHashCode().ToString(),
                Text = query.ToString()
            };
        }

        protected void SetQuery(SearchState state, QQuery query, QParam owner)
        {
            this.state = state;
            this.owner = owner;
            Query = query;
        }

        public void Initialize(SearchState state, QQuery expression, QParam owner, IDockMain mainForm)
        {
            //this.mainForm = mainForm;
            SetQuery(state, expression, owner);
            //IntDBSchema();
        }

        private void ToolDeleteClick(object sender, EventArgs e)
        {
            if (listParams.SelectedItem == null)
                return;
            QParam param = (QParam)listParams.SelectedItem;
            param.List.Delete(param);
        }

        private void ToolUpClick(object sender, EventArgs e)
        {

        }

        private void ToolDownClick(object sender, EventArgs e)
        {

        }

        private void ToolStateClick(object sender, EventArgs e)
        {

        }

        private async void TooSearchClick(object sender, EventArgs e)
        {
            if (query == null || query.Table == null)
                return;

            if (viewResult.IsLoad())
            {
                if (MessageDialog.AskQuestion("Executing", "Suspend?", Command.No, Command.Yes) == Command.Yes)
                    viewResult.LoadCancel();
                return;
            }
            viewResult.SetCommand(query.ToCommand(false), query.Table.Schema, query.Table.Name + query.Order.ToString());
            await viewResult.SyncAsync();
            toolCount.Text = "<Executing>";
            gresult.Expand = true;
        }

        private void ToolCreateClick(object sender, EventArgs e)
        {
            if (Table == null)
                return;

            //if (DataEnvir.Items.Queries.Contains (toolExpName.Text)) {
            //	MessageBox.Show ("Запрос с данным кодом уже имеется!");
            //	return;
            //}

            Table = Table;
            Query = new QQuery { Table = Table };
            //DataEnvir.Items.Queries.Add (expression);
        }

        //private void toolExpression_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        //{
        //    QQuery exp = e.ClickedItem.Tag as QQuery;
        //    Query = exp;
        //}

        private void ToolParseClick(object sender, EventArgs e)
        {
            if (Query == null)
                Query = new QQuery();

            var test = textQuery.PlainText;
            Query.Parse(test);
        }

        private void ToolColumnsClick(object sender, EventArgs e)
        {

        }

        private void ToolAddGroupClick(object sender, EventArgs e)
        {
            var list = listParams.Selection.GetItems<QParam>();
            if (list.Count > 0)
            {
                QParam group = Query.Add();
                foreach (var s in list)
                {
                    group.Parameters.Add(s);
                }
            }
        }

        private void ListOnCellValueChanged(object sender, EventArgs e)
        {
            textQuery.LoadText(Query == null ? "<empty>" : Query.Format(), TextFormat.Plain);
        }

        public override void Localize()
        {
            string name = "QueryEditor";
            toolAdd.Text = Locale.Get(name, "Add");
            toolAdd.Image = (Image)Locale.GetImage(name, "Add");
            toolAddGroup.Text = Locale.Get(name, "Group");
            toolAddGroup.Image = (Image)Locale.GetImage(name, "Group");
            toolDelete.Text = Locale.Get(name, "Delete");
            toolDelete.Image = (Image)Locale.GetImage(name, "Delete");
            toolUp.Text = Locale.Get(name, "Up");
            toolUp.Image = (Image)Locale.GetImage(name, "Up");
            toolDown.Text = Locale.Get(name, "Down");
            toolDown.Image = (Image)Locale.GetImage(name, "Down");
            tooSearch.Text = Locale.Get(name, "Execute");
            tooSearch.Image = (Image)Locale.GetImage(name, "Execute");
            toolParce.Text = Locale.Get(name, "Parse");
            toolParce.Image = (Image)Locale.GetImage(name, "Parse");
            gtext.Text = Locale.Get(name, "Query Text");
            gparam.Text = Locale.Get(name, "Parameters");
            gcolumn.Text = Locale.Get(name, "Columns");
            gtable.Text = Locale.Get(name, "Tables");
            gresult.Text = Locale.Get(name, "Results");
        }

        protected override void Dispose(bool disposing)
        {
            viewResult.Dispose();
            base.Dispose(disposing);
        }

        public bool CloseRequest()
        {
            throw new NotImplementedException();
        }
    }
}
