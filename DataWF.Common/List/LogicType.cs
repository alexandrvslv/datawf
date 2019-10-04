using System;

namespace DataWF.Common
{
    public struct LogicType
    {
        public static readonly LogicType Undefined = new LogicType(LogicTypes.Undefined);
        public static readonly LogicType And = new LogicType(LogicTypes.And);
        public static readonly LogicType AndNot = new LogicType(LogicTypes.And, true);
        public static readonly LogicType Or = new LogicType(LogicTypes.Or);
        public static readonly LogicType OrNot = new LogicType(LogicTypes.Or, true);

        public static bool operator ==(LogicType x, LogicType y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(LogicType x, LogicType y)
        {
            return !x.Equals(y);
        }

        public LogicType(LogicTypes type, bool not = false)
        {
            Type = type;
            Not = not;
        }

        public readonly LogicTypes Type;
        public readonly bool Not;

        public string Format()
        {
            switch (Type)
            {
                case (LogicTypes.And):
                    return Not ? "and not" : "and";
                case (LogicTypes.Or):
                    return Not ? "or not" : "or";
            }
            return "";
        }

        public override string ToString()
        {
            return Locale.Get("LogicType", Format());
        }

        public bool Equals(LogicType obj)
        {
            return Type == obj.Type && Not == obj.Not;
        }

        public override bool Equals(object obj)
        {
            return obj is LogicType logic && Equals(logic);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static LogicTypes Parse(string code)
        {
            LogicTypes type = LogicTypes.Undefined;
            if (code.Equals("and", StringComparison.OrdinalIgnoreCase))
                type = LogicTypes.And;
            else if (code.Equals("or", StringComparison.OrdinalIgnoreCase))
                type = LogicTypes.Or;
            return type;
        }
    }
}
