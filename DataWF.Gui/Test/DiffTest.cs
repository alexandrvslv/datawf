using DataWF.Gui;
using DataWF.Common;
using System;
using Xwt;

namespace DataWF.TestGui
{
    public class DiffTest : VPanel
    {
        private Label textALabel = new Label();
        private Label textBLabel = new Label();
        private TextEntry textA = new TextEntry();
        private TextEntry textB = new TextEntry();
        private Button testChar = new Button();
        private Button testWord = new Button();
        private LayoutList result = new LayoutList();
        private GroupBoxItem itemParam = new GroupBoxItem();
        private GroupBoxItem itemResult = new GroupBoxItem();
        private GroupBox map = new GroupBox();
        private Table panel = new Table();
        public DiffTest()
        {
            textALabel.Text = "Param1:";
            textALabel.TextAlignment = Alignment.Center;

            textBLabel.Text = "Param2:";
            textBLabel.TextAlignment = Alignment.Center;

            textA.Text = "Sequential Read: Up to 550 MB/s ";

            textB.Text = "Sequential Write: Up to 510 MB/s ";

            testChar.Label = "Test Char";
            testChar.Clicked += TestCharOnClick;

            testWord.Label = "Test Word";
            testWord.Clicked += TestWordOnClick;

            result.GenerateToString = false;
            result.SelectionChanged += result_SelectionChanged;

            panel.Add(textALabel, 0, 0);
            panel.Add(textA, 1, 0, colspan: 2, hexpand: true);
            panel.Add(textBLabel, 0, 1);
            panel.Add(textB, 1, 1, colspan: 2, hexpand: true);
            panel.Add(testChar, 0, 2);
            panel.Add(testWord, 1, 2, hexpand: false);
            //test.RowSpan = 

            itemParam.Widget = panel;
            itemParam.FillWidth = true;
            itemParam.Text = "Params";

            itemResult.Widget = result;
            itemResult.Row = 1;
            itemResult.FillHeight = true;
            itemResult.Text = "Result";

            map.Add(itemParam);
            map.Add(itemResult);

            PackStart(map, true, true);
            Text = "Diff Test";
        }

        private void result_SelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (result.SelectedItem != null)
            {
                var rez = (DiffResult)result.SelectedItem;
                var box = rez.Type == DiffType.Deleted ? textA : textB;
                box.SetFocus();
                box.SelectionStart = rez.Index;
                box.SelectionLength = rez.Length;
            }
        }

        private void TestCharOnClick(object sender, EventArgs e)
        {
            result.ListSource = DiffResult.Diff(textA.Text, textB.Text, false);
        }

        private void TestWordOnClick(object sender, EventArgs e)
        {
            result.ListSource = DiffResult.Diff(textA.Text.Split(' '), textB.Text.Split(' '));
        }
    }
}

