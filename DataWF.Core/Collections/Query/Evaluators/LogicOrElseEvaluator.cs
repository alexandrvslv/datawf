using System;
using System.Collections.Generic;
using System.Linq;
using DataWF.Comparers;

namespace DataWF.Collections.Query.Evaluators;

public class LogicOrElseEvaluator : BaseCompareEvaluator<bool>
{
    public static readonly LogicOrElseEvaluator Instance = new();

    public override bool Eval(bool param1, IUniversalComparer<bool> comparer) => throw new NotSupportedException();
    
    public override bool Eval(bool param1, bool param2, IUniversalComparer<bool> comparer) => param1 || param2;

    public override bool Eval(bool param1, object param2, IUniversalComparer<bool> comparer) => param1 || System.Convert.ToBoolean(param2);

    public IEnumerable<T> Eval<T>(IEnumerable<T> lparam, IEnumerable<T> rparam) => lparam.Union(rparam);

    public IEnumerable<T> Eval<T>(IEnumerable<T> items, IQParameter lparam, IQParameter rparam) => lparam.Filter(items).Union(rparam.Filter<T>(items));

    public override IFilteredCollection<T> Eval<T>(Indexes.IIndexGetter<T, bool> param1, bool param2) => throw new NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.IIndexGetter<T, bool> param1, object param2) => throw new NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.InvokerIndex<T, bool> param1, bool param2) => throw new NotImplementedException();

    public override IFilteredCollection<T> Eval<T>(Indexes.InvokerIndex<T, bool> param1, object param2) => throw new NotImplementedException();
}
