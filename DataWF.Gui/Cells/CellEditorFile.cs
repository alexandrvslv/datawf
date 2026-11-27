using DataWF;
using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Linq;

namespace DataWF.Gui
{
    public class CellEditorFile : CellEditorText
    {
        private string propertyFileName;
        private IInvoker invoker;

        public CellEditorFile()
            : base()
        {
            HandleText = false;
            DropDownWindow = false;
            DropDownVisible = true;
            DropDownExVisible = true;
            Format = "size";
        }

        public string PropertyFileName
        {
            get => propertyFileName;
            set => propertyFileName = value;
        }

        public override void InitDropDown()
        {
        }

        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            base.InitializeEditor(editor, value, dataSource);

            if (!ReadOnly)
            {
                editor.DropDownClick += OnDropDownClick;
            }
            editor.DropDownExClick += OnDropDownExClick;
            invoker = null;

            if (dataSource != null)
            {
                invoker = GetFileNameInvoker(dataSource);
            }

            if (invoker != null)
            {
                fdOpen.InitialFileName = invoker.GetValue(dataSource) as string;
            }
        }

        protected virtual IInvoker GetFileNameInvoker(object dataSource)
        {
            return InvokeProvider.Initialize(dataSource.GetType(), propertyFileName);
        }

        private async void OnDropDownClick(object sender, EventArgs e)
        {
            var file = await fdOpen.ShowAsync((Window)Editor.GetVisualRoot());
            if (file?.Any() ?? false)
            {
                var fileName = file.First();
                if (File.Exists(fileName))
                {
                    using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var buf = new byte[stream.Length];
                        int count = buf.Length;
                        int sum = 0;
                        while ((count = stream.Read(buf, sum, buf.Length - sum)) > 0)
                            sum += count;  // sum is a buffer offset for next reading

                        Value = buf;
                    }
                }
                string name = Path.GetFileName(fileName);
                if (invoker != null)
                    invoker.SetValue(EditItem, name);
                EntryText = name;
            }
        }

        private async void OnDropDownExClick(object sender, EventArgs e)
        {
            if (invoker != null)
            {
                fdSave.InitialFileName = invoker.GetValue(base.EditItem) as string;
            }
            if (Value != null)
            {
                var file = await fdSave.ShowAsync((Window)Editor.GetVisualRoot());
                if (file != null)
                {
                    File.WriteAllBytes(file, (byte[])Value);
                }
            }
        }

        public override void FreeEditor()
        {
            if (Editor != null)
            {
                Editor.DropDownClick -= OnDropDownClick;
                Editor.DropDownExClick -= OnDropDownExClick;
            }
            base.FreeEditor();
        }

        public override void Dispose()
        {
            base.Dispose();
            fdOpen = null;
            fdSave = null;
        }
    }
}

