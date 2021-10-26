using System;

namespace DataWF.Common
{
    public struct JoinType
    {
        public static readonly JoinType Undefined = new JoinType(JoinTypes.Undefined);
        public static readonly JoinType Left = new JoinType(JoinTypes.Left | JoinTypes.Join);
        public static readonly JoinType LeftOuter = new JoinType(JoinTypes.Left | JoinTypes.Outer | JoinTypes.Join);
        public static readonly JoinType Right = new JoinType(JoinTypes.Right | JoinTypes.Join);
        public static readonly JoinType RightOuter = new JoinType(JoinTypes.Right | JoinTypes.Outer | JoinTypes.Join);

        public static bool operator ==(JoinType typeLeft, JoinType typeRight)
        {
            return typeLeft.Equals(typeRight);
        }

        public static bool operator !=(JoinType typeLeft, JoinType typeRight)
        {
            return !typeLeft.Equals(typeRight);
        }

        public static string Format(JoinTypes type)
        {
            string rezult = string.Empty;
            if ((type & JoinTypes.Left) == JoinTypes.Left)
                rezult = "left";
            else if ((type & JoinTypes.Right) == JoinTypes.Right)
                rezult = "right";
            if ((type & JoinTypes.Outer) == JoinTypes.Outer)
                rezult += " outer";
            if (type != JoinTypes.Undefined)
                rezult += " join";
            return rezult;
        }

        public static JoinTypes Parse(string code)
        {
            JoinTypes type = JoinTypes.Join;
            if (code.Equals("left", StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Left;
            else if (code.Equals("right", StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Right;
            else if (code.Equals("joint", StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Join;
            else if (code.Equals("outer", StringComparison.OrdinalIgnoreCase))
                type = JoinTypes.Outer;
            return type;
        }

        public JoinTypes Type;

        public JoinType(JoinTypes type)
        {
            Type = type;
        }

        public bool Equals(JoinType obj)
        {
            return Type == obj.Type;
        }

        public override bool Equals(object obj)
        {
            return obj is JoinType && Equals((JoinType)obj);
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public string Format()
        {
            return Format(Type);
        }

        public override string ToString()
        {
            return Locale.Get("JoinType", Format());
        }
    }
}
