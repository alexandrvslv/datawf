using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DataWF.Data;
using Xwt;
using Mono.TextEditor;

namespace DataWF.Data.Gui
{
    [Project(typeof(SqlDocument), ".hql")]
    public class DataQuery : VPanel, IDockContent, IProjectEditor
    {
        public static Exception exception;
        private DBTable<DBItem> table = new DBTable<DBItem>("ExecuteQuery");
        private DateTime timeStart = DateTime.Now;
        private ExecuteEventArgs arg = new ExecuteEventArgs(null, null, null);
        private System.Timers.Timer timeInterval = new System.Timers.Timer();

        private ToolWindow tw = new ToolWindow();
        private ListEditor dfield = new ListEditor();
        private TableLayoutList list;
        private ToolItem toolSave;
        private ToolItem toolLoad;
        private ToolItem toolExecute;
        private ToolItem toolParser;
        private ToolItem toolStop;
        private ToolItem toolResult;
        private ToolItem toolGenerate;
        private ToolLabel toolTimer;
        private Toolsbar bar;
        private FindWindow toolFind;

        private ToolFieldEditor toolSchems;
        private TextEditor queryText;
        private HPaned container;

        public DataQuery()
        {
            tw.Mode = ToolShowMode.Dialog;
            tw.ButtonAccept.Click += ButtonAccept_Click;

            timeInterval.Interval = 1000;
            timeInterval.Elapsed += TimeIntervalTick;

            toolSave = new ToolItem(ToolSaveClick) { Name = "Save", Glyph = GlyphType.SaveAlias };
            toolLoad = new ToolItem(ToolLoadClick) { Name = "Load", Glyph = GlyphType.FolderOpen };
            toolExecute = new ToolItem(ToolExecuteClick) { Name = "Execute", Glyph = GlyphType.Play };
            toolParser = new ToolItem(ToolParseClick) { Name = "Parse", Glyph = GlyphType.Code };
            toolGenerate = new ToolItem(toolGenerateClick) { Name = "Generate", Glyph = GlyphType.GearAlias };
            toolStop = new ToolItem(ToolStopClick) { Name = "Stop", Glyph = GlyphType.Stop };
            toolResult = new ToolItem(ToolSpliterClick) { Name = "Result", CheckOnClick = true, Glyph = GlyphType.List };
            toolTimer = new ToolLabel { Name = "Timer", Text = "_:_" };
            toolSchems = new ToolFieldEditor
            {
                Name = "toolSchems",
                Editor = new CellEditorList
                {
                    DataType = typeof(DBSchema),
                    DataSource = DBService.Schems
                }
            };

            //var fieldstring = new DataField<string>();
            //var fieldschema = new DataField<DBSchema>();
            //ListStore store = new ListStore(fieldstring, fieldschema);
            //foreach (var schema in DBService.Schems)
            //store.SetValues(store.AddRow(), fieldstring, schema.Name, fieldschema, schema);
            //toolSchems.ComboBox.Views.Add(new TextCellView(fieldstring));
            //toolSchems.ComboBox.ItemsSource = store;
            //toolSchems.ComboBox.DataSource = DBService.Schems;

            bar = new Toolsbar(
                toolSchems,
                toolLoad,
                toolSave,
                new ToolSeparator(),
                toolExecute,
                toolStop,
                new ToolSeparator(),
                toolGenerate,
                toolParser,
                new ToolSeparator() { FillWidth = true },
                toolResult,
                toolTimer)
            { Name = "bar" };

            queryText = new TextEditor { Name = "queryText", Text = "" };
            queryText.Document.MimeType = "text/x-sql";
            //queryText.TextArea.ColorStyle = Mono.TextEditor.Highlighting.ColorScheme.LoadFrom[""];

            var scroll = new ScrollView() { Content = queryText };

            list = new TableLayoutList()
            {
                GenerateToString = false,
                Visible = false
            };

            container = new HPaned();
            container.Panel1.Content = scroll;
            container.Panel2.Content = list;

            Name = "DataQuery";
            PackStart(bar, false, false);
            PackStart(container, true, true);

            Localize();

            toolFind = new FindWindow { Editor = queryText };
            CurrentSchema = DBService.DefaultSchema;
        }

        public string Query
        {
            get { return queryText.Text; }
            set { queryText.Text = value; }
        }

        private void TimeIntervalTick(object sender, EventArgs e)
        {
            Application.Invoke(() =>
            {
                toolTimer.Text = (DateTime.Now - timeStart).ToString("hh':'mm':'ss");
                toolTimer.Visible = true;
            });
        }

        public DockType DockType
        {
            get { return DockType.Content; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        public override void Localize()
        {
            base.Localize();
            bar.Localize();
            list.Localize();
            GuiService.Localize(this, "DataQuery", "Data Query", GlyphType.FileText);
        }


        public DBSchema CurrentSchema
        {
            get { return toolSchems.DataValue as DBSchema; }
            set { toolSchems.DataValue = value; }
        }

        #region IProjectEditor implementation

        private ProjectHandler project;

        public ProjectHandler Project
        {
            get
            {
                if (project == null)
                    project = new ProjectHandler() { Type = new ProjectType(this.GetType(), typeof(SqlDocument), "*.hql") };
                ((SqlDocument)project.Project).Text = queryText.Text;
                return project;
            }
            set
            {
                if (value == project)
                    return;
                project = value;
                Reload();
            }
        }

        public void Reload()
        {
            if (project != null && project.Project != null)
            {
                queryText.Text = ((SqlDocument)project.Project).Text;
                Name = project.Name;
            }
        }

        #endregion

        public void ExecuteAsynch(ExecuteEventArgs arg)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    Execute(arg);
                    Application.Invoke(() => Callback());
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }

            });

            timeStart = DateTime.Now;
            timeInterval.Start();
        }

        private void Callback()
        {
            timeInterval.Stop();
            toolExecute.Sensitive = true;
            toolStop.Sensitive = false;

        }

        public object Execute(ExecuteEventArgs arg)
        {
            object flag = null;

            var regex = new Regex(@"\s*go\s*(\n|$)", RegexOptions.IgnoreCase);
            string[] split = regex.Split(arg.Query);
            foreach (string s in split)
            {
                string query = s.Trim();
                if (query.Length == 0)
                    continue;
                if (arg.Cancel)
                    break;
                using (var transaction = new DBTransaction(arg, arg.Schema.Connection))
                {
                    var command = transaction.AddCommand(query);
                    command.CommandTimeout = 30000;
                    try
                    {
                        arg.Table.Access = null;
                        using (transaction.Reader = transaction.ExecuteQuery(command, DBExecuteType.Reader) as IDataReader)
                        {
                            arg.Table.CheckColumns(transaction);
                            Application.Invoke(() => list.ResetColumns());
                            while (transaction.Reader.Read())
                            {
                                if (arg.Cancel)
                                {
                                    command.Cancel();
                                    break;
                                }
                                if (arg.Table != null)
                                {
                                    arg.Table.Add(arg.Table.LoadItemFromReader(transaction));
                                    flag = arg.Table.Count;
                                }
                                else
                                {
                                    flag = transaction.Reader.GetValue(0);
                                    break;
                                }
                            }
                            transaction.Reader.Close();
                        }
                        if (GuiService.Main != null)
                            GuiService.Main.SetStatus(new StateInfo("Data Query", "Execution complete!", s));
                    }
                    catch (Exception ex)
                    {
                        if (GuiService.Main != null)
                            GuiService.Main.SetStatus(new StateInfo("Data Query", ex.Message, s, StatusType.Error));
                        flag = ex;
                        break;
                    }
                    if (!(flag is Exception))
                        transaction.Commit();
                }
            }
            return flag;
        }


        protected override void Dispose(bool disposing)
        {
            table.Dispose();
            base.Dispose(disposing);
        }

        private void ToolLoadClick(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filters.Add(new FileDialogFilter("Query", "*.sql"));
                if (dialog.Run(ParentWindow))
                {
                    string file = dialog.FileName;
                    queryText.Text = System.IO.File.ReadAllText(file);
                }
            }
        }

        private void ToolSaveClick(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filters.Add(new FileDialogFilter("Query", "*.sql"));
                if (dialog.Run(ParentWindow))
                {
                    string file = dialog.FileName;
                    System.IO.File.WriteAllText(file, queryText.Text, System.Text.Encoding.UTF8);
                }
            }
        }

        private void ToolStopClick(object sender, EventArgs e)
        {
            arg.Cancel = true;
        }


        private void ToolExecuteClick(object sender, EventArgs e)
        {
            if (CurrentSchema == null)
                return;
            table.Schema = CurrentSchema;
            if (list.ListSource == null)
                list.ListSource = table.CreateView();
            table.Clear();
            table.Columns.Clear();
            table.BlockSize = 5000;
            if (!toolResult.Checked)
                if (queryText.Text.IndexOf("select", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ToolSpliterClick(null, null);
                }
            toolExecute.Sensitive = false;
            toolStop.Sensitive = true;

            arg.Cancel = false;
            arg.Schema = CurrentSchema;
            arg.Table = table;
            arg.Query = queryText.Text;

            ExecuteAsynch(arg);
        }

        private void ToolSpliterClick(object sender, EventArgs e)
        {
            list.Visible = toolResult.Checked;
        }


        private void toolGenerateClick(object sender, EventArgs e)
        {
            var param = new DBExport
            {
                Mode = ExportMode.Patch,
                Stamp = DateTime.Today
            };

            var dtree = new DataTree
            {
                AllowCheck = true,
                DataKeys = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table,
                DataFilter = CurrentSchema
            };

            var list = new LayoutList
            {
                EditMode = EditModes.ByClick,
                FieldSource = param
            };

            var box = new VPaned();
            box.Panel1.Resize = false;
            box.Panel1.Content = list;
            box.Panel2.Content = dtree;

            tw.Target = box;
            tw.Show(this, new Point(0, 0));
            tw.ButtonAccept.Click += (o, a) =>
            {
                var tables = new List<DBTable>();
                foreach (Node n in dtree.Nodes)
                {
                    if (n.Check && n.Tag is DBTable && ((DBTable)n.Tag).Type == DBTableType.Table)
                    {
                        tables.Add(n.Tag as DBTable);
                    }
                }
                this.queryText.Text += param.GeneratePatch(tables);
            };
        }

        private void ParceXlsx(string fileName)
        {
            var builder = new StringBuilder();
            DBTable table = null;

            using (var xl = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(fileName, false))
            {
                var sp = xl.WorkbookPart.SharedStringTablePart;
                foreach (DocumentFormat.OpenXml.Packaging.WorksheetPart part in xl.WorkbookPart.WorksheetParts)
                {
                    var worksheet = part.Worksheet;
                    var sd = worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
                    foreach (DocumentFormat.OpenXml.OpenXmlElement element in sd)
                    {
                        var row = element as DocumentFormat.OpenXml.Spreadsheet.Row;
                        if (row != null)
                        {
                            if (table == null)
                            {
                                table = new DBTable<DBItem>("sheet");
                                table.Schema = CurrentSchema;

                                foreach (DocumentFormat.OpenXml.OpenXmlElement celement in element)
                                {
                                    DocumentFormat.OpenXml.Spreadsheet.Cell cell = celement as DocumentFormat.OpenXml.Spreadsheet.Cell;
                                    if (cell != null)
                                    {
                                        DBColumn column = new DBColumn(Parser.ReadCell(cell, sp));
                                        table.Columns.Add(column);
                                    }
                                }

                                builder.AppendLine("-- -================================= " + table.Name + " =================================");
                                builder.AppendLine(table.FormatSql(DDLType.Create));
                                builder.AppendLine("go");
                                builder.AppendLine();
                                builder.AppendLine("-- -================================= Data =================================");

                            }
                            else
                            {
                                int index = 0;
                                DBItem drow = table.NewItem();
                                foreach (DocumentFormat.OpenXml.OpenXmlElement celement in element)
                                {
                                    DocumentFormat.OpenXml.Spreadsheet.Cell cell = celement as DocumentFormat.OpenXml.Spreadsheet.Cell;
                                    if (cell != null && index < table.Columns.Count)
                                    {
                                        drow[index] = Parser.ReadCell(cell, sp);
                                        index++;
                                    }
                                }
                                builder.AppendLine(CurrentSchema.System.FormatInsert(drow));
                            }
                        }
                    }
                    if (table != null)
                        break;
                }
            }
            this.queryText.Text += builder.ToString();
        }

        private void ToolParseClick(object sender, EventArgs e)
        {
            tw.Target = dfield;
            dfield.DataSource = new DQParserParam();
            tw.Show(bar, new Point(toolGenerate.Bound.X, bar.Size.Height));
        }

        private void ButtonAccept_Click(object sender, EventArgs e)
        {
            var sb = new StringBuilder();

            if (this.tw.Target == dfield)
            {
                DQParserParam param = (DQParserParam)this.dfield.DataSource;
                string[] split = null;
                if (param.FileName == null || param.FileName.Length == 0)
                {
                    split = Regex.Split(this.queryText.Text, param.RecordSeparator);
                }
                else if (System.IO.File.Exists(param.FileName))
                {
                    if (System.IO.Path.GetExtension(param.FileName) == ".xlsx")
                    {
                        ParceXlsx(param.FileName);
                        return;
                    }
                    string s = System.IO.File.ReadAllText(param.FileName, param.Encod.GetEncoding());
                    split = Regex.Split(s, param.RecordSeparator);
                }

                if (split.Length > 1)
                {
                    var table = new DBTable<DBItem>(param.TableName);
                    table.Schema = CurrentSchema;
                    string[] csplit = Regex.Split(split[0], param.FieldSeparator);
                    for (int i = 0; i < csplit.Length; i++)
                    {
                        DBColumn column = new DBColumn(csplit[i].Trim().Replace(" ", "_"));
                        table.Columns.Add(column);
                    }
                    sb.AppendLine("-- -================================= " + table.Name + " =================================");
                    sb.AppendLine(table.FormatSql(DDLType.Create));
                    sb.AppendLine("go");
                    sb.AppendLine();
                    sb.AppendLine("-- -================================= Data =================================");

                    for (int i = 1; i < split.Length; i++)
                    {
                        if (split[i].Length != 0)
                        {
                            DBItem row = table.NewItem();
                            string[] rsplit = Regex.Split(split[i], param.FieldSeparator);
                            for (int j = 0; j < rsplit.Length; j++)
                            {
                                string val = rsplit[j].Trim();
                                object dbval = DBNull.Value;
                                if (val.Length != 0)
                                    dbval = val;
                                row[j] = dbval;
                            }
                            sb.AppendLine(CurrentSchema.System.FormatInsert(row));
                        }
                    }
                    sb.AppendLine("go");
                }
            }
            this.queryText.Text += sb.ToString();
        }

        public bool CloseRequest()
        {
            throw new NotImplementedException();
        }

#if GTK
        void QueryTextOnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        void QueryTextOnDragDrop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {

                TreeNode NewNode = (TreeNode)e.Data.GetData(typeof(TreeNode));

                if (NewNode.Tag is DBTable)
                {
                    queryText.Text = DBService.BuildQuery((DBTable)NewNode.Tag, "", ((DBTable)NewNode.Tag).Columns, false);
                    this.CurrentSchema = ((DBTable)NewNode.Tag).Schema;
                }
                //NewNode.Parent.Remove ();
                //DestinationNode.Nodes.Add (NewNode);
                //DestinationNode.Expand ();
                //Remove Original Node
                //NewNode.Remove();
            }
        }
#endif

    }

    public class DQParserParam
    {
        public DQParserParam()
        {
            FileName = string.Empty;
            TableName = "newtable";
            EncodingInfo[] list = Encoding.GetEncodings();
            foreach (EncodingInfo item in list)
                if (item.CodePage == Encoding.Default.CodePage)
                {
                    Encod = item;
                    break;
                }
            RecordSeparator = @"\r\n|\r";
            FieldSeparator = @"\t";
            DateFormat = "dd'.'MM'.'yyyy hh':'mm':'ss";
        }

        [System.ComponentModel.Description("Path")]
        public string FileName { get; set; }

        public EncodingInfo Encod { get; set; }

        public string EncodName
        {
            get { return Encod.DisplayName; }
        }

        public string TableName { get; set; }

        public string RecordSeparator { get; set; }

        public string FieldSeparator { get; set; }

        public string DateFormat { get; set; }
    }
}
