using System.Collections.Generic;
using DataWF.Comparers;

namespace DataWF.Collections.Query.Evaluators;

public class LogicAndAlsoEvaluator : BaseCompareEvaluator<bool>
{
    public static readonly LogicAndAlsoEvaluator Instance = new();

    public override bool Eval(bool param1, IUniversalComparer<bool> comparer) => throw new System.NotSupportedException();

    public override bool Eval(bool param1, bool param2, IUniversalComparer<bool> comparer) => param1 && param2;

    public override bool Eval(bool param1, object param2, IUniversalComparer<bool> comparer) => param1 && System.Convert.ToBoolean(param2);

    public override IFilteredCollection<T> Eval<T>(Indexes.IIndexGetter<T, bool> param1, bool param2) => throw new System.NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.IIndexGetter<T, bool> param1, object param2) => throw new System.NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.InvokerIndex<T, bool> param1, bool param2) => throw new System.NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.InvokerIndex<T, bool> param1, object param2) => throw new System.NotImplementedException();

    public IEnumerable<T> Eval<T>(IEnumerable<T> items, IQParameter lparam, IQParameter rparam) =>
        //TODO one more abstraction для приориетизации
        rparam.Filter(lparam.Filter<T>(items));


}

