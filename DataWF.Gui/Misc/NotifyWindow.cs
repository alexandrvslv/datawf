using DataWF.Common;
using System;
using System.Collections;
using System.ComponentModel;
using Xwt;

namespace DataWF.Gui
{
    public class TaskWindow : ToolWindow
    {
        private LayoutList taskList = new LayoutList();
        public TaskWindow()
            : base()
        {
            taskList.Text = "Task";
            taskList.GenerateToString = false;
            taskList.GenerateColumns = false;
            taskList.ListInfo.ColumnsVisible = false;
            taskList.ListInfo.Columns.Add(new LayoutColumn() { Name = "Name", FillWidth = true });
            taskList.CellDoubleClick += TaskListCellDoubleClick;

            this.Label.Text = "Tasks";
            this.HeaderVisible = true;
            this.Mode = ToolShowMode.ToolTip;
            this.Target = taskList;
            //this.TopMost = true;
            this.Width = 400;
            this.Height = 250;
            this.ButtonClose.Visible = false;
            this.TimerInterval = 4000;
        }

        protected override void OnCloseClick(object sender, EventArgs e)
        {
            var task = taskList.SelectedItem as TaskExecutor;
            if (task != null)
                task.Cancel();
        }

        private void TaskListCellDoubleClick(object sender, LayoutHitTestEventArgs e)
        {
            var task = e.HitTest.Item as TaskExecutor;
            if (task != null)
                task.Cancel();
        }

        public IList TaskList
        {
            get { return taskList.ListSource; }
            set
            {
                taskList.ListSource = value;
            }
        }
    }

    public class NotifyWindow : ToolWindow
    {
        private SelectableList<StateInfo> notifies = new SelectableList<StateInfo>();
        private LayoutList notifyList = new LayoutList();
        public event EventHandler ItemClick;

        public NotifyWindow()
            : base()
        {
            var style = GuiEnvironment.StylesInfo["Notify"];

            LayoutColumn module = new LayoutColumn()
            {
                Name = "Module",
                FillWidth = true,
                Col = 2,
                Height = 25,
                Style = style
            };

            var notifyInfo = new LayoutListInfo();
            notifyInfo.Columns.Add("Icon", 25);
            notifyInfo.Columns.Add(module);
            notifyInfo.Columns.Add("Date", 50, 0, 3).Format = "hh:mm";
            notifyInfo.Columns.Add("Message", 100, 1, 0);
            notifyInfo.Columns.Add("Description", 100, 2, 0);
            notifyInfo.ColumnsVisible = false;
            notifyInfo.HeaderVisible = false;
            notifyInfo.HotTrackingCell = true;
            notifyInfo.CalcHeigh = false;
            notifyInfo.Indent = 5;

            notifies.ApplySort(new InvokerComparer(typeof(StateInfo), "Date", ListSortDirection.Descending));

            notifyList.Text = "Notify";
            notifyList.GenerateColumns = false;
            notifyList.GenerateToString = false;
            notifyList.ListInfo = notifyInfo;
            notifyList.ListSource = notifies;
            notifyList.CellDoubleClick += NotifyPListCellClick;

            //dock.PagesAlign = LayoutAlignType.Bottom;
            //dock.Add(notifyList);
            //dock.Add(taskList);

            this.Label.Text = "Notify";
            this.HeaderVisible = false;
            this.Mode = ToolShowMode.ToolTip;
            this.Target = notifyList;
            this.Width = 400;
            this.Height = 250;
            this.ButtonClose.Visible = false;
            this.TimerInterval = 4000;
            //notifyWondow.Opacity = 0.5D;
            //this.MaximumSize = new Size(400, Screen.PrimaryScreen.WorkingArea.Height - 50);
        }



        public void SetStatus(StateInfo item, bool show)
        {
            //dock.SelectControlPage(notifyList);
            notifies.Add(item);
            if (notifies.Count > 30)
                notifies.RemoveAt(notifies.Count - 1);
            notifyList.SelectedItem = item;

            if (show)
            {
                Point p = new Point(10, ScreenBounds.Height - (Height + 20));
                Mode = ToolShowMode.ToolTip;
                Show(null, p);//(Control)GuiService.Main
            }
        }

        private void NotifyPListCellClick(object sender, LayoutHitTestEventArgs e)
        {
            if (ItemClick != null)
            {
                ItemClick(this, EventArgs.Empty);
            }
        }

        public StateInfo Info
        {
            get { return notifyList.SelectedItem as StateInfo; }
        }
    }
}
