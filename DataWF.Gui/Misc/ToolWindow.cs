using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xwt;

namespace DataWF.Gui
{
    public class ToolWindow : Window
    {
        protected System.Timers.Timer timerHide = new System.Timers.Timer();
        protected System.Timers.Timer timerStart = new System.Timers.Timer();
        protected ToolShowMode mode = ToolShowMode.Default;
        protected Widget senderWidget;
        protected WindowFrame senderWindow;
        protected Widget target;
        protected ToolLabel toolLabel;
        protected ToolItem toolAccept;
        protected ToolItem toolClose;
        protected VBox vbox;
        protected Toolsbar bar;
        protected ScrollView panel;
        private Point movePoint;
        private bool byDeactivate;
        private bool closeOnAccept = true;
        private bool closeOnClose = true;
        private Widget tempSender;
        private Point tempLocation;
        private PointerButton moveButton;
        private Point moveBounds;
        private TaskCompletionSource<Command> token;

        public ToolWindow()// : base(PopupType.Menu)
        {
            var p = 6;

            panel = new ScrollView
            {
                Name = "panel"
            };

            toolLabel = new ToolLabel
            {
                Name = "toolLabel",
                Text = "Label",
                FillWidth = true,
            };

            toolClose = new ToolItem(OnCloseClick)
            {
                Name = "Close",
                Text = "Close",
                DisplayStyle = ToolItemDisplayStyle.Text
            };

            toolAccept = new ToolItem(OnAcceptClick)
            {
                Name = "Accept",
                Text = "Ok",
                DisplayStyle = ToolItemDisplayStyle.Text
            };

            bar = new Toolsbar(
                toolLabel,
                toolClose,
                toolAccept)
            { Name = "Bar" };
            bar.ButtonPressed += OnContentMouseDown;
            bar.ButtonReleased += OnContentMouseUp;
            bar.MouseEntered += OnContentMouseEntered;
            bar.MouseExited += OnContentMouseExited;
            bar.MouseMoved += OnContentMouseMove;
            //hbox.Margin = new WidgetSpacing(padding, 0, padding, padding);

            vbox = new VBox
            {
                //Margin = new WidgetSpacing(padding, padding, padding, padding),
                Name = "tools"
            };
            vbox.PackStart(panel, true, true);
            vbox.PackStart(bar, false, false);
            vbox.KeyPressed += OnContentKeyPress;


            BackgroundColor = GuiEnvironment.Theme["Window"].BaseColor.WithIncreasedLight(0.1D);
            Content = vbox;
            Decorated = false;
            Name = "ToolWindow";
            //Resizable = false;
            Resizable = true;
            Size = new Size(360, 320);
            ShowInTaskbar = false;
            InitialLocation = WindowLocation.Manual;
            Padding = new WidgetSpacing(p, p, p, p);

            timerHide.Interval = 8000;
            timerHide.Elapsed += TimerHideTick;

            timerStart.Interval = 500;
            timerStart.Elapsed += TimerStartTick;
        }

        public bool HeaderVisible
        {
            get { return bar.Visible; }
            set
            {
                if (bar.Visible != value)
                {
                    bar.Visible = value;
                }
            }
        }

        public ToolShowMode Mode
        {
            get { return mode; }
            set { mode = value; }
        }

        public ToolWindow OwnerToolForm
        {
            get { return senderWindow as ToolWindow; }
        }

        public System.Timers.Timer StartTimer
        {
            get { return timerStart; }
        }

        public double TimerInterval
        {
            get { return timerHide.Interval; }
            set { timerHide.Interval = value; }
        }

        public virtual Widget Target
        {
            get { return target; }
            set
            {
                if (target == value)
                    return;
                target = value;
                if (target is ILocalizable localizeable)
                {
                    localizeable.Localize();
                }
                panel.Content = target;
                var size = target.Surface.GetPreferredSize();
                if (size.Width > Size.Width || size.Height > Size.Height)
                    Size = new Size(Math.Max(Size.Width, Math.Min(size.Width, 1024)) + 35,
                                   Math.Max(Size.Height, Math.Min(size.Height, 768)) + 70);
            }
        }

        public Command DResult { get; set; }

        public ToolLabel Label
        {
            get { return toolLabel; }
        }

        public ToolItem ButtonAccept
        {
            get { return toolAccept; }
        }

        public string ButtonAcceptText
        {
            get { return toolAccept.Text; }
            set { toolAccept.Text = value; }
        }

        public bool ButtonAcceptEnabled
        {
            get { return toolAccept.Sensitive; }
            set { toolAccept.Sensitive = value; }
        }

        public event EventHandler ButtonAcceptClick
        {
            add { toolAccept.Click += value; }
            remove { toolAccept.Click -= value; }
        }

        public ToolItem ButtonClose
        {
            get { return toolClose; }
        }

        public string ButtonCloseText
        {
            get { return toolClose.Text; }
            set { toolClose.Text = value; }
        }

        public event EventHandler ButtonCloseClick
        {
            add { toolClose.Click += value; }
            remove { toolClose.Click -= value; }
        }

        public Widget Sender
        {
            get { return senderWidget; }
            set
            {
                byDeactivate = false;
                senderWidget = value;
                Owner = senderWidget?.ParentWindow as WindowFrame;
            }
        }

        public WindowFrame Owner
        {
            get { return senderWindow; }
            set
            {
                senderWindow = value;
                TransientFor = value;
            }
        }

        protected void OnSenderClick(object sender, ButtonEventArgs e)
        {
            Hide();
        }

        protected void OnContentMouseDown(object sender, ButtonEventArgs e)
        {
            if (e.Button == PointerButton.Left)
            {
                movePoint = Desktop.MouseLocation;
                moveButton = e.Button;
                bar.Cursor = CursorType.Move;
            }
        }

        protected void OnContentMouseUp(object sender, ButtonEventArgs e)
        {
            moveButton = 0;
            moveBounds = Location;
            bar.Cursor = CursorType.Arrow;
        }

        private void OnContentMouseExited(object sender, EventArgs e)
        {
            if (Mode == ToolShowMode.ToolTip)
            {
                Debug.WriteLine($"Handle Mouse Exited!");
                timerHide.Start();
            }
        }

        private void OnContentMouseEntered(object sender, EventArgs e)
        {
            if (Mode == ToolShowMode.ToolTip)
            {
                Debug.WriteLine($"Handle Mouse Entered!");
                timerHide.Stop();
            }
        }

        protected void OnContentMouseMove(object sender, MouseMovedEventArgs e)
        {
            if (moveButton == PointerButton.Left)
            {
                var location = Desktop.MouseLocation;
                var diff = new Point(location.X - movePoint.X, location.Y - movePoint.Y);
                Debug.WriteLine($"Location Diff:{diff} Bound:{moveBounds}");

                if (bar.Cursor == CursorType.Move)
                {
                    Location = new Point(moveBounds.X + diff.X, moveBounds.Y + diff.Y);
                }

            }
        }

        public virtual void Show(Widget sender, Point location)
        {
            DResult = null;
            if (mode == ToolShowMode.ToolTip)
            {
                tempSender = sender;
                tempLocation = location;

                if (!timerStart.Enabled)
                {
                    timerStart.Start();
                    return;
                }
            }
            Sender = sender;

            CheckLocation(sender?.ConvertToScreenCoordinates(location) ?? location);
            base.Show();
            CheckLocation(sender?.ConvertToScreenCoordinates(location) ?? location);

            //if (Owner != null) Owner.Show();
            if (mode == ToolShowMode.AutoHide || mode == ToolShowMode.ToolTip)
            {
                if (timerHide.Enabled)
                    timerHide.Stop();
                timerHide.Start();
            }

            byDeactivate = false;
        }

        public async Task<Command> ShowAsync(Widget sender, Point location)
        {
            Show(sender, location);
            token = new TaskCompletionSource<Command>();
            return await token.Task;
        }

        public void ShowCancel()
        {
            timerStart.Stop();
            Hide();
        }

        private void CheckLocation(Point location)
        {
            Rectangle screen = (TransientFor?.Screen ?? Desktop.PrimaryScreen).VisibleBounds;
            if (location.Y + Height > screen.Height)
            {
                location.Y -= (location.Y + Height) - screen.Height;
                //Left += 10;
            }
            if (location.X + Width > screen.Right)
            {
                location.X -= (location.X + Width) - screen.Right;
            }
            Location = moveBounds = location;
            //moveBounds.Size = Size = Content.Surface.GetPreferredSize();
        }

        public IEnumerable<ToolWindow> GetOwners()
        {
            var window = this;
            while (window.OwnerToolForm != null)
            {
                yield return window.OwnerToolForm;
                window = window.OwnerToolForm;
            }
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            var temp = Owner;
            Owner = null;

            moveButton = 0;

            if (temp != null)
            {
                if (temp is ToolWindow)
                {
                    if (!byDeactivate)
                        ((ToolWindow)temp).byDeactivate = true;
                }
                // temp.Visible = true;
            }

            if (timerHide.Enabled)
                timerHide.Stop();
            byDeactivate = false;
        }

        public new void Hide()
        {
            base.Hide();

            if (token != null)
            {
                token.TrySetResult(DResult);
                token = null;
            }
        }

        protected void OnContentKeyPress(object sender, KeyEventArgs e)
        {
            //prevent alt from closing it and allow alt+menumonic to work
            if (Keyboard.CurrentModifiers == ModifierKeys.Alt)
                e.Handled = true;
            if (e.Key == Key.Escape)
                Hide();
        }

        private void TimerHideTick(object sender, EventArgs e)
        {
            Application.Invoke(() =>
            {
                Hide();
                timerHide.Stop();
            });
        }

        private void TimerStartTick(object sender, EventArgs e)
        {
            Application.Invoke(() =>
            {
                Show(tempSender, tempLocation);
                timerStart.Stop();
            });
        }

        protected virtual void OnCloseClick(object sender, EventArgs e)
        {
            DResult = Command.Cancel;
            if (CloseOnClose)
                Hide();
        }

        protected virtual void OnAcceptClick(object sender, EventArgs e)
        {
            DResult = Command.Ok;
            if (CloseOnAccept)
                Hide();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Mode == ToolShowMode.ToolTip)
                    return;
                //if (target != null)
                //    target.Dispose();
                if (timerHide != null)
                    timerHide.Dispose();
                if (timerStart != null)
                    timerStart.Dispose();
            }
            base.Dispose(disposing);
        }

        public new string Title
        {
            get { return base.Title; }
            set
            {
                base.Title = value;
                Label.Text = value;
            }
        }

        public void AddButton(string text, EventHandler click)
        {
            var button = new ToolItem(click)
            {
                Name = text,
                Text = text
            };
            bar.Add(button);
        }

        public static ToolWindow InitEditor(string label, object obj, bool dispose = true)
        {
            var list = new LayoutList
            {
                EditMode = EditModes.ByClick,
                FieldSource = obj
            };

            var window = new ToolWindow
            {
                Mode = ToolShowMode.Dialog,
                HeaderVisible = true,
                Target = list
            };
            window.Label.Text = label;
            if (dispose)
                window.Closed += (s, e) => window.Dispose();

            return window;
        }

        public bool CloseOnAccept
        {
            get { return closeOnAccept; }
            set { closeOnAccept = value; }
        }

        public bool CloseOnClose
        {
            get { return closeOnClose; }
            set { closeOnClose = value; }
        }


    }

    [Flags]
    public enum DialogResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 4,
        No = 8
    }
}
