using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Gui
{

    public class PSelectionAggregate
    {
        public LayoutGroup Group;
        public LayoutColumn Column;
    }

    public class LayoutSelection : IEnumerable<LayoutSelectionRow>
    {
        [NonSerialized()]
        private LayoutSelectionEventArgs cacheArg = new LayoutSelectionEventArgs();
        private LayoutSelectionRow search = new LayoutSelectionRow();
        protected List<LayoutSelectionRow> _items = new List<LayoutSelectionRow>();
        protected LayoutSelectionEventArgs current = new LayoutSelectionEventArgs();
        protected LayoutSelectionEventArgs hover = new LayoutSelectionEventArgs();
        protected LayoutSelectionEventArgs editor = new LayoutSelectionEventArgs();

        public LayoutSelection()
        {
        }

        public LayoutSelectionRow EditorRow
        {
            get { return editor.Value as LayoutSelectionRow; }
        }

        public object EditorValue
        {
            get { return editor.Value; }
            set { editor.Value = value; }
        }

        public LayoutSelectionRow HoverRow
        {
            get { return hover.Value as LayoutSelectionRow; }
        }

        public object HoverValue
        {
            get { return hover.Value; }
        }

        public void SetHover(object value)
        {
            if (hover.Value == value)
                return;
            var temp = hover.Value;
            hover.Value = value;
            if (temp != null)
                if (temp is LayoutSelectionRow && temp.Equals(value) && ((LayoutSelectionRow)temp).Column != null)
                    OnSelectionChanged(LayoutSelectionChange.Cell, temp);
                else
                    OnSelectionChanged(LayoutSelectionChange.Hover, temp);
            if (value != null)
                if (value is LayoutSelectionRow && value.Equals(temp) && ((LayoutSelectionRow)value).Column != null)
                    OnSelectionChanged(LayoutSelectionChange.Cell, value);
                else
                    OnSelectionChanged(LayoutSelectionChange.Hover, value);
        }

        public LayoutSelectionRow CurrentRow
        {
            get { return current.Value as LayoutSelectionRow; }
        }

        public object CurrentValue
        {
            get { return current.Value; }
        }

        public void SetCurrent(object value)
        {
            if (current.Value == value)
                return;
            var temp = current.Value;
            current.Value = value;

            if (temp != null)
            {
                if (temp is LayoutSelectionRow && temp.Equals(value) && ((LayoutSelectionRow)temp).Column != null)
                    OnSelectionChanged(LayoutSelectionChange.Cell, temp);
                else
                    OnSelectionChanged(LayoutSelectionChange.Hover, temp);
            }
            if (value != null)
            {
                if (value is LayoutSelectionRow && value.Equals(temp) && ((LayoutSelectionRow)value).Column != null)
                    OnSelectionChanged(LayoutSelectionChange.Cell, value);
                else
                    OnSelectionChanged(LayoutSelectionChange.Hover, temp);
            }
            //SetHover(null);
        }

        public event EventHandler<LayoutSelectionEventArgs> SelectionChanged;

        public void OnSelectionChanged(LayoutSelectionChange type, object item = null)
        {
            if (SelectionChanged != null)
            {
                if (GuiService.InvokeRequired)
                {
                    Xwt.Application.Invoke(() => OnSelectionChanged(type, item));
                }
                else
                {
                    cacheArg.Type = type;
                    cacheArg.Value = item;
                    SelectionChanged(this, cacheArg);
                }
            }
        }

        public bool All
        {
            get { return _items.Count == List.ListSource.Count; }
            set
            {
                if (value != All)
                    return;
                for (int i = 0; i < List.ListSource.Count; i++)
                    Add(List.ListSource[i], i, false);
                OnSelectionChanged(LayoutSelectionChange.Reset);
            }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public void Add(LayoutSelectionRow item, bool raise = true)
        {
            if (!(current.Value is LayoutSelectionRow))
                current.Value = item;
            int i = _items.BinarySearch(item);
            if (i < 0)
                _items.Insert(-i - 1, item);
            else
                _items[i] = item;
            if (raise)
                OnSelectionChanged(LayoutSelectionChange.Add, item);
        }

        public LayoutSelectionRow Add(object value, int index, bool raise = true)
        {
            var item = new LayoutSelectionRow() { Item = value, Index = index };
            Add(item, raise);
            return item;
        }

        public void AddRange(IEnumerable items)
        {
            foreach (object item in items)
                Add(item, List.ListSource.IndexOf(item), false);
            OnSelectionChanged(LayoutSelectionChange.Reset);
        }

        public void Remove(LayoutSelectionRow item, bool raise = true)
        {
            if (CurrentRow != null && item.Index == CurrentRow.Index)
                current.Value = null;
            _items.Remove(item);
            if (raise)
                OnSelectionChanged(LayoutSelectionChange.Remove, item);
        }

        public void RemoveBy(int index)
        {
            int i = 0;
            var item = GetItem(index, out i);
            if (item != null)
            {
                if (CurrentRow != null && item.Index == CurrentRow.Index)
                    current.Value = null;
                _items.RemoveAt(i);
                OnSelectionChanged(LayoutSelectionChange.Remove, item);
            }
        }

        public void RemoveBy(object value)
        {
            var item = GetItem(value);
            if (item != null)
                Remove(item);
        }

        public LayoutSelectionRow this[int index]
        {
            get { return _items[index]; }
        }

        public LayoutSelectionRow GetItem(int index, out int i)
        {
            search.Index = index;
            i = _items.BinarySearch(search);
            return i < 0 ? null : _items[i];
        }

        public LayoutSelectionRow GetItem(object value)
        {
            foreach (var item in _items)
                if (item.Item == value)
                    return item;
            return null;
        }

        public bool Contains(int index)
        {
            if (current.Mode == LayoutSelectionMode.Row && index == ((LayoutSelectionRow)current.Value).Index)
                return true;
            else
            {
                int i;
                return GetItem(index, out i) != null;
            }
        }

        public bool Contains(object value)
        {
            if (current.Mode == LayoutSelectionMode.Row && value == ((LayoutSelectionRow)current.Value).Item)
                return true;
            else
                return GetItem(value) != null;
        }

        public void _Clear()
        {
            current.Value = null;
            hover.Value = null;
            _items.Clear();
        }

        public void Clear()
        {
            if (_items.Count > 0)
            {
                _Clear();
                OnSelectionChanged(LayoutSelectionChange.Reset);
            }
        }

        private void RemoveItem(int i, LayoutSelectionRow item)
        {
            if (current.Value is LayoutSelectionRow && ((LayoutSelectionRow)current.Value).Index == item.Index)
                current.Value = null;
            _items.RemoveAt(i);
        }

        public void RefreshIndex(bool raise)
        {
            if (_items.Count == 0 || List.ListSource == null)
                return;
            bool flag = false;
            for (int i = 0; i < _items.Count;)
            {
                var item = _items[i];
                if (item.Index >= 0 && List.ListSource.Count > item.Index)
                {
                    if (List.ListSource[item.Index] != item.Item)
                    {
                        flag = true;
                        var index = List.ListSource.IndexOf(item.Item);
                        if (index < 0)
                        {
                            RemoveItem(i, item);
                            continue;
                        }
                        else
                        {
                            item.Index = index;
                        }
                    }
                }
                else
                {
                    flag = true;
                    RemoveItem(i, item);
                    continue;
                }
                i++;
            }
            if (flag)
            {
                hover.Value = null;
                _items.Sort();
                if (raise)
                    OnSelectionChanged(LayoutSelectionChange.Reset);
            }
        }

        public List<T> GetItems<T>()
        {
            List<T> list = new List<T>(_items.Count);
            foreach (var item in _items)
                if (item.Item is T)
                    list.Add((T)item.Item);
            return list;
        }

        public IEnumerator<LayoutSelectionRow> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public ILayoutList List { get; set; }

        public LayoutColumn HoverColumn
        {
            get { return hover.Value as LayoutColumn; }
            set { SetHover(value); }
        }

        public LayoutColumn CurrentColumn
        {
            get { return current.Value as LayoutColumn; }
            set { SetCurrent(value); }
        }
    }
}
