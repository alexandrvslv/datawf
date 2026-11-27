using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace DataWF.Gui
{
    public class ToolTipControl : ContentControl
    {
        protected TextBox content = new TextBox();

        public ToolTipControl()
            : base()
        {
            InitializeControl();
        }

        public string Text
        {
            get => content.Text;
            set => content.Text = value;
        }

        public void InitializeControl()
        {
            content.IsReadOnly = true;
            this.FontFamily = content.FontFamily;
            Content = content;
            Background = new ImmutableSolidColorBrush(Colors.LightSlateGray);
        }
    }
}

