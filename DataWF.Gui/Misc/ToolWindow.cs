using DataWF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;

namespace DataWF.Gui
{
    public class ToolWindow : Window
    {
        protected System.Timers.Timer timerHide = new System.Timers.Timer();
        protected System.Timers.Timer timerStart = new System.Timers.Timer();
        protected ToolShowMode mode = ToolShowMode.Default;
        protected Visual senderControl;
        protected Window senderWindow;
        protected Visual target;
        protected ToolLabel toolLabel;
        protected ToolItem toolAccept;
        protected ToolItem toolClose;
        protected StackPanel vbox;
        protected Toolsbar bar;
        protected ScrollViewer panel;
        private Point movePoint;
        private bool byDeactivate;
        private bool closeOnAccept = true;
        private bool closeOnClose = true;
        private Visual tempSender;
        private PixelPoint tempLocation;
        private MouseButton moveButton;
        private PixelPoint moveBounds;
        private TaskCompletionSource<DialogResult> token;

        public ToolWindow()// : base(PopupType.Menu)
        {
            var p = 6;

            panel = new ScrollViewer
            {
                Name = "panel",
                VerticalAlignment = VerticalAlignment.Stretch,
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
            bar.PointerPressed += OnContentMouseDown;
            bar.PointerReleased += OnContentMouseUp;
            bar.PointerEntered += OnContentMouseEntered;
            bar.PointerExited += OnContentMouseExited;
            bar.PointerMoved += OnContentMouseMove;
            //hbox.Margin = new Thickness(padding, 0, padding, padding);

            vbox = new StackPanel
            {
                //Margin = new Thickness(padding, padding, padding, padding),
                Orientation = Orientation.Vertical,
                Name = "tools"
            };
            vbox.Children.Add(panel);
            vbox.Children.Add(bar);
            vbox.KeyDown += OnContentKeyPress;


            Background = GuiEnvironment.Theme["Window"].BaseColor.WithIncreasedLight(0.1D);
            Content = vbox;
            SystemDecorations = SystemDecorations.None;
            Name = "ToolWindow";
            CanResize = true;
            Width = 360;
            Height = 320;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Padding = new Thickness(p, p, p, p);

            timerHide.Interval = 8000;
            timerHide.Elapsed += TimerHideTick;

            timerStart.Interval = 500;
            timerStart.Elapsed += TimerStartTick;
        }

        public bool HeaderVisible
        {
            get => bar.IsVisible;
            set
            {
                if (bar.IsVisible != value)
                {
                    bar.IsVisible = value;
                }
            }
        }

        public ToolShowMode Mode
        {
            get => mode;
            set => mode = value;
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
            get => timerHide.Interval;
            set => timerHide.Interval = value;
        }

        public virtual IControl Target
        {
            get => target;
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
                var size = target.DesiredSize;
                if (size.Width > Width || size.Height > Height)
                    FrameSize = new Size(
                        Math.Max(Width, Math.Min(size.Width, 1024)) + 35,
                        Math.Max(Height, Math.Min(size.Height, 768)) + 70);
            }
        }

        public DialogResult? DResult { get; set; }

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
            get => toolAccept.Text;
            set => toolAccept.Text = value;
        }

        public bool ButtonAcceptEnabled
        {
            get => toolAccept.Sensitive;
            set => toolAccept.Sensitive = value;
        }

        public event EventHandler ButtonAcceptClick
        {
            add => toolAccept.Click += value;
            remove => toolAccept.Click -= value;
        }

        public ToolItem ButtonClose
        {
            get { return toolClose; }
        }

        public string ButtonCloseText
        {
            get => toolClose.Text;
            set => toolClose.Text = value;
        }

        public event EventHandler ButtonCloseClick
        {
            add => toolClose.Click += value;
            remove => toolClose.Click -= value;
        }

        public IControl Sender
        {
            get => senderControl;
            set
            {
                byDeactivate = false;
                senderControl = value;
                Owner = senderControl?.VisualRoot as Window;
            }
        }

        protected void OnSenderClick(object sender, PointerEventArgs e)
        {
            Hide();
        }

        protected void OnContentMouseDown(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(this);
            if (point.Properties.IsLeftButtonPressed)
            {
                movePoint = Desktop.MouseLocation;
                moveButton = e.MouseButton;
                bar.Cursor = new Cursor(StandardCursorType.DragMove);
            }
        }

        protected void OnContentMouseUp(object sender, PointerEventArgs e)
        {
            moveButton = 0;
            moveBounds = Location;
            bar.Cursor = new Cursor(StandardCursorType.Arrow);
        }

        private void OnContentMouseExited(object sender, EventArgs e)
        {
            if (Mode == ToolShowMode.ToolTip)
            {
                Debug.WriteLine($"Handle Mouse Exited!");
                timerHide.Start();
            }
        }

        private void OnContentMouseEntered(object sender, PointerEventArgs e)
        {
            if (Mode == ToolShowMode.ToolTip)
            {
                Debug.WriteLine($"Handle Mouse Entered!");
                timerHide.Stop();
            }
        }

        protected void OnContentMouseMove(object sender, PointerEventArgs e)
        {
            if (moveButton == MouseButton.Left)
            {
                var location = Desktop.MouseLocation;
                var diff = new Point(location.X - movePoint.X, location.Y - movePoint.Y);
                Debug.WriteLine($"Location Diff:{diff} Bound:{moveBounds}");

                if (bar.Cursor == StandardCursorType.Move)
                {
                    Position = new PixelPoint(
                        (int)(moveBounds.X + diff.X),
                        (int)(moveBounds.Y + diff.Y));
                }

            }
        }

        public virtual void Show(IControl sender, PixelPoint location)
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
            var locationPoint = location.ToPoint(1);
            CheckLocation(sender?.PointToScreen(locationPoint) ?? locationPoint);
            base.Show();
            CheckLocation(sender?.PointToScreen(locationPoint) ?? locationPoint);

            //if (Owner != null) Owner.Show();
            if (mode == ToolShowMode.AutoHide || mode == ToolShowMode.ToolTip)
            {
                if (timerHide.Enabled)
                    timerHide.Stop();
                timerHide.Start();
            }

            byDeactivate = false;
        }

        public async Task<DialogResult> ShowAsync(Control sender, PixelPoint location)
        {
            Show(sender, location);
            token = new TaskCompletionSource<DialogResult>();
            return await token.Task;
        }

        public void ShowCancel()
        {
            timerStart.Stop();
            Hide();
        }

        private void CheckLocation(PixelPoint location)
        {
            Rect screen = (Owner?.Screen ?? Desktop.PrimaryScreen).VisibleBounds;
            if (location.Y + Height > screen.Height)
            {
                location = location.WithY(location.Y - ((location.Y + (int)Height) - (int)screen.Height));
                //Left += 10;
            }
            if (location.X + Width > screen.Right)
            {
                location = location.WithX(location.X - ((location.X + (int)Width) - (int)screen.Right));
            }
            Position = moveBounds = location;
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

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
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
                // temp.IsVisible = true;
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
            if (e.KeyModifiers == KeyModifiers.Alt)
                e.Handled = true;
            if (e.Key == Key.Escape)
                Hide();
        }

        private void TimerHideTick(object sender, EventArgs e)
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                Hide();
                timerHide.Stop();
            });
        }

        private void TimerStartTick(object sender, EventArgs e)
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                Show(tempSender, tempLocation);
                timerStart.Stop();
            });
        }

        protected virtual void OnCloseClick(object sender, EventArgs e)
        {
            DResult = DialogResult.Cancel;
            if (CloseOnClose)
                Hide();
        }

        protected virtual void OnAcceptClick(object sender, EventArgs e)
        {
            DResult = DialogResult.OK;
            if (CloseOnAccept)
                Hide();
        }

        protected void Dispose(bool disposing)
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
        }

        public new string Title
        {
            get => base.Title;
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

            return window;
        }

        public bool CloseOnAccept
        {
            get => closeOnAccept;
            set => closeOnAccept = value;
        }

        public bool CloseOnClose
        {
            get => closeOnClose;
            set => closeOnClose = value;
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
