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
		private SelectableList<DBSchemaChange> changes = new SelectableList<DBSchemaChange>();

		public DataExplorer()
		{
			contextTools = new Menubar(new ToolItem[]{
				new ToolMenuItem(ToolMainRefreshOnClick) { Name = "Refresh Tree", ForeColor = Colors.DarkBlue, Glyph = GlyphType.Refresh },
				new ToolMenuItem(ToolDBCheckClick) { Name = "Check Connection" },
				new ToolMenuItem(ToolDBRefreshClick) { Name = "Refresh Schema Info" },
				new ToolMenuItem(ToolDBGenerateClick) { Name = "Generate Database" },
				new ToolSeparator(),
				new ToolMenuItem(ToolDBExportClick) { Name = "Export" },
				new ToolMenuItem(ToolDBPatchCreateClick) { Name = "Patch Create" },
				new ToolMenuItem(ToolDBPatchLoadClick) { Name = "Patch Load" },
				new ToolSeparator(),
				new ToolMenuItem(ToolTableRefreshOnClick) { Name = "Refresh Table Info" },
				new ToolMenuItem(ToolTableReportOnClick) { Name = "Table Report" },
				new ToolMenuItem(ToolTableExplorerOnClick) { Name = "Table Explorer" },
				new ToolSeparator(),
				new ToolMenuItem(ToolExtractDDLOnClick) { Name = "Extract DDL" },
				new ToolMenuItem(ToolSerializeOnClick) { Name = "Serialize" },
				new ToolMenuItem(ToolDeSerializeOnClick) { Name = "Deserialize" },
				new ToolMenuItem(ToolLoadFileClick) { Name = "Load Files" }})
			{ Name = "DataExplorer" };

			contextAdd = new Menubar(new ToolItem[]{
				new ToolMenuItem(ToolAddDBClick) { Name = "Schema", Glyph = GlyphType.Database},
				new ToolMenuItem(ToolAddTableGroupClick) { Name = "Table Group" , Glyph = GlyphType.FolderOTable},
				new ToolMenuItem(ToolAddTableClick) { Name = "Table" , Glyph = GlyphType.Table},
				new ToolMenuItem(ToolAddColumnGroupClick) { Name = "Column Group", Glyph = GlyphType.FolderOColumn },
				new ToolMenuItem(ToolAddColumnClick) { Name = "Column" , Glyph = GlyphType.Columns},
				new ToolMenuItem(ToolAddIndexClick) { Name = "Index" , Glyph = GlyphType.Anchor},
				new ToolMenuItem(ToolAddConstraintClick) { Name = "Constraint", Glyph = GlyphType.Check },
				new ToolMenuItem(ToolAddForeignClick) { Name = "Foreign", Glyph = GlyphType.Link },
				new ToolMenuItem(ToolAddProcedureClick) { Name = "Procedure", Glyph = GlyphType.GearAlias },
				new ToolMenuItem(ToolAddProcedureParamClick) { Name = "Procedure Parameter", Glyph = GlyphType.Columns }})
			{ Name = "DBSchema" };

			contextMain = new Menubar(new ToolItem[]{
				new ToolMenuItem { Name = "Add", ForeColor = Colors.DarkGreen, DropDown = contextAdd, Glyph = GlyphType.PlusCircle },
				new ToolMenuItem(ToolCopyClick) { Name = "Copy", Glyph = GlyphType.CopyAlias },
				new ToolMenuItem(ToolRemoveClick) { Name = "Remove", ForeColor = Colors.DarkRed,  Glyph = GlyphType.MinusCircle },
				new ToolSeparator(),
				new ToolMenuItem { Name = "Tools", DropDown = contextTools, Glyph = GlyphType.Wrench },
				new ToolMenuItem(ToolPropertyClick) { Name = "Properties" }})
			{ Name = "Bar" };

			barMain = new Toolsbar(new ToolItem[] {
				new ToolDropDown() { Name = "Add", ForeColor = Colors.DarkGreen, DropDown = contextAdd, Glyph = GlyphType.PlusCircle },
				new ToolItem(ToolRemoveClick) { Name = "Remove", ForeColor = Colors.DarkRed, Glyph = GlyphType.MinusCircle },
				new ToolItem(ToolCopyClick) { Name = "Copy", Glyph = GlyphType.CopyAlias },
				new ToolDropDown { Name = "Tools", DropDown = contextTools, Glyph = GlyphType.Wrench },
				new ToolSearchEntry() })
			{ Name = "Bar" };

			barChanges = new Toolsbar(new ToolItem[] {
				new ToolItem(ToolChangesCommitOnClick) { Name = "Commit", DisplayStyle = ToolItemDisplayStyle.Text },
				new ToolItem(ToolChangesSkipOnClick) { Name = "Skip", DisplayStyle = ToolItemDisplayStyle.Text },
			})
			{ Name = "Changes" };

			changesView = new LayoutList
			{
				Name = "changesView",
				GenerateColumns = false,
				AutoToStringFill = true,
				CheckView = true,
				ListSource = changes
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

			listExplorer.GetCellEditor += LayoutDBTable.InitCellEditor;
			ose.Target = listExplorer;
			ose.ButtonAcceptClick += AcceptOnActivated;

			DBService.DBSchemaChanged += OnDBSchemaChanged;

			Localize();
		}

		public DBSchema CurrentSchema
		{
			get { return dataTree.CurrentSchema; }
		}

		private void OnDBSchemaChanged(object sender, DBSchemaChangedArgs e)
		{
			DBSchemaItem item = sender as DBSchemaItem;
			if (item.Container == null)
				return;
			if (item is IDBTableContent)
			{
				var table = ((IDBTableContent)item).Table;
				if (table is IDBVirtualTable || table.Container == null)
					return;
			}
			DBSchemaChange change = null;

			var list = changes.Select("Item", CompareType.Equal, item).ToList();

			if (list.Count > 0)
			{
				change = list[0];
				if (change.Change != e.Type)
				{
					if (change.Change == DDLType.Create && e.Type == DDLType.Alter)
						return;
					//if (change.Change == DDLType.Create && e.Type == DDLType.Drop)
					//{
					//    changes.Remove(change);
					//    return;
					//}
					change = null;
				}
			}

			if (change == null)
			{
				change = new DBSchemaChange() { Item = item, Change = e.Type };
				changes.Add(change);
			}
			Application.Invoke(() => container.Panel2.Content.Visible = true);
		}

		private void ToolDeSerializeOnClick(object sender, EventArgs e)
		{
			string file = null;
			var dialog = new OpenFileDialog();
			dialog.Filters.Add(new FileDialogFilter("Config(*.xml)", "*.xml"));
			if (dialog.Run(ParentWindow))
			{
				file = dialog.FileName;
				var item = Serialization.Deserialize(file);
				if (item is DBTable)
				{
					DBSchema schema = dataTree.CurrentSchema;

					if (schema.Tables.Contains(((DBTable)item).Name))
						schema.Tables.Remove(((DBTable)item).Name);
					schema.Tables.Add((DBTable)item);
				}
				else if (item is DBSchema)
				{
					DBSchema schema = (DBSchema)item;
					if (DBService.Schems.Contains(schema.Name))
						schema.Name = schema.Name + "1";
					DBService.Schems.Add((DBSchema)item);
				}
				else if (item is DBColumn)
				{
					var table = dataTree.SelectedDBItem as DBTable;
					if (table != null)
						table.Columns.Add((DBColumn)item);
				}
				else if (item is SelectableList<DBSchemaItem>)
				{
					var list = (SelectableList<DBSchemaItem>)item;
					foreach (var i in list)
					{
						if (i is DBColumn && dataTree.SelectedDBItem is DBTable)
							((DBTable)dataTree.SelectedDBItem).Columns.Add((DBColumn)i);
						else if (i is DBTable && dataTree.SelectedDBItem is DBSchema)
							((DBSchema)dataTree.SelectedDBItem).Tables.Add((DBTable)i);
					}

				}
			}
			dialog.Dispose();
		}

		private void ToolSerializeOnClick(object sender, EventArgs e)
		{
			if (dataTree.SelectedNode != null)
			{
				string file = null;
				using (var dialog = new SaveFileDialog())
				{
					dialog.Filters.Add(new FileDialogFilter("Config(*.xml)", "*.xml"));
					if (dialog.Run(ParentWindow))
					{
						file = dialog.FileName;
						if (dataTree.Selection.Count > 1)
						{
							var nodes = dataTree.Selection.GetItems<Node>();
							var items = new SelectableList<DBSchemaItem>();
							foreach (var node in nodes)
								items.Add((DBSchemaItem)node.Tag);
							Serialization.Serialize(items, file);
						}
						else
							Serialization.Serialize(dataTree.SelectedDBItem, file);
					}
				}
			}
		}

		private void ToolChangesSkipOnClick(object sender, EventArgs e)
		{
			changes.Clear();
			HideChanges();
		}

		private void ToolChangesCommitOnClick(object sender, EventArgs e)
		{
			var builder = new StringBuilder();
			foreach (var item in changes)
			{
				string val = item.Generate();
				if (item.Check && !string.IsNullOrEmpty(val))
				{
					builder.Append("-- ");
					builder.AppendLine(item.ToString());
					builder.AppendLine(val);
					builder.AppendLine("go");
					builder.AppendLine();
				}
				item.Item.OldName = null;
			}
			var query = new DataQuery();
			query.Query = builder.ToString();
			query.ShowDialog(this);

			changes.Clear();
			HideChanges();
		}

		public void HideChanges()
		{
			container.Panel2.Content.Hide();
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

		private void ToolDBPatchLoadClick(object sender, EventArgs e)
		{
			using (var dialog = new OpenFileDialog())
			{
				dialog.Filters.Add(new FileDialogFilter("Patch(zip)", "*.zip"));
				if (dialog.Run(ParentWindow))
				{
					DBExport export = null;
					string dbfile = null;
					var path = Path.Combine(Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(dialog.FileName));
					if (Helper.ReadZip(dialog.FileName, path))
					{
						var files = Directory.GetFiles(path);
						foreach (var file in files)
						{
							if (file.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
								export = Serialization.Deserialize(file) as DBExport;
							if (file.EndsWith("sdb", StringComparison.OrdinalIgnoreCase))
								dbfile = file;
						}
						export.Source.Connection.Host = dbfile;
						var editor = new DataExport { Export = export };
						editor.ShowWindow(this);
					}
				}
			}
		}

		private void ToolDBPatchCreateClick(object sender, EventArgs e)
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

		private void ToolDBGenerateClick(object sender, EventArgs e)
		{
			if (dataTree.SelectedDBItem is DBSchema)
			{
				var list = new LayoutList();
				list.ListSource = AppDomain.CurrentDomain.GetAssemblies();

				var window = new ToolWindow();
				window.Label.Text = "Select Assembly";
				window.Target = list;
				window.Show(this, Point.Zero);
				window.ButtonAcceptClick += (s, a) =>
				  {
					  if (list.SelectedItem is Assembly)
					  {
						  var schema = DBService.Generate((Assembly)list.SelectedItem);
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
				  };
			}
			if (dataTree.SelectedNode == null)
			{
				//DataQuery dq = new DataQuery();
				//dq.Query = dwf.flow.FlowEnvir.Config.Generate(dwf.flow.FlowEnvir.Config.Schema);
				//GuiService.Main.DockPanel.Put(dq);
			}
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

		private void ToolExtractDDLOnClick(object sender, EventArgs e)
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
			cont.Initialize(table, null, null, TableFormMode.Table, false);
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
			DBTable table = new DBTable<DBItem>();
			table.Name = "newtable";
			table.Group = gp;
			table.Schema = schema;
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
			var item = new DBColumnGroup();
			item.Table = table;
			item.Name = "newcolumngroup";

			ShowNewItem(item);
		}

		public void AddColumn(DBTable table, DBColumnGroup gp)
		{
			var item = new DBColumn();
			item.Group = gp;
			item.Table = table;
			item.Name = "newcolumn";

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
				//var doc = new System.Xml.XmlDocument();
				using (var stream = new MemoryStream())
				using (var writer = System.Xml.XmlWriter.Create(stream))
				{
					writer.WriteStartDocument(true);
					writer.WriteStartElement("html");
					writer.WriteElementString("title", schema.DisplayName);
					writer.WriteStartElement("body");
					writer.WriteElementString("H1", schema.DisplayName);
					schema.Tables.Sort(new InvokerComparer<DBTable>("Code"));
					foreach (var table in schema.Tables)
					{
						if (table.Type == DBTableType.Table)
						{
							writer.WriteElementString("H2", table.DisplayName + " (" + table.Name + ")");
							writer.WriteStartElement("table");
							writer.WriteAttributeString("border", "1");
							writer.WriteAttributeString("cellspacing", "0");
							writer.WriteAttributeString("cellpadding", "5");
							writer.WriteStartElement("tr");

							writer.WriteStartElement("th");
							writer.WriteElementString("p", "Code");
							writer.WriteEndElement();//th

							writer.WriteStartElement("th");
							writer.WriteElementString("p", "Name");
							writer.WriteEndElement();//th

							writer.WriteStartElement("th");
							writer.WriteElementString("p", "Type");
							writer.WriteEndElement();//th

							writer.WriteStartElement("th");
							writer.WriteElementString("p", "Size");
							writer.WriteEndElement();//th
							writer.WriteStartElement("th");
							writer.WriteElementString("p", "Prec");
							writer.WriteEndElement();//th

							writer.WriteStartElement("th");
							writer.WriteElementString("p", "Spec");
							writer.WriteEndElement();//th        

							writer.WriteStartElement("th");
							writer.WriteElementString("p", "Reference");
							writer.WriteEndElement();//th


							writer.WriteEndElement();//tr

							foreach (var column in table.Columns)
							{
								writer.WriteStartElement("tr");

								writer.WriteStartElement("td");
								writer.WriteElementString("p", column.Name);
								writer.WriteEndElement();//td

								writer.WriteStartElement("td");
								writer.WriteElementString("p", column.Name);
								writer.WriteEndElement();//td

								writer.WriteStartElement("td");
								writer.WriteElementString("p", column.DBDataType.ToString());
								writer.WriteEndElement();//td

								writer.WriteStartElement("td");
								writer.WriteElementString("p", column.Size.ToString());
								writer.WriteEndElement();//td

								writer.WriteStartElement("td");
								writer.WriteElementString("p", column.Scale.ToString());
								writer.WriteEndElement();//td

								writer.WriteStartElement("td");
								writer.WriteElementString("p", column.Keys.ToString());
								writer.WriteEndElement();//td

								writer.WriteStartElement("td");
								writer.WriteElementString("p", column.ReferenceTable != null ? (column.ReferenceTable + " (" + column.ReferenceTable.Name + ")") : null);
								writer.WriteEndElement();//td

								writer.WriteEndElement();//tr
							}
							writer.WriteEndElement();//table
						}
					}

					writer.WriteEndElement();//body
					writer.WriteEndElement();//html
					writer.WriteEndDocument();
					writer.Flush();

					File.WriteAllBytes("temp.html", stream.ToArray());
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
				MessageDialog.ShowMessage(ParentWindow, Common.Locale.Get("FlowExplorer", "Files load complete!"), "File Loader!");
			}
		}
	}

	public class DBSchemaChange : ICheck
	{
		private DBSchemaItem item;
		private DDLType change;
		private bool check = true;

		public string Type
		{
			get { return item == null ? null : Locale.Get(item.GetType().FullName, item.GetType().Name); }
		}

		public DBSchemaItem Item
		{
			get { return item; }
			set { item = value; }
		}

		public DDLType Change
		{
			get { return change; }
			set { change = value; }
		}

		public string Generate()
		{
			return item.FormatSql(change);
		}

		public override string ToString()
		{
			return string.Format("{0} {1} {2}", change, Type, item);
		}

		public bool Check
		{
			get { return check; }
			set { check = value; }
		}
	}
}
