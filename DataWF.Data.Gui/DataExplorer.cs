using DataWF.Common;
using DataWF.Gui;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Data.Gui
{
    [Module(true)]
    public class DataExplorer : VPanel, IDockContent, IGlyph
    {
        private ToolWindow itemWindow = new ToolWindow();
        private ListExplorer listExplorer = new ListExplorer();
        private DataTree dataTree;
        private Toolsbar barMain;
        private Toolsbar barChanges;
        private Menubar contextMain;
        private Menubar contextAdd;
        private Menubar contextTools;
        private VPaned container;
        private LayoutList changesView;
        private VBox panel1;
        private VBox panel2;

        public DataExplorer()
        {
            contextTools = new Menubar(
                new ToolMenuItem(ToolMainRefreshOnClick) { Name = "Refresh Tree", GlyphColor = Colors.DarkBlue, Glyph = GlyphType.Refresh },
                new ToolMenuItem(ToolDBCheckClick) { Name = "Check Connection" },
                new ToolMenuItem(ToolDBRefreshClick) { Name = "Refresh Schema Info" },
                new ToolMenuItem(ToolDBGenerateClick) { Name = "Generate Database" },
                new ToolSeparator(),
                new ToolMenuItem(ToolDBExportClick) { Name = "Export" },
                new ToolMenuItem(ToolPatchCreateClick) { Name = "Patch Create" },
                new ToolMenuItem(ToolPatchLoadClick) { Name = "Patch Load" },
                new ToolSeparator(),
                new ToolMenuItem(ToolTableRefreshOnClick) { Name = "Refresh Table Info" },
                new ToolMenuItem(ToolTableReportOnClick) { Name = "Table Report" },
                new ToolMenuItem(ToolTableExplorerOnClick) { Name = "Table Explorer" },
                new ToolMenuItem(ToolSequenceRefreshOnClick) { Name = "Refresh Sequence" },
                new ToolSeparator(),
                new ToolMenuItem(ToolExtractDDLClick) { Name = "Extract DDL" },
                new ToolMenuItem(ToolSerializeClick) { Name = "Serialize" },
                new ToolMenuItem(ToolDeserializeClick) { Name = "Deserialize" },
                new ToolMenuItem(ToolLoadFileClick) { Name = "Load Files" })
            { Name = "DataExplorer" };

            contextAdd = new Menubar(
                new ToolMenuItem(ToolAddConnectionClick) { Name = "Connection", Glyph = GlyphType.Connectdevelop },
                new ToolMenuItem(ToolAddSchemaClick) { Name = "Schema", Glyph = GlyphType.Database },
                new ToolMenuItem(ToolAddTableGroupClick) { Name = "TableGroup", Glyph = GlyphType.FolderOTable },
                new ToolMenuItem(ToolAddTableClick) { Name = "Table", Glyph = GlyphType.Table },
                new ToolMenuItem(ToolAddColumnGroupClick) { Name = "ColumnGroup", Glyph = GlyphType.FolderOColumn },
                new ToolMenuItem(ToolAddColumnClick) { Name = "Column", Glyph = GlyphType.Columns },
                new ToolMenuItem(ToolAddIndexClick) { Name = "Index", Glyph = GlyphType.Anchor },
                new ToolMenuItem(ToolAddConstraintClick) { Name = "Constraint", Glyph = GlyphType.Check },
                new ToolMenuItem(ToolAddForeignClick) { Name = "Foreign", Glyph = GlyphType.Link },
                new ToolMenuItem(ToolAddSequenceClick) { Name = "Sequence", Glyph = GlyphType.Plus },
                new ToolMenuItem(ToolAddProcedureClick) { Name = "Procedure", Glyph = GlyphType.GearAlias },
                new ToolMenuItem(ToolAddProcedureParamClick) { Name = "Procedure Parameter", Glyph = GlyphType.Columns })
            { Name = "DBSchema" };

            contextMain = new Menubar(
                new ToolMenuItem { Name = "Add", GlyphColor = Colors.DarkGreen, DropDown = contextAdd, Glyph = GlyphType.PlusCircle },
                new ToolMenuItem(ToolCopyClick) { Name = "Copy", Glyph = GlyphType.CopyAlias },
                new ToolMenuItem(ToolRemoveClick) { Name = "Remove", GlyphColor = Colors.DarkRed, Glyph = GlyphType.MinusCircle },
                new ToolSeparator(),
                new ToolMenuItem { Name = "Tools", DropDown = contextTools, Glyph = GlyphType.Wrench },
                new ToolMenuItem(ToolPropertyClick) { Name = "Properties" })
            { Name = "Bar" };

            barMain = new Toolsbar(
                new ToolDropDown() { Name = "Add", GlyphColor = Colors.DarkGreen, DropDown = contextAdd, Glyph = GlyphType.PlusCircle },
                new ToolItem(ToolRemoveClick) { Name = "Remove", GlyphColor = Colors.DarkRed, Glyph = GlyphType.MinusCircle },
                new ToolItem(ToolCopyClick) { Name = "Copy", Glyph = GlyphType.CopyAlias },
                new ToolDropDown { Name = "Tools", DropDown = contextTools, Glyph = GlyphType.Wrench },
                new ToolSearchEntry() { Name = "FilterText" })
            { Name = "Bar" };

            barChanges = new Toolsbar(
                new ToolItem(ToolChangesCommitClick) { Name = "Commit", DisplayStyle = ToolItemDisplayStyle.Text },
                new ToolItem(ToolChangesSkipClick) { Name = "Skip", DisplayStyle = ToolItemDisplayStyle.Text })
            { Name = "Changes" };

            changesView = new LayoutList
            {
                Name = "changesView",
                GenerateColumns = false,
                AutoToStringFill = true,
                CheckView = true,
                ListSource = DBService.Changes
            };
            changesView.ListInfo.ColumnsVisible = false;

            dataTree = new DataTree()
            {
                Name = "dataTree",
                DataKeys = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table | DataTreeKeys.LogTable |
                DataTreeKeys.ColumnGroup | DataTreeKeys.Column |
                DataTreeKeys.Index | DataTreeKeys.Constraint | DataTreeKeys.Foreign |
                DataTreeKeys.Procedure | DataTreeKeys.ProcedureParam | DataTreeKeys.Sequence,
                Menu = contextMain,
                FilterEntry = ((ToolSearchEntry)barMain["FilterText"]).Entry
            };
            dataTree.CellMouseClick += DataTreeOnNodeMouseClick;
            dataTree.CellDoubleClick += DataTreeOnDoubleClick;
            dataTree.SelectionChanged += DataTreeOnAfterSelect;

            panel1 = new VBox { Name = "Panel1" };
            panel1.PackStart(barMain, false, false);
            panel1.PackStart(dataTree, true, true);

            panel2 = new VBox { Name = "Panel2" };
            panel2.PackStart(barChanges, false, false);
            panel2.PackStart(changesView, true, true);

            container = new VPaned();
            container.Panel1.Content = panel1;

            PackStart(container, true, true);
            Name = "DataExplorer";

            itemWindow.Target = listExplorer;
            itemWindow.ButtonAcceptClick += AcceptOnActivated;

            DBService.DBSchemaChanged += OnDBSchemaChanged;
        }


        public DBSchema CurrentSchema
        {
            get { return dataTree.CurrentSchema; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        public override void Localize()
        {
            base.Localize();
            GuiService.Localize(this, Name, "Database", GlyphType.Database);
        }

        private void ShowNewItem(object item)
        {
            listExplorer.Value = item;
            itemWindow.Title = "New item " + Locale.Get(item.GetType());
            itemWindow.Mode = ToolShowMode.Dialog;
            itemWindow.Show(this, new Point(0, 0));
        }

        private class PatchParam
        {
            public ExportMode Mode { get; set; }
            public DateTime Stamp { get; set; }
            public string File { get; set; }
        }

        private void OnDBSchemaChanged(object sender, DBSchemaChangedArgs e)
        {
            Application.Invoke(() =>
            {
                ChangesVisible = true;
            });
        }

        private void ToolDeserializeClick(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filters.Add(new FileDialogFilter("Config(*.xml)", "*.xml"));
                if (dialog.Run(ParentWindow))
                {
                    DBService.Deserialize(dialog.FileName, dataTree.SelectedDBItem);
                }
            }
        }

        private void ToolSerializeClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedNode == null)
            {
                return;
            }
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filters.Add(new FileDialogFilter("Config(*.xml)", "*.xml"));
                if (dialog.Run(ParentWindow))
                {
                    if (dataTree.Selection.Count > 1)
                    {
                        var nodes = dataTree.Selection.GetItems<SchemaItemNode>().Select(item => item.Item).ToList();
                        Serialization.Serialize(nodes, dialog.FileName);
                    }
                    else
                    {
                        Serialization.Serialize(dataTree.SelectedDBItem, dialog.FileName);
                    }
                }
            }
        }

        private void ToolChangesSkipClick(object sender, EventArgs e)
        {
            HideChanges();
        }

        private void ToolChangesCommitClick(object sender, EventArgs e)
        {
            var query = new DataQuery
            {
                Query = DBService.BuildChangesQuery(CurrentSchema),
                CurrentSchema = CurrentSchema
            };
            query.ShowDialog(this);

            HideChanges();
        }

        public bool ChangesVisible
        {
            get { return container.Panel2 != null; }
            set
            {
                if (ChangesVisible != value)
                {
                    container.Panel2.Content = value ? panel2 : null;
                    QueueForReallocate();
                }
            }
        }

        public void HideChanges()
        {
            DBService.Changes.Clear();
            ChangesVisible = false;
        }

        private void ToolPatchLoadClick(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filters.Add(new FileDialogFilter("Patch(zip)", "*.zip"));
                if (dialog.Run(ParentWindow))
                {
                    var editor = new DataExport { Export = DBExport.PatchRead(dialog.FileName) };
                    editor.ShowWindow(this);
                }
            }
        }

        private void ToolPatchCreateClick(object sender, EventArgs e)
        {
            var export = new DBExport()
            {
                Mode = ExportMode.Patch,
                Stamp = DateTime.Today.AddDays(-7),
                Source = DBService.DefaultSchema,
                Target = new DBSchema()
                {
                    Name = "patch",
                    Connection = new DBConnection()
                    {
                        System = DBSystem.SQLite,
                        Host = "dataPatch" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".sdb"
                    }
                }
            };
            var editor = new DataExport();
            editor.Export = export;
            editor.ExportComplete += (oo, ee) =>
            {
                editor.Patch();
                editor.ParentWindow.Close();
            };
            editor.ShowWindow(this);
            editor.Initialise();
        }

        private async void AcceptOnActivated(object sender, EventArgs e)
        {
            var value = listExplorer.Value;
            if (value is DBSchema)
            {
                var schema = (DBSchema)value;
                DBService.Schems.Add(schema);
                if (schema.Connection != null)
                {
                    var text = new StringBuilder();
                    text.AppendLine(schema.FormatSql(DDLType.Create));
                    text.AppendLine("go");
                    text.AppendLine(schema.FormatSql());

                    var query = new DataQuery();
                    query.Query = text.ToString();

                    GuiService.Main.DockPanel.Put(query);
                }
            }
            else if (value is DBConnection)
            {
                var connection = (DBConnection)value;
                var schema = new DBSchema
                {
                    Name = connection.Name,
                    Connection = connection
                };
                value = schema;
                DBService.Schems.Add(schema);
            }
            else if (value is DBSequence)
            {
                ((DBSequence)value).Schema.Sequences.Add((DBSequence)value);
            }
            else if (value is DBProcedure)
            {
                ((DBProcedure)value).Schema.Procedures.Add((DBProcedure)value);
            }
            else if (value is DBProcParameter)
            {
                ((DBProcParameter)value).Procedure.Parameters.Add((DBProcParameter)value);
            }
            else if (value is DBTable)
            {
                ((DBTable)value).Schema.Tables.Add((DBTable)value);
            }
            else if (value is DBTableGroup)
            {
                ((DBTableGroup)value).Schema.TableGroups.Add((DBTableGroup)value);
            }
            else if (value is DBColumnGroup)
            {
                ((DBColumnGroup)value).Table.ColumnGroups.Add((DBColumnGroup)value);
            }
            else if (value is DBColumn)
            {
                ((DBColumn)value).Table.Columns.Add((DBColumn)value);
            }
            else if (value is DBIndex)
            {
                ((DBIndex)value).Table.Indexes.Add((DBIndex)value);
            }
            else if (value is DBForeignKey)
            {
                ((DBForeignKey)value).Table.Foreigns.Add((DBForeignKey)value);
            }
            else if (value is DBConstraint)
            {
                ((DBConstraint)value).Table.Constraints.Add((DBConstraint)value);
            }

            dataTree.SelectedDBItem = value as DBSchemaItem;

            if (value is DBSchema)
            {
                await ((DBSchema)value).LoadTablesInfoAsync();
            }
        }

        public class AsseblyCheck : ICheck
        {
            public AsseblyCheck(Assembly assembly)
            {
                Assembly = assembly;
            }

            public Assembly Assembly { get; set; }

            public bool Check { get; set; }

            public override string ToString()
            {
                return Assembly.ToString();
            }
        }

        private void ToolDBGenerateClick(object sender, EventArgs e)
        {
            var assemblyList = new SelectableList<AsseblyCheck>();
            string[] asseblies = Directory.GetFiles(Helper.GetDirectory(), "*.dll");
            foreach (string dll in asseblies)
            {
                AssemblyDefinition assemblyDefinition = null;
                try { assemblyDefinition = AssemblyDefinition.ReadAssembly(dll); }
                catch { continue; }

                var moduleAttribute = assemblyDefinition.CustomAttributes
                                                       .Where(item => item.AttributeType.Name == nameof(AssemblyMetadataAttribute))
                                                       .Select(item => item.ConstructorArguments.Select(sitem => sitem.Value.ToString())
                                                       .ToArray());
                if (moduleAttribute.Any(item => item[0] == "module"))
                {
                    assemblyList.Add(new AsseblyCheck(Assembly.LoadFile(dll)));
                }
            }

            var list = new LayoutList
            {
                AllowCheck = true,
                CheckRecursive = true,
                AutoToStringFill = true,
                GenerateToString = true,
                GenerateColumns = false,
                ListSource = assemblyList
            };

            var window = new ToolWindow
            {
                Title = "Select Assembly",
                Target = list
            };
            window.Show(this, Point.Zero);
            window.ButtonAcceptClick += (s, a) =>
                  {
                      var schema = new DBSchema("NewSchema");
                      schema.Generate(assemblyList.Where(p => p.Check).Select(p => p.Assembly));
                      DBService.Schems.Add(schema);

                      ShowNewItem(schema);
                  };
        }

        private void ToolDBCheckClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedNode == null)
                return;
            var schema = dataTree.SelectedDBItem as DBSchema;
            if (schema != null)
            {
                try
                {
                    schema.Connection.CheckConnection();
                    string message = Locale.Get("DataExplorer", "Connection Test Complete!");
                    MessageDialog.ShowMessage(ParentWindow, message, "DB Manager");
                    GuiService.Main.SetStatus(new StateInfo("DB Manager", message, null, StatusType.Warning, schema));
                }
                catch (Exception exception)
                {
                    string message = Locale.Get("DataExplorer", "Connection Test Fail!");
                    MessageDialog.ShowMessage(ParentWindow, message, "DB Manager");
                    GuiService.Main.SetStatus(new StateInfo("DB Manager", message + "\n" + exception.Message, null, StatusType.Error, schema));
                }
            }
        }

        private void ToolSequenceRefreshOnClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedDBItem is DBTable table && table.Sequence != null)
            {
                table.RefreshSequence();
                //DBSystem.LoadColumns(.SelectedObject);
            }
        }


        private void ToolTableRefreshOnClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedDBItem is DBTable)
            {
                //DBSystem.LoadColumns(.SelectedObject);
            }
        }

        private void ToolTableReportOnClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedDBItem is DBTable)
            {
            }
        }

        private void ToolExtractDDLClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedDBItem != null)
            {
                var query = new DataQuery();
                query.Query = dataTree.SelectedDBItem.FormatSql(DDLType.Create);
                GuiService.Main.DockPanel.Put(query);
            }
        }

        private void EditTableData(DBTable table)
        {
            TableExplorer cont = null;
            if (cont == null)
                cont = new TableExplorer();
            cont.Initialize(table, null, null, TableEditorMode.Table, false);
            GuiService.Main.DockPanel.Put(cont, DockType.Content);
        }

        public DockType DockType
        {
            get { return DockType.Left; }
        }

        protected void ShowItem(Widget editor)
        {
            if (GuiService.Main != null)
            {
                GuiService.Main.DockPanel.Put(editor, DockType.Content);
            }
            else
            {
                editor.ShowWindow(this);
            }
        }

        private void ToolRemoveClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedNode == null)
                return;
            var text = Locale.Get(base.Name, "Remove select items?");
            if (MessageDialog.AskQuestion("Confirmation", text, Command.No, Command.Yes) == Command.Yes)
            {
                var items = dataTree.Selection.GetItems<Node>();
                foreach (SchemaItemNode node in items)
                {
                    var obj = node.Item;
                    if (obj != null)
                        obj.Container.Remove(obj);
                }
            }
        }

        private void ToolDbEditClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedDBItem is DBSchema)
            {
                var editor = new ListExplorer();
                editor.DataSource = dataTree.SelectedDBItem;
                editor.ShowDialog(this);
            }
        }

        private void ToolDBRefreshClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedDBItem is DBSchema)
            {
                ((DBSchema)dataTree.SelectedDBItem).LoadTablesInfoAsync();
            }
        }

        private void ToolReportClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedDBItem is DBTable)
            {
                var projecth = new ProjectHandler();
                projecth.Project = new QQuery();
                ((QQuery)projecth.Project).Table = (DBTable)dataTree.SelectedDBItem;
                GuiService.Main.CurrentProject = projecth;
            }
        }

        private void ToolAddSchemaClick(object sender, EventArgs e)
        {
            ShowNewItem(new DBSchema()
            {
                Connection = new DBConnection() { Name = "nc" + DBService.Connections.Count },
                Name = "new"
            });
        }

        private void ToolAddConnectionClick(object sender, EventArgs e)
        {
            ShowNewItem(new DBConnection() { Name = "nc" + DBService.Connections.Count });
        }

        private void ToolAddTableGroupClick(object sender, EventArgs e)
        {
            var schema = dataTree.SelectedDBItem.Schema;
            var group = dataTree.SelectedDBItem as DBTableGroup;

            if (schema == null)
                return;

            ShowNewItem(new DBTableGroup()
            {
                Name = "new_group",
                Group = group,
                Schema = schema
            });
        }

        private void ToolAddTableClick(object sender, EventArgs e)
        {
            var schema = dataTree.SelectedDBItem.Schema as DBSchema;
            var group = dataTree.SelectedDBItem as DBTableGroup;
            if (dataTree.SelectedDBItem is DBTable)
            {
                group = ((DBTable)dataTree.SelectedDBItem).Group;
            }

            if (schema == null)
                return;

            ShowNewItem(new DBTable<DBItem>
            {
                Name = "new_table",
                Schema = schema,
                Group = group
            });
        }

        private void ToolAddColumnGroupClick(object sender, EventArgs e)
        {
            var table = dataTree.SelectedDBItem as DBTable;
            if (dataTree.SelectedDBItem is IDBTableContent)
            {
                table = ((IDBTableContent)dataTree.SelectedDBItem).Table;
            }

            if (table == null)
                return;

            ShowNewItem(new DBColumnGroup()
            {
                Table = table,
                Name = "new_group"
            });
        }

        private void ToolAddColumnClick(object sender, EventArgs e)
        {
            var table = dataTree.SelectedDBItem as DBTable;
            var group = dataTree.SelectedDBItem as DBColumnGroup;
            if (dataTree.SelectedDBItem is IDBTableContent)
            {
                table = ((IDBTableContent)dataTree.SelectedDBItem).Table;
            }

            if (table == null)
                return;

            if (dataTree.SelectedDBItem is DBColumn)
            {
                group = ((DBColumn)dataTree.SelectedDBItem).Group;
            }
            ShowNewItem(new DBColumn()
            {
                Table = table,
                Group = group,
                Name = "new_column"
            });
        }

        private void ToolAddIndexClick(object sender, EventArgs e)
        {
            var table = dataTree.SelectedDBItem as DBTable;
            var column = dataTree.SelectedDBItem as DBColumn;
            if (dataTree.SelectedDBItem is IDBTableContent)
            {
                table = ((IDBTableContent)dataTree.SelectedDBItem).Table;
            }

            if (table == null)
                return;

            var columns = new List<DBColumn>();
            if (column != null)
            {
                columns.Add(column);
            }
            else if (dataTree.SelectedDBItem is DBColumnGroup)
            {
                columns.AddRange(((DBColumnGroup)dataTree.SelectedDBItem).GetColumns());
            }
            ShowNewItem(new DBIndex
            {
                Table = table,
                Name = table.Name + "_new_index",
                Columns = new DBColumnReferenceList(columns)
            });
        }

        private void ToolAddConstraintClick(object sender, EventArgs e)
        {
            var table = dataTree.SelectedDBItem as DBTable;
            var column = dataTree.SelectedDBItem as DBColumn;
            if (dataTree.SelectedDBItem is IDBTableContent)
            {
                table = ((IDBTableContent)dataTree.SelectedDBItem).Table;
            }

            if (table == null)
                return;

            var columns = new List<DBColumn>();
            if (column != null)
            {
                columns.Add(column);
            }
            else if (dataTree.SelectedDBItem is DBColumnGroup)
            {
                columns.AddRange(((DBColumnGroup)dataTree.SelectedDBItem).GetColumns());
            }

            ShowNewItem(new DBConstraint
            {
                Table = table,
                Columns = new DBColumnReferenceList(columns)
            });
        }

        private void ToolAddForeignClick(object sender, EventArgs e)
        {
            var table = dataTree.SelectedDBItem as DBTable;
            var column = dataTree.SelectedDBItem as DBColumn;
            if (dataTree.SelectedDBItem is IDBTableContent)
            {
                table = ((IDBTableContent)dataTree.SelectedDBItem).Table;
            }

            if (table == null)
                return;

            var columns = new List<DBColumn>();
            if (column != null)
            {
                columns.Add(column);
            }

            ShowNewItem(new DBForeignKey
            {
                Table = table,
                Columns = new DBColumnReferenceList(columns)
            });
        }

        private void ToolAddSequenceClick(object sender, EventArgs e)
        {
            var schema = dataTree.SelectedDBItem.Schema as DBSchema;

            if (schema == null)
                return;

            ShowNewItem(new DBSequence
            {
                Name = "new_sequence",
                Schema = schema
            });
        }

        private void ToolAddProcedureClick(object sender, EventArgs e)
        {
            var schema = dataTree.SelectedDBItem.Schema as DBSchema;
            var group = dataTree.SelectedDBItem as DBProcedure;

            if (schema == null)
                return;

            ShowNewItem(new DBProcedure
            {
                Name = "new_procedure",
                Schema = schema,
                Group = group
            });
        }

        private void ToolAddProcedureParamClick(object sender, EventArgs e)
        {
            var procedure = dataTree.SelectedDBItem as DBProcedure;
            var group = dataTree.SelectedDBItem as DBProcParameter;
            if (group != null)
            {
                procedure = group.Procedure;
            }

            if (procedure == null)
                return;
            ShowNewItem(new DBProcParameter()
            {
                Procedure = procedure,
                Name = "new_parameter"
            });
        }

        private void ToolDBExportClick(object sender, EventArgs e)
        {
            //var schema = dataTree.SelectedDBItem as DBSchema;
            //if (schema != null)
            //{
            //    using (var dialog = new SaveFileDialog())
            //    {
            //        dialog.Filters.Add(new FileDialogFilter("Web Page(*.xhtml)", "*.xhtml"));
            //        if (dialog.Run(ParentWindow))
            //            schema.ExportXHTML(dialog.FileName);
            //    }
            //}
            //ProjectHandler ph = new ProjectHandler();
            if (CurrentSchema == null)
                return;
            var export = new DBExport() { Source = CurrentSchema };
            var exportEditor = new DataExport() { Export = export };
            GuiService.Main.DockPanel.Put(exportEditor);

        }

        private void ToolTableExplorerOnClick(object sender, EventArgs e)
        {
            if (dataTree.SelectedDBItem is DBTable)
            {
                EditTableData((DBTable)dataTree.SelectedDBItem);
            }
        }

        private void ToolCopyClick(object sender, EventArgs e)
        {
            var dbItem = dataTree.SelectedDBItem;
            if (dbItem == null)
                return;
            if (dbItem is DBTable)
            {
                DBTable newTable = (DBTable)((DBTable)dbItem).Clone();
                ShowNewItem(newTable);
            }
            else if (dbItem is DBColumn)
            {
                var selected = (DBColumn)dbItem;
                var column = (DBColumn)selected.Clone();
                column.Table = selected.Table;
                ShowNewItem(column);
            }
            else if (dbItem is DBProcedure)
            {
                var procedure = (DBProcedure)((DBProcedure)dbItem).Clone();
                foreach (var par in ((DBProcedure)dbItem).Parameters)
                {
                    var parameter = (DBProcParameter)par.Clone();
                    parameter.Procedure = procedure;
                    procedure.Parameters.Add(parameter);
                }
            }
            //dataTree.SelectedDBItem
            //           if (dataTree.SelectedDBItem is DBSchema) {
            //               DataEnvir.Schems.Remove (dataTree.SelectedDBItem as DBSchema);
            //           } else if (dataTree.SelectedObject is DBTableGroup) {
            //               RemoveTableGroup ((DBTableGroup)dataTree.SelectedObject);
            //           } else if (dataTree.SelectedObject is DBTable) {
            //               RemoveTable ((DBTable)dataTree.SelectedObject);
            //           } else if (dataTree.SelectedObject is DBColumn) {
            //               RemoveColumn ((DBColumn)dataTree.SelectedObject);
            //           } else if (dataTree.SelectedObject is DBColumnGroup) {
            //               RemoveColumnGroup ((DBColumnGroup)dataTree.SelectedObject);
            //           }
        }

        private void ToolPropertyClick(object sender, EventArgs e)
        {
            GuiService.Main.ShowProperty(this, dataTree.SelectedNode, true);
        }

        private void DataTreeOnNodeMouseClick(object sender, LayoutHitTestEventArgs e)
        {
            if (dataTree.SelectedNode != null && e.HitTest.MouseButton == PointerButton.Right)
            {
                if (dataTree.SelectedDBItem is DBSchema)
                {
                    //contextMain.Items.Add(toolMainAdd);
                }

                contextMain.Popup(this, e.HitTest.ItemBound.BottomLeft);
            }
        }

        private void DataTreeOnAfterSelect(object sender, EventArgs e)
        {
            if (dataTree.SelectedNode != null)
                GuiService.Main.ShowProperty(this, dataTree.SelectedDBItem, false);
        }

        private void DataTreeOnDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            if (dataTree.SelectedDBItem != null)
            {
                DBTable table = dataTree.SelectedDBItem as DBTable;
                if (table != null)
                {
                    EditTableData(table);
                }
                else if (dataTree.SelectedDBItem is DBProcedure)
                {
                    var procedure = (DBProcedure)dataTree.SelectedDBItem;
                    var editor = GuiService.Main.DockPanel.Find(ProcedureEditor.GetName(procedure)) as ProcedureEditor;
                    if (editor == null)
                        editor = new ProcedureEditor() { Procedure = procedure };
                    GuiService.Main.DockPanel.Put(editor, DockType.Content);
                }
            }
        }

        private void ToolMainRefreshOnClick(object sender, EventArgs e)
        {
            dataTree.Localize();
        }

        private void ToolLoadFileClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog() { Multiselect = true };
            if (dialog.Run(ParentWindow))
            {
                foreach (string fileName in dialog.FileNames)
                {
                    string name = Path.GetFileName(fileName);
                    var query = new Query<DBProcedure>(new[]
                        {
                        new QueryParameter<DBProcedure>(){
                            Invoker = EmitInvoker.Initialize<DBProcedure>(nameof(DBProcedure.DataName)),
                            Value = name
                        },
                        new QueryParameter<DBProcedure>(){
                            Invoker = EmitInvoker.Initialize<DBProcedure>(nameof(DBProcedure.ProcedureType)),
                            Value = ProcedureTypes.File
                        },
                        });
                    var procedire = CurrentSchema.Procedures.Find(query) as DBProcedure;
                    if (procedire == null)
                    {
                        procedire = new DBProcedure();
                        procedire.ProcedureType = ProcedureTypes.File;
                        procedire.DataName = name;
                        procedire.Name = Path.GetFileNameWithoutExtension(name);
                    }
                    procedire.Data = File.ReadAllBytes(fileName);
                    procedire.Stamp = File.GetLastWriteTime(fileName);
                    procedire.Save();
                }
                MessageDialog.ShowMessage(ParentWindow, Locale.Get("FlowExplorer", "Files load complete!"), "File Loader!");
            }
        }

        public bool Closing()
        {
            return true;
        }

        public void Activating()
        {
        }
    }


}
