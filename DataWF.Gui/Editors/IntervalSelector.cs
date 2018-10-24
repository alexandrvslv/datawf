using DataWF.Common;
using System;
using Xwt;

namespace DataWF.Gui
{
    public class IntervalSelector : HBox
    {
        protected DateInterval _value = new DateInterval(DateTime.Now, DateTime.Now);
        private CalendarEditor _pickerMin = new CalendarEditor();
        private CalendarEditor _pickerMax = new CalendarEditor();
        protected bool _twoDate = true;

        public IntervalSelector()
        {
            _pickerMin.Name = "_pickerMin";
            _pickerMin.ValueChanged += PickerMinDateChanged;

            _pickerMax.Name = "_pickerMax";
            _pickerMax.ValueChanged += PickerMaxDateChanged;

            PackStart(_pickerMin, true, true);
            PackStart(_pickerMax, true, true);
            Name = "IntervalPicker";
        }

        public bool TwoDate
        {
            get { return _twoDate; }
            set
            {
                if (_twoDate != value)
                {
                    _pickerMax.Visible = _twoDate = value;
                }
            }
        }

        public CalendarEditor PickerMin
        {
            get { return _pickerMin; }
        }

        public CalendarEditor PickerMax
        {
            get { return _pickerMin; }
        }

        public DateInterval Value
        {
            get { return _value; }
            set
            {
                if (_value == value)
                    return;

                _value = value;
                if (_value.Min == DateTime.MinValue)
                    _value.Min = DateTime.Now;
                if (!_twoDate)
                    _value.Max = _value.Min;

                _pickerMin.ValueChanged -= PickerMinDateChanged;
                _pickerMax.ValueChanged -= PickerMaxDateChanged;
                _pickerMin.Date = _value.Min;
                _pickerMax.Date = _value.Max;
                _pickerMin.ValueChanged += PickerMinDateChanged;
                _pickerMax.ValueChanged += PickerMaxDateChanged;
            }
        }

        private void PickerMinDateChanged(object sender, EventArgs e)
        {
            _value.Min = _pickerMin.Date;
            _pickerMax.ValueChanged -= PickerMaxDateChanged;
            _pickerMax.Date = _value.Max;
            _pickerMax.ValueChanged += PickerMaxDateChanged;
            OnValueChanged();
        }

        private void PickerMaxDateChanged(object sender, EventArgs e)
        {
            _value.Max = _pickerMax.Date;
            _pickerMin.ValueChanged -= PickerMinDateChanged;
            _pickerMin.Date = _value.Min;
            _pickerMin.ValueChanged += PickerMinDateChanged;
            OnValueChanged();
        }

        public event EventHandler ValueChanged;

        protected void OnValueChanged()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

    }
}
