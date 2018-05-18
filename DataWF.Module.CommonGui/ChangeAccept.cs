using DataWF.Data;
using DataWF.Gui;
using DataWF.Common;
using System;
using System.Collections.Generic;
using DataWF.Module.Common;
using Xwt;

namespace DataWF.Module.CommonGui
{
    public class ChangeAccept : ToolWindow
    {
        private LayoutList listRows = new LayoutList();
        private LayoutList listDiff = new LayoutList();
        //private LayoutList listChilds = new LayoutList();
        private ItemDataLog _change;
        private List<ItemDataLog> _changes;
        private VPaned split = new VPaned();
        private VPaned splitd = new VPaned();

        public ChangeAccept()
        {
            listRows.EditMode = EditModes.None;
            listRows.EditState = EditListState.ReadOnly;
            listRows.GenerateColumns = false;
            listRows.GenerateToString = false;
            listRows.Mode = LayoutListMode.List;
            listRows.Name = "listRows";
            listRows.Text = "List Rows";
            listRows.SelectionChanged += RowsSelectionChanged;
            listRows.ListInfo.Columns.Add("User", 120);
            listRows.ListInfo.Columns.Add("Row.Status", 60);
            listRows.ListInfo.Columns.Add("Row", 150).FillWidth = true;
            listRows.ListInfo.StyleRow = GuiEnvironment.Theme["ChangeRow"];
            listRows.ListInfo.HeaderVisible = false;

            listDiff.EditMode = EditModes.ByClick;
            listDiff.EditState = EditListState.Edit;
            listDiff.GenerateColumns = false;
            listDiff.GenerateToString = false;
            listDiff.Mode = LayoutListMode.List;
            listDiff.Name = "listDiff";
            listDiff.Text = "List Details";
            listDiff.ListInfo.Columns.Add(new LayoutColumn() { Name = "User", Width = 120, Editable = false });
            listDiff.ListInfo.Columns.Add(new LayoutColumn() { Name = "Column", Width = 120, Editable = false });
            listDiff.ListInfo.Columns.Add(new LayoutColumn() { Name = "OldFormat", Width = 150, FillWidth = true });
            listDiff.ListInfo.Columns.Add(new LayoutColumn() { Name = "NewFormat", Width = 150, FillWidth = true });
            listDiff.ListInfo.StyleRow = GuiEnvironment.Theme["ChangeRow"];
            listDiff.ListInfo.HeaderVisible = false;

            //listChilds.GenerateColumns = false;
            //listChilds.GenerateToString = false;
            //listChilds.Mode = LayoutListMode.List;
            //listChilds.Name = "listChilds";
            //listChilds.Text = "List Childs";
            //listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "User", Width = 120, Editable = false });
            //listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "DBTable", Width = 100, Editable = false });
            //listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "DBRow", Width = 120, Editable = false });
            //listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "Text", Width = 150, FillWidth = true });

            split.Visible = true;

            split.Panel1.Content = null;
            split.Panel2.Content = splitd;
            

            splitd.Panel1.Content = listDiff;
            splitd.Panel2.Content = null;

            Width = 650;
            ButtonAcceptText = Locale.Get("ChangeAccept", "Accept");
            ButtonCloseText = Locale.Get("ChangeAccept", "Reject");
            AddButton(Locale.Get("ChangeAccept", "Cancel"), CancelClick);
            Mode = ToolShowMode.Dialog;
            Target = split;
            Label.Text = Locale.Get("ChangeAccept", "Accept Changes?");
        }

        private void CancelClick(object sender, EventArgs e)
        {
            Close();
        }

        private void RowsSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            listDiff.Visible = true;
            Change = listRows.SelectedItem as ItemDataLog;
        }

        public DBItem Row
        {
            get { return _change == null ? null : _change.Row; }
            set
            {
                Change = new ItemDataLog(value);
            }
        }

        public ItemDataLog Change
        {
            get { return _change; }
            set
            {
                _change = value;
                listDiff.ListSource = _change != null ? _change.Changes : null;
                //listChilds.ListSource = value.GetChilds();
                //foreach (UserLog log in listChilds.ListSource)
                //{
                //    var cache = log.GetCache(UserLog.DBTable.ParseProperty(nameof(UserLog.TextData)));
                //    if (cache == null)
                //        log.RefereshText();
                //}
                //listChilds.Visible = listChilds.ListSource.Count == 0;
            }
        }

        public List<ItemDataLog> Rows
        {
            get { return _changes; }
            set
            {
                _changes = value;
                listDiff.ListInfo.Columns["User"].Visible = false;
                listRows.ListSource = _changes;
                listRows.Visible = _changes != null;
                listDiff.Visible = _changes == null;
            }
        }

        protected override void OnAcceptClick(object sender, EventArgs e)
        {
            if (_changes != null)
                foreach (var item in _changes)
                    Accept(item);
            else
            {
                //foreach (UserLog log in listChilds.ListSource)
                //    _change.Logs.Add(log);
                Accept(_change);
            }
            base.OnAcceptClick(sender, e);
        }

        protected void Accept(ItemDataLog _change)
        {
            if (_change.Check())
                _change.Accept();
            else
                MessageDialog.ShowMessage(this, Locale.Get("ChangeAccept", "Access denied for " + _change.Row + "!\nYour login in editor list."));
        }

        protected override void OnCloseClick(object sender, EventArgs e)
        {
            if (_changes != null)
                foreach (var item in _changes)
                    item.Reject();
            else
            {
                //foreach (UserLog log in listChilds.ListSource)
                //    _change.Logs.Add(log);
                _change.Reject();
            }
            base.OnCloseClick(sender, e);
        }
    }


}
