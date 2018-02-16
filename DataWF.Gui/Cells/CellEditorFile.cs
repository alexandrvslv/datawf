using System;
using System.IO;
using DataWF.Common;
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
            HandleTextChanged = false;
            DropDownWindow = false;
            DropDownVisible = true;
            DropDownExVisible = true;
            fdOpen = new OpenFileDialog();
            fdSave = new SaveFileDialog();
        }

        public string PropertyFileName
        {
            get { return propertyFileName; }
            set { propertyFileName = value; }
        }

        public override object FormatValue(object value, object dataSource, Type valueType)
        {
            if (value is byte[])
            {
                return Helper.LengthFormat(((byte[])value).LongLength);
            }
            return base.FormatValue(value, dataSource, valueType);
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
                fdOpen.InitialFileName = invoker.Get(dataSource) as string;
            }
        }

        protected virtual IInvoker GetFileNameInvoker(object dataSource)
        {
            return EmitInvoker.Initialize(TypeHelper.GetMemberInfo(dataSource.GetType(), propertyFileName, false), true);
        }

        private void OnDropDownClick(object sender, EventArgs e)
        {
            if (fdOpen.Run(editor.ParentWindow))
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

                        editor.Value = buf;
                    }
                }
                string name = Path.GetFileName(fdOpen.FileName);
                if (invoker != null)
                    invoker.Set(EditItem, name);
                EditorText = name;
            }
        }

        private void OnDropDownExClick(object sender, EventArgs e)
        {
            if (invoker != null)
            {
                fdSave.InitialFileName = invoker.Get(base.EditItem) as string;
            }
            if (editor.Value != null && fdSave.Run(editor.ParentWindow))
            {
                File.WriteAllBytes(fdSave.FileName, (byte[])editor.Value);
            }
        }

        public override void FreeEditor()
        {
            if (editor != null)
            {
                editor.DropDownClick -= OnDropDownClick;
                editor.DropDownExClick -= OnDropDownExClick;
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

