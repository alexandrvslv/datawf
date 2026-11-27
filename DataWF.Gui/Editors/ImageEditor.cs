using System;
using System.IO;
using DataWF;
using Avalonia;
using Avalonia.Controls;
using System.Linq;
using Avalonia.Media.Imaging;
using System.Collections.Generic;

namespace DataWF.Gui
{
    public class ImageEditor : VPanel
    {
        protected string fileName;

        private Toolsbar tools = new Toolsbar();
        private ToolItem toolNew = new ToolItem();
        private ToolItem toolOpen = new ToolItem();
        private ToolItem toolSave = new ToolItem();
        private ToolItem toolPrint = new ToolItem();
        private ToolItem toolEnlarge = new ToolItem();
        private ToolItem toolReduce = new ToolItem();
        private ToolItem toolRotate = new ToolItem();
        private ToolItem toolUndo = new ToolItem();
        private ImageCanvas imageView = new ImageCanvas();
        private ScrollViewer panel = new ScrollViewer();

        public ImageEditor()
        {
            imageView.Name = "panel1";
            panel.Name = "panel2";

            tools.Items.Add(toolNew);
            tools.Items.Add(toolOpen);
            tools.Items.Add(toolSave);
            tools.Items.Add(toolPrint);
            tools.Items.Add(new ToolSeparator());
            tools.Items.Add(toolEnlarge);
            tools.Items.Add(toolReduce);
            tools.Items.Add(toolRotate);
            tools.Items.Add(toolUndo);
            tools.Name = "toolStrip2";

            toolNew.Name = "toolNew";
            toolNew.Glyph = GlyphType.FileO;
            toolNew.Text = "&New";

            toolOpen.Name = "toolOpen";
            toolOpen.Text = "&Open";
            toolOpen.Glyph = GlyphType.FolderOpen;
            toolOpen.Click += toolLoadClick;

            toolSave.Name = "toolSave";
            toolSave.Text = "&Save";
            toolSave.Glyph = GlyphType.SaveAlias;
            toolSave.Click += toolSaveClick;

            toolPrint.Name = "toolPrint";
            toolPrint.Text = "&Print";
            toolPrint.Glyph = GlyphType.Print;
            toolPrint.Click += toolPrintClick;

            toolEnlarge.Name = "toolEnlarge";
            toolEnlarge.Text = "Enlarge";
            toolEnlarge.Glyph = GlyphType.SearchPlus;
            toolEnlarge.Click += toolEnlargeClick;

            toolReduce.Name = "toolReduce";
            toolReduce.Text = "Reduce";
            toolReduce.Glyph = GlyphType.SearchMinus;
            toolReduce.Click += toolReduceClick;

            toolRotate.Name = "toolRotate";
            toolRotate.Text = "Rotate";
            toolRotate.Glyph = GlyphType.Circle;
            toolRotate.Click += toolRotateClick;

            toolUndo.Name = "toolUndo";
            toolUndo.Text = "Undo";
            toolUndo.Glyph = GlyphType.Undo;
            toolUndo.Click += toolUndoClick;

            panel.Content = imageView;

            Children.Add(tools);
            Children.Add(panel);
            Name = "ImageEditor";
        }

        public Bitmap EditImage
        {
            get => imageView.Image;
            set
            {
                if (EditImage == value)
                    return;
                imageView.Image = value;
            }
        }

        public void LoadImage(byte[] value)
        {
            using (var ms = new MemoryStream(value))
            {
                EditImage = new Bitmap(ms);
            }
        }

        private async void toolSaveClick(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Filters.Add(new FileDialogFilter { Name = "PNG", Extensions = new List<string> { "*.png" } });
            var file = await dialog.ShowAsync((Window)VisualRoot);
            if (file != null)
            {
                imageView.Save(file);

            }
        }

        private void toolPrintClick(object sender, EventArgs e)
        {
        }

        private void toolRotateClick(object sender, EventArgs e)
        {
            imageView.Degrees += 15;
        }


        private async void toolLoadClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            var files = await dialog.ShowAsync((Window)VisualRoot);
            if (files?.Any() ?? false)
            {
                EditImage = new Bitmap(files.First());
            }
        }

        private void toolUndoClick(object sender, EventArgs e)
        {
            imageView.Degrees = 0;
            imageView.Scale = 1;
        }

        private void toolEnlargeClick(object sender, EventArgs e)
        {
            imageView.Scale += 0.1D;
        }

        private void toolReduceClick(object sender, EventArgs e)
        {
            imageView.Scale -= 0.1F;
        }
    }
}
