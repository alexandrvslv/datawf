using System;
using System.Reflection;

namespace DataWF.Common
{
    public readonly struct MetadataToken : IComparable<MetadataToken>, IEquatable<MetadataToken>
    {
        public static MetadataToken GetToken(MemberInfo info, bool declaringType = true)
        {
            return new MetadataToken(info.Module.GetHashCode(), declaringType ? info.DeclaringType.GetHashCode(): info.ReflectedType.GetHashCode(), info.GetHashCode());
        }

        public readonly int Module;
        public readonly int Type;
        public readonly int Member;
        public MetadataToken(int module, int type, int member)
        {
            Module = module;
            Type = type;
            Member = member;
        }

        public int CompareTo(MetadataToken other)
        {
            var rezult = Module.CompareTo(other.Module);
            rezult = rezult == 0 ? Type.CompareTo(other.Type) : rezult;
            rezult = rezult == 0 ? Member.CompareTo(other.Member) : rezult;
            return rezult;
        }

        public override bool Equals(object obj)
        {
            return obj is MetadataToken token && Equals(token);
        }

        public bool Equals(MetadataToken other)
        {
            return Module == other.Module &&
                   Type == other.Type &&
                   Member == other.Member;
        }

        public override int GetHashCode()
        {
            int hashCode = -2079221147;
            hashCode = hashCode * -1521134295 + Module;
            hashCode = hashCode * -1521134295 + Type;
            hashCode = hashCode * -1521134295 + Member;
            return hashCode;
        }
    }



}
