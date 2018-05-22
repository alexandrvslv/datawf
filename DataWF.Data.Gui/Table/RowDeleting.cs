using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using DataWF.Gui;
using DataWF.Data;
using Xwt;

namespace DataWF.Data.Gui
{
    public class RowDeleting : ToolWindow
    {
        private List<DBItem> rowsDelete = null;
        private SelectableList<DBItem> rows = new SelectableList<DBItem>();
        private DBItem row;
        private string message = string.Empty;

        private LayoutList list;
        private ToolLabel toolExecute;
        private ToolProgressBar toolProgress;

        public RowDeleting()
        {
            toolExecute = new ToolLabel();
            toolProgress = new ToolProgressBar { Name = "toolProgress" };

            list = new LayoutList()
            {
                AutoToStringFill = true,
                GenerateColumns = false,
                Name = "list",
                Text = "Reference List",
                ListInfo = new LayoutListInfo(
                    new[]{
                        new LayoutColumn{Name = nameof(DBItem.Table), Width = 100, Visible = false},
                        new LayoutColumn{Name = nameof(DBItem.Status), Width = 100 },
                        new LayoutColumn { Name = nameof(DBItem.UpdateState), Width = 100 },
                        new LayoutColumn { Name = nameof(DBItem.Attached), Width = 30 }
                    },
                    new[] { new LayoutSort("Table", ListSortDirection.Ascending, true) })
                { ColumnsVisible = false, },
                ListSource = rows
            };

            bar.Add(toolExecute);
            bar.Add(toolProgress);

            Label.Text = "Row Deleting";

            Name = "RowDeleting";
            Target = list;
            Mode = ToolShowMode.Modal;

            Localizing();
        }

        public DBItem Row
        {
            get { return row; }
            set
            {
                if (row == value)
                    return;
                row = value;
                Label.Text = row.ToString();
                toolProgress.Visible = true;
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    try
                    {
                        rowsDelete = row.GetChilds(2, DBLoadParam.Load);
                        rows.Clear();
                        rows.AddRange(rowsDelete);
                        Callback();
                    }
                    catch (Exception ex)
                    {
                        Helper.OnException(ex);
                    }
                });
            }
        }

        private void Deleting()
        {
            try
            {
                foreach (DBItem r in rowsDelete)
                {
                    r.Delete();
                    r.Save();
                }
                row.Delete();
                row.Save();
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
        }

        private void Callback()
        {
            Application.Invoke(() =>
            {
                toolProgress.Visible = false;
                list.RefreshBounds(false);
                if (message.Length != 0)
                {
                    MessageDialog.ShowMessage(Content.ParentWindow, message);
                }
                else if (!row.Attached)
                {
                    toolExecute.Text = Locale.Get("RowDeleting", "Delete Complete!");
                    MessageDialog.ShowMessage(Content.ParentWindow, toolExecute.Text);
                    Hide();
                }
                else
                {
                    toolExecute.Text = Locale.Get("RowDeleting", "Start Delete?");
                }
                message = string.Empty;
            });
        }

        protected override void OnAcceptClick(object sender, EventArgs e)
        {
            toolExecute.Text = Locale.Get("RowDeleting", "Deleting");
            toolProgress.Visible = true;
            ButtonAcceptEnabled = false;
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    Deleting();
                    Callback();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                }
            });
        }

        public DockType DockType
        {
            get { return DockType.Content; }
        }

        public bool HideOnClose
        {
            get { return false; }
        }

        public void Localizing()
        {
            Label.Text = Locale.Get("RowDeleting", "Row Deleting");
            ButtonAcceptText = Locale.Get("RowDeleting", "Start Deleting");
            toolExecute.Text = Locale.Get("RowDeleting", "Loading");
        }

        //public object Picture
        //{
        //    get { return Localize.GetImage("RowDeleting", "Row Deleting"); }
        //}

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

    }
}
