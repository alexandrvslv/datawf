using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace DataWF.Gui
{
    public class CellEditorPath : CellEditorText
    {
        private FilePickerOpenOptions options;

        public CellEditorPath() : base()
        {
            DropDownWindow = false;
            HandleText = false;
            options = new FilePickerOpenOptions { AllowMultiple = false };

        }

        private async void OnDropDownClick(object sender, EventArgs e)
        {
            if (Editor is IControl control
                && control.VisualRoot is Window window)
            {
                var file = await window.StorageProvider.OpenFilePickerAsync(options);
                if (file.Any())
                {
                    Value = file.First().Name;
                }
            }
        }

        public override void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            base.InitializeEditor(editor, value, dataSource);

            Editor.DropDownVisible = true;
            Editor.DropDownClick += OnDropDownClick;


            options.Title = value?.ToString();
        }

        public override void FreeEditor()
        {
            Editor.DropDownClick -= OnDropDownClick;
            base.FreeEditor();
        }
    }
}

