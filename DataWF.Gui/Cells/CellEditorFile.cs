using DataWF.Common;
using System;
using System.IO;
using Xwt;

namespace DataWF.Gui
{
    public class CellEditorFile : CellEditorText
    {
        private string propertyFileName;
        private OpenFileDialog fdOpen;
        private SaveFileDialog fdSave;
        private IInvoker invoker;

        public CellEditorFile()
            : base()
        {
            HandleText = false;
            DropDownWindow = false;
            DropDownVisible = true;
            DropDownExVisible = true;
            fdOpen = new OpenFileDialog();
            fdSave = new SaveFileDialog();
            Format = "size";
        }

        public string PropertyFileName
        {
            get { return propertyFileName; }
            set { propertyFileName = value; }
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
            return EmitInvoker.Initialize(dataSource.GetType(), propertyFileName);
        }

        private void OnDropDownClick(object sender, EventArgs e)
        {
            if (fdOpen.Run(Editor.ParentWindow))
            {
                if (File.Exists(fdOpen.FileName))
                {
                    using (var stream = File.Open(fdOpen.FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var buf = new byte[stream.Length];
                        int count = buf.Length;
                        int sum = 0;
                        while ((count = stream.Read(buf, sum, buf.Length - sum)) > 0)
                            sum += count;  // sum is a buffer offset for next reading

                        Value = buf;
                    }
                }
                string name = Path.GetFileName(fdOpen.FileName);
                if (invoker != null)
                    invoker.SetValue(EditItem, name);
                EntryText = name;
            }
        }

        private void OnDropDownExClick(object sender, EventArgs e)
        {
            if (invoker != null)
            {
                fdSave.InitialFileName = invoker.GetValue(base.EditItem) as string;
            }
            if (Value != null && fdSave.Run(Editor.ParentWindow))
            {
                File.WriteAllBytes(fdSave.FileName, (byte[])Value);
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
            fdOpen.Dispose();
            fdSave.Dispose();
        }
    }
}

