using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public static class GroupHelper
    {
        public static void ApplyFilter(IFilterable filterable)
        {
            filterable.FilterQuery.Parameters.Add(TreeInvoker.Instance.CreateParameter(null));
            filterable.FilterQuery.Orders.Add(TreeInvoker.Instance.CreateComparer(null));
        }

        public static void ApplyFilter<T>(IFilterable<T> filterable) where T : IGroup
        {
            filterable.FilterQuery.Parameters.Add(TreeInvoker<T>.Instance.CreateParameter<T>());
            filterable.FilterQuery.Orders.Add(TreeInvoker<T>.Instance.CreateComparer<T>());
        }

        public static bool IsExpand(IGroup item)
        {
            if (item.Group == null)
                return true;
            if (!item.Group.Expand)
                return false;
            return IsExpand(item.Group);
        }

        public static void ExpandAll(IGroup item, bool expand)
        {
            IGroup g = item.Group;
            while (g != null && g != g.Group)
            {
                g.Expand = expand;
                g = g.Group;
            }
        }

        public static bool GetAllParentExpand(IGroup item)
        {
            IGroup g = item.Group;
            while (g != null && g != g.Group)
            {
                if (!g.Expand)
                    return false;
                g = g.Group;
            }
            return true;
        }

        public static bool IsParent(IGroup item, IGroup parent)
        {
            IGroup g = item.Group;
            while (g != null && g != g.Group)
            {
                if (g == parent)
                    return true;
                g = g.Group;
            }
            return false;
        }

        public static IEnumerable<T> GetAllParent<T>(IGroup item, bool addSender = false)
        {
            if (addSender)
                yield return (T)item;
            IGroup g = item.Group;
            while (g != null && g != g.Group)
            {
                yield return (T)g;
                g = g.Group;
            }
        }

        public static string GetFullName(IGroup item, string separator)
        {
            return GetFullName(item, separator, "ToString");
        }

        public static string GetFullName(IGroup item, string separator, string member)
        {
            var invoker = EmitInvoker.Initialize(item.GetType(), member);
            string rez = "";
            IGroup g = item;
            while (g != null && g != g.Group)
            {
                object val = invoker.GetValue(g);
                if (val != null)
                    rez = val + separator + rez;
                g = g.Group;
            }
            return rez.Substring(0, rez.Length - separator.Length);
        }

        public static IGroup TopGroup(IGroup item)
        {
            IGroup g = item;
            while (g.Group != null && g != g.Group)
            {
                g = g.Group;
            }
            return g ?? item;
        }

        public static IGroup TopGroup(IGroup item, out int level)
        {
            level = 0;
            IGroup g = item;
            while (g.Group != null && g != g.Group)
            {
                level++;
                g = g.Group;
            }
            return g ?? item;
        }

        public static int Level(IGroup item)
        {
            int level = 0;
            IGroup g = item;
            while (g.Group != null && g != g.Group)
            {
                level++;
                g = g.Group;
            }
            return level;
        }

        public static IEnumerable<T> Sort<T>(IEnumerable<T> items) where T : IGroup
        {
            var list = items.ToList();
            list.Sort(Compare);
            return list;
        }

        public static int Compare(object x, object y)
        {
            return Compare(x as IGroup, y as IGroup, null);
        }

        public static int Compare<T>(T x, T y) where T : IGroup
        {
            return Compare(x, y, null);
        }

        public static int Compare(IGroup x, IGroup y)
        {
            return Compare(x, y, null);
        }

        public static int Compare(IGroup x, IGroup y, IComparer comp)
        {
            if (x == null)
                return (y == null) ? 0 : -1;
            if (y == null)
                return 1;
            if (x == y)
                return 0;

            IGroup ox = TopGroup(x, out int xLevel), oy = TopGroup(y, out int yLevel);

            if (ox == oy)
            {
                ox = x; oy = y;
                int oxLevel = xLevel, oyLevel = yLevel;

                while (ox.Group != oy.Group)
                {
                    if (oxLevel > oyLevel)
                    {
                        ox = ox.Group;
                        oxLevel--;
                    }
                    else if (oxLevel < oyLevel)
                    {
                        oy = oy.Group;
                        oyLevel--;
                    }
                    else
                    {
                        ox = ox.Group; oy = oy.Group;
                        oxLevel--; oyLevel--;
                    }
                }
                if (ox == oy)
                {
                    return xLevel.CompareTo(yLevel);
                }
            }

            return ListHelper.Compare(ox, oy, comp);
        }

        public static IEnumerable<IGroup> GetSubGroups(IGroup group, bool addSender = false)
        {
            if (addSender)
                yield return group;
            foreach (var item in group.GetGroups())
            {
                if (item == group)
                {
                    continue;
                }

                yield return item;
                foreach (var subItem in GetSubGroups(item))
                {
                    yield return subItem;
                }
            }
        }


    }

}

