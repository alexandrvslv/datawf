using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using DataWF.Gui;
using DataWF.Data;
using Xwt;
using System.Linq;

namespace DataWF.Data.Gui
{
    public class RowDeleting : ToolWindow
    {
        private List<DBItem> rowsDelete = new List<DBItem>();
        private SelectableList<DBItem> rows = new SelectableList<DBItem>();
        private DBItem row;
        private string message = string.Empty;

        private LayoutList list;
        private ToolProgressBar toolProgress;

        public RowDeleting()
        {
            toolProgress = new ToolProgressBar { Name = "toolProgress", DisplayStyle = ToolItemDisplayStyle.Text };

            list = new LayoutList()
            {
                GenerateToString = false,
                GenerateColumns = false,
                Name = "list",
                Text = "Reference List",
                ListInfo = new LayoutListInfo(
                    new[]{
                        new LayoutColumn{Name = nameof(ToString), FillWidth = true},
                        new LayoutColumn{Name = nameof(DBItem.Table), Width = 100, Visible = false},
                        new LayoutColumn{Name = nameof(DBItem.Status), Width = 100 },
                        new LayoutColumn { Name = nameof(DBItem.UpdateState), Width = 100 },
                        new LayoutColumn { Name = nameof(DBItem.Attached), Width = 30 }
                    },
                    new[] { new LayoutSort("Table", ListSortDirection.Ascending, true) })
                { ColumnsVisible = false, },
                ListSource = rows
            };

            bar.Add(toolProgress);

            Label.Text = "Row Deleting";

            Name = "RowDeleting";
            Target = list;
            Mode = ToolShowMode.Modal;
            Size = new Size(800, 600);
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
                toolProgress.Text = Locale.Get("RowDeleting", "Loading");
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    try
                    {
                        rowsDelete.Clear();
                        rows.Clear();
                        rows.Add(row);
                        foreach (var item in row.GetChilds(10, DBLoadParam.Load).Distinct())
                        {
                            rowsDelete.Add(item);
                            rows.Add(item);
                        }
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
                using (var transaction = DBTransaction.GetTransaction(row, row.Table.Schema.Connection))
                {
                    foreach (DBItem r in rowsDelete)
                    {
                        r.Delete();
                        r.Save();
                    }
                    row.Delete();
                    row.Save();
                    transaction.Commit();
                }
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
                    MessageDialog.ShowMessage(Content.ParentWindow, Locale.Get("RowDeleting", "Delete Complete!"));
                    Hide();
                }

                message = string.Empty;
            });
        }

        protected override void OnAcceptClick(object sender, EventArgs e)
        {
            toolProgress.Text = Locale.Get("RowDeleting", "Deleting");
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
