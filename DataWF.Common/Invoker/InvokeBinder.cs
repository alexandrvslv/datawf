using System;
using System.ComponentModel;

namespace DataWF.Common
{
    public class InvokeBinder<D, V> : InvokeBinder where V : class
    {
        private D data;
        private V view;

        public InvokeBinder(D data, string dataProperty, V view, string viewProeprty)
           : this(data, EmitInvoker.Initialize(data.GetType(), dataProperty),
               view, EmitInvoker.Initialize(view.GetType(), viewProeprty))
        {
        }

        public InvokeBinder(D data, IInvoker dataInvoker, V view, IInvoker viewInvoker)
        {
            ViewInvoker = viewInvoker;
            View = view;
            DataInvoker = dataInvoker;
            Data = data;
        }

        public D Data
        {
            get { return data; }
            set
            {
                if (data?.Equals(value) ?? false)
                    return;
                if (data is INotifyPropertyChanged oldNotify)
                {
                    oldNotify.PropertyChanged -= DataPropertyChanged;
                }
                data = value;
                if (data != null)
                {
                    DataPropertyChanged(data, new PropertyChangedEventArgs(null));
                }
                if (data is INotifyPropertyChanged newNotify)
                {
                    newNotify.PropertyChanged += DataPropertyChanged;
                }
            }
        }

        public V View
        {
            get { return view; }
            set
            {
                if (view == value)
                    return;
                if (view is INotifyPropertyChanged oldNotify)
                {
                    oldNotify.PropertyChanged -= ViewPropertyChanged;
                }
                view = value;
                if (view is INotifyPropertyChanged newNotify)
                {
                    newNotify.PropertyChanged += ViewPropertyChanged;
                }
            }
        }

        public override void Bind(object data, object view)
        {
            View = (V)view;
            Data = (D)data;
        }

        public override void Unbind()
        {
            Bind(null, null);
        }

        private void DataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ViewInvoker == null)
                throw new InvalidOperationException($"{nameof(ViewInvoker)} is not specified!");
            if (string.Equals(DataInvoker.Name, e.PropertyName, StringComparison.Ordinal) || string.IsNullOrEmpty(e.PropertyName))
            {
                if (view is INotifyPropertyChanged notify)
                {
                    notify.PropertyChanged -= ViewPropertyChanged;
                    ViewInvoker.SetValue(view, DataInvoker?.GetValue(data));
                    notify.PropertyChanged += ViewPropertyChanged;
                }
                else
                {
                    ViewInvoker.SetValue(view, DataInvoker?.GetValue(data));
                }
            }
        }

        private void ViewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(ViewInvoker.Name, e.PropertyName, StringComparison.Ordinal) || string.IsNullOrEmpty(e.PropertyName))
            {
                if (data is INotifyPropertyChanged notify)
                {
                    notify.PropertyChanged -= DataPropertyChanged;
                    DataInvoker.SetValue(data, ViewInvoker?.GetValue(view));
                    notify.PropertyChanged += DataPropertyChanged;
                }
                else
                {
                    DataInvoker.SetValue(data, ViewInvoker?.GetValue(view));
                }
            }
        }

        public override void Dispose()
        {
            if (view is INotifyPropertyChanged viewNotify)
            {
                viewNotify.PropertyChanged -= ViewPropertyChanged;
            }
            if (data is INotifyPropertyChanged dataNotify)
            {
                dataNotify.PropertyChanged -= DataPropertyChanged;
            }
        }

        public override object GetData()
        {
            return Data;
        }

        public override object GetView()
        {
            return View;
        }


    }
}

