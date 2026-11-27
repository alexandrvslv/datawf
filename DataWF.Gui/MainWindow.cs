using DataWF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System.ComponentModel;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform.Storage;

namespace DataWF.Gui
{

    public class MainWindow : Window, IDockMain, IXMLSerializable
    {
        public Action SaveAction;
        private TrayIcon icon;
        protected Toolsbar bar;
        protected Toolsbar statusBar;
        protected ToolLabel toolLabel;
        protected ToolDropDown menuWindow;
        protected ToolMenuItem menuWindowLang;
        protected ToolMenuItem menuWindowTheme;
        protected ToolMenuItem menuHelpAbout;
        protected ToolDropDown menuHelp;
        protected DockBox dock;
        protected IControl currentControl;
        protected List<ProjectType> editors = new List<ProjectType>();
        protected ToolSplit toolTasks;
        protected ToolProgressBar toolProgress;
        protected SelectableList<TaskExecutor> tasks = new SelectableList<TaskExecutor>();
        protected NotifyWindow notify;
        protected TaskWindow task;
        protected ToolDropDown menuProject;
        protected ToolMenuItem menuProjectProps;
        protected ToolMenuItem menuProjectCreate;
        protected ToolMenuItem menuProjectOpen;
        protected ToolMenuItem menuProjectSave;
        protected ToolMenuItem menuProjectSaveAs;
        protected ToolMenuItem menuProjectRecent;
        protected ToolMenuItem menuProjectClose;
        protected ToolMenuItem menuProjectExit;
        protected ToolMenuItem menuEditUIEnvironment;
        protected ToolMenuItem menuEditLocalize;
        protected ToolDropDown menuView;
        protected FilePickerOpenOptions openFD;
        protected FilePickerSaveOptions saveFD;

        public MainWindow()
        {
            GuiService.Main = this;
            Helper.ThreadException += OnThreadException;

            ListEditor.StatusClick += FieldsEditorStatusClick;
            ListEditor.LogClick += FieldsEditorLogClick;

            icon = new TrayIcon();

            openFD = new FilePickerOpenOptions() { Title = "Open File" };
            saveFD = new FilePickerSaveOptions() { Title = "Save File" };

            menuProjectCreate = new ToolMenuItem { Name = "Create", DropDown = new Menubar { Name = "Create" } };
            menuProjectCreate.ItemClick += ToolProjectCreateItemClick;
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

            menuView = new ToolDropDown(
                    BuildMenuItem(typeof(PropertiesEditor), "Properties"),
                    BuildMenuItem(typeof(LogExplorer), "Logs"),
                    new ToolSeparator())
            { Name = "View", DisplayStyle = ToolItemDisplayStyle.Text };

            menuEditUIEnvironment = new ToolMenuItem(ToolEditUIEnvironment) { Name = "UI Environment" };
            menuEditLocalize = new ToolMenuItem(ToolEditLocalizeClick) { Name = "Localize" };

            menuWindowLang = new ToolMenuItem { Name = "Language", DropDown = new Menubar { Name = "Language" } };
            foreach (var info in Locale.Instance.Cultures)
            {
                menuWindowLang.DropDown.Items.Add(new ToolLangItem(LangItemClick)
                {
                    Culture = info,
                    Name = info.Name,
                    Text = info.DisplayName,
                });
            }
            menuWindowTheme = new ToolMenuItem { Name = "Theme", DropDown = new Menubar { Name = "Theme" } };
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
                menuEditUIEnvironment,
                menuEditLocalize,
                new ToolSeparator(),
                menuWindowLang,
                menuWindowTheme)
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

            var vbox = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 0
            };
            vbox.Children.Add(bar);
            vbox.Children.Add(dock);
            vbox.Children.Add(statusBar);

            Padding = new Thickness(5, 5, 5, 5);
            Icon = new WindowIcon(new Bitmap("datawf.png"));
            Content = vbox;
            Name = "Main";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Title = "Main Form";
            Width = 1024;
            Height = 768;
            Background = new ImmutableSolidColorBrush(GuiEnvironment.Theme["Window"].BaseColor);
        }

        public LayoutListCache Cache { get; set; } = new LayoutListCache();

        public void LoadConfiguration()
        {
            XmlDeserialize("MainWindow.xml");
            Localize();
        }

        public void SaveConfiguration()
        {
            Helper.SetDirectory();
            Locale.Save();
            SaveAction?.Invoke();
            GuiEnvironment.Save();
            XmlSerialize("MainWindow.xml");
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
                _ = Dispatcher.UIThread.InvokeAsync(() => ShowException(e));
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
            exceptionWindow.Show(null, PixelPoint.Origin);
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
            dock.InvalidateArrange();
            //TODO DBService.RefreshToString();
        }

        public ListEditor Properties
        {
            get
            {
                var item = (ToolControlHandler)menuView.DropDownItems["Properties"];
                if (item.Control == null)
                {
                    MenuViewItemClick(item, EventArgs.Empty);
                }
                return (ListEditor)item.Control;
            }
        }

        public LogExplorer Logs
        {
            get { return dock.Find("Logs") as LogExplorer; }
        }

        #region IAppMainForm implementation

        public ProjectHandler CurrentProject
        {
            get => (currentControl as IProjectEditor)?.Project;
            set
            {
                if (value == CurrentProject)
                    return;
                if (value.Editor == null)
                    value.Load();
                if (value.Editor != null)
                    dock.Put((Control)value.Editor);
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
            var control = (IControl)sender;
            if (control is DockPanel)
            {
                control = ((DockPanel)control).CurrentControl;
            }
            currentControl = control;
        }

        protected void ToolExitOnClick(object sender, EventArgs e)
        {
            Close();
        }

        public IControl GetControl(string name)
        {
            return dock.GetPage(name)?.Control;
        }

        protected void ShowControl(string name)
        {
            ShowControl(GetControl(name));
        }

        protected void ShowControl(IControl Control)
        {
            dock.Put(Control);
        }

        protected void MenuViewItemClick(object sender, EventArgs e)
        {
            var item = (ToolControlHandler)sender;
            if (item.Control == null)
            {
                item.Control = dock.Find(item.Name);
                if (item.Control == null)
                {
                    item.Control = (Control)InvokeProvider.CreateObject(item.ControlType);
                    item.Control.Name = item.Name;
                    if (item.Control is ILocalizable)
                    {
                        ((ILocalizable)item.Control).Localize();
                    }
                }
            }
            ShowControl(item.Control);
        }

        public TrayIcon NotifyIcon
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

        public ToolControlHandler BuildMenuItem(Type ControlType, string name = null, string text = null)
        {
            return new ToolControlHandler(MenuViewItemClick)
            {
                Name = name ?? ControlType.Name,
                Text = text ?? Locale.Get(Locale.GetTypeCategory(ControlType), name ?? ControlType.Name),
                ControlType = ControlType,
                Image = (Bitmap)Locale.GetImage(Locale.GetTypeCategory(ControlType), ControlType.Name),
                //Glyph = Locale.GetGlyph(ControlType, ControlType.Name, GlyphType.EditAlias)
            };
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            foreach (DockPage page in dock.GetPages())
            {
                if (page.Control is IProjectEditor && !page.HideOnClose)
                {
                    if (!((IProjectEditor)page.Control).CloseRequest())
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
            SaveConfiguration();
        }


        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            //Application.Current.Close();
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
            {
                _ = Dispatcher.UIThread.InvokeAsync(() => SetStatus(info));
            }
            else
            {
                notify ??= new NotifyWindow();
                notify.SetStatus(info, true);
            }
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
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                toolTasks.Text = $"Task ({tasks.Count})";
                if (tasks.Count > 0)
                {
                    toolProgress.ProgressBar.IsIndeterminate = true;
                    toolProgress.Visible = true;
                }
                else
                {
                    toolProgress.ProgressBar.IsIndeterminate = false;
                    toolProgress.Visible = false;
                    toolLabel.Text = string.Empty;
                }
            });
        }

        protected void TaskCallback(RProcedureEventArgs e)
        {
            tasks.Remove(e.Task);
            _ = Dispatcher.UIThread.InvokeAsync(TaskRun);
        }

        protected void ToolTaskClick(object sender, EventArgs e)
        {
            task ??= new TaskWindow() { TaskList = tasks };
            task.Show(statusBar, PixelPoint.FromPoint(toolTasks.Bound.TopRight, 1D));
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
            IProjectEditor ip = (IProjectEditor)currentControl;

            if (!File.Exists(ip.Project.FileName))
            {
                ToolProjectSaveAsClick(sender, e);
            }
            else
            {
                ip.Project.Save();
            }
        }

        protected async void ToolProjectSaveAsClick(object sender, EventArgs e)
        {
            IProjectEditor ip = (IProjectEditor)currentControl;
            saveFD.Title = "Save";
            saveFD.FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType(ip.Project.TypeName)
                {
                    Patterns = new List<string> { "*" + ip.Project.Type.Filter}
                }
            };
            saveFD.SuggestedFileName = ip.Project.FileName;

            var file = await StorageProvider.SaveFilePickerAsync(saveFD);
            if (file.CanOpenRead)
            {
                ip.Project.FileName = file.Name;
                ip.Project.Save();
            }
            if (!GuiEnvironment.ProjectsInfo.Contains(ip.Project))
            {
                GuiEnvironment.ProjectsInfo.Add(ip.Project);
            }
        }

        protected async void ToolProjectOpenClick(object sender, EventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(openFD);
            if (files.FirstOrDefault() is IStorageFile file
                && file.TryGetUri(out var uri))
            {
                //string fileExt = System.IO.Path.GetExtension(openFD.FileName);
                List<ProjectHandler> list = GuiEnvironment.ProjectsInfo.Select(ProjectHandler.FileNameInvoker.Instance, CompareOperator.Equal, openFD.FileName.I).ToList();
                if (list.Count != 0)
                {
                    CurrentProject = list[0];
                }
                else
                {
                    object project = SerializeProvider.Deserialize(openFD.FileName);

                    foreach (ProjectType type in editors)
                    {
                        if (project.GetType() == type.Project)
                        {
                            CurrentProject = new ProjectHandler
                            {
                                Project = project,
                                Type = type,
                                FileName = openFD.FileName
                            };
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

        public virtual void XmlSerialize(string file)
        {
            using (var serializer = new XmlTextSerializer())
            {
                serializer.Serialize(file, this);
            }
        }

        public virtual void Serialize(XmlInvokerWriter writer)
        {
            writer.WriteAttribute("Width", Width);
            writer.WriteAttribute("Height", Height);
            writer.Write(Cache, "Cache", true);
            writer.Write(dock, dock.Name, true);
        }

        public virtual void XmlDeserialize(string file)
        {
            using (var serializer = new XmlTextSerializer())
            {
                serializer.Deserialize(file, this, false);
            }
        }

        public virtual void Deserialize(XmlInvokerReader reader)
        {
            Width = reader.ReadAttribute<double>("Width");
            Height = reader.ReadAttribute<double>("Height");
            reader.Read(Cache);
            reader.Read(dock);
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

    public class ToolControlHandler : ToolItem
    {
        public ToolControlHandler(EventHandler click) : base(click)
        {
            DisplayStyle = ToolItemDisplayStyle.ImageAndText;
        }

        public Control Control { get; set; }

        public Type ControlType { get; set; }

        public override void Localize()
        { }
    }

}
