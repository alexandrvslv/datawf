using System;
using Xwt;
using Xwt.Drawing;
using DataWF.Common;
using System.Collections;
using System.ComponentModel;

namespace DataWF.Gui
{
    //public class CellEditorButton
    public class CellEditorText : ILayoutCellEditor, IDisposable
    {
        static CellEditorText()
        {
        }

        protected bool headerVisible = true;
        protected bool dropDownWindow = true;
        protected bool dropDownVisible = true;
        protected bool dropDownExVisible;
        protected bool dropDownAutoHide;
        protected bool handleText = true;
        //protected bool dropDownByKey = true;
        protected LayoutEditor editor;
        protected Widget content;
        public string ListProperty = "ToString";
        public bool ListAutoSort = true;
        protected string filter = string.Empty;

        public CellEditorText()
        {
            EditType = typeof(TextEntry);
        }

        public bool Masked { get; set; }

        public bool MultiLine { get; set; }

        public bool Filtering { get; set; }

        public bool DropDownWindow
        {
            get { return dropDownWindow; }
            set { DropDownVisible = dropDownWindow = value; }
        }

        public bool DropDownVisible
        {
            get { return dropDownVisible; }
            set { dropDownVisible = value; }
        }

        public bool DropDownExVisible
        {
            get { return dropDownExVisible; }
            set { dropDownExVisible = value; }
        }

        public string Header { get; set; }

        public string Format { get; set; }

        public bool ReadOnly { get; set; }

        public bool HandleTextChanged
        {
            get { return handleText; }
            set { handleText = value; }
        }

        public ToolWindow DropDown
        {
            get { return editor?.DropDown; }
            set
            {
                if (editor != null)
                    editor.DropDown = value;
            }
        }

        public bool HeaderVisible
        {
            get { return headerVisible; }
            set
            {
                if (headerVisible == value)
                    return;
                headerVisible = value;
                if (DropDown != null)
                    DropDown.HeaderVisible = headerVisible;
            }
        }

        public string EditorText
        {
            get
            {
                return (editor?.Widget as TextEntry)?.Text;
            }
            set
            {
                bool flag = handleText;
                handleText = false;
                if (editor?.Widget is TextEntry)
                    ((TextEntry)editor.Widget).Text = value;
                if (DropDown != null && DropDown.Target is RichTextView)
                    ((RichTextView)DropDown.Target).LoadText(value ?? string.Empty, Xwt.Formats.TextFormat.Plain);
                handleText = flag;
            }
        }

        public Type EditType { get; set; }

        public virtual Type DataType { get; set; }

        public LayoutEditor Editor
        {
            get { return editor; }
            protected set { editor = value; }
        }

        public object EditItem { get; set; }

        public virtual object Value
        {
            get { return editor?.Value; }
            set
            {
                if (editor != null)
                    editor.Value = ParseValue(value, EditItem, DataType);
                if (Editor != null)
                {
                    EditorText = FormatValue(value) as string;
                }
            }
        }

        public TextEntry TextControl
        {
            get { return editor.Widget as TextEntry; }
        }

        protected virtual void SetFilter(string filter)
        { }

        protected virtual void OnControlValueChanged(object sender, EventArgs e)
        {
            if (handleText)
            {
                var text = sender is TextEntry ? ((TextEntry)sender).Text : sender is RichTextView ? ((RichTextView)sender).PlainText : string.Empty;
                handleText = false;
                if (Filtering && !text.Equals(filter, StringComparison.OrdinalIgnoreCase))
                {
                    SetFilter(text.Replace("\r\n", "").Replace("\r", ""));
                }

                try
                {
                    if (sender != editor.Widget && EditorText != text)
                        ((TextEntry)editor.Widget).Text = text;
                    editor.Value = ParseValue(EditorText);
                }
                catch (Exception ex)
                {
                    if (editor != null)
                    {
                        GuiService.ToolTip.LableText = "Input error";
                        GuiService.ToolTip.ContentText = ex.Message + "\n" + ex.StackTrace;
                        GuiService.ToolTip.Show(editor, new Point(0, editor.Size.Height));
                    }
                }
                handleText = true;
            }
        }

        public virtual object ParseValue(object value)
        {
            return ParseValue(value, EditItem, DataType);
        }

        public virtual object FormatValue(object value)
        {
            return FormatValue(value, EditItem, DataType);
        }

        public virtual object ParseValue(object value, object dataSource, Type valueType)
        {
            if (value == null)
            {
                return null;
            }
            if (value.GetType() == valueType)
            {
                return value;
            }
            if (value is string)
            {
                return Helper.TextParse((string)value, valueType, Format);
            }
            return value;
        }

        public virtual object FormatValue(object value, object dataSource, Type valueType)
        {
            if (value is Image)
                return value;
            return Helper.TextDisplayFormat(value, Format);
        }

        protected void InitDefault(LayoutEditor editor, object value, object dataSource)
        {
            filter = string.Empty;
            if (editor.CurrentEditor != null)
            {
                editor.CurrentEditor.FreeEditor();
            }

            editor.CurrentEditor = this;
            editor.DropDownVisible = DropDownVisible;
            editor.DropDownExVisible = DropDownExVisible;
            editor.DropDownAutoHide = dropDownAutoHide;

            Editor = editor;
            EditItem = dataSource;
        }

        public virtual void InitDropDown()
        {
            var tool = editor.GetCacheControl<ToolWindow>();
            tool.HeaderVisible = headerVisible;
            tool.Label.Text = string.IsNullOrEmpty(Header) ? editor?.Cell?.Text : Header;
            tool.ButtonAcceptClick += OnDropDownAccept;
            DropDown = tool;
            DropDown.Target = InitDropDownContent();
        }

        public virtual Widget InitDropDownContent()
        {
            var reachText = editor.GetCacheControl<RichTextView>();
            reachText.ReadOnly = ReadOnly;
            return reachText;
        }

        public virtual Widget InitEditorContent()
        {
            if (Masked && Format != null)
            {
                var box = editor.GetCacheControl<TextEntry>();
                box.MultiLine = MultiLine;
                box.KeyPressed += TextBoxKeyPress;
                box.KeyReleased += TextBoxKeyUp;
                box.ShowFrame = false;
                box.ReadOnly = ReadOnly;
                //box.Mask = format;
                //box.ValidatingType = type;
                if (!ReadOnly && handleText)
                    box.Changed += OnControlValueChanged;
                return box;
            }
            else
            {
                var box = editor.GetCacheControl<TextEntry>();
                box.MultiLine = MultiLine;
                box.KeyPressed += TextBoxKeyPress;
                box.KeyReleased += TextBoxKeyUp;
                box.ShowFrame = false;
                box.ReadOnly = ReadOnly;
                if (!ReadOnly && handleText)
                    box.Changed += OnControlValueChanged;
                return box;
            }
        }

        public virtual void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            editor.Initialize = true;
            InitDefault(editor, value, dataSource);
            editor.Widget = InitEditorContent();
            if (DropDownWindow)
            {
                InitDropDown();
            }
            Value = value;
            editor.IsValueChanged = false;
            editor.Initialize = false;
        }

        protected void OnDropDownAccept(object sender, EventArgs e)
        {
            Value = GetDropDownValue();
        }

        protected virtual object GetDropDownValue()
        {
            if (DropDown.Target is RichTextView)
            {
                return ((RichTextView)DropDown.Target).PlainText;
            }
            return null;
        }

        protected virtual void TextBoxKeyUp(object sender, KeyEventArgs e)
        {
            var box = sender as TextEntry;
            if (!box.MultiLine && DropDown?.Target != null)
            {
                if (e.Key == Key.Down)
                {
                    if (!DropDown.Visible)
                    {
                        editor.ShowDropDown(ToolShowMode.Default);
                    }
                }
                else if (e.Key == Key.Escape)
                {
                    if (DropDown.Visible)
                        DropDown.Hide();
                }
            }
        }

        protected virtual void TextBoxKeyPress(object sender, KeyEventArgs e)
        {
            if (DataType.IsPrimitive || DataType == typeof(decimal))
            {
                if (!char.IsControl((char)e.NativeKeyCode) && !char.IsDigit((char)e.NativeKeyCode) && e.Key != Key.Minus)
                {
                    e.Handled = true;
                }

                // only allow one decimal point
                bool dpoint = (sender as TextEntry).Text.IndexOf('.') != -1 || (sender as TextEntry).Text.IndexOf(',') != -1;
                if (!dpoint && (e.Key == Key.Comma || e.Key == Key.Period))
                {
                    e.Handled = false;
                }
            }
        }

        public virtual void FreeEditor()
        {
            if (DropDown != null)
            {
                DropDown.Hide();
                DropDown.ButtonAcceptClick -= OnDropDownAccept;
            }
            if (editor != null)
            {
                if (editor.Widget is TextEntry)
                {
                    ((TextEntry)editor.Widget).Changed -= OnControlValueChanged;
                    ((TextEntry)editor.Widget).KeyPressed -= TextBoxKeyPress;
                    ((TextEntry)editor.Widget).KeyReleased -= TextBoxKeyUp;
                }
                editor.Image = null;
                editor.DropDown = null;
                editor.CurrentEditor = null;
            }
            editor.ClearValue();
            editor = null;
            EditItem = null;
        }

        public virtual void Dispose()
        {
        }

        public virtual void DrawCell(LayoutListDrawArgs e)
        {
            var textBound = e.LayoutList.GetCellTextBound(e);
            e.Context.DrawCell(e.Style, e.Formated, e.Bound, textBound, e.State);
        }
    }

    public class ListSourceAttribute : Attribute
    {
        public IList List;
        public ListSourceAttribute(IList list)
        {
            List = list;
        }
    }
}
