using System;
using System.Reflection;

namespace DataWF.Common
{
    public struct MetadataToken : IComparable<MetadataToken>
    {
        public static MetadataToken GetToken(MemberInfo info)
        {
            return new MetadataToken(info.Module.GetHashCode(), info.DeclaringType.GetHashCode(), info.GetHashCode());
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

        public override int GetHashCode()
        {
            return Module ^ Type ^ Member;
        }

        public override bool Equals(object obj)
        {
            if (obj is MetadataToken metadataToken)
                return Module == metadataToken.Module
                        && Type == metadataToken.Type
                        && Member == metadataToken.Member;
            return false;
        }

        public int CompareTo(MetadataToken other)
        {
            var rezult = Module.CompareTo(other.Module);
            rezult = rezult == 0 ? Type.CompareTo(other.Type) : rezult;
            rezult = rezult == 0 ? Member.CompareTo(other.Member) : rezult;
            return rezult;
        }
    }



}
