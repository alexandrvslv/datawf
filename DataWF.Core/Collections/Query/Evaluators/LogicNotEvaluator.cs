using System.Collections.Generic;
using System.Linq;
using DataWF.Comparers;

namespace DataWF.Collections.Query.Evaluators;

public class LogicNotEvaluator : BaseCompareEvaluator<bool>
{
    public static readonly LogicNotEvaluator Instance = new();

    public override bool Eval(bool param1, IUniversalComparer<bool> comparer) => !param1;
    
    public override bool Eval(bool param1, bool param2, IUniversalComparer<bool> comparer) => !param1;

    public override bool Eval(bool param1, object param2, IUniversalComparer<bool> comparer) => !param1;

    public IEnumerable<T> Eval<T>(IEnumerable<T> items, IQParameter lparam, IQParameter rparam) => items.Except(lparam.Filter<T>(items));

    public override IFilteredCollection<T> Eval<T>(Indexes.IIndexGetter<T, bool> param1, bool param2) => throw new System.NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.IIndexGetter<T, bool> param1, object param2) => throw new System.NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.InvokerIndex<T, bool> param1, bool param2) => throw new System.NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.InvokerIndex<T, bool> param1, object param2) => throw new System.NotImplementedException();
}

