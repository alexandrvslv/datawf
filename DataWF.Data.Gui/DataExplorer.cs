using DataWF.Gui;
using DataWF.Common;
using DataWF.Data;
using System;
using Xwt.Drawing;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xwt;
using System.Linq;
using Mono.Cecil;

namespace DataWF.Data.Gui
{
	[Module(true)]
	public class DataExplorer : VPanel, IDockContent, IGlyph
	{
		private ToolWindow ose = new ToolWindow();
		private ListExplorer listExplorer = new ListExplorer();
		private DataTree dataTree;
		private Toolsbar barMain;
		private Toolsbar barChanges;
		private Menubar contextMain;
		private Menubar contextAdd;
		private Menubar contextTools;
		private VPaned container;
		private LayoutList changesView;


		public DataExplorer()
		{
			contextTools = new Menubar(
				new ToolMenuItem(ToolMainRefreshOnClick) { Name = "Refresh Tree", ForeColor = Colors.DarkBlue, Glyph = GlyphType.Refresh },
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
				new ToolSeparator(),
				new ToolMenuItem(ToolExtractDDLClick) { Name = "Extract DDL" },
				new ToolMenuItem(ToolSerializeClick) { Name = "Serialize" },
				new ToolMenuItem(ToolDeserializeClick) { Name = "Deserialize" },
				new ToolMenuItem(ToolLoadFileClick) { Name = "Load Files" })
			{ Name = "DataExplorer" };

			contextAdd = new Menubar(
				new ToolMenuItem(ToolAddDBClick) { Name = "Schema", Glyph = GlyphType.Database },
				new ToolMenuItem(ToolAddTableGroupClick) { Name = "Table Group", Glyph = GlyphType.FolderOTable },
				new ToolMenuItem(ToolAddTableClick) { Name = "Table", Glyph = GlyphType.Table },
				new ToolMenuItem(ToolAddColumnGroupClick) { Name = "Column Group", Glyph = GlyphType.FolderOColumn },
				new ToolMenuItem(ToolAddColumnClick) { Name = "Column", Glyph = GlyphType.Columns },
				new ToolMenuItem(ToolAddIndexClick) { Name = "Index", Glyph = GlyphType.Anchor },
				new ToolMenuItem(ToolAddConstraintClick) { Name = "Constraint", Glyph = GlyphType.Check },
				new ToolMenuItem(ToolAddForeignClick) { Name = "Foreign", Glyph = GlyphType.Link },
				new ToolMenuItem(ToolAddProcedureClick) { Name = "Procedure", Glyph = GlyphType.GearAlias },
				new ToolMenuItem(ToolAddProcedureParamClick) { Name = "Procedure Parameter", Glyph = GlyphType.Columns })
			{ Name = "DBSchema" };

			contextMain = new Menubar(
				new ToolMenuItem { Name = "Add", ForeColor = Colors.DarkGreen, DropDown = contextAdd, Glyph = GlyphType.PlusCircle },
				new ToolMenuItem(ToolCopyClick) { Name = "Copy", Glyph = GlyphType.CopyAlias },
				new ToolMenuItem(ToolRemoveClick) { Name = "Remove", ForeColor = Colors.DarkRed, Glyph = GlyphType.MinusCircle },
				new ToolSeparator(),
				new ToolMenuItem { Name = "Tools", DropDown = contextTools, Glyph = GlyphType.Wrench },
				new ToolMenuItem(ToolPropertyClick) { Name = "Properties" })
			{ Name = "Bar" };

			barMain = new Toolsbar(
				new ToolDropDown() { Name = "Add", ForeColor = Colors.DarkGreen, DropDown = contextAdd, Glyph = GlyphType.PlusCircle },
				new ToolItem(ToolRemoveClick) { Name = "Remove", ForeColor = Colors.DarkRed, Glyph = GlyphType.MinusCircle },
				new ToolItem(ToolCopyClick) { Name = "Copy", Glyph = GlyphType.CopyAlias },
				new ToolDropDown { Name = "Tools", DropDown = contextTools, Glyph = GlyphType.Wrench },
				new ToolSearchEntry())
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
				DataKeys = DataTreeKeys.Schema | DataTreeKeys.TableGroup | DataTreeKeys.Table |
				DataTreeKeys.ColumnGroup | DataTreeKeys.Column |
				DataTreeKeys.Index | DataTreeKeys.Constraint | DataTreeKeys.Foreign,
				Menu = contextMain
			};
			dataTree.CellMouseClick += DataTreeOnNodeMouseClick;
			dataTree.CellDoubleClick += DataTreeOnDoubleClick;
			dataTree.SelectionChanged += DataTreeOnAfterSelect;

			var panel1Box = new VPanel();
			panel1Box.PackStart(barMain, false, false);
			panel1Box.PackStart(dataTree, true, true);

			var panel2Box = new VPanel();
			panel2Box.PackStart(barChanges, false, false);
			panel2Box.PackStart(changesView, true, true);
			panel2Box.Visible = false;

			container = new VPaned();
			container.Panel1.Content = panel1Box;
			container.Panel2.Content = panel2Box;

			PackStart(container, true, true);
			Name = "DataExplorer";

			ose.Target = listExplorer;
			ose.ButtonAcceptClick += AcceptOnActivated;

			DBService.DBSchemaChanged += OnDBSchemaChanged;
			Localize();
		}

		public DBSchema CurrentSchema
		{
			get { return dataTree.CurrentSchema; }
		}

		public bool HideOnClose
		{
			get { return true; }
		}

		public void Localize()
		{
			barChanges.Localize();
			barMain.Localize();
			contextMain.Localize();
			contextAdd.Localize();
			contextTools.Localize();

			GuiService.Localize(this, Name, "Data Explorer", GlyphType.Database);

			dataTree.Localize();
		}

		private void ShowNewItem(object item)
		{
			listExplorer.Value = item;
			ose.Mode = ToolShowMode.Dialog;
			ose.Show(this, new Point(0, 0));
		}

		private class PatchParam
		{
			public ExportMode Mode { get; set; }
			public DateTime Stamp { get; set; }
			public string File { get; set; }
		}

		private void OnDBSchemaChanged(object sender, DBSchemaChangedArgs e)
		{
			Application.Invoke(() => container.Panel2.Content.Visible = true);
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

		public void HideChanges()
		{
			DBService.Changes.Clear();

			container.Panel2.Content.Hide();
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

		private void AcceptOnActivated(object sender, EventArgs e)
		{
			if (listExplorer.Value is DBSchema)
				DBService.Schems.Add((DBSchema)listExplorer.Value);
			else if (listExplorer.Value is DBTable)
				((DBTable)listExplorer.Value).Schema.Tables.Add((DBTable)listExplorer.Value);
			else if (listExplorer.Value is DBTableGroup)
				((DBTableGroup)listExplorer.Value).Schema.TableGroups.Add((DBTableGroup)listExplorer.Value);
			else if (listExplorer.Value is DBColumnGroup)
				((DBColumnGroup)listExplorer.Value).Table.ColumnGroups.Add((DBColumnGroup)listExplorer.Value);
			else if (listExplorer.Value is DBColumn)
				((DBColumn)listExplorer.Value).Table.Columns.Add((DBColumn)listExplorer.Value);
			else if (listExplorer.Value is DBIndex)
				((DBIndex)listExplorer.Value).Table.Indexes.Add((DBIndex)listExplorer.Value);
			else if (listExplorer.Value is DBForeignKey)
				((DBForeignKey)listExplorer.Value).Table.Foreigns.Add((DBForeignKey)listExplorer.Value);
			else if (listExplorer.Value is DBConstraint)
				((DBConstraint)listExplorer.Value).Table.Constraints.Add((DBConstraint)listExplorer.Value);
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
                                                       .Select(item => item.ConstructorArguments.Select(sitem => sitem.Value.ToString()).ToArray());
                if (moduleAttribute.Any(item => item[0] == "module"))
                {
                    assemblyList.Add(new AsseblyCheck(Assembly.LoadFile(dll)));
                }
            }

            var list = new LayoutList
            {
                AllowCheck = true,
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
					  foreach (AsseblyCheck assebly in list.ListSource)
					  {
						  if (assebly.Check)
						  {
							  var schema = DBService.Generate(assebly.Assembly);
							  if (schema != null)
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
					  }
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

		private void AddSchema()
		{
			var connection = new DBConnection();
			connection.Name = "nc" + DBService.Connections.Count;
			DBService.Connections.Add(connection);

			var schema = new DBSchema();
			schema.Connection = connection;
			schema.Name = "new";
			ShowNewItem(schema);
		}

		public void AddTableGroup(DBSchema schema, DBTableGroup parent)
		{
			var tgroup = new DBTableGroup();
			tgroup.Name = "newtablegroup";
			tgroup.Group = parent;
			tgroup.Schema = schema;
			ShowNewItem(tgroup);
		}

		public void AddTable(DBSchema schema, DBTableGroup gp)
		{
            var table = new DBTable<DBItem>
            {
                Name = "newtable",
                Group = gp,
                Schema = schema
            };
            table.Columns.Add(new DBColumn()
			{
				Name = "unid",
				Keys = DBColumnKeys.Primary,
				DBDataType = DBDataType.Decimal,
				Size = 28
			});
			table.Columns.Add(new DBColumn()
			{
				Name = "datec",
				Keys = DBColumnKeys.Date,
				DBDataType = DBDataType.DateTime
			});
			table.Columns.Add(new DBColumn()
			{
				Name = "dateu",
				Keys = DBColumnKeys.Stamp,
				DBDataType = DBDataType.DateTime
			});
			table.Columns.Add(new DBColumn()
			{
				Name = "stateid",
				Keys = DBColumnKeys.State,
				DBDataType = DBDataType.Decimal,
				Size = 28
			});
			table.Columns.Add(new DBColumn()
			{
				Name = "access",
				Keys = DBColumnKeys.Access,
				DBDataType = DBDataType.Blob,
				Size = 2000
			});

			ShowNewItem(table);
		}

        public void AddColumnGroup(DBTable table)
        {
            var item = new DBColumnGroup()
            {
                Table = table,
                Name = "newcolumngroup"
            };

            ShowNewItem(item);
        }

        public void AddColumn(DBTable table, DBColumnGroup gp)
        {
            var item = new DBColumn()
            {
                Group = gp,
                Table = table,
                Name = "newcolumn"
            };

            ShowNewItem(item);
        }

        public void AddIndex(DBTable table, DBColumn column)
		{
			var item = new DBIndex();
			item.Table = table;
			item.Name = item.Table.Name + "newindex";
			if (column != null)
				item.Columns.Add(column);

			ShowNewItem(item);
		}

		public void AddConstraint(DBTable table, DBColumn column)
		{
			var item = new DBConstraint();
			item.Table = table;
			if (column != null)
				item.Columns.Add(column);
			item.GenerateName();

			ShowNewItem(item);
		}

		public void AddForeign(DBTable table, DBColumn column)
		{
			var item = new DBForeignKey();
			item.Table = table;
			if (column != null)
				item.Columns.Add(column);
			item.GenerateName();

			ShowNewItem(item);
		}

		public DockType DockType
		{
			get { return DockType.Left; }
		}

		protected void ShowItem(Widget editor)
		{
			if (GuiService.Main != null)
				GuiService.Main.DockPanel.Put(editor, DockType.Content);
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

		private void ToolAddDBClick(object sender, EventArgs e)
		{
			AddSchema();
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
				ThreadPool.QueueUserWorkItem((o) =>
				{
					try
					{
						var schema = (DBSchema)dataTree.SelectedDBItem;
						schema.GetTablesInfo();
					}
					catch (Exception ex)
					{
						Helper.OnException(ex);
					}
				});
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

		private void ToolAddTableGroupClick(object sender, EventArgs e)
		{
			if (dataTree.SelectedDBItem is DBSchema)
				AddTableGroup((DBSchema)dataTree.SelectedDBItem, null);
			else if (dataTree.SelectedDBItem is DBTableGroup)
				AddTableGroup(((DBTableGroup)dataTree.SelectedDBItem).Schema, (DBTableGroup)dataTree.SelectedDBItem);
		}

		private void ToolAddTableClick(object sender, EventArgs e)
		{
			if (dataTree.SelectedDBItem is DBSchema)
				AddTable((DBSchema)dataTree.SelectedDBItem, null);
			else if (dataTree.SelectedDBItem is DBTableGroup)
				AddTable(((DBTableGroup)dataTree.SelectedDBItem).Schema, (DBTableGroup)dataTree.SelectedDBItem);
			else if (dataTree.SelectedDBItem is DBTable)
				AddTable(((DBTable)dataTree.SelectedDBItem).Schema, ((DBTable)dataTree.SelectedDBItem).Group);
		}

		private void ToolAddColumnGroupClick(object sender, EventArgs e)
		{
			if (dataTree.SelectedDBItem is DBTable)
				AddColumnGroup((DBTable)dataTree.SelectedDBItem);
			else if (dataTree.SelectedDBItem is DBColumn)
				AddColumnGroup(((DBColumn)dataTree.SelectedDBItem).Table);
			else if (dataTree.SelectedDBItem is DBColumnGroup)
				AddColumnGroup(((DBColumnGroup)dataTree.SelectedDBItem).Table);
		}

		private void ToolAddColumnClick(object sender, EventArgs e)
		{
			var obj = dataTree.SelectedDBItem;
			if (obj is DBTable)
				AddColumn((DBTable)obj, null);
			else if (obj is DBColumn)
				AddColumn(((DBColumn)obj).Table, ((DBColumn)obj).Group);
			else if (obj is DBColumnGroup)
				AddColumn(((DBColumnGroup)obj).Table, (DBColumnGroup)obj);
		}

		private void ToolAddIndexClick(object sender, EventArgs e)
		{
			var obj = dataTree.SelectedDBItem;
			if (obj is DBTable)
				AddIndex((DBTable)obj, null);
			else if (obj is DBColumn)
				AddIndex(((DBColumn)obj).Table, (DBColumn)obj);
			else if (obj is DBColumnGroup)
				AddIndex(((DBColumnGroup)obj).Table, null);
		}

		private void ToolAddConstraintClick(object sender, EventArgs e)
		{
			var obj = dataTree.SelectedDBItem;
			if (obj is DBTable)
				AddConstraint((DBTable)obj, null);
			else if (obj is DBColumn)
				AddConstraint(((DBColumn)obj).Table, (DBColumn)obj);
			else if (obj is DBColumnGroup)
				AddConstraint(((DBColumnGroup)obj).Table, null);
		}

		private void ToolAddForeignClick(object sender, EventArgs e)
		{
			var obj = dataTree.SelectedDBItem;
			if (obj is DBTable)
				AddForeign((DBTable)obj, null);
			else if (obj is DBColumn)
				AddForeign(((DBColumn)obj).Table, (DBColumn)obj);
			else if (obj is DBColumnGroup)
				AddForeign(((DBColumnGroup)obj).Table, null);
		}

		private void ToolAddProcedureClick(object sender, EventArgs e)
		{
			var row = new DBProcedure();
			if (dataTree.SelectedDBItem is DBProcedure)
			{
				((DBProcedure)row).Parent = (DBProcedure)dataTree.SelectedDBItem;
			}
		}

		private void ToolAddProcedureParamClick(object sender, EventArgs e)
		{
			var row = new DBProcParameter();
			if (dataTree.SelectedDBItem is DBProcedure)
			{
				row.Procedure = (DBProcedure)dataTree.SelectedDBItem;
			}
		}

		private void ToolDBExportClick(object sender, EventArgs e)
		{
			var schema = dataTree.SelectedDBItem as DBSchema;
			if (schema != null)
			{
				using (var dialog = new SaveFileDialog())
				{
					dialog.Filters.Add(new FileDialogFilter("Web Page(*.xhtml)", "*.xhtml"));
					if (dialog.Run(ParentWindow))
						schema.ExportXHTML(dialog.FileName);
				}
			}
			//ProjectHandler ph = new ProjectHandler();
			//ph.Project = new DBExport();
			//if (dataTree.SelectedObject is DBSchema)
			//    ((DBExport)ph.Project).SourceSchema = schema;
			//GuiService.Main.CurrentProject = ph;
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

				contextMain.Popup(this, e.HitTest.ItemBound);
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
					var query = new Query(new[]
						{
						new QueryParameter(){
							Invoker = EmitInvoker.Initialize<DBProcedure>(nameof(DBProcedure.DataName)),
							Value = name
						},
						new QueryParameter(){
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
					procedire.Date = File.GetLastWriteTime(fileName);
					procedire.Save();
				}
				MessageDialog.ShowMessage(ParentWindow, Locale.Get("FlowExplorer", "Files load complete!"), "File Loader!");
			}
		}
	}


}
