using DataWF.Common;
using DataWF.Gui;
using System.Collections.Generic;

namespace DataWF.Data.Gui
{
    public class QueryResultList : LayoutList
    {
        private QResult view = null;

        public QResult Query
        {
            get { return view; }
            set
            {
                if (view != value)
                {
                    view = value;
                    ListSource = view.Values.DefaultView;
                }
            }
        }

        protected override string GetCacheKey()
        {
            return view != null && view.Name != null ? view.Name : base.GetCacheKey();
        }

        protected override void OnGetProperties(LayoutListPropertiesArgs args)
        {
            if (view != null && args.Cell == null)
            {
                args.Properties = new List<string>();
                foreach (var item in view.Columns)
                {
                    args.Properties.Add(item.Key);
                }
            }
            else
            {
                base.OnGetProperties(args);
            }
        }

        public override LayoutColumn CreateColumn(string name)
        {
            if (view != null)
            {
                if (view.Columns.TryGetValue(name, out QField index))
                {
                    return new LayoutColumn()
                    {
                        Name = name,
                        Invoker = index
                    };
                }
            }
            return base.CreateColumn(name);
        }

    }
}
