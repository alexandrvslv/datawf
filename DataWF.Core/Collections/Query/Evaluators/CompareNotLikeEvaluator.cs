using DataWF.Collections.Indexes;
using DataWF.Comparers;
using System;
using System.Text.RegularExpressions;

namespace DataWF.Collections.Query.Evaluators
{
    public class CompareNotLikeEvaluator<V> : BaseCompareEvaluator<V>
    {
        public static CompareNotLikeEvaluator<V> Instance = new();

        public override bool Eval(V param1, IUniversalComparer<V> comparer) => throw new NotSupportedException();

        public override bool Eval(V param1, V param2, IUniversalComparer<V> comparer) => Eval(param1, Helper.BuildLike(param2.ToString()), comparer);

        public override bool Eval(V param1, object param2, IUniversalComparer<V> comparer) => !((Regex)param2).IsMatch(param1.ToString());

        public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, V param2) => Eval(param1, Helper.BuildLike(param2.ToString()));

        public override IFilteredCollection<T> Eval<T>(IIndexGetter<T, V> param1, object param2) => param1.Select(this, param2);

        public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, V param2) => Eval(param1, Helper.BuildLike(param2.ToString()));

        public override IFilteredCollection<T> Eval<T>(InvokerIndex<T, V> param1, object param2) => param1.Select(this, param2);
    }

}

