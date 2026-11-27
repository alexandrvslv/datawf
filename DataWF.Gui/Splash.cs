using System;
using System.ComponentModel;
using System.IO;
using DataWF.Gui;
using DataWF;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Threading;

namespace DataWF.Gui
{
    public class Splash : Window
    {
        public Action LoadAction;

        private Label labelHeader;
        private Label labelEvent;
        private bool exeception;

        public Splash()
        {
            labelHeader = new Label
            {
                Name = "labelHeader",
                FontStyle = FontStyle.Oblique,
                FontWeight = FontWeight.Bold,
                Content = "Data/Document Workflow",
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            labelEvent = new Label
            {
                Name = "labelEvent",
                Content = "Load config",
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                //Wrap = WrapMode.Word
            };

            var vbox = new StackPanel
            {
                Orientation = Orientation.Vertical
            };
            vbox.Children.Add(labelHeader);
            vbox.Children.Add(labelEvent);

            Content = vbox;
            Name = "Splash";
            Title = "Splash";
            ShowInTaskbar = false;
            SystemDecorations = SystemDecorations.None;
            //Icon = Image.FromResource(GetType(), "datawf.png");
            Width = 340;
            Height = 220;

            Helper.Logs.CollectionChanged += OnLogListChanged;

            Task.Run(() => LoadConfiguration()).ConfigureAwait(false);
        }

        public virtual void LoadConfiguration()
        {
            Helper.SetDirectory();
            try
            {
                Locale.Load();
                LoadAction?.Invoke();
                GuiEnvironment.Load();
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
                DialogResult = Command.Stop;
            }
            finally
            {
                Helper.SetDirectory();
                OnInitialized();
            }
        }

        public string HeaderText
        {
            get => (string)labelHeader.Content;
            set => labelHeader.Content = value;
        }

        public DialogResult? DialogResult { get; private set; }

        private void OnInitialized()
        {
            if (GuiService.InvokeRequired)
            {
                _ = Dispatcher.UIThread.InvokeAsync(() => OnInitialized());
            }
            else
            {
                Helper.Logs.CollectionChanged -= OnLogListChanged;
                labelEvent.Content = "Initialization Complete!";
                DialogResult ??= Gui.DialogResult.OK;
                if (exeception)
                {
                    MessageDialog.ShowError(this, "Some Exception during startup. For more deatail see startup.log");
                }
                Close();
            }
        }

        private void OnLogListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (GuiService.InvokeRequired)
            {
                _ = Dispatcher.UIThread.InvokeAsync(() => OnLogListChanged(sender, e));
            }
            else
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    var log = e.NewItems?.Cast<StateInfo>().FirstOrDefault();
                    if (log.Type != StatusType.Warning)
                    {
                        labelEvent.Content = $"{log.Module} {log.Message}\n{log.Description}";
                    }
                    if (log.Type == StatusType.Error)
                    {
                        exeception = true;
                    }
                }
            }
        }

    }
}
