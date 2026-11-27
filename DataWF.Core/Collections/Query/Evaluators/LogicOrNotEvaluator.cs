using System.Collections.Generic;
using System.Linq;

namespace DataWF.Collections.Query.Evaluators
{
    public class LogicOrNotEvaluator : ILogicEvaluator
    {
        public static readonly LogicOrNotEvaluator Instance = new();

        public bool Eval(bool param1, bool param2) => param1 || !param2;

        public bool Eval<T>(T item, IQParameter param1, IQParameter param2) => param1.Compare(item) || !param2.Compare(item);

        public bool Eval(QTuple item, IQParameter lparam, IQParameter rparam) => lparam.Compare(item) || !rparam.Compare(item);

        public IEnumerable<T> Eval<T>(IEnumerable<T> items, IQParameter lparam, IQParameter rparam)
        {
            return lparam.Filter<T>(items).Union(items.Except(rparam.Filter<T>(items)));
        }
    }
}

