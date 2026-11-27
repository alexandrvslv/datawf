using System.Collections.Generic;

namespace DataWF.Collections.Query.Evaluators
{
    public interface IJoinEvaluator
    {
        IEnumerable<QTuple> Join<T1, T2>(IEnumerable<T1> leftSource, IEnumerable<T2> rightSource, IQParameter on)
        {
            foreach (var lItem in leftSource)
                foreach (var rItem in rightSource)
                {
                    var tuple = new QTuple(lItem) { Item1 = rItem };
                    if (on.Compare(tuple))
                        yield return tuple;
                }
        }
    }


}

