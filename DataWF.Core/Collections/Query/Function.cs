//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Collections.Query.Evaluators;

public readonly struct Function
{
    public static readonly Function Convert = new(FunctionType.Convert, new FunctionConvertEvaluator());
    public static readonly Function Coalesce = new(FunctionType.Coalesce, new FunctionCoalesceEvaluator());
    public static readonly Function Concat = new(FunctionType.Concat, new FunctionConcatEvaluator());
    public static readonly Function ConcatWS = new(FunctionType.Concat_WS, new FunctionConcatWSEvaluator());
    public static readonly Function Initcap = new(FunctionType.Initcap, new FunctionInitcapEvaluator());
    public static readonly Function Trim = new(FunctionType.Trim, new FunctionTrimEvaluator());
    public static readonly Function RTrim = new(FunctionType.RTrim, new FunctionRTrimEvaluator());
    public static readonly Function LTrim = new(FunctionType.LTrim, new FunctionLTrimEvaluator());
    public static readonly Function Upper = new(FunctionType.Upper, new FunctionUpperEvaluator());
    public static readonly Function Lower = new(FunctionType.Lower, new FunctionLowerEvaluator());
    public static readonly Function Length = new(FunctionType.Length, new FunctionLengthEvaluator());
    public static readonly Function Strpos = new(FunctionType.Strpos, new FunctionStrposEvaluator());
    public static readonly Function Substr = new(FunctionType.Substr, new FunctionSubstrEvaluator());
    public static readonly Function GetDate = new(FunctionType.GetDate, new FunctionGetDateEvaluator());
    public static readonly Function Abs = new(FunctionType.Abs, new MathDecimalAbsEvaluator());
    public static readonly Function Power = new(FunctionType.Power, new MathDecimalPowerEvaluator());
    public static readonly Function Sign = new(FunctionType.Sign, new MathDecimalSignEvaluator());

    public Function(FunctionType type, IEvaluator evaluator)
    {
        Type = type;
        Evaluator = evaluator;
    }

    public FunctionType Type { get; }

    public IEvaluator Evaluator { get; }

    public object Evaluate(IEnumerable<object> param) => Evaluator.Eval(param);

    public static Function Fabrica(FunctionType type) => type switch
    {
        FunctionType.Coalesce => Coalesce,
        FunctionType.Concat => Concat,
        FunctionType.Concat_WS => ConcatWS,
        FunctionType.Initcap => Initcap,
        FunctionType.Trim => Trim,
        FunctionType.RTrim => RTrim,
        FunctionType.LTrim => LTrim,
        FunctionType.Length => Length,
        FunctionType.Upper => Upper,
        FunctionType.Lower => Lower,
        FunctionType.Strpos => Strpos,
        FunctionType.Substr => Substr,
        FunctionType.Convert => Convert,
        FunctionType.Cast => Convert,
        FunctionType.Group => throw new NotImplementedException(),
        FunctionType.To_Char => throw new NotImplementedException(),
        FunctionType.To_Date => throw new NotImplementedException(),
        FunctionType.GetDate => GetDate,
        FunctionType.DateName => throw new NotImplementedException(),
        FunctionType.Format => throw new NotImplementedException(),
        FunctionType.parse => throw new NotImplementedException(),
        FunctionType.None => throw new NotSupportedException(),

    };
    public static FunctionType Parse(string val)
    {
        if (!Enum.TryParse<FunctionType>(val, true, out FunctionType f))
            f = FunctionType.None;
        return f;
    }

    public static FunctionType Parse(ReadOnlySpan<char> val)
    {
        if (MemoryExtensions.Equals(val, nameof(FunctionType.Cast).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.Cast;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.Coalesce).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.Coalesce;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.Concat).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.Concat;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.Convert).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.Convert;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.DateName).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.DateName;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.Format).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.Format;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.GetDate).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.GetDate;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.Group).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.Group;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.Initcap).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.Initcap;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.Lower).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.Lower;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.LTrim).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.LTrim;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.parse).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.parse;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.RTrim).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.RTrim;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.To_Char).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.To_Char;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.To_Date).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.To_Date;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.Trim).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.Trim;
        else if (MemoryExtensions.Equals(val, nameof(FunctionType.Upper).AsSpan(), StringComparison.OrdinalIgnoreCase))
            return FunctionType.Upper;

        return FunctionType.None;

    }
}
