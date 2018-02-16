using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Xwt;


namespace DataWF.Gui
{
    public class LocalizeEditor : ListExplorer, ILocalizable
    {
        private ToolItem toolLoadImages = new ToolItem();
        private ToolItem toolSave = new ToolItem();
        private ToolItem toolImages = new ToolItem();
        private ToolWindow window = new ToolWindow();

        public LocalizeEditor()
        {
            toolImages.DisplayStyle = ToolItemDisplayStyle.Text;
            toolImages.Name = "toolImages";
            toolImages.Text = "Images";
            toolImages.Click += ToolImagesClick;

            toolLoadImages.DisplayStyle = ToolItemDisplayStyle.Text;
            toolLoadImages.Name = "toolLoadImages";
            toolLoadImages.Text = "Load Images";
            toolLoadImages.Click += ToolLoadImagesClick;

            toolSave.DisplayStyle = ToolItemDisplayStyle.Text;
            toolSave.Name = "toolSave";
            toolSave.Text = "Save";
            toolSave.Click += this.ToolSaveClick;

            Editor.Bar.Items.Add(new SeparatorToolItem());
            Editor.Bar.Items.Add(toolSave);
            Editor.Bar.Items.Add(toolImages);
            Editor.Bar.Items.Add(toolLoadImages);
            Editor.ReadOnly = false;
            Editor.DataSource = Common.Locale.Data.Names;
            Editor.List.RetriveCellEditor += (object listItem, object value, ILayoutCell cell) =>
            {
                if (cell.GetEditor(listItem) == null)
                {
                    if (Equals(cell.Name, "ImageKey"))
                        return new CellEditorLocalizeImage();
                    else if (Equals(cell.Name, "Glyph"))
                        return new CellEditorGlyph();
                }
                return null;
            };

            var images = new ListEditor();
            images.List.ListInfo.ColumnsVisible = false;
            images.List.ListInfo.HeaderVisible = false;
            images.DataSource = Common.Locale.Data.Images;

            window.Target = images;

            Name = "LocalizeEditor";
            Text = "LocalizeEditor";
        }

        public override void Localize()
        {
            var name = GetType().Name;
            GuiService.Localize(this, name, "Localize Editor");
            GuiService.Localize(toolLoadImages, name, "Load Images");
            GuiService.Localize(toolImages, name, "Images", GlyphType.PictureO);
            GuiService.Localize(toolSave, name, "Save", GlyphType.SaveAlias);
        }

        private void ToolImagesClick(object sender, EventArgs e)
        {
            window.Show(this, Point.Zero);
        }

        private void ToolLoadImagesClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            if (dialog.Run(ParentWindow))
            {
                foreach (string name in dialog.FileNames)
                {
                    Common.Locale.Data.Images.Add(new LImage()
                    {
                        Data = File.ReadAllBytes(name),
                        FileName = name
                    });
                }
            }
        }

        private void ToolSaveClick(object sender, EventArgs e)
        {
            Common.Locale.Save();
        }
    }

}
