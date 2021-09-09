using System;

namespace DataWF.Common
{

    public readonly struct CompareType
    {
        private const string StrEquals = "=";
        private const string StrNotEquals = "!=";
        private const string StrIn = "in";
        private const string StrNotIn = "not in";
        private const string StrIs = "is";
        private const string StrIsNot = "is not";
        private const string StrLike = "like";
        private const string StrNotLike = "not like";
        private const string StrBetween = "between";
        private const string StrNotBetween = "not between";
        private const string StrGreater = ">";
        private const string StrGreaterOrEqual = ">=";
        private const string StrLess = "<";
        private const string StrLessOrEqual = "<=";
        private const string StrAs = "as";
        private const string StrUsing = "using";
        private const string StrDistinct = "distinct";
        private const string StrNot = "not";
        public static readonly CompareType Undefined = new CompareType(CompareTypes.Undefined);
        public static readonly CompareType Like = new CompareType(CompareTypes.Like);
        public static readonly CompareType NotLike = new CompareType(CompareTypes.Like, true);
        public static readonly CompareType Equal = new CompareType(CompareTypes.Equal);
        public static readonly CompareType NotEqual = new CompareType(CompareTypes.Equal, true);
        public static readonly CompareType In = new CompareType(CompareTypes.In);
        public static readonly CompareType NotIn = new CompareType(CompareTypes.In, true);
        public static readonly CompareType Is = new CompareType(CompareTypes.Is);
        public static readonly CompareType IsNot = new CompareType(CompareTypes.Is, true);
        public static readonly CompareType GreaterOrEqual = new CompareType(CompareTypes.GreaterOrEqual);
        public static readonly CompareType Greater = new CompareType(CompareTypes.Greater);
        public static readonly CompareType LessOrEqual = new CompareType(CompareTypes.LessOrEqual);
        public static readonly CompareType Less = new CompareType(CompareTypes.Less);
        public static readonly CompareType Between = new CompareType(CompareTypes.Between);
        public static readonly CompareType NotBetween = new CompareType(CompareTypes.Between, true);
        public static readonly CompareType Contains = new CompareType(CompareTypes.Contains, false);
        public static readonly CompareType NotContains = new CompareType(CompareTypes.Contains, true);
        public static readonly CompareType Intersect = new CompareType(CompareTypes.Intersect, false);
        public static readonly CompareType NotIntersect = new CompareType(CompareTypes.Intersect, true);
        public static readonly CompareType Distinct = new CompareType(CompareTypes.Distinct);

        public static bool operator ==(CompareType typeLeft, CompareType typeRight)
        {
            return typeLeft.Equals(typeRight);
        }

        public static bool operator !=(CompareType typeLeft, CompareType typeRight)
        {
            return !typeLeft.Equals(typeRight);
        }

        public readonly CompareTypes Type;
        public readonly bool Not;

        public CompareType(CompareTypes type, bool not = false)
        {
            Type = type;
            Not = not;
        }

        public string Format()
        {
            return Format(Type, Not);
        }

        public static string Format(CompareTypes type, bool Not = false)
        {
            switch (type)
            {
                case CompareTypes.Equal:
                    return Not ? StrNotEquals : StrEquals;
                case CompareTypes.In:
                    return Not ? StrNotIn : StrIn;
                case CompareTypes.Is:
                    return Not ? StrIsNot : StrIs;
                case CompareTypes.Like:
                    return Not ? StrNotLike : StrLike;
                case CompareTypes.Between:
                    return Not ? StrNotBetween : StrBetween;
                case CompareTypes.Greater:
                    return StrGreater;
                case CompareTypes.GreaterOrEqual:
                    return StrGreaterOrEqual;
                case CompareTypes.Less:
                    return StrLess;
                case (CompareTypes.LessOrEqual):
                    return StrLessOrEqual;
                case CompareTypes.As:
                    return StrAs;
                case CompareTypes.Using:
                    return StrUsing;
                case CompareTypes.Distinct:
                    return StrDistinct;
            }
            return "";
        }

        public override string ToString()
        {
            return Locale.Get("CompareType", Format());
        }

        public bool Equals(CompareType obj)
        {
            return Not == obj.Not && Type == obj.Type;
        }

        public override bool Equals(object obj)
        {
            return obj is CompareType compare && Equals(compare);
        }

        public override int GetHashCode()
        {
            return Not.GetHashCode() ^ Type.GetHashCode();
        }

        public static CompareTypes Parse(ReadOnlySpan<char> code)
        {
            CompareTypes type = CompareTypes.Undefined;
            if (MemoryExtensions.Equals(code, StrEquals.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Equal;
            else if (MemoryExtensions.Equals(code, StrGreater.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Greater;
            else if (MemoryExtensions.Equals(code, StrGreaterOrEqual.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.GreaterOrEqual;
            else if (MemoryExtensions.Equals(code, StrLess.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Less;
            else if (MemoryExtensions.Equals(code, StrLessOrEqual.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.LessOrEqual;
            else if (MemoryExtensions.Equals(code, StrIn.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.In;
            else if (MemoryExtensions.Equals(code, StrLike.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Like;
            else if (MemoryExtensions.Equals(code, StrBetween.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Between;
            else if (MemoryExtensions.Equals(code, StrIs.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Is;
            else if (MemoryExtensions.Equals(code, StrIn.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.In;
            else if (MemoryExtensions.Equals(code, StrAs.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.As;
            else if (MemoryExtensions.Equals(code, StrUsing.AsSpan(), StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Using;
            return type;
        }

        public static CompareTypes Parse(string code)
        {
            CompareTypes type = CompareTypes.Undefined;
            if (string.Equals(code, StrEquals, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Equal;
            else if (string.Equals(code, StrGreater, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Greater;
            else if (string.Equals(code, StrGreaterOrEqual, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.GreaterOrEqual;
            else if (string.Equals(code, StrLess, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Less;
            else if (string.Equals(code, StrLessOrEqual, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.LessOrEqual;
            else if (string.Equals(code, StrIn, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.In;
            else if (string.Equals(code, StrLike, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Like;
            else if (string.Equals(code, StrBetween, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Between;
            else if (string.Equals(code, StrIs, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Is;
            else if (string.Equals(code, StrIn, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.In;
            else if (string.Equals(code, StrAs, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.As;
            else if (string.Equals(code, StrUsing, StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Using;
            return type;
        }

        public static CompareType ParseFull(string value)
        {
            var compareSplit = value.Trim().Split(' ');
            if (compareSplit.Length > 1)
            {
                var compareValue = string.Equals(compareSplit[0], StrNot, StringComparison.OrdinalIgnoreCase) ? compareSplit[1] : compareSplit[0];
                return new CompareType(Parse(compareValue), true);
            }
            else if (compareSplit[0][0] == '!')
            {
                return new CompareType(Parse(compareSplit[0].Substring(1)), true);
            }

            return new CompareType(Parse(compareSplit[0]));
        }
    }
}
