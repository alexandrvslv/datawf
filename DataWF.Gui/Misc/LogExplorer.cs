using DataWF;
using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using System.Linq;

namespace DataWF.Gui
{
    public class LogExplorer : VPanel, IDockContent
    {
        private LogList list;
        private Toolsbar tools;
        private ToolItem toolLoad;
        private ToolItem toolSave;

        public LogExplorer()
        {
            list = new LogList()
            {
                AllowEditColumn = true,
                EditMode = EditModes.ByClick,
                GenerateToString = false,
                Grouping = false,
                Name = "list",
                ReadOnly = true,
                ListSource = Helper.Logs
            };

            toolLoad = new ToolItem(OnToolLoadClick) { Name = "Load" };
            toolSave = new ToolItem(OnToolSaveClick) { Name = "Save" };
            tools = new Toolsbar(toolLoad, toolSave);
            Name = "LogEditor";

            Children.Add(tools);
            Children.Add(list, true, true);

            Localize();
            //System.Drawing.SystemIcons.
        }

        public void Add(string source, string message, string description, StatusType type)
        {
            Helper.Log(source, message, description, type);
        }

        public LogList List
        {
            get { return list; }
        }

        #region IDocContent implementation
        public DockType DockType
        {
            get { return DockType.Bottom; }
        }

        public bool HideOnClose
        {
            get { return true; }
        }

        #endregion

        #region ILocalizable implementation
        public override void Localize()
        {
            GuiService.Localize(toolLoad, "LogExplorer", "Load", GlyphType.FolderOpen);
            GuiService.Localize(toolSave, "LogExplorer", "Save", GlyphType.SaveAlias);
            GuiService.Localize(this, "LogExplorer", "Logs", GlyphType.InfoCircle);

            list.Localize();
        }
        #endregion

        private async void OnToolLoadClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            var files = await dialog.ShowAsync((Window)VisualRoot);
            if (files?.Any() ?? false)
            {
                StateInfoList newList = null;
                using (var fileStream = File.Open(dialog.FileName, FileMode.Open, FileAccess.Read))
                {
                    Stream stream;
                    if (Helper.IsGZip(fileStream))
                        stream = Helper.GetUnGZipStrem(fileStream);
                    else
                        stream = fileStream;
                    newList = SerializeProvider.Deserialize(stream) as StateInfoList;
                    stream.Close();
                }
                var form = new ToolWindow();
                form.Label.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                form.Target = new LogList() { ListSource = newList };
                form.Mode = ToolShowMode.Dialog;
                form.Show(this, new Point(0, 0));
            }
        }

        private async void OnToolSaveClick(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            var file = await dialog.ShowAsync((Window)VisualRoot);
            if (file != null)
            {
                using (Stream f = File.Open(file, FileMode.Create, FileAccess.Write))
                    SerializeProvider.Serialize(Helper.Logs, f);
            }
        }


        public bool Closing()
        {
            return true;
        }

        public void Activating()
        {
        }
    }
}
