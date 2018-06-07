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

        public CellStyle GenerateStyle(string name, Font font, Color backColor, Color fontColor, double diff,
            int round = 0,
            bool alter = true,
            bool emptyBack = true,
            bool emptyBorder = true,
            double lineWidth = 0.8,
            Alignment alignment = Alignment.Start,
            CellStyleBrushType brushType = CellStyleBrushType.Solid)
        {
            //var fontColor = backColor.WithIncreasedLight(diff).Invert();
            return new CellStyle
            {
                Name = name,
                Font = font,
                BaseColor = backColor,
                Round = round,
                Alternate = alter,
                LineWidth = lineWidth,
                Alignment = alignment,
                BackBrush = new CellStyleBrush
                {
                    Type = brushType,
                    Color = emptyBack ? CellStyleBrush.ColorEmpty : backColor,
                    ColorHover = backColor.WithIncreasedLight(diff),
                    ColorSelect = backColor.WithIncreasedLight(diff * 2),
                    ColorPress = backColor.WithIncreasedLight(diff * 3),
                    ColorAlternate = backColor.WithIncreasedLight(diff / 4.0)
                },
                BorderBrush = new CellStyleBrush
                {
                    Color = emptyBorder ? CellStyleBrush.ColorEmpty : backColor.WithIncreasedLight(-diff),
                    ColorHover = backColor.WithIncreasedLight(-diff),
                    ColorSelect = backColor.WithIncreasedLight(-diff * 2),
                    ColorPress = backColor.WithIncreasedLight(-diff * 3),
                    ColorAlternate = backColor.WithIncreasedLight(-diff / 4.0)
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

        public void Generate(Font defaultFont, Color baseBackground, Color baseForeColor, double diff = -0.1)
        {
            if (Toolkit.CurrentEngine.Type == ToolkitType.Gtk)
            {
                defaultFont = defaultFont.WithSize(defaultFont.Size * 0.92);
            }
            else
            {
                defaultFont = defaultFont.WithSize(defaultFont.Size * 1.08);
            }

            AddRange(new CellStyle[]{
                GenerateStyle("Window",
                    defaultFont,
                    baseBackground,
                    baseForeColor,
                    diff ),
                GenerateStyle("Dock",
                    defaultFont.WithWeight(FontWeight.Semibold),
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff,
                    round:5,
                    emptyBack:false),
                GenerateStyle("Page",
                    defaultFont.WithWeight(FontWeight.Semibold),
                    baseBackground,
                    baseForeColor,
                    diff,
                    round:5),
                GenerateStyle("PageClose",
                    defaultFont,
                    baseBackground.WithIncreasedLight(-diff/2D),
                    baseForeColor,
                    diff,
                    3),
                GenerateStyle("List",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff * 3.5),
                    baseForeColor,
                    diff,
                    emptyBack:false),
                GenerateStyle("Row",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff * 3.5),
                    baseForeColor,
                    -diff * 2,
                    emptyBack: false),
                GenerateStyle("ChangeRow",
                    defaultFont.WithStyle(FontStyle.Italic),
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff),
                GenerateStyle("MessageRow",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff,
                    alter: false),
                GenerateStyle("Node",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff * 3),
                    baseForeColor,
                    -diff * 2,
                    alter: false),
                GenerateStyle("Cell",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff * 2),
                    baseForeColor,
                    -diff,
                    lineWidth: 0),
                GenerateStyle("Value",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff * 2),
                    baseForeColor,
                    diff,
                    emptyBorder: false),
                GenerateStyle("CellCenter",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff * 3),
                    baseForeColor,
                    diff,
                    alignment: Alignment.Center),
                GenerateStyle("Calendar",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff * 2),
                    baseForeColor,
                    diff,
                    alignment: Alignment.Center,
                    emptyBack: false,
                    emptyBorder: false),
            GenerateStyle("CellFar",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff * 3),
                    baseForeColor,
                    diff,
                    alignment: Alignment.End),
                GenerateStyle("Column",
                    defaultFont.WithWeight(FontWeight.Semibold),
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff,
                    emptyBack: false,
                    brushType: CellStyleBrushType.Gradient,
                    lineWidth: 0.5),
                GenerateStyle("Group",
                    defaultFont.WithSize(defaultFont.Size + 1).WithWeight(FontWeight.Semibold),
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff,
                    round: 5,
                    emptyBack: false,
                    brushType: CellStyleBrushType.Gradient),
                GenerateStyle("Red",
                    defaultFont,
                    Colors.Red,
                    baseForeColor,
                    diff,
                    emptyBack: false),
                GenerateStyle("Header",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff,
                    alter: false,
                    alignment: Alignment.End),
                GenerateStyle("Field",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff * 3),
                    baseForeColor,
                    diff,
                    alter: false),
                GenerateStyle("FieldEditor",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff,
                    alter: false),
                GenerateStyle("Collect",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff * 2),
                    baseForeColor,
                    diff),
                GenerateStyle("GroupBoxHeader",
                    defaultFont.WithSize(defaultFont.Size).WithWeight(FontWeight.Bold),
                    baseBackground.WithIncreasedLight(diff * 3),
                    baseForeColor,
                    diff,
                    round: 5,
                    emptyBack: false,
                    brushType: CellStyleBrushType.Gradient),
                GenerateStyle("GroupBox",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff,
                    round: 4),
                GenerateStyle("Logs",
                    defaultFont,
                    Colors.DarkBlue,
                    baseForeColor,
                    diff),
                GenerateStyle("Notify",
                    defaultFont.WithSize(defaultFont.Size + 1).WithWeight(FontWeight.Bold),
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff),
                GenerateStyle("DropDown",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff,
                    round: 4,
                    emptyBack: false,
                    brushType: CellStyleBrushType.Gradient),
                GenerateStyle("Glyph",
                    defaultFont,
                    Colors.LightGray,
                    baseForeColor,
                    diff),
                GenerateStyle("Selection",
                    defaultFont,
                    Colors.SkyBlue.WithAlpha(0.5),
                    baseForeColor,
                    diff,
                    emptyBack:false,
                    emptyBorder:false),
                GenerateStyle("Tool",
                    defaultFont.WithSize(defaultFont.Size * 1.08).WithWeight(FontWeight.Semibold),
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff,
                    lineWidth: 1.5,
                    round: 3),
                GenerateStyle("Toolsbar",
                    defaultFont.WithSize(defaultFont.Size * 1.08).WithWeight(FontWeight.Semibold),
                    baseBackground.WithIncreasedLight(diff),
                    baseForeColor,
                    diff,
                    lineWidth: 1.5,
                    round: 3,
                    emptyBack: false,
                    emptyBorder: false,
                    brushType: CellStyleBrushType.Gradient)
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
