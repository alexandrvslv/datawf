using DataWF.Common;
using DataWF.Gui;
using System;
using System.Collections.Generic;
using Xwt;

namespace DataWF.Data.Gui
{
    public class ChangeAccept : ToolWindow
    {
        private LayoutList listRows;
        private LayoutList listDiff;
        //private LayoutList listChilds = new LayoutList();
        private LogMap map;
        private VPaned split = new VPaned();

        public ChangeAccept()
        {
            listRows = new LayoutList()
            {
                EditMode = EditModes.None,
                EditState = EditListState.ReadOnly,
                GenerateColumns = false,
                GenerateToString = false,
                Name = "listRows",
                Text = "List Rows",
                ListInfo = new LayoutListInfo(
                    new LayoutColumn { Name = "User", Width = 120 },
                    new LayoutColumn { Name = "Row.Status", Width = 60 },
                    new LayoutColumn { Name = "Row", Width = 150, FillWidth = true })
                {
                    StyleRowName = "ChangeRow",
                    HeaderVisible = false
                }
            };
            listRows.SelectionChanged += RowsSelectionChanged;

            listDiff = new LayoutList()
            {
                EditMode = EditModes.ByClick,
                EditState = EditListState.Edit,
                GenerateColumns = false,
                GenerateToString = false,
                Mode = LayoutListMode.List,
                Name = "listDiff",
                Text = "List Details",
                ListInfo = new LayoutListInfo(
                    new LayoutColumn() { Name = "User", Width = 120, Editable = false },
                    new LayoutColumn() { Name = "Column", Width = 120, Editable = false },
                    new LayoutColumn() { Name = "OldFormat", Width = 150, FillWidth = true },
                    new LayoutColumn() { Name = "NewFormat", Width = 150, FillWidth = true })
                {
                    StyleRowName = "ChangeRow",
                    HeaderVisible = false
                }
            };

            //listChilds.GenerateColumns = false;
            //listChilds.GenerateToString = false;
            //listChilds.Mode = LayoutListMode.List;
            //listChilds.Name = "listChilds";
            //listChilds.Text = "List Childs";
            //listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "User", Width = 120, Editable = false });
            //listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "DBTable", Width = 100, Editable = false });
            //listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "DBRow", Width = 120, Editable = false });
            //listChilds.ListInfo.Columns.Add(new LayoutColumn() { Name = "Text", Width = 150, FillWidth = true });

            split.Panel1.Content = null;
            split.Panel2.Content = listDiff;

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
            Change = listRows.SelectedItem as LogMap;
        }

        public DBItem Row
        {
            get { return map?.Row; }
            set
            {
                Change = new LogMap(value);
            }
        }

        public LogMap Change
        {
            get { return map; }
            set
            {
                map = value;
                listDiff.ListSource = map?.Changes;
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

        public List<LogMap> Rows
        {
            get { return listRows.ListSource as List<LogMap>; }
            set
            {
                listRows.ListSource = value;
                split.Panel1.Content = value != null ? listRows : null;
            }
        }

        protected override void OnAcceptClick(object sender, EventArgs e)
        {
            if (Rows != null)
            {
                foreach (var item in Rows)
                    item.Accept(GuiEnvironment.User);
            }
            else
            {
                //MessageDialog.ShowMessage(this, Locale.Get("ChangeAccept", "Access denied for " + _change.Row + "!\nYour login in editor list."));
                map.Accept(GuiEnvironment.User);
            }
            base.OnAcceptClick(sender, e);
        }

        protected override void OnCloseClick(object sender, EventArgs e)
        {
            if (Rows != null)
            {
                foreach (var item in Rows)
                    item.Reject(GuiEnvironment.User);
            }
            else
            {
                map.Reject(GuiEnvironment.User);
            }
            base.OnCloseClick(sender, e);
        }
    }


}
