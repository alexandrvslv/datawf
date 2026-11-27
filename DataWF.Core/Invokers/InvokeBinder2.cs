using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Invokers
{
    public class InvokeBinder<D, V, K> : InvokeBinder where V : class
    {
        private D data;
        private V view;

        public InvokeBinder(D data, string dataProperty, V view, string viewProeprty)
           : this(data, InvokeProvider.GetInvoker<D, K>(dataProperty),
               view, InvokeProvider.GetInvoker<V, K>(viewProeprty))
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
            get => data;
            set
            {
                if (EqualityComparer<D>.Default.Equals(data, value))
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
            get => view;
            set
            {
                if (EqualityComparer<V>.Default.Equals(view, value))
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
            Bind(default, default);
        }

        private void DataPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (ViewInvoker == null)
                throw new InvalidOperationException($"{nameof(ViewInvoker)} is not specified!");
            if (string.Equals(DataInvoker.Name, e.PropertyName, StringComparison.Ordinal) 
                || string.IsNullOrEmpty(e.PropertyName))
            {
                if (view is INotifyPropertyChanged notify)
                {
                    notify.PropertyChanged -= ViewPropertyChanged;
                    SetViewValue(e);
                    notify.PropertyChanged += ViewPropertyChanged;
                }
                else
                {
                    SetViewValue(e);
                }
            }
        }

        private void SetViewValue(PropertyChangedEventArgs e)
        {
            if (e is PropertyChangedDetailEventArgs details)
                ViewInvoker.SetValue(view, details.GetNewValue());
            else
                ViewInvoker.SetValue(view, DataInvoker?.GetValue(data));
        }

        private void ViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(ViewInvoker.Name, e.PropertyName, StringComparison.Ordinal) 
                || string.IsNullOrEmpty(e.PropertyName))
            {
                var value = ViewInvoker?.GetValue(view);
                if (data is INotifyPropertyChanged notify)
                {
                    notify.PropertyChanged -= DataPropertyChanged;
                    DataInvoker.SetValue(data, value);
                    notify.PropertyChanged += DataPropertyChanged;
                }
                else
                {
                    DataInvoker.SetValue(data, value);
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

        public override object GetData() => Data;

        public override object GetView() => View;


    }
}

