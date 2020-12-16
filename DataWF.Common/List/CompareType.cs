using System;

namespace DataWF.Common
{

    public readonly struct CompareType
    {
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
                case (CompareTypes.Equal):
                    return Not ? "!=" : "=";
                case (CompareTypes.In):
                    return Not ? "not in" : "in";
                case (CompareTypes.Is):
                    return Not ? "is not" : "is";
                case (CompareTypes.Like):
                    return Not ? "not like" : "like";
                case (CompareTypes.Between):
                    return Not ? "not between" : "between";
                case (CompareTypes.Greater):
                    return ">";
                case (CompareTypes.GreaterOrEqual):
                    return ">=";
                case (CompareTypes.Less):
                    return "<";
                case (CompareTypes.LessOrEqual):
                    return "<=";
                case (CompareTypes.As):
                    return "as";
                case (CompareTypes.Using):
                    return "using";
                case (CompareTypes.Distinct):
                    return "distinct";
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

        public static CompareTypes Parse(string code)
        {
            CompareTypes type = CompareTypes.Undefined;
            if (code.Equals("=", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Equal;
            else if (code.Equals(">", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Greater;
            else if (code.Equals(">=", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.GreaterOrEqual;
            else if (code.Equals("<", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Less;
            else if (code.Equals("<=", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.LessOrEqual;
            else if (code.Equals("in", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.In;
            else if (code.Equals("like", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Like;
            else if (code.Equals("between", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Between;
            else if (code.Equals("is", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Is;
            else if (code.Equals("in", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.In;
            else if (code.Equals("as", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.As;
            else if (code.Equals("using", StringComparison.OrdinalIgnoreCase))
                type = CompareTypes.Using;
            return type;
        }

        public static CompareType ParseFull(string value)
        {
            var compareSplit = value.Trim().Split(' ');
            if (compareSplit.Length > 1)
            {
                var compareValue = string.Equals(compareSplit[0], "not", StringComparison.OrdinalIgnoreCase) ? compareSplit[1] : compareSplit[0];
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
