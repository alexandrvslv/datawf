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

        private LayoutList list = new LayoutList();
        private Toolsbar toolsb = new Toolsbar();
        private ToolLabel toolExecute = new ToolLabel();
        private ToolProgressBar toolProgress = new ToolProgressBar();
        private VPanel box = new VPanel();

        public RowDeleting()
        {
            list.AutoToStringFill = true;
            list.GenerateColumns = false;
            list.Name = "list";
            list.Text = "Reference List";
            list.ListInfo.Columns.Add("Table", 100).Visible = false;
            list.ListInfo.Columns.Add("Status", 100);
            list.ListInfo.Columns.Add("DBState", 100);
            list.ListInfo.Columns.Add("Attached", 30);
            list.ListInfo.ColumnsVisible = false;
            list.ListInfo.Sorters.Add(new LayoutSort("Table", ListSortDirection.Ascending, true));
            list.ListSource = rows;

            toolsb.Items.Add(toolExecute);
            toolsb.Items.Add(toolProgress);
            toolsb.Name = "tools";

            toolExecute.Name = "toolExecute";
            toolProgress.Name = "toolProgress";

            box.Name = "groupBox1";
            //box.Text = "Referencing Rows";
            box.PackStart(toolsb, false, false);
            box.PackStart(list, true, true);

            Label.Text = "Row Deleting";

            this.Name = "RowDeleting";
            this.Target = box;
            this.Mode = ToolShowMode.Modal;

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
                        rowsDelete = DBService.GetChilds(row, 2, DBLoadParam.Load);
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
                    MessageDialog.ShowMessage(Content.ParentWindow, message);
                else if (!row.Attached)
                {
                    toolExecute.Text = Locale.Get("RowDeleting", "Delete Complete!");
                    MessageDialog.ShowMessage(Content.ParentWindow, toolExecute.Text);
                    Hide();
                }
                else
                    toolExecute.Text = Locale.Get("RowDeleting", "Start Delete?");
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
