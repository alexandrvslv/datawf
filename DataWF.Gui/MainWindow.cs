using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using Xwt;
using System.Linq;

namespace DataWF.Gui
{

    public class MainWindow : Window, IDockMain
    {
        public Action SaveAction;
        private StatusIcon icon;
        protected Toolsbar bar;
        protected Toolsbar statusBar;
        protected ToolLabel toolLabel;
        protected ToolDropDown menuWindow;
        protected ToolMenuItem menuWindowLang;
        protected ToolMenuItem menuWindowTheme;
        protected ToolMenuItem menuHelpAbout;
        protected ToolDropDown menuHelp;
        protected DockBox dock;
        protected Widget currentWidget;
        protected List<ProjectType> editors = new List<ProjectType>();
        protected ToolSplit toolTasks;
        protected ToolProgressBar toolProgress;
        protected SelectableList<TaskExecutor> tasks = new SelectableList<TaskExecutor>();
        protected NotifyWindow notify = new NotifyWindow();
        protected TaskWindow task = new TaskWindow();
        protected ToolDropDown menuProject;
        protected ToolMenuItem menuProjectProps;
        protected ToolMenuItem menuProjectCreate;
        protected ToolMenuItem menuProjectOpen;
        protected ToolMenuItem menuProjectSave;
        protected ToolMenuItem menuProjectSaveAs;
        protected ToolMenuItem menuProjectRecent;
        protected ToolMenuItem menuProjectClose;
        protected ToolMenuItem menuProjectExit;
        protected ToolDropDown menuEdit;
        protected ToolMenuItem menuEditUIEnvironment;
        protected ToolMenuItem menuEditLocalize;
        protected ToolDropDown menuView;
        protected OpenFileDialog openFD;
        protected SaveFileDialog saveFD;
        private ListEditor properties;
        private LogExplorer logs;

        public MainWindow()
        {
            GuiService.Main = this;
            Helper.ThreadException += OnThreadException;

            ListEditor.StatusClick += FieldsEditorStatusClick;
            ListEditor.LogClick += FieldsEditorLogClick;

            icon = Application.CreateStatusIcon();

            openFD = new OpenFileDialog() { Title = "Open File" };
            saveFD = new SaveFileDialog() { Title = "Save File" };

            menuProjectCreate = new ToolMenuItem { Name = "Create" };
            menuProjectCreate.DropDown.Bar.ItemClick += ToolProjectCreateItemClick;
            menuProjectOpen = new ToolMenuItem(ToolProjectOpenClick) { Name = "Open" };
            menuProjectProps = new ToolMenuItem(ToolProjectPropertiesClick) { Name = "Properties" };
            menuProjectSave = new ToolMenuItem(ToolProjectSaveClick) { Name = "Save" };
            menuProjectSaveAs = new ToolMenuItem(ToolProjectSaveAsClick) { Name = "SaveAs" };
            menuProjectRecent = new ToolMenuItem() { Name = "Recent" };
            menuProjectClose = new ToolMenuItem(ToolProjectCloseClick) { Name = "Close" };
            menuProjectExit = new ToolMenuItem(ToolExitOnClick) { Name = "Exit" };

            menuProject = new ToolDropDown(
                    menuProjectCreate,
                    menuProjectOpen,
                    menuProjectProps,
                    menuProjectSave,
                    menuProjectSaveAs,
                    menuProjectRecent,
                    menuProjectClose,
                    menuProjectExit)
            { Name = "Project", DisplayStyle = ToolItemDisplayStyle.Text };

            properties = new ListEditor();
            logs = new LogExplorer();

            menuView = new ToolDropDown(
                    BuildMenuItem(properties),
                    BuildMenuItem(logs),
                    new ToolSeparator())
            { Name = "View", DisplayStyle = ToolItemDisplayStyle.Text };

            menuEditUIEnvironment = new ToolMenuItem(ToolEditUIEnvironment) { Name = "UI Environment" };
            menuEditLocalize = new ToolMenuItem(ToolEditLocalizeClick) { Name = "Localize" };

            menuEdit = new ToolDropDown(
                menuEditUIEnvironment,
                menuEditLocalize)
            { Name = "Edit", DisplayStyle = ToolItemDisplayStyle.Text };

            menuWindowLang = new ToolMenuItem { Name = "Language" };
            foreach (var info in Locale.Instance.Cultures)
            {
                menuWindowLang.DropDown.Items.Add(new ToolLangItem(LangItemClick)
                {
                    Culture = info,
                    Name = info.Name,
                    Text = info.DisplayName,
                });
            }
            menuWindowTheme = new ToolMenuItem { Name = "Theme" };
            foreach (var theme in GuiEnvironment.Instance.Themes)
            {
                menuWindowTheme.DropDown.Items.Add(new ToolThemeItem(ThemeItemClick)
                {
                    Theme = theme,
                    Name = theme.Name,
                    Text = theme.Name,
                });
            }
            menuWindow = new ToolDropDown(
                menuWindowLang,
                menuWindowTheme
                )
            {
                Name = "Window",
                DisplayStyle = ToolItemDisplayStyle.Text
            };
            menuHelpAbout = new ToolMenuItem() { Name = "About" };
            menuHelp = new ToolDropDown(menuHelpAbout) { Name = "Help", DisplayStyle = ToolItemDisplayStyle.Text };

            bar = new Toolsbar(
                new ToolSeparator { FillWidth = true },
                menuView,
                menuWindow,
                menuHelp)
            { Name = "MainBar" };

            toolTasks = new ToolSplit { DisplayStyle = ToolItemDisplayStyle.Text, Name = "Tasks" };
            toolProgress = new ToolProgressBar { Name = "Progress", Visible = false };
            toolLabel = new ToolLabel() { Name = "Label", Text = "_" };

            statusBar = new Toolsbar(
                            toolTasks,
                            toolProgress,
                            toolLabel)
            { Name = "StatusBar" };

            dock = new DockBox() { Name = "dock" };
            dock.ContentFocus += DockOnContentFocus;
            //icon.Image = Image.FromResource(GetType(), "datawf.ico"); ;

            var vbox = new VBox() { Spacing = 0 };
            vbox.PackStart(bar, false, false);
            vbox.PackStart(dock, true, true);
            vbox.PackStart(statusBar, false, false);

            Padding = new WidgetSpacing(5, 5, 5, 5);
            Icon = Image.FromResource(typeof(MainWindow), "datawf.png");
            Content = vbox;
            Name = "Main";
            InitialLocation = WindowLocation.CenterScreen;
            Title = "Main Form";
            Size = new Size(1024, 768);
            task.TaskList = tasks;
            BackgroundColor = GuiEnvironment.Theme["Window"].BaseColor;
        }

        public void SaveConfiguration()
        {
            Helper.SetDirectory();
            Locale.Save();
            SaveAction?.Invoke();
            //DBService.Save();
            //DBService.SaveCache();
            GuiEnvironment.Save();
        }


        protected virtual void FieldsEditorLogClick(object sender, ListEditorEventArgs e)
        {

        }

        protected virtual void FieldsEditorStatusClick(object sender, ListEditorEventArgs e)
        {

        }

        protected void OnThreadException(Exception e)
        {
            if (GuiService.InvokeRequired)
                Application.Invoke(() => ShowException(e));
            else
                ShowException(e);
        }

        protected void ShowException(Exception ex)
        {
            var list = new LayoutList() { FieldSource = new ExceptionInfo(ex) };
            list.ListInfo.CalcHeigh = true;

            var exceptionWindow = new ToolWindow()
            {
                Title = "Exception!",
                Mode = ToolShowMode.Dialog,
                Width = 640,
                Target = list
            };
            exceptionWindow.ButtonClose.Visible = false;
            exceptionWindow.Show(null, Point.Zero);
        }

        protected virtual void LangItemClick(object sender, EventArgs e)
        {
            var item = (ToolLangItem)sender;
            if (Locale.Instance.Culture == item.Culture)
            {
                return;
            }
            Locale.Instance.Culture = item.Culture;
            Localize();
            //TODO DBService.RefreshToString();
        }

        protected virtual void ThemeItemClick(object sender, EventArgs e)
        {
            var item = (ToolThemeItem)sender;
            if (GuiEnvironment.Theme == item.Theme)
            {
                return;
            }
            GuiEnvironment.Instance.CurrentTheme = item.Theme;
            dock.QueueForReallocate();
            //TODO DBService.RefreshToString();
        }

        public ListEditor Properties
        {
            get { return properties; }
        }

        public LogExplorer Logs
        {
            get { return logs; }
        }

        #region IAppMainForm implementation

        public ProjectHandler CurrentProject
        {
            get { return (currentWidget as IProjectEditor)?.Project; }
            set
            {
                if (value == CurrentProject)
                    return;
                if (value.Editor == null)
                    value.Load();
                if (value.Editor != null)
                    dock.Put((Widget)value.Editor);
            }
        }

        public void ShowProperty(object sender, object item, bool onTop)
        {
            if (Properties != null)
            {
                Properties.List.EditState = EditListState.Edit;
                Properties.DataSource = item;
                dock.Put(Properties, DockType.Right);
            }
        }

        public void Notify(string text)
        {
            icon.Menu.Popup();
        }

        public virtual void Localize()
        {
            bar.Localize();
            statusBar.Localize();
            dock.Localize();
            Title = Locale.Get(nameof(MainWindow), "Data\\Document Workflow");
        }

        #endregion

        protected virtual void DockOnContentFocus(object sender, EventArgs e)
        {
            var widget = (Widget)sender;
            if (widget is DockPanel)
            {
                widget = ((DockPanel)widget).CurrentWidget;
            }
            currentWidget = widget;
        }

        protected void ToolExitOnClick(object sender, EventArgs e)
        {
            Close();
        }

        public Widget GetControl(string name)
        {
            return dock.GetPage(name)?.Widget;
        }

        protected void ShowControl(string name)
        {
            ShowControl(GetControl(name));
        }

        protected void ShowControl(Widget widget)
        {
            if (widget is IDockContent)
                dock.Put(widget);
        }

        protected void MenuViewItemClick(object sender, EventArgs e)
        {
            ShowControl(((ToolWidgetHandler)sender).Widget);
        }

        public StatusIcon NotifyIcon
        {
            get { return icon; }
        }

        public IDockContainer DockPanel
        {
            get { return dock; }
        }

        public ToolMenuItem BuildButton(ProjectType type)
        {
            var item = new ToolProjectItem()
            {
                Name = type.Project.Name,
                Text = type.Name,
                Type = type
            };
            return item;
        }

        public ToolMenuItem BuildButton(Type type)
        {
            var item = new ToolMenuItem
            {
                Name = Locale.GetTypeCategory(type),
                Text = Locale.Get(type),
                Tag = type
            };
            return item;
        }

        public ToolWidgetHandler BuildMenuItem(Widget widget)
        {
            if (widget is ILocalizable)
            {
                ((ILocalizable)widget).Localize();
            }
            var item = new ToolWidgetHandler(MenuViewItemClick)
            {
                Name = widget.GetType().Name,
                Text = widget is IText ? ((IText)widget).Text : widget.Name,
                Widget = widget
            };
            if (widget is IGlyph)
            {
                item.Image = ((IGlyph)widget).Image;
                item.Glyph = ((IGlyph)widget).Glyph;
            }
            return item;
        }

        protected override bool OnCloseRequested()
        {
            foreach (DockPage page in dock.GetPages())
            {
                if (page.Widget is IProjectEditor && !page.HideOnClose)
                {
                    if (!((IProjectEditor)page.Widget).CloseRequest())
                    {
                        return false;
                    }
                }
            }
            SaveConfiguration();
            return true;
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            Application.Exit();
        }

        public void SetStatusAdd(string message)
        {
            //context.Post(new SendOrPostCallback(SetStatus), message);
        }

        public void SetStatus(object obj)
        {
            if (obj is string)
                SetStatus(new StateInfo("Event Server", (string)obj));
            else if (obj is StateInfo)
                SetStatus((StateInfo)obj);
        }

        public void SetStatus(StateInfo info)
        {
            if (GuiService.InvokeRequired)
                Application.Invoke(() => SetStatus(info));
            else
                notify.SetStatus(info, true);
        }

        protected void ToolEditLocalizeClick(object sender, EventArgs e)
        {
            var editor = new LocalizeEditor();
            dock.Put(editor);
        }

        protected void ToolEditUIEnvironment(object sender, EventArgs e)
        {
            var editor = new ListExplorer { DataSource = GuiEnvironment.Instance };
            dock.Put(editor);
        }

        protected void EditOptions(object option, string title)
        {
            dock.Put(new ListExplorer { Value = option, Text = title });
        }

        public void AddTask(object sender, TaskExecutor task)
        {
            tasks.Add(task);
            TaskRun();
        }

        protected void TaskRun()
        {
            if (tasks.Count > 0)
            {
                TaskExecutor task = tasks[0];
                if (!task.IsAsynchStart)
                {
                    task.Callback += TaskCallback;
                    task.ExecuteAsynch();
                    toolLabel.Text = $"Execute ({task.Name})";
                }
            }
            Application.Invoke(() =>
            {
                toolTasks.Text = $"Task ({tasks.Count})";
                if (tasks.Count > 0)
                {
                    toolProgress.ProgressBar.Indeterminate = true;
                    toolProgress.Visible = true;
                }
                else
                {
                    toolProgress.ProgressBar.Indeterminate = false;
                    toolProgress.Visible = false;
                    toolLabel.Text = string.Empty;
                }
            });
        }

        protected void TaskCallback(RProcedureEventArgs e)
        {
            tasks.Remove(e.Task);
            Application.Invoke(TaskRun);
        }

        protected void ToolTaskClick(object sender, EventArgs e)
        {
            notify.Show(statusBar, toolTasks.Bound.TopRight);
        }

        protected void ToolProjectPropertiesClick(object sender, EventArgs e)
        {
            if (CurrentProject == null)
                return;
            ShowProperty(this, CurrentProject.Project, true);
        }

        protected void ToolProjectCreateItemClick(object sender, ToolItemEventArgs e)
        {
            CurrentProject = new ProjectHandler { Type = e.Item.Tag as ProjectType };
        }

        protected void ToolProjectRecentItemClick(object sender, EventArgs e)
        {
        }

        protected void ToolProjectSaveClick(object sender, EventArgs e)
        {
            IProjectEditor ip = (IProjectEditor)currentWidget;

            if (!File.Exists(ip.Project.FileName))
            {
                ToolProjectSaveAsClick(sender, e);
            }
            else
            {
                ip.Project.Save();
            }
        }

        protected void ToolProjectSaveAsClick(object sender, EventArgs e)
        {
            IProjectEditor ip = (IProjectEditor)currentWidget;
            saveFD.Title = "Save";
            saveFD.Filters.Clear();
            saveFD.Filters.Add(new FileDialogFilter(ip.Project.TypeName, "*" + ip.Project.Type.Filter));
            saveFD.InitialFileName = ip.Project.FileName;

            if (saveFD.Run())
            {
                ip.Project.FileName = saveFD.FileName;
                ip.Project.Save();
            }
            if (!GuiEnvironment.ProjectsInfo.Contains(ip.Project))
            {
                GuiEnvironment.ProjectsInfo.Add(ip.Project);
            }
        }

        protected void ToolProjectOpenClick(object sender, EventArgs e)
        {
            if (openFD.Run())
            {
                //string fileExt = System.IO.Path.GetExtension(openFD.FileName);
                List<ProjectHandler> list = GuiEnvironment.ProjectsInfo.Select("FileName", CompareType.Equal, openFD.FileName).ToList();
                if (list.Count != 0)
                {
                    CurrentProject = list[0];
                }
                else
                {
                    object project = Serialization.Deserialize(openFD.FileName);

                    foreach (ProjectType type in editors)
                    {
                        if (project.GetType() == type.Project)
                        {
                            var handler = new ProjectHandler();
                            handler.Project = project;
                            handler.Type = type;
                            handler.FileName = openFD.FileName;
                            CurrentProject = handler;
                            return;
                        }
                        if (project != null)
                        {
                            EditOptions(project, project.ToString());
                        }
                    }
                }
            }
        }

        protected void ToolProjectCloseClick(object sender, EventArgs e)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    public class ToolProjectItem : ToolMenuItem
    {
        public ToolProjectItem()
        { }

        public ProjectType Type { get; set; }
    }

    public class ToolLangItem : ToolMenuItem
    {
        public ToolLangItem(EventHandler click) : base(click)
        { }

        public CultureInfo Culture { get; set; }
    }

    public class ToolThemeItem : ToolMenuItem
    {
        public ToolThemeItem(EventHandler click) : base(click)
        { }

        public GuiTheme Theme { get; set; }
    }

    public class ToolWidgetHandler : ToolItem
    {
        public ToolWidgetHandler(EventHandler click) : base(click)
        {
            DisplayStyle = ToolItemDisplayStyle.ImageAndText;
            indent = 6;
        }

        public Widget Widget { get; set; }

        public override void Localize()
        { }
    }

}
