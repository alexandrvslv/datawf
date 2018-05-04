using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xwt;

namespace DataWF.Gui
{
    public class CalendarEditor : VPanel
    {
        private Month month;
        private ToolLabel lable;
        private LayoutList list;
        private DateTime value;

        public CalendarEditor()
        {
            month = new Month();

            var style = GuiEnvironment.Theme["CellCenter"].Clone();
            style.BackBrush.Color = style.BackBrush.ColorHover.WithIncreasedLight(-0.1);
            var bar = new Toolsbar(
                lable = new ToolLabel()
                {
                    Font = style.Font.WithScaledSize(1.5).WithWeight(Xwt.Drawing.FontWeight.Bold)
                },
                new ToolSeparator { FillWidth = true },
                new ToolItem((s, e) => Value = Value.AddMonths(-1)) { Name = "Prev Month", Glyph = Common.GlyphType.ChevronUp },
                new ToolItem((s, e) => Value = Value.AddMonths(1)) { Name = "Next Month", Glyph = Common.GlyphType.ChevronDown }
                );

            list = new LayoutList
            {
                GenerateColumns = false,
                GenerateToString = false,
                ListInfo = new LayoutListInfo(new LayoutColumn() { Name = "Number", Width = 50, Height = 50, Style = style })
                {
                    Indent = 4,
                    StyleRow = GuiEnvironment.Theme["Node"],
                    GridCol = 7,
                    ColumnsVisible = false,
                    HeaderVisible = false
                },
                ListSource = month.Days
            };
            list.SelectionChanged += ListSelectionChanged;
            PackStart(bar, false, false);
            PackStart(list, true, true);
            Value = DateTime.Today;
        }

        private void ListSelectionChanged(object sender, LayoutSelectionEventArgs e)
        {
            if (list.SelectedItem != null)
            {
                var day = (Day)list.SelectedItem;
                if (day.Date != value.Date)
                {
                    value = day.Date;
                    OnSelectionChanged(EventArgs.Empty);
                }

                lable.Text = day.Date.ToString("MMMM yyyy");
            }
        }

        private void OnSelectionChanged(EventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        public DateTime Value
        {
            get { return value; }
            set
            {
                this.value = value;
                month.SetDate(value.Year, value.Month);
                list.SelectedItem = month.GetDay(value);
            }
        }

        public event EventHandler SelectionChanged;
    }

    public class Month
    {
        public Day[] Days = new Day[42];

        public void SetDate(int year, int month)
        {
            DateTime date = new DateTime(year, month, 1);
            var dayofWeek = (int)date.DayOfWeek;
            if (dayofWeek == 0)
                dayofWeek = 7;
            int i = 0;
            int d = 0;
            for (; i < dayofWeek; i++)
            {
                Days[i] = new Day(date.AddDays(-(dayofWeek - i)));
            }
            for (; d < DateTime.DaysInMonth(year, month); d++, i++)
            {
                Days[i] = new Day(date.AddDays(d));
            }
            for (; i < Days.Length; i++, d++)
            {
                Days[i] = new Day(date.AddDays(d));
            }
        }

        public Day GetDay(DateTime date)
        {
            date = date.Date;
            return Days.FirstOrDefault(item => item.Date == date);
        }
    }

    public struct Day
    {
        public Day(DateTime date)
        {
            Date = date;
        }

        public DateTime Date { get; set; }

        public DayOfWeek DayOfWeek { get => Date.DayOfWeek; }

        public string Number { get => Date.ToString("ddd d"); }

    }
}
