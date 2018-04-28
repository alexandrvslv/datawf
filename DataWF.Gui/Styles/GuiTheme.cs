using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using Xwt;
using Xwt.Drawing;

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

        public CellStyle GenerateStyle(string name, Font font, Color value, 
            double diff = 0.09D, 
            int round = 0, 
            bool alter = true, 
            bool emptyBack = true, 
            double lineWidth = 1, 
            Alignment alignment = Alignment.Center,
            CellStyleBrushType brushType = CellStyleBrushType.Gradient)
        {
            var baseColor = value;
            var fontColor = value.Invert().WithIncreasedContrast(diff);
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
                    Color = emptyBack ? CellStyleBrush.ColorEmpty : baseColor,
                    ColorHover = baseColor.WithIncreasedLight(diff),
                    ColorSelect = baseColor.WithIncreasedLight(diff * 2),
                    ColorPress = baseColor.WithIncreasedLight(diff * 3),
                    ColorAlternate = baseColor.WithIncreasedLight(diff / 3)
                },
                BorderBrush = new CellStyleBrush
                {
                    Color = emptyBack ? CellStyleBrush.ColorEmpty : baseColor.WithIncreasedContrast(diff),
                    ColorHover = baseColor.WithIncreasedLight(diff).WithIncreasedContrast(diff),
                    ColorSelect = baseColor.WithIncreasedLight(diff * 2).WithIncreasedContrast(diff),
                    ColorPress = baseColor.WithIncreasedLight(diff * 3).WithIncreasedContrast(diff),
                    ColorAlternate = baseColor.WithIncreasedLight(diff / 3).WithIncreasedContrast(diff)
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
                defaultFont = defaultFont.WithSize(defaultFont.Size * 0.1);
            }

            AddRange(new CellStyle[]{
                GenerateStyle("Window",
                    defaultFont,
                    baseBackground,
                    diff ),
                GenerateStyle("Page",
                    defaultFont.WithWeight(FontWeight.Semibold),
                    baseBackground.WithIncreasedLight(diff/2D),
                    diff ),
                GenerateStyle("PageClose",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff/2D),
                    diff,
                    3),
                GenerateStyle("List",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    diff ),
                GenerateStyle("Row",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    diff),
                GenerateStyle("ChangeRow",
                    defaultFont.WithStyle(FontStyle.Italic),
                    baseBackground.WithIncreasedLight(diff),
                    diff),
                GenerateStyle("MessageRow",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    diff,
                    0,
                    false),
                GenerateStyle("Node",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*2),
                    diff,
                    0,
                    false),
                GenerateStyle("Cell",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff),
                    diff,
                    lineWidth:0),
                GenerateStyle("Value",
                    defaultFont,
                    baseBackground.WithIncreasedLight(diff*2),
                    diff),
                GenerateStyle("CellCenter",
                    defaultFont,                    
                    baseBackground.WithIncreasedLight(-0.1),
                    diff,
                    alignment:Alignment.Center),
                GenerateStyle("CellFar",
                    defaultFont,
                    baseBackground.WithIncreasedLight(-0.1),
                    diff,
                    alignment:Alignment.End),
                GenerateStyle("Column",
                    defaultFont.WithWeight(FontWeight.Semibold),
                    baseBackground.WithIncreasedLight(0.1),
                    diff,
                    emptyBack:false,
                    brushType: CellStyleBrushType.Gradient),
                new CellStyle()
                {
                    Name = "Group",
                    Font = defaultFont.WithSize(defaultFont.Size + 1).WithWeight(FontWeight.Semibold),
                    Round = 5,
                    BaseColor = baseBackground.WithIncreasedLight(0.1)
                },
                new CellStyle()
                {
                    Name = "Red",
                    Font = defaultFont,
                    Round = 0,
                    BaseColor = Colors.Red
                },
                new CellStyle()
                {
                    Name = "Header",
                    Alternate = false,
                    Alignment = Alignment.End,
                    BaseColor = baseBackground.WithIncreasedLight(0.1)
                },
                new CellStyle()
                {
                    Name = "Field",
                    Font = defaultFont,
                    Alternate = false,
                    BaseColor = baseBackground.WithIncreasedLight(-0.1)
                },
                new CellStyle()
                {
                    Name = "FieldEditor",
                    Font = defaultFont,
                    Alternate = false,
                    BaseColor = baseBackground.WithIncreasedLight(0.2)
                },
                new CellStyle()
                {
                    Name = "Collect",
                    Font = defaultFont,
                    BaseColor = baseBackground.WithIncreasedLight(0.2)
                },
                new CellStyle()
                {
                    Name = "GroupBoxHeader",
                    Round = 5,
                    Font = defaultFont.WithSize(defaultFont.Size).WithWeight(FontWeight.Bold),
                    BaseColor = baseBackground.WithIncreasedLight(0.3)
                },
                new CellStyle()
                {
                    Name = "GroupBox",
                    Round = 4,
                    BaseColor = baseBackground.WithIncreasedLight(0.1)
                },
                new CellStyle()
                {
                    Name = "Logs",
                    BaseColor = Colors.DarkBlue
                },
                new CellStyle()
                {
                    Name = "Notify",
                    Font = defaultFont.WithSize(defaultFont.Size + 1).WithWeight(FontWeight.Bold)
                },
                new CellStyle()
                {
                    Name = "DropDown",
                    Round = 4,
                    BaseColor = baseBackground.WithIncreasedLight(0.1)
                },
                new CellStyle()
                {
                    Name = "Glyph",
                    FontBrush = new CellStyleBrush() { ColorHover = Colors.LightGray }
                },
                new CellStyle()
                {
                    Name = "Selection",
                    BackBrush = new CellStyleBrush() { Color = Colors.LightSkyBlue.WithAlpha(0.5) },
                    BorderBrush = new CellStyleBrush() { Color = Colors.SkyBlue.WithAlpha(0.5) }
                },
                new CellStyle()
                {
                    Name = "Tool",
                    Font = defaultFont.WithSize(defaultFont.Size + 0.1),
                    LineWidth = 1.5,
                    Round = 3,
                    BaseColor = baseBackground.WithIncreasedLight(0.2)
                }
            });

            this["Red"].BackBrush.Color = this["Red"].BaseColor;
            this["Value"].BackBrush.Color = this["Value"].BaseColor;
            this["Value"].BorderBrush.Color = this["Value"].BaseColor.WithIncreasedLight(0.1);
            this["Row"].BackBrush.Color = this["Row"].BaseColor;
            this["Column"].BackBrush.Color = this["Column"].BaseColor;
            this["Column"].BackBrush.Type = CellStyleBrushType.Gradient;
            this["GroupBoxHeader"].BackBrush.Color = this["GroupBoxHeader"].BaseColor;
            this["GroupBoxHeader"].BackBrush.Type = CellStyleBrushType.Gradient;
            this["Group"].BackBrush.Color = this["Group"].BaseColor;
            this["Group"].BackBrush.Type = CellStyleBrushType.Gradient;
            this["DropDown"].BackBrush.Color = this["DropDown"].BaseColor;
            this["DropDown"].BackBrush.Type = CellStyleBrushType.Gradient;
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
