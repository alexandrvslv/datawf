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
        protected bool dropDownAutoHide;
        protected Widget content;
        public bool ListAutoSort = true;
        protected string filter = string.Empty;

        public CellEditorText()
        {
        }

        public bool Masked { get; set; }

        public bool MultiLine { get; set; }

        public bool Filtering { get; set; }

        public bool DropDownWindow
        {
            get { return dropDownWindow; }
            set { DropDownVisible = dropDownWindow = value; }
        }

        public bool DropDownVisible { get; set; } = true;

        public bool DropDownExVisible { get; set; }

        public string Header { get; set; }

        public string Format { get; set; }

        public bool ReadOnly { get; set; }

        public ToolWindow DropDown
        {
            get { return Editor?.DropDown; }
            set
            {
                if (Editor != null)
                    Editor.DropDown = value;
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

        public virtual string EditorText
        {
            get { return TextWidget?.Text; }
            set
            {
                bool flag = HandleText;
                HandleText = false;
                if (TextWidget != null)
                    TextWidget.Text = value;
                if (DropDown != null && DropDown.Target is RichTextView)
                {
                    ((RichTextView)DropDown.Target).LoadText(value ?? string.Empty, Xwt.Formats.TextFormat.Plain);
                    ((RichTextView)DropDown.Target).ReadOnly = ReadOnly;
                }
                HandleText = flag;
            }
        }

        public virtual Type DataType { get; set; }

        public LayoutEditor Editor { get; protected set; }

        public object EditItem { get; set; }

        public virtual object Value
        {
            get { return Editor?.Value; }
            set
            {
                if (Editor != null)
                {
                    Editor.Value = ParseValue(value);
                    EditorText = FormatValue(value) as string;
                }
            }
        }

        public TextEntry TextWidget
        {
            get { return Editor.Widget as TextEntry; }
        }

        protected bool HandleText { get; set; } = true;

        protected virtual void SetFilter(string filter)
        { }

        protected virtual void OnTextChanged(object sender, EventArgs e)
        {
            if (HandleText && Editor != null)
            {
                var text = sender == Editor.Widget ? EditorText : sender is RichTextView ? ((RichTextView)sender).PlainText.TrimEnd() : string.Empty;
                HandleText = false;
                if (Filtering && !text.Equals(filter, StringComparison.OrdinalIgnoreCase))
                {
                    SetFilter(text.Replace("\r\n", "").Replace("\r", ""));
                }
                else
                {

                    try
                    {
                        Value = text;
                    }
                    catch (Exception ex)
                    {
                        if (Editor != null)
                        {
                            GuiService.ToolTip.LableText = "Input error";
                            GuiService.ToolTip.ContentText = ex.Message + "\n" + ex.StackTrace;
                            GuiService.ToolTip.Show(Editor, new Point(0, Editor.Size.Height));
                        }
                    }
                }
                HandleText = true;
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
            var tool = Editor.GetCached<ToolWindow>();
            tool.HeaderVisible = headerVisible;
            tool.Label.Text = string.IsNullOrEmpty(Header) ? Editor?.Cell?.Text : Header;
            tool.ButtonAcceptClick += OnDropDownAccept;
            DropDown = tool;
            DropDown.Target = InitDropDownContent();
        }

        public virtual Widget InitDropDownContent()
        {
            var reachText = Editor.GetCached<RichTextView>();
            return reachText;
        }

        public virtual Widget InitEditorContent()
        {
            if (Masked && Format != null)
            {
                var box = Editor.GetCached<TextEntry>();
                box.MultiLine = MultiLine;
                box.KeyPressed += OnTextKeyPressed;
                box.KeyReleased += OnTextKeyReleased;
                box.ShowFrame = false;
                box.ReadOnly = ReadOnly;
                var style = Editor?.Cell?.Style;
                if (style != null)
                {
                    box.Font = style.Font;
                    box.BackgroundColor = style.BackBrush.ColorSelect;
                    box.TextColor = style.FontBrush.ColorSelect;
                }
                //box.Mask = format;
                //box.ValidatingType = type;
                if (!ReadOnly && HandleText)
                    box.Changed += OnTextChanged;
                return box;
            }
            else
            {
                var box = Editor.GetCached<TextEntry>();
                box.MultiLine = MultiLine;
                box.KeyPressed += OnTextKeyPressed;
                box.KeyReleased += OnTextKeyReleased;
                box.ShowFrame = false;
                box.ReadOnly = ReadOnly;
                var style = Editor?.Cell?.Style;
                if (style != null)
                {
                    box.Font = style.Font;
                    box.BackgroundColor = style.BackBrush.ColorSelect;
                    box.TextColor = style.FontBrush.ColorSelect;
                }
                if (!ReadOnly && HandleText)
                    box.Changed += OnTextChanged;
                return box;
            }
        }

        public virtual void InitializeEditor(LayoutEditor editor, object value, object dataSource)
        {
            editor.Initialize = true;
            try
            {
                InitDefault(editor, value, dataSource);
                editor.Widget = InitEditorContent();
                if (DropDownWindow)
                {
                    InitDropDown();
                }
                Value = value;
                if (editor.Widget is TextEntry textEntry)
                {
                    textEntry.SelectionStart = 0;
                    textEntry.SelectionLength = textEntry.Text.Length;
                }
                editor.IsValueChanged = false;
            }
            finally
            {
                editor.Initialize = false;
            }
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

        protected virtual void OnTextKeyReleased(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (!DropDown.Visible && !e.Handled)
                {
                    Editor.ShowDropDown(ToolShowMode.Default);
                }
            }
            else if (e.Key == Key.Escape && DropDown?.Target != null)
            {
                if (DropDown.Visible)
                    DropDown.Hide();
            }
        }

        protected virtual void OnTextKeyPressed(object sender, KeyEventArgs e)
        {
            if (DataType.IsPrimitive || DataType == typeof(decimal))
            {
                if (!char.IsControl((char)e.NativeKeyCode) && !char.IsDigit((char)e.NativeKeyCode) && e.Key != Key.Minus)
                {
                    e.Handled = true;
                }

                // only allow one decimal point
                bool dpoint = EditorText.IndexOf('.') != -1 || EditorText.IndexOf(',') != -1;
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
            if (Editor != null)
            {
                if (TextWidget != null)
                {
                    TextWidget.Changed -= OnTextChanged;
                    TextWidget.KeyPressed -= OnTextKeyPressed;
                    TextWidget.KeyReleased -= OnTextKeyReleased;
                }
                Editor.Image = null;
                Editor.DropDown = null;
                Editor.CurrentEditor = null;
                Editor.ClearValue();
            }
            Editor = null;
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
