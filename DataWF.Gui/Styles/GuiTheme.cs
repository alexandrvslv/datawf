using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using Xwt;
using Xwt.Drawing;
using static System.Math;

namespace DataWF.Gui
{
    public class GuiTheme : NamedList<CellStyle>, INamed
    {
        static readonly Invoker<CellStyle, string> nameInvoker = new Invoker<CellStyle, string>(nameof(CellStyle.Name), (item) => item.Name);

        public GuiTheme()
        {
            Indexes.Add(nameInvoker);
        }

        public string Name { get; set; }

        public CellStyle GenerateStyle(string name, Font font, Color value, double diff,
            int round = 0,
            bool alter = true,
            bool emptyBack = true,
            double lineWidth = 0.8,
            Alignment alignment = Alignment.Start,
            CellStyleBrushType brushType = CellStyleBrushType.Solid)
        {
            var fontColor = value.WithIncreasedLight(diff).Invert();
            return new CellStyle
            {
                Name = name,
                Font = font,
                BaseColor = value,
                Round = round,
                Alternate = alter,
                LineWidth = lineWidth,
                Alignment = alignment,
                BackBrush = new CellStyleBrush
                {
                    Type = brushType,
                    Color = emptyBack ? CellStyleBrush.ColorEmpty : value,
                    ColorHover = value.WithIncreasedLight(diff),
                    ColorSelect = value.WithIncreasedLight(diff * 2),
                    ColorPress = value.WithIncreasedLight(diff * 3),
                    ColorAlternate = value.WithIncreasedLight(diff / 4.0)
                },
                BorderBrush = new CellStyleBrush
                {
                    Color = emptyBack ? CellStyleBrush.ColorEmpty : value.WithIncreasedContrast(Abs(diff)),
                    ColorHover = value.WithIncreasedLight(diff).WithIncreasedContrast(Abs(diff)),
                    ColorSelect = value.WithIncreasedLight(diff * 2).WithIncreasedContrast(Abs(diff)),
                    ColorPress = value.WithIncreasedLight(diff * 3).WithIncreasedContrast(Abs(diff)),
                    ColorAlternate = value.WithIncreasedLight(diff / 4.0).WithIncreasedContrast(Abs(diff))
                },
                FontBrush = new CellStyleBrush
                {
                    Color = fontColor,
                    ColorHover = fontColor,
                    ColorSelect = fontColor,
                    ColorPress = fontColor,
                    ColorAlternate = fontColor
                }
            };
        }

        public void Generate(Font defaultFont, Color baseBackground, double diff = -0.1)
        {
            if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk)
            {
                defaultFont = defaultFont.WithSize(defaultFont.Size * 0.9);
            }
            else
            {
                defaultFont = defaultFont.WithSize(defaultFont.Size);
            }

            AddRange(new CellStyle[]{
                GenerateStyle("Window",
                    defaultFont,
                    baseBackground,
                    diff ),
                GenerateStyle("Page",
                    defaultFont.WithWeight(FontWeight.Semibold),
                    baseBackground,
                    diff ),
                GenerateStyle("PageClose",
                    defaultFont,
                    baseBackground.WithIncreasedLight(-diff/2D),
                    diff,
                    3),
                GenerateStyle("List",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*4),
                    diff ),
                GenerateStyle("Row",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*4),
                    -diff*2,
                    emptyBack:false),
                GenerateStyle("ChangeRow",
                    defaultFont.WithStyle(FontStyle.Italic),
                    baseBackground.WithIncreasedLight(diff),
                    diff),
                GenerateStyle("MessageRow",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    diff,
                    alter:false),
                GenerateStyle("Node",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*4),
                    -diff*2,
                    alter:false),
                GenerateStyle("Cell",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*3),
                    -diff,
                    lineWidth:0),
                GenerateStyle("Value",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*3),
                    diff,
                    emptyBack:false),
                GenerateStyle("CellCenter",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*3),
                    diff,
                    alignment:Alignment.Center),
                GenerateStyle("CellFar",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*3),
                    diff,
                    alignment:Alignment.End),
                GenerateStyle("Column",
                    defaultFont.WithWeight(FontWeight.Semibold),
                    baseBackground.WithIncreasedLight(diff),
                    diff,
                    emptyBack:false,
                    brushType: CellStyleBrushType.Gradient,
                    lineWidth:0.5),
                GenerateStyle("Group",
                    defaultFont.WithSize(defaultFont.Size + 1).WithWeight(FontWeight.Semibold),
                    baseBackground.WithIncreasedLight(diff),
                    diff,
                    round:5,
                    emptyBack:false,
                    brushType: CellStyleBrushType.Gradient ),
                GenerateStyle("Red",
                    defaultFont,
                    Colors.Red,
                    diff,
                    emptyBack:false),
                GenerateStyle("Header",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    diff,
                    alter:false,
                    alignment:Alignment.End),
                GenerateStyle("Field",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*3),
                    diff,
                    alter:false),
                GenerateStyle("FieldEditor",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*3),
                    diff,
                    alter:false),
                GenerateStyle("Collect",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*2),
                    diff),
                GenerateStyle("GroupBoxHeader",
                    defaultFont.WithSize(defaultFont.Size).WithWeight(FontWeight.Bold),
                    baseBackground.WithIncreasedLight(diff*3),
                    diff,
                    round:5,
                    emptyBack:false,
                    brushType: CellStyleBrushType.Gradient),
                GenerateStyle("GroupBox",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    diff,
                    round:4),
                GenerateStyle("Logs",
                    defaultFont,
                    Colors.DarkBlue,
                    diff),
                GenerateStyle("Notify",
                    defaultFont.WithSize(defaultFont.Size + 1).WithWeight(FontWeight.Bold),
                    baseBackground.WithIncreasedLight(diff),
                    diff),
                GenerateStyle("DropDown",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    diff,
                    round:4,
                    emptyBack:false,
                    brushType: CellStyleBrushType.Gradient),
                GenerateStyle("Glyph",
                    defaultFont,
                    Colors.LightGray,
                    diff),
                GenerateStyle("Selection",
                    defaultFont,
                    Colors.SkyBlue.WithAlpha(0.5),
                    diff),
                GenerateStyle("Tool",
                    defaultFont.WithSize(defaultFont.Size + 0.1),
                    baseBackground.WithIncreasedLight(diff),
                    diff,
                    lineWidth:1.5,
                    round:3)
            });
        }

        public override int AddInternal(CellStyle item)
        {
            if (item == null)
                throw new ArgumentException();
            var exist = this[item.Name];
            if (exist != item)
            {
                if (exist != null)
                    item.Name += "Clone";
                return base.AddInternal(item);
            }
            return -1;
        }

        public bool Contains(string name)
        {
            return this[name] != null;
        }

        public bool Remove(string name)
        {
            return Remove(this[name]);
        }

        public override void Dispose()
        {
            foreach (var item in items)
                item.Dispose();
            base.Dispose();
        }
    }


}
