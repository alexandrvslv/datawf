using DataWF.Gui;
using DataWF;
using System;
using Avalonia;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;

namespace DataWF.TestGui
{
    public class DiffTest : VPanel
    {
        private Label textALabel;
        private Label textBLabel;
        private TextBox textA;
        private TextBox textB;
        private Button testChar;
        private Button testWord;
        private LayoutList result;
        private GroupBoxItem itemParam;
        private GroupBoxItem itemResult;
        private GroupBox map;
        private Grid panel;
        private DIffMode diffMode;

        public DiffTest()
        {
            textALabel = new Label() { Content = "Param1:", HorizontalContentAlignment = HorizontalAlignment.Center };
            textBLabel = new Label() { Content = "Param2:", HorizontalContentAlignment = HorizontalAlignment.Center };
            textA = new TextBox() { Text = "Sequential Read: Up to 550 MB/s " };
            textB = new TextBox() { Text = "Sequential Write: Up to 510 MB/s " };

            testChar = new Button() { Content = "Test Char" };
            testChar.Click += TestCharOnClick;

            testWord = new Button() { Content = "Test Word" };
            testWord.Click += TestWordOnClick;

            result = new LayoutList() { GenerateToString = false };
            result.SelectionChanged += result_SelectionChanged;

            panel = new Grid
            {
                RowDefinitions = new RowDefinitions {
                    new RowDefinition(),
                    new RowDefinition(),
                    new RowDefinition()
                },
                ColumnDefinitions = new ColumnDefinitions {
                    new ColumnDefinition(),
                    new ColumnDefinition(),
                }
            };
            Grid.SetRow(textALabel, 0);
            Grid.SetColumn(textALabel, 0);
            Grid.SetRow(textA, 0);
            Grid.SetColumn(textA, 1);
            Grid.SetColumnSpan(textA, 2);
            Grid.SetRow(textBLabel, 1);
            Grid.SetColumn(textBLabel, 0);
            Grid.SetRow(textB, 1);
            Grid.SetColumn(textB, 1);
            Grid.SetColumnSpan(textB, 2);
            Grid.SetRow(testChar, 2);
            Grid.SetColumn(testChar, 0);
            Grid.SetRow(testWord, 2);
            Grid.SetColumn(testWord, 1);

            panel.Children.Add(textALabel);
            panel.Children.Add(textA);
            panel.Children.Add(textBLabel);
            panel.Children.Add(textB);
            panel.Children.Add(testChar);
            panel.Children.Add(testWord);
            //test.RowSpan = 

            itemParam = new GroupBoxItem()
            {
                Control = panel,
                FillWidth = true,
                Name = "Params"
            };

            itemResult = new GroupBoxItem()
            {
                Control = result,
                Row = 1,
                FillHeight = true,
                Name = "Result"
            };

            map = new GroupBox(itemParam, itemResult);

            Children.Add(map);
            Text = "Diff Test";
        }

        private void result_SelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (result.SelectedItem != null)
            {
                var rez = (DiffResult)result.SelectedItem;
                var box = rez.Type == DiffType.Inserted ? textB : textA;
                box.Focus();
                if (diffMode == DIffMode.Char)
                {
                    box.SelectionStart = rez.Index;
                    box.SelectionEnd = rez.Length;
                }
                else
                {
                    var array = box.Text.Split(' ');

                    box.SelectionStart = array.Take(rez.Index).Sum(p => p.Length) + rez.Index;
                    box.SelectionEnd = array.Skip(rez.Index + 1).Take(rez.Length).Sum(p => p.Length) + (rez.Length - 1);
                }
            }
        }

        private void TestCharOnClick(object sender, EventArgs e)
        {
            diffMode = DIffMode.Char;
            result.ListSource = DiffResult.Diff(textA.Text, textB.Text, false);
        }

        private void TestWordOnClick(object sender, EventArgs e)
        {
            diffMode = DIffMode.Word;
            result.ListSource = DiffResult.Diff(textA.Text.Split(' '), textB.Text.Split(' '));
        }
    }

    internal enum DIffMode
    {
        Char,
        Word
    }
}

