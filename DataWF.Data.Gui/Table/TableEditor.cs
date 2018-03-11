using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xwt.Drawing;
using DataWF.Gui;
using DataWF.Common;
using DataWF.Data;
using Xwt;
using System.Text;

namespace DataWF.Data.Gui
{
	public enum TableEditorStatus
	{
		Adding,
		Clone,
		Search,
		Default
	}

	public enum TableEditorMode
	{
		Table,
		Item,
		Reference,
		Referencing,
		Empty
	}

	public class TableEditor : ListEditor, ILocalizable, ILoader, IReadOnly
	{
		public event EventHandler<ListEditorEventArgs> RowDeleting;
		public event EventHandler<ListEditorEventArgs> SelectionChanged;
		public event Updating Updating;
		public event Updated Updated;

		private bool showDetails;
		private IDBTableView view;
		private DBColumn baseColumn = null;
		private DBItem baseRow = null;
		private DBItem clonedRow = null;
		private DBItem searchRow = null;
		private DBItem newRow = null;
		private TableEditorStatus status = TableEditorStatus.Default;
		private TableEditorMode mode = TableEditorMode.Empty;
		private bool _insert = false;
		private bool _update = false;
		private bool _delete = false;
		private TableLoader loader = new TableLoader();
		private ToolTableLoader toolProgress = new ToolTableLoader();
		private ToolDropDown refButton = new ToolDropDown();
		private ToolDropDown toolParam = new ToolDropDown();
		private ToolMenuItem toolInsertLine = new ToolMenuItem();
		private ToolItem toolReport = new ToolItem();
		private ToolItem toolMerge = new ToolItem();
		protected ToolWindow _currentControl;
		private QuestionMessage question;

		public TableEditor()
			: base(new TableLayoutList())
		{
			toolProgress.Loader = loader;

			Bar.Items.Add(refButton);
			Bar.Items.Add(toolMerge);
			Bar.Items.Add(toolReport);
			Bar.Items.Add(toolParam);
			Bar.Items.Add(toolProgress);

			toolAdd.DropDownItems.Add(toolInsertLine);

			toolMerge.Name = "toolMerge";
			toolMerge.Click += OnToolMergeClick;

			toolInsertLine.Name = "toolInsertLine";
			toolInsertLine.Click += OnToolInsertLineClick;

			toolReport.Name = "toolReport";
			toolReport.Click += ToolReportClick;

			toolParam.Name = "toolParam";
			toolParam.Click += ToolParamClick;

			List.CellValueWrite += FieldsCellValueChanged;
			Name = "TableEditor";
			refButton.DisplayStyle = ToolItemDisplayStyle.Text;

			toolInsertLine.Click += OnToolInsertItemClicked;
			//toolParam.Alignment = ToolStripItemAlignment.Right;

			question = new QuestionMessage();
			question.Text = "Checkout";
			question.Buttons.Add(Command.No);
			question.Buttons.Add(Command.Yes);
		}

		private void ToolParamClick(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void Closing()
		{
			loader.Cancel();

			if (Table != null && Table.IsEdited)
			{
				question.SecondaryText = Locale.Get("TableEditor", "Save changes?");
				if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
				{
					Table.Save();
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				loader.Dispose();
				if (view != null)
					view.Dispose();
			}
			base.Dispose(disposing);
		}

		private void OnToolInsertItemClicked(object sender, EventArgs e)
		{
			var column = ((MenuItem)sender).Tag as DBColumn;
			if (column != null)
			{
				var cont = new ToolWindow();
				cont.Target = new TableEditor();
				cont.Label.Text = column.ReferenceTable.ToString();
				//cont.Closing += new ToolStripDropDownClosingEventHandler(cont_Closing);
				((TableEditor)cont.Target).Initialize(column.ReferenceTable.CreateItemsView("", DBViewKeys.None, DBStatus.Current), null, column, TableEditorMode.Reference, false);
				((TableEditor)cont.Target).ItemSelect += OnRowSelected;
				((MenuItem)sender).Tag = cont;
				//((ToolStripDropDownButton)e.ClickedItem).DropDown = e.ClickedItem.Tag as ToolForm;
				//((ToolStripDropDownButton)e.ClickedItem).ShowDropDown();
			}
			_currentControl = ((MenuItem)sender).Tag as ToolWindow;
			// _currentControl.Show();//sender as Control, new Point(toolStrip1.Left, toolStrip1.Height));
		}

		[DefaultValue(false)]
		public bool ShowDetails
		{
			get { return showDetails; }
			set { showDetails = value; }
		}

		public TableLoader Loader
		{
			get { return loader; }
		}

		public TableLayoutList DBList
		{
			get { return (TableLayoutList)List; }
		}

		protected override void OnFilterChanging(object sender, EventArgs e)
		{
			loader.Cancel();
		}

		protected override void OnFilterChanged(object sender, EventArgs e)
		{
			if (DBList.Mode != LayoutListMode.Fields)
			{
				if (DBList.Expression?.Parameters.Count > 0)
					loader.Load(DBList.View.Query);
				else
					loader.Cancel();
			}
		}

		protected void ViewStatusFilterChanged(object sender, EventArgs e)
		{
			if (DBList.Expression?.Parameters.Count > 0)
			{
				DBList.SetFilter(DBList.Expression.ToWhere());
				OnFilterChanged(sender, e);
			}
		}

		public override void OnItemSelect(ListEditorEventArgs ea)
		{
			var row = ea.Item as DBItem;
			if (List.Mode == LayoutListMode.Fields)
			{
				var field = List.SelectedItem as LayoutDBField;
				var column = field.Invoker as DBColumn;
				row = List.FieldSource as DBItem;
				if (column != null && column.IsReference && column.ReferenceTable.Access.View)
				{
					row = field.GetReference(row);
				}
			}
			ea.Item = row;
			base.OnItemSelect(ea);
		}

		public override void ShowItemDialog(object item)
		{
			if (item is DBItem && ((DBItem)item).DBState == DBUpdateState.Default)
			{
				var explorer = new TableExplorer();
				explorer.Initialize((DBItem)item, TableEditorMode.Item, false);
				explorer.ShowDialog(this);
			}
			else
			{
				base.ShowItemDialog(item);
			}
		}

		private void FieldsCellValueChanged(object sender, LayoutValueEventArgs e)
		{
			if (ListMode && Status != TableEditorStatus.Default)
			{
				LayoutList flist = (LayoutList)sender;
				QQuery Expression = new QQuery(string.Empty, Table);

				LayoutField ff = (LayoutField)e.Cell;
				if (e.Data != DBNull.Value)
					Expression.BuildParam(ff.Name, e.Data, true);

				foreach (LayoutField field in flist.Fields)
				{
					object val = flist.ReadValue(field, (LayoutColumn)flist.ListInfo.Columns["Value"]);
					if (!field.Visible || val == null || val == DBNull.Value || GroupHelper.Level(field) != 0)
						continue;
					if (field == ff)
						continue;
					if (val.ToString() == "")
						continue;
					Expression.BuildParam(field.Name, val, true);
				}

				if (Expression.Parameters.Count == 0)
				{
					loader.Cancel();
				}
				else if (!Table.IsSynchronized)
					loader.Load(Expression);

				DBList.View.Filter = Expression.ToWhere();
				//list.View.UpdateFilter();
			}
		}

		public DBItem Selected
		{
			get
			{
				if (ListMode)
					return DBList.SelectedRow as DBItem;
				else
					return DBList.FieldSource as DBItem;
			}
			set
			{
				if (SelectionChanged != null)
				{
					SelectionChanged(this, new ListEditorEventArgs() { Item = value });
				}
				else if (value != null && GuiService.Main != null && showDetails)
				{
					GuiService.Main.ShowProperty(this, value, false);
				}
				//if (ListMode)
				//    list.SelectedRow = value;
				//else
				//    fields.DataSource = value;
			}
		}

		public List<DBItem> SelectedRows
		{
			get { return DBList.Selection.GetItems<DBItem>(); }
		}

		public DBTable Table
		{
			get { return view?.Table ?? baseRow?.Table; }
		}

		public IDBTableView TableView
		{
			get { return view; }
			set
			{
				if (value == view)
					return;

				if (view != null)
					view.StatusFilterChanged -= ViewStatusFilterChanged;

				view = value;
				loader.View = view;
				searchRow = null;
				newRow = null;

				if (view != null)
				{
					view.StatusFilterChanged += ViewStatusFilterChanged;
					toolGroup.Visible = view.Table.GroupKey != null;
					DataSource = view;
				}

			}
		}

		public bool AllowDelete
		{
			get { return _delete; }
			set
			{
				_delete = value;
				toolRemove.Visible = value;
				toolMerge.Visible = value;
			}
		}

		public bool AllowInsert
		{
			get { return _insert; }
			set
			{
				_insert = value;
				toolInsert.Visible = value;
				toolCopy.Visible = value;
				toolInsertLine.Visible = value;
			}
		}

		public bool AllowUpdate
		{
			get { return _update; }
			set
			{
				_update = value;
				if (_update)
				{
					List.EditState = EditListState.Edit;
				}
				else
				{
					List.EditState = EditListState.ReadOnly;
				}
			}
		}

		public DBItem OwnerRow
		{
			get { return baseRow; }
			set
			{
				baseRow = value;
				if (view == null)
					DataSource = value;
				else if (baseColumn != null && view != null && value != null)
					view.DefaultFilter = $"{baseColumn.Name}={baseRow.PrimaryId}";
			}
		}

		public DBColumn OwnerColumn
		{
			get { return baseColumn; }
			set { baseColumn = value; }
		}

		public string RowFilter
		{
			get { return view == null ? null : view.Filter; }
			set
			{
				if (view == null)
					return;
				view.Filter = value;
			}
		}

		public override bool ReadOnly
		{
			get { return base.ReadOnly; }
			set
			{
				base.ReadOnly = value;
				if (!value)
				{
					AllowInsert = Table?.Access?.Create ?? false;
					AllowUpdate = Table?.Access?.Edit ?? false;
					AllowDelete = Table?.Access?.Delete ?? false;
				}
				else
				{
					AllowInsert = false;
					AllowUpdate = false;
					AllowDelete = false;
				}
			}
		}

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TableEditorStatus Status
		{
			get { return status; }
			set
			{
				status = value;
				if (value != TableEditorStatus.Search)
					Updated?.Invoke(this, EventArgs.Empty);
				switch (status)
				{
					case TableEditorStatus.Adding:
						newRow = (DBItem)TableView.NewItem();
						if (OwnerRow != null && baseColumn != null)
						{
							newRow[baseColumn] = OwnerRow.PrimaryId;
						}
						if (Table.GroupKey != null && Selected != null && Selected.Table == Table)
						{
							newRow[Table.GroupKey] = Selected.PrimaryId;
						}
						((LayoutList)toolWindow.Target).FieldSource = newRow;
						toolWindow.Show(bar, toolAdd.Bound.BottomLeft);
						break;
					case TableEditorStatus.Clone:
						clonedRow = Selected;
						if (newRow == null || newRow.Attached)
						{
							newRow = (DBItem)clonedRow.Clone();
						}
						((LayoutList)toolWindow.Target).FieldSource = newRow;
						toolCopy.Sensitive = true;
						break;
					case TableEditorStatus.Search:
						if (searchRow == null)
						{
							searchRow = (DBItem)TableView.NewItem();
						}
						//rowControl.RowEditor.State = FeldEditorState.EditEmpty;
						toolRemove.Sensitive = Table.Access.Delete;
						break;
					case TableEditorStatus.Default:
						OpenMode = OpenMode;
						RowFilter = "";
						break;
				}
			}
		}


		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TableEditorMode OpenMode
		{
			get { return mode; }
			set
			{
				mode = value;
				ReadOnly = ReadOnly;
				switch (mode)
				{
					case TableEditorMode.Table:
						ListMode = true;
						break;
					case TableEditorMode.Item:
						ListMode = false;
						foreach (MenuItemRelation item in refButton.DropDownItems)
							item.Visible = false;
						if (OwnerRow != null)
						{
							foreach (DBForeignKey relation in OwnerRow.Table.GetChildRelations())
							{
								if (!relation.Table.Access.View)
									continue;
								var itemRelation = refButton.DropDownItems[relation.Name] as MenuItemRelation;
								if (itemRelation != null)
								{
									itemRelation.Visible = true;
								}
								else
								{
									itemRelation = new MenuItemRelation();
									itemRelation.Name = relation.Name;
									itemRelation.Text = relation.Table + "(" + relation.Column + ")";
									itemRelation.Relation = relation;
									itemRelation.Click += ToolReferencesClick;
									refButton.DropDown.Items.Add(itemRelation);
								}
								if (TableView == null)
									ToolReferencesClick(itemRelation, null);
							}
						}
						break;
					case TableEditorMode.Referencing:
						ListMode = true;
						list.AutoToStringSort = false;
						if (baseColumn != null && baseRow != null)
						{
							view.DefaultFilter = $"{baseColumn.Name} = {DBService.FormatToSqlText(baseRow.PrimaryId)}";
						}
						break;
					case TableEditorMode.Reference:
						ListMode = true;
						if (baseRow != null)
						{
							Selected = OwnerRow;
						}
						break;
				}
				Text = GetText(this);
			}
		}

		private void ToolReferencesClick(object sender, EventArgs e)
		{
			var tool = (MenuItemRelation)sender;

			refButton.Text = tool.Text;
			string filter = string.Format("{0}={1}", tool.Relation.Column.Name, DBService.FormatToSqlText(OwnerRow.PrimaryId));

			if (tool.View == null)
				tool.View = tool.Relation.Table.CreateItemsView(filter, DBViewKeys.None, DBStatus.Current);
			else
				tool.View.DefaultFilter = filter;
			baseColumn = tool.Relation.Column;
			TableView = tool.View;
			loader.Load(tool.View.Query);
			//if (ReferenceClick != null)
			//    ReferenceClick(this, new TableEditReferenceEventArgs(relation));
			//else
			//{
			//    TableEditor te = new TableEditor();
			//    te.Initialize(relation.Table.CreateRowView(DBViewInitMode.None, DBStatus.Current), Selected, relation.Column, TableFormMode.RefingTable, _access);
			//    Form f = DataCtrlService.WrapControl(te);
			//    f.ShowDialog(this);
			//}
		}

		public void Initialize(DBItem row, bool readOnly)
		{
			Initialize(null, row, null, TableEditorMode.Item, readOnly);
		}

		public void Initialize(TableEditorInfo info)
		{
			Initialize(info.TableView, info.Item, info.Column, info.Mode, info.ReadOnly);
		}

		public void Initialize(IDBTableView view, DBItem row, DBColumn ownColumn, TableEditorMode openmode, bool readOnly)
		{
			if (view != null)
			{
				switch (openmode)
				{
					case TableEditorMode.Table:
						if (view.Table.IsCaching)
							if (!view.Table.IsSynchronized)
							{
								loader.View = view;
								loader.Load(new QQuery(string.Empty, view.Table));
							}
						break;
					case TableEditorMode.Referencing:
						if (row == null)
							view.Filter = view.Table.PrimaryKey.Name + "=0";
						else
						{
							loader.View = view;
							view.Filter = "";
							loader.Load(new QQuery($"where {ownColumn.Name} = {row.PrimaryId}", view.Table));
						}
						break;
				}
			}
			TableView = view;
			OwnerRow = row;

			if (Table == null)
				return;

			OwnerColumn = ownColumn;
			ReadOnly = readOnly;
			OpenMode = openmode;

			Name = Table.Name + ownColumn;
			Text = GetText(this);

			// toolInsert.DropDownItems.Clear();

			if (openmode == TableEditorMode.Referencing)
			{
				for (int i = 0; i < Table.Columns.Count; i++)
				{
					DBColumn cs = Table.Columns[i];
					if (cs.ReferenceTable != null && cs.Name.ToLower() != baseColumn.Name.ToLower())
					{
						var item = new ToolMenuItem();
						item.Tag = cs;
						item.Name = cs.Name;
						item.Text = cs.ToString();
						toolAdd.DropDownItems.Add(item);
					}
				}
				//toolInsert.Add(new SeparatorToolItem ());
			}
		}

		public static string GetText(TableEditor form)
		{
			if (form.Table == null)
				return "<empty>";
			string name = form.Table.ToString();

			string selectdeRow = form.Selected == null ? "" : form.Selected.ToString();

			if (form.OpenMode == TableEditorMode.Table)
				return name;

			if (form.OpenMode == TableEditorMode.Referencing)
			{
				string ownerColumnName = form.OwnerColumn.ToString();
				return $"{name} ({ownerColumnName})";
			}

			if (form.OpenMode == TableEditorMode.Reference)
				return $" ({name})";

			if (form.OpenMode == TableEditorMode.Item)
				return $"{name} ({selectdeRow})";

			return "";
		}

		private bool CheckP(SortedList<LayoutField, object> val, QParam p)
		{
			foreach (LayoutField f in val.Keys)
				if (f.Invoker.Equals(p.Column))
					return true;
			return false;
		}

		protected override void OnListSelectionChanged(object sender, EventArgs e)
		{
			var value = List.SelectedItem as DBItem;
			if (List.Mode != LayoutListMode.Fields && value != null)
			{
				if (SelectionChanged != null)
				{
					SelectionChanged(this, new ListEditorEventArgs() { Item = value });
				}
				else if (GuiService.Main != null && showDetails)
				{
					GuiService.Main.ShowProperty(this, value, false);
				}
			}
		}

		protected override void OnToolInsertClick(object sender, EventArgs e)
		{
			Status = TableEditorStatus.Adding;
		}

		protected override void OnToolRemovClick(object sender, EventArgs e)
		{
			if (Selected == null)
				return;
			var rowsText = new StringBuilder();
			var temp = DBList.Selection.GetItems<DBItem>();
			foreach (DBItem refRow in temp)
				rowsText.AppendLine(refRow.ToString());

			var text = new RichTextView();
			text.LoadText(rowsText.ToString(), Xwt.Formats.TextFormat.Plain);

			var window = new ToolWindow();
			window.Target = text;
			window.Mode = ToolShowMode.Dialog;
			if (mode == TableEditorMode.Referencing || mode == TableEditorMode.Item)
			{
				window.AddButton("Exclude", (object se, EventArgs arg) =>
				{
					foreach (DBItem refRow in temp)
						refRow[OwnerColumn] = null;
					Table.Save();
					window.Hide();
				});
				//tw.ButtonAccept.Location = new Point (b.Location.X - 60, 3);
				//tw.ButtonClose.Location = new Point (b.Location.X - 120, 3);
			}
			window.Label.Text = Common.Locale.Get("TableEditor", "Deleting!");
			window.ButtonAcceptText = Common.Locale.Get("TableEditor", "Delete");
			window.ButtonAcceptClick += (p1, p2) =>
			{
				question.SecondaryText = Common.Locale.Get("TableEditor", "Check Reference?");
				bool flag = MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes;
				list.ListSensetive = false;
				foreach (DBItem selectedRow in temp)
				{
					RowDeleting?.Invoke(this, new ListEditorEventArgs() { Item = selectedRow });

					if (flag)
					{
						foreach (var relation in selectedRow.Table.GetChildRelations())
						{
							var childs = selectedRow.GetReferencing<DBItem>(relation, DBLoadParam.Load | DBLoadParam.Synchronize).ToList();
							if (childs.Count == 0)
								continue;
							rowsText.Clear();
							foreach (DBItem refRow in childs)
								rowsText.AppendLine(refRow.ToString());
							question.SecondaryText = string.Format(Common.Locale.Get("TableEditor", "Found reference on {0}. Delete?\n{1}"), relation.Table, rowsText);
							if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
								for (int j = 0; j < childs.Count; j++)
									((DBItem)childs[j]).Delete();
							else
							{
								question.SecondaryText = string.Format(Common.Locale.Get("TableEditor", "Found reference on {0}. Remove Refence?\n{1}"), relation.Table, rowsText);
								if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
									for (int j = 0; j < childs.Count; j++)
										((DBItem)childs[j])[relation.Column] = null;
							}
							relation.Table.Save();
						}
					}
					selectedRow.Delete();
				}

				Table.Save();
				list.ListSensetive = true;
				// list.QueueDraw(true, true);
				window.Hide();
			};
			window.Show(this, Point.Zero);
		}

		protected override void OnToolCopyClick(object sender, EventArgs e)
		{
			if (Selected == null)
				return;
			Status = TableEditorStatus.Clone;
			toolWindow.Show(Bar, toolAdd.Bound.BottomLeft);
		}

		private void ToolClearClick(object sender, EventArgs e)
		{
			Table.Clear();
		}

		private void ToolNFirstClick(object sender, EventArgs e)
		{
			if (view.Count == 0)
				return;
			DBList.SelectedRow = (DBItem)view[0];
		}

		private void ToolNPrevClick(object sender, EventArgs e)
		{
			if (DBList.SelectedRow == null)
				return;
			int index = view.IndexOf(DBList.SelectedRow);
			if (index > 0)
				DBList.SelectedRow = (DBItem)view[index - 1];
		}

		private void ToolNNextClick(object sender, EventArgs e)
		{
			if (DBList.SelectedRow == null)
				return;
			int index = list.ListSource.IndexOf(DBList.SelectedRow);
			if (index < list.ListSource.Count - 2)
				DBList.SelectedRow = (DBItem)view[index + 1];
		}

		private void ToolNLastClick(object sender, EventArgs e)
		{
			if (view.Count == 0)
				return;
			DBList.SelectedRow = (DBItem)view[DBList.View.Count - 1];
		}

		protected override void OnToolWindowCancelClick(object sender, EventArgs e)
		{
			base.OnToolWindowCancelClick(sender, e);
			DBItem bufRow = ((TableLayoutList)toolWindow.Target).FieldSource as DBItem;
			RowFilter = string.Empty;
			bufRow.Reject();
		}

		protected override void OnToolWindowAcceptClick(object sender, EventArgs e)
		{
			DBItem bufRow = ((TableLayoutList)toolWindow.Target).FieldSource as DBItem;
			if (!bufRow.Attached)
			{
				question.SecondaryText = "Check";

				if (Status == TableEditorStatus.Search && list.ListSource.Count > 0)
				{
					question.Text = Locale.Get("TableEditor", "Found duplicate records!\nContinee?");
					if (MessageDialog.AskQuestion(ParentWindow, question) == Command.No)
						return;
				}
				if (mode == TableEditorMode.Referencing)
				{
					if (bufRow[baseColumn].ToString() != OwnerRow.PrimaryId.ToString())
					{
						question.Text = Locale.Get("TableEditor", "Change reference?");
						if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
							bufRow[baseColumn] = OwnerRow.PrimaryId;
					}
				}

				Table.Add(bufRow);
				try
				{
					bufRow.Save();
				}
				catch (Exception ex)
				{
					Helper.OnException(ex);
					toolWindow.Visible = true;
				}

				if (bufRow.DBState != DBUpdateState.Default)
					return;

				if (status == TableEditorStatus.Clone)
				{
					question.Text = Locale.Get("TableEditor", "Clone References?");
					if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
					{
						var relations = Table.GetChildRelations();
						foreach (var relation in relations)
						{
							question.Text = string.Format(Locale.Get("TableEditor", "Clone Reference {0}?"), relation.Table);
							if (MessageDialog.AskQuestion(ParentWindow, question) == Command.No)
								continue;

							var refrows = clonedRow.GetReferencing(relation, DBLoadParam.Load | DBLoadParam.Synchronize);

							foreach (DBItem refrow in refrows)
							{
								var newRow = refrow.Clone() as DBItem;
								newRow.PrimaryId = DBNull.Value;
								newRow[relation.Column] = bufRow.PrimaryId;
								relation.Table.Add(newRow);
							}
							relation.Table.Save();
						}
					}
				}
				//if (MessageDialog.ShowMessage(_rowTool, "Добавить еще поле?", "Добавление", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==  Command.Yes)
				//{
				//    Status = _status;
				//    return;
				//}
			}
			//else if ((bufRow.DBState & DBRowState.Default) != DBRowState.Default)
			//{
			// //   if (_mode == TableFormMode.RefingTable)
			//        bufRow[_ocolumn] = _orow.Id;
			//    Save(Table);
			//    if ((bufRow.DBState & DBRowState.Default) != DBRowState.Default)
			//        return;
			//}
			Status = TableEditorStatus.Default;
			Selected = bufRow;
			if (mode == TableEditorMode.Reference)
				OnItemSelect(new ListEditorEventArgs(bufRow));
		}


		private void OnRowSelected(object sender, ListEditorEventArgs e)
		{
			TableEditor tab = (TableEditor)sender;
			List<DBItem> rows = tab.SelectedRows;
			foreach (DBItem row in rows)
			{
				DBItem dr = Table.NewItem();
				dr[baseColumn] = baseRow.PrimaryId;
				dr[tab.baseColumn] = row.PrimaryId;
				Table.Add(dr);
			}
			_currentControl.Hide();
			_currentControl = null;
		}

		private void ToolLoadOnClick(object sender, EventArgs e)
		{
			if (view == null)
				return;

			if (!loader.IsLoad())
				loader.Load();
			else
				loader.Cancel();
		}

		private void ToolReportClick(object sender, EventArgs e)
		{
			var editor = new QueryEditor();
			editor.Initialize(SearchState.Edit, new QQuery(string.Empty, Table), null, null);
			editor.ShowDialog(this);
			editor.Dispose();
		}

		protected override void OnToolRefreshClick(object sender, EventArgs e)
		{
			if (Table.IsEdited)
			{
				var question = new QuestionMessage(Locale.Get("TableEditor", "Continue Rejecting?"), "Check");
				question.Buttons.Add(Command.No);
				question.Buttons.Add(Command.Yes);
				if (MessageDialog.AskQuestion(ParentWindow, question) == Command.Yes)
					Table.RejectChanges();
			}
		}

		protected override void OnToolSaveClick(object sender, EventArgs e)
		{
			Table.Save();
		}

		private void OnToolMergeClick(object sender, EventArgs e)
		{
			if (list.Selection.Count >= 2)
			{
				var itemlist = new List<DBItem>(Table);
				foreach (var item in list.Selection)
					itemlist.Add((DBItem)item.Item);
				var merge = new TableRowMerge();
				merge.Items = itemlist;

				merge.Run(ParentWindow);
			}
		}


		private void ToolClearFilterClick(object sender, EventArgs e)
		{
			RowFilter = "";
		}

		private void OnToolInsertLineClick(object sender, EventArgs e)
		{
			DBItem newRow = (DBItem)TableView.NewItem();
			newRow.Status = DBStatus.New;
			if (mode == TableEditorMode.Referencing)
				newRow[baseColumn] = OwnerRow.PrimaryId;
			TableView.ApplySort(null);
			TableView.Add(newRow);
			DBList.SelectedRow = newRow;
			//list.VScrollToItem(newRow);
		}


		#region ILocalizable implementation

		public override void Localize()
		{
			base.Localize();
			GuiService.Localize(toolInsertLine, "TableEditor", "Insert Line", GlyphType.ChevronCircleRight);
			GuiService.Localize(toolReport, "TableEditor", "Report", GlyphType.FileExcelO);
			GuiService.Localize(toolParam, "TableEditor", "View Params", GlyphType.GearAlias);
			GuiService.Localize(toolMerge, "TableEditor", "Merge", GlyphType.PaperPlane);
		}

		#endregion
	}

	public delegate void Updated(object sender, EventArgs e);

	public delegate void Updating(object sender, EventArgs e);

	public delegate void ClosedControl(object sender, EventArgs e);

}
