using System.Collections.Generic;
using System.Linq;
using DataWF.Comparers;

namespace DataWF.Collections.Query.Evaluators;

public class LogicOrEvaluator : BaseCompareEvaluator<bool>
{
    public static readonly LogicOrEvaluator Instance = new();

    public bool Eval(bool param1, bool param2) => param1 | param2;

    public bool Eval<T>(T item, IQParameter param1, IQParameter param2) => param1.Compare(item) | param2.Compare(item);

    public bool Eval(QTuple item, IQParameter lparam, IQParameter rparam) => lparam.Compare(item) | rparam.Compare(item);

    public IEnumerable<T> Eval<T>(IEnumerable<T> lparam, IEnumerable<T> rparam) => lparam.Union(rparam);

    public IEnumerable<T> Eval<T>(IEnumerable<T> items, IQParameter lparam, IQParameter rparam)
    {
        return lparam.Filter(items).Union(rparam.Filter<T>(items));
    }

    public override bool Eval(bool param1, IUniversalComparer<bool> comparer)
    {
        throw new System.NotImplementedException();
    }

    public override bool Eval(bool param1, bool param2, IUniversalComparer<bool> comparer)
    {
        throw new System.NotImplementedException();
    }

    public override bool Eval(bool param1, object param2, IUniversalComparer<bool> comparer)
    {
        throw new System.NotImplementedException();
    }

    public override IFilteredCollection<T> Eval<T>(Indexes.IIndexGetter<T, bool> param1, bool param2)
    {
        throw new System.NotImplementedException();
    }

    public override IFilteredCollection<T> Eval<T>(Indexes.IIndexGetter<T, bool> param1, object param2)
    {
        throw new System.NotImplementedException();
    }

    public override IFilteredCollection<T> Eval<T>(Indexes.InvokerIndex<T, bool> param1, bool param2)
    {
        throw new System.NotImplementedException();
    }

    public override IFilteredCollection<T> Eval<T>(Indexes.InvokerIndex<T, bool> param1, object param2)
    {
        throw new System.NotImplementedException();
    }
}
