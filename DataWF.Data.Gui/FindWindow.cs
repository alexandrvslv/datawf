using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using DataWF.Gui;
using DataWF.Common;
using Mono.TextEditor;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Data.Gui
{
    public class FindParam
    {
        public string Filter { get; set; }

        public bool CaseSensitive { get; set; }

        public bool WholeWord { get; set; }

        [Browsable(false)]
        public int LastFind;
    }

    public class FindWindow : ToolWindow
    {
        private FindParam prm = new FindParam();

        private List<TextSegmentMarker> list = new List<TextSegmentMarker>();
        private TextEditor editor;


        public FindWindow()
            : base()
        {
            this.Height = 100;

            ButtonAcceptText = "Next";
            Label.Text = "Find in text";

            LayoutList findParam = new LayoutList();
            findParam.CellValueChanged += FindParamCellValueChanged;
            findParam.Mode = LayoutListMode.Fields;
            findParam.EditMode = EditModes.ByClick;
            findParam.FieldSource = prm;

            this.Target = findParam;
            this.Mode = ToolShowMode.Modal;
        }

        private CancelEventArgs arg = new CancelEventArgs();

        private void ClearMark()
        {
            arg.Cancel = true;
            foreach (var item in list)
                editor.TextArea.Document.RemoveMarker(item);
            list.Clear();
        }

        private void Find(object sender, CancelEventArgs arg)
        {
            if (prm.Filter != null && prm.Filter.Length > 1 && prm.Filter.Trim().Length > 0)
            {
                int flen = prm.Filter.Length;
                int len = editor.TextArea.Document.LineCount;
                for (int i = 0; i < len; i++)
                {
                    var segment = editor.TextArea.Document.GetLine(i);
                    var ch = editor.TextArea.Document.GetText(segment);
                    int find = 0;
                    while (find < segment.Length)
                    {
                        if (arg.Cancel)
                            return;
                        if (!prm.CaseSensitive)
                            find = ch.IndexOf(prm.Filter, find, StringComparison.OrdinalIgnoreCase);
                        else
                            find = ch.IndexOf(prm.Filter, find, StringComparison.Ordinal);

                        if (find >= 0)
                        {
                            var marker = new TextSegmentMarker(segment.Offset + find, flen);
                            editor.TextArea.Document.AddMarker(marker);
                            list.Add(marker);
                            find = find + flen;
                        }
                        else
                            break;
                    }
                }
            }
        }

        private void FindComplete(IAsyncResult result)
        {
            Application.Invoke(() =>
            {
                toolLabel.Text = "found (" + list.Count + ")";
                editor.QueueDraw();
            });
        }

        private void FindParamCellValueChanged(object sender, LayoutValueChangedEventArgs e)
        {
            ClearMark();
            arg.Cancel = false;
            var handler = new EventHandler<CancelEventArgs>(Find);
            //handler.BeginInvoke(sender, arg, new AsyncCallback(FindComplete), editor);
            handler.Invoke(sender, arg);
            FindComplete(null);
        }


        public TextEditor Editor
        {
            get { return editor; }
            set
            {
                if (editor == value)
                    return;
                if (editor != null)
                    editor.TextArea.KeyPressed -= QueryTextKeyDown;
                editor = value;
                if (editor != null)
                    editor.TextArea.KeyPressed += QueryTextKeyDown;
            }
        }

        private void QueryTextKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.F)
            {
                //if (this.Parent != editor)
                //    editor.AddChild(this);//TextArea.
                this.Visible = true;
            }
            if ((e.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Q)
            {
                //this.editor.Document.GetLineIndent(this.editor.TextArea, 0, this.editor.Document.LineCount - 1);
            }
        }

        protected override void OnAcceptClick(object sender, EventArgs e)
        {
            if (list.Count > 0)
            {
                prm.LastFind = prm.LastFind >= list.Count ? 0 : prm.LastFind;
                var marker = list[prm.LastFind];
                var start = editor.TextArea.Document.OffsetToLocation(marker.Offset);
                var end = editor.TextArea.Document.OffsetToLocation(marker.Offset + marker.Length);
                editor.SetSelection(start, end);
                editor.ScrollTo(end.Line, end.Column);

                prm.LastFind++;
                base.toolLabel.Text = "Find (" + prm.LastFind + "/" + list.Count + ")";
                if (prm.LastFind >= list.Count)
                    prm.LastFind = 0;
            }
        }

#if GTK
        public override void Show(System.Windows.Forms.Control c, Point location)
        {
            prm.LastFind = 0;
            base.Show(c, location);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible)
                FindParamCellValueChanged(null, null);
            else
            {
                ClearMark();
                editor.Refresh();
            }
            base.OnVisibleChanged(e);
        }
#endif
    }

}
