using System.Reflection;

namespace DataWF.Common
{
    public class MemberParseInfo
    {
        public MemberParseInfo(MemberInfo info, object index)
        {
            this.Info = info;
            this.Index = index;
        }

        public MemberInfo Info { get; }

        public object Index { get; }
    }
}