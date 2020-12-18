using DataWF.Common;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using Xwt;

namespace DataWF.Gui
{

    public class CellEditorList : CellEditorText
    {
        protected IList listSource;
        public string ListProperty = nameof(ToString);

        public CellEditorList() : base()
        {
            //HandleTextChanged = false;
        }

        public IList DataSource
        {
            get { return listSource; }
            set
            {
                listSource = value;
                Filtering = true;
            }
        }

        public virtual LayoutList List
        {
            get { return DropDown?.Target as LayoutList; }
        }

        protected virtual void ListReset()
        {
        }

        protected virtual IEnumerable ListFind(string filter)
        {
            if (listSource is ISelectable selectable)
            {
                return selectable.Select(ListProperty, CompareType.Like, filter);
            }

            return ListHelper.Search(listSource, EmitInvoker.Initialize(listSource[0].GetType(), ListProperty), CompareType.Like, filter, null);
        }

        protected virtual void ListSelect(IEnumerable flist)
        {
            if (flist != null)
            {
                List.Selection._Clear();
                List.Selection.AddRange(flist);
            }
            else
            {
                List.Selection.Clear();
            }
        }

        protected override void SetFilter(string filter)
        {
            this.filter = filter;
            if (filter.Length != 0)
            {
                var flist = ListFind(filter).Cast<object>();
                ListSelect(flist);
                var count = flist.Count();
                if (count == 1)
                {
                    Value = ParseValue(flist.First(), EditItem, DataType);
                    string value = EntryWidget.Text;
                    int index = value.IndexOf(filter, StringComparison.OrdinalIgnoreCase);
                    EntryWidget.SelectionStart = index + filter.Length;
                    EntryWidget.SelectionLength = value.Length - EntryWidget.SelectionStart;

                    DropDown.Hide();
                }
                else if (count > 1)
                {
                    Editor.ShowDropDown(ToolShowMode.AutoHide);
                }
            }
            else
            {
                ListReset();
            }
        }

        public override Widget InitDropDownContent()
        {
            var list = Editor.GetCached<LayoutList>();
            list.AllowCheck = false;
            list.GenerateColumns = false;
            list.GenerateToString = false;
            list.ListInfo.HeaderVisible = false;
            list.ListInfo.ColumnsVisible = false;
            list.ListInfo.Tree = true;
            list.Mode = LayoutListMode.List;
            if (list.ListSource != listSource)
            {
                list.ListInfo.Columns.Clear();
                list.ListInfo.Columns.Add(ListProperty, 100).FillWidth = true;
                list.ListSource = listSource;
                if (ListAutoSort)
                    list.OnColumnSort(ListProperty, ListSortDirection.Ascending);
            }
            if (!ReadOnly)
            {
                list.CellDoubleClick += ListCellDoubleClick;
                list.KeyPressed += ListCellKeyDown;
                //list.SelectionChanged += PListSelectionChanged;
            }
            return list;
        }

        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            base.InitializeEditor(editor, value, dataSource);
        }

        protected override object GetDropDownValue()
        {
            return List?.SelectedItem;
        }

        public override object Value
        {
            get { return base.Value; }
            set
            {
                base.Value = value;
                if (value != null && value.GetType() == List.ListType)
                {
                    List.SelectedItem = value;
                }
            }
        }

        protected void ListCellKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return || e.Key == Key.NumPadEnter)
            {
                Value = GetDropDownValue();
                DropDown.Hide();
            }
        }

        protected void ListCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            Value = GetDropDownValue();
            DropDown.Hide();
        }

        private void PListSelectionChanged(object sender, EventArgs e)
        {
            Value = GetDropDownValue();
        }

        public override void FreeEditor()
        {
            if (List != null)
            {
                List.CellDoubleClick -= ListCellDoubleClick;
                List.KeyPressed -= ListCellKeyDown;
            }
            base.FreeEditor();
        }

        public override void Dispose()
        {
            base.Dispose();
            if (listSource is IDisposable)
                ((IDisposable)listSource).Dispose();
        }
    }
}

