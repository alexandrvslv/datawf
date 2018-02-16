using System;

namespace DataWF.Common
{
    public struct CompareType
    {
        public static CompareType Undefined = new CompareType(CompareTypes.Undefined);
        public static CompareType Like = new CompareType(CompareTypes.Like);
        public static CompareType NotLike = new CompareType(CompareTypes.Like, true);
        public static CompareType Equal = new CompareType(CompareTypes.Equal);
        public static CompareType NotEqual = new CompareType(CompareTypes.Equal, true);
        public static CompareType In = new CompareType(CompareTypes.In);
        public static CompareType NotIn = new CompareType(CompareTypes.In, true);
        public static CompareType Is = new CompareType(CompareTypes.Is);
        public static CompareType IsNot = new CompareType(CompareTypes.Is, true);
        public static CompareType GreaterOrEqual = new CompareType(CompareTypes.GreaterOrEqual);
        public static CompareType Greater = new CompareType(CompareTypes.Greater);
        public static CompareType LessOrEqual = new CompareType(CompareTypes.LessOrEqual);
        public static CompareType Less = new CompareType(CompareTypes.Less);
        public static CompareType Between = new CompareType(CompareTypes.Between);
        public static CompareType NotBetween = new CompareType(CompareTypes.Between, true);

        public static bool operator ==(CompareType typeLeft, CompareType typeRight)
        {
            return typeLeft.Equals(typeRight);
        }

        public static bool operator !=(CompareType typeLeft, CompareType typeRight)
        {
            return !typeLeft.Equals(typeRight);
        }

        public CompareTypes Type;
        public bool Not;


        public CompareType(CompareTypes type = CompareTypes.Undefined, bool not = false)
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
            return obj is CompareType && Equals((CompareType)obj);
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

    }
}
