using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Xwt.Drawing;
using Xwt;
using System.Globalization;
using DataWF.Gui;

namespace DataWF.TestGui
{
    public class TestResult
    {
        public TestResult(string group, string name, TimeSpan span)
        {
            Group = group;
            Name = name;
            Span = span;
        }

        public string Group { get; set; }

        public string Name { get; set; }

        public TimeSpan Span { get; set; }

        public static SelectableList<TestResult> Test(int c = 100000)
        {
            var list = new SelectableList<TestResult>();

            var test = new TestClass();
            var aTest = test as TestAbstract;
            var iTest = test as IOrder;
            var pOrder = test.GetType().GetProperty("Order");
            var pItem = test.GetType().GetProperty("Item", new Type[] { typeof(string) });
            var cDefault = typeof(TestClass).GetConstructor(Type.EmptyTypes);
            var cParam = typeof(TestClass).GetConstructor(new Type[] { typeof(int), typeof(string) });

            var aOrder = EmitInvoker.Initialize(pOrder);
            var aItem = EmitInvoker.Initialize(pItem, "fdsfds");
            var aDefault = EmitInvoker.Initialize(cDefault);
            var aParam = EmitInvoker.Initialize(cParam);

            var param = new object[] { "cfsdf" };
            var paramDefault = new object[] { };
            var paramParam = new object[] { 12, "1dasdas" };

            var actionBinder = new Invoker<TestClass, int>(
                nameof(TestClass.Order),
                (item) => item.Order,
                (item, value) => item.Order = value);


            int val;
            string sval;
            object oval;
            Stopwatch watch = new Stopwatch();

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                val = actionBinder.GetValue(test);
                //tc.Order = val;
            }
            watch.Stop();
            list.Add(new TestResult("Property Get", "Action", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                val = test.Order;
                //tc.Order = val;
            }
            watch.Stop();
            list.Add(new TestResult("Property Get", "Direct", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                val = aTest.Order;
                //tc.Order = val;
            }
            watch.Stop();
            list.Add(new TestResult("Property Get", "Abstract", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                val = iTest.Order;
                //tc.Order = val;
            }
            watch.Stop();
            list.Add(new TestResult("Property Get", "Interface", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                oval = pOrder.GetValue(test, null);
            }
            watch.Stop();
            list.Add(new TestResult("Property Get", "Reflection", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                val = (int)pOrder.GetValue(test, null);
            }
            watch.Stop();
            list.Add(new TestResult("Property Get", "Reflection UNBOX", watch.Elapsed));


            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                oval = aOrder.GetValue(test);
            }
            watch.Stop();
            list.Add(new TestResult("Property Get", "Emit Invoke", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                val = (int)aOrder.GetValue(test);
            }
            watch.Stop();
            list.Add(new TestResult("Property Get", "Emit Invoke UNBOX", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                sval = test["fsdfdsf"];
            }
            watch.Stop();
            list.Add(new TestResult("Property Index", "Direct", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                oval = pItem.GetValue(test, param);
            }
            watch.Stop();
            list.Add(new TestResult("Property Index", "Reflection", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                sval = (string)pItem.GetValue(test, param);
            }
            watch.Stop();
            list.Add(new TestResult("Property Index", "Reflection UNBOX", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                oval = aItem.GetValue(test);
            }
            watch.Stop();
            list.Add(new TestResult("Property Index", "Emit Invoke", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                sval = (string)aItem.GetValue(test);
            }
            watch.Stop();
            list.Add(new TestResult("Property Index", "Emit Invoke UNBOX", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                test = new TestClass();
            }
            watch.Stop();
            list.Add(new TestResult("Constructor", "Direct", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                test = (TestClass)cDefault.Invoke(paramDefault);
            }
            watch.Stop();
            list.Add(new TestResult("Constructor", "Reflection UNBOX", watch.Elapsed));

            watch.Reset();
            watch.Start();
            //object[] obj = new object[] { };
            for (int i = 0; i <= c; i++)
            {
                test = (TestClass)aDefault.Create(paramDefault);
            }
            watch.Stop();
            list.Add(new TestResult("Constructor", "Emit Invoke UNBOX", watch.Elapsed));


            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                test = new TestClass(12, "dsadas");
            }
            watch.Stop();
            list.Add(new TestResult("Constructor Params", "Direct", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                oval = cParam.Invoke(paramParam);
            }
            watch.Stop();
            list.Add(new TestResult("Constructor Params", "Reflection", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                test = (TestClass)cParam.Invoke(paramParam);
            }
            watch.Stop();
            list.Add(new TestResult("Constructor Params", "Reflection UNBOX", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                oval = aParam.Create(paramParam);
            }
            watch.Stop();
            list.Add(new TestResult("Constructor Params", "Emit Invoke", watch.Elapsed));

            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                test = (TestClass)aParam.Create(paramParam);
            }
            watch.Stop();
            list.Add(new TestResult("Constructor Params", "Emit Invoke UNBOX", watch.Elapsed));

            TestClass p1 = new TestClass(123112365, "test string compa3rision");
            TestClass p2 = new TestClass(124312312, "test string comp4arision");

            //Compare string
            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                p1.Comment.CompareTo(p2.Comment);
            }
            watch.Stop();
            list.Add(new TestResult("Compare String", "Direct", watch.Elapsed));

            //Compare string Invariant
            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                CultureInfo.InvariantCulture.CompareInfo.Compare(p1.Comment, p2.Comment, CompareOptions.Ordinal);
            }
            watch.Stop();
            list.Add(new TestResult("Compare String", "Direct Invariant", watch.Elapsed));

            //Compare Accessor string
            watch.Reset();
            watch.Start();
            var ce = new InvokerComparer(EmitInvoker.Initialize(typeof(TestClass).GetProperty("Comment")), ListSortDirection.Ascending);
            for (int i = 0; i <= c; i++)
            {
                ce.Compare(p1, p2);
            }
            watch.Stop();
            list.Add(new TestResult("Compare String", "Emit Invoke Property", watch.Elapsed));

            //Compare integer
            watch.Reset();
            watch.Start();
            for (int i = 0; i <= c; i++)
            {
                p1.Order.CompareTo(p2.Order);
            }
            watch.Stop();
            list.Add(new TestResult("Compare Int", "Direct", watch.Elapsed));

            //Compare Accessor Int
            watch.Reset();
            watch.Start();
            ce = new InvokerComparer(EmitInvoker.Initialize(typeof(TestClass).GetProperty("Order")), ListSortDirection.Ascending);
            for (int i = 0; i <= c; i++)
            {
                ce.Compare(p1, p2);
            }
            watch.Stop();
            list.Add(new TestResult("Compare Int", "Emit Invoke Property", watch.Elapsed));


            return list;
        }
    }

    public enum TestEnum
    {
        Value1,
        Value2,
        Value3,
        Value4
    }

    [Flags]
    public enum TestFlag
    {
        Flag0 = 0,
        Flag1 = 1,
        Flag2 = 2,
        Flag3 = 4,
        Flag4 = 8
    }

    public interface IOrder
    {
        int Order { get; set; }
    }

    public abstract class TestAbstract
    {
        public abstract int Order { get; set; }
    }

    public class TestClass : TestAbstract, IGlyph, IGroup, INotifyPropertyChanged, IOrder
    {
        public static SelectableList<TestClass> Generate(int count)
        {
            SelectableList<TestClass> list = new SelectableList<TestClass>();
            list.Capacity = count;
            Random rand = new Random();
            for (int i = 0; i < count; i++)
            {
                TestClass tc = new TestClass(i, "TestClass" + i);
                if (Locale.Instance.Images.Count > 0)
                    tc.Image = Locale.Instance.Images[i % Locale.Instance.Images.Count].Image as Image;
                tc.Enum = (TestEnum)(i % 4);
                tc.Flag = (TestFlag)(i % 15);
                if (i > 10)
                {
                    tc.Group = list[rand.Next(0, i - 1)];
                    ((TestClass)tc.Group).List.Add(tc);
                }

                list.Add(tc);
            }
            return list;
        }

        private int _order = 0;
        private decimal _price = 0;
        private TestEnum _enum = TestEnum.Value1;
        private TestFlag _flag = TestFlag.Flag1;
        private DateTime _date = DateTime.Now;
        private string _comment;
        private LocaleString _name = new LocaleString();
        private Size _size;
        private List<TestClass> _list = new List<TestClass>();
        private bool _expand;
        private IGroup _group;
        private Image image;

        public TestClass()
        {
        }

        public TestClass(int order, string name)
        {
            _order = order;
            _name.Value = name;
            _comment = name;
        }

        #region IPicture implementation

        public Image Image
        {
            get { return image; }
            set
            {
                image = value;
                OnPropertyChanged(nameof(Image));
            }
        }

        [Browsable(false)]
        public GlyphType Glyph
        {
            get { return GlyphType.None; }
            set { }
        }

        #endregion

        [Category("Value Type")]
        public override int Order
        {
            get { return _order; }
            set
            {
                _order = value;
                OnPropertyChanged(nameof(Order));
            }
        }

        [Category("Value Type")]
        public decimal Price
        {
            get { return _price; }
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
            }
        }

        [Category("Value Type")]
        public TestEnum Enum
        {
            get { return _enum; }
            set
            {
                _enum = value;
                OnPropertyChanged(nameof(Enum));
            }
        }

        [Category("Value Type")]
        public TestFlag Flag
        {
            get { return _flag; }
            set
            {
                _flag = value;
                OnPropertyChanged(nameof(Flag));
            }
        }

        [Category("Value Type")]
        public DateTime Date
        {
            get { return _date; }
            set
            {
                _date = value;
                OnPropertyChanged(nameof(Date));
            }
        }

        [Category("Value Type")]
        public string Comment
        {
            get { return _comment; }
            set
            {
                _comment = value;
                OnPropertyChanged(nameof(Comment));
            }
        }

        [Category("Compaund")]
        public LocaleString Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        [Category("Compaund")]
        public Size Size
        {
            get { return _size; }
            set
            {
                _size = value;
                OnPropertyChanged(nameof(Size));
            }
        }

        [Category("Compaund")]
        public List<TestClass> List
        {
            get { return _list; }
        }

        public string this[string index]
        {
            get { return index; }
            set { }
        }

        public override string ToString()
        {
            return _name.Value;
        }

        #region IGroupable implementation

        [Browsable(false), Category("Tree")]
        public bool IsExpanded
        {
            get { return GroupHelper.IsExpand(this); }
        }

        [Category("Tree")]
        public IGroup Group
        {
            get { return _group; }
            set
            {
                _group = value;
                OnPropertyChanged("Group");
            }
        }

        [Category("Tree")]
        public bool Expand
        {
            get { return _expand; }
            set
            {
                _expand = value;
                OnPropertyChanged("Expand");
            }
        }

        [Category("Tree")]
        public bool IsCompaund
        {
            get { return true; }
        }

        #endregion

        public int CompareTo(object obj)
        {
            return _order.CompareTo(((TestClass)obj)._order);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public IEnumerable<IGroup> GetGroups()
        {
            throw new NotImplementedException();
        }
    }


}

