using System;

namespace DataWF.Common
{
    public struct HttpJsonSettings
    {
        public static readonly HttpJsonSettings Default = new HttpJsonSettings(HttpJsonKeys.Full);
        public static readonly HttpJsonSettings None = new HttpJsonSettings(HttpJsonKeys.None);
        public static readonly HttpJsonSettings OnlyReferencing = new HttpJsonSettings(HttpJsonKeys.Refing | HttpJsonKeys.Ref);
        public static readonly HttpJsonSettings OnlyReferenced = new HttpJsonSettings(HttpJsonKeys.Refed | HttpJsonKeys.Ref);

        [Obsolete]
        public static readonly string JsonKeys = "json_keys";
        public static readonly string XJsonKeys = "X-Json-Keys";
        [Obsolete]
        public static readonly string JsonReferenced = "json_refed";
        public static readonly string XJsonKeyRefered = "X-Json-KeyRefed";
        [Obsolete]
        public static readonly string JsonReferencing = "json_refing";
        public static readonly string XJsonKyeRefering = "X-Json-KeyRefing";
        [Obsolete]
        public static readonly string JsonReference = "json_ref";
        public static readonly string XJsonKeyRef = "X-Json-KeyRef";
        [Obsolete]
        public static readonly string JsonMaxDepth = "json_max_depth";
        public static readonly string XJsonMaxDepth = "X-Json-MaxDepth";

        public HttpJsonSettings(HttpJsonKeys keys = HttpJsonKeys.Full, int maxDepth = 3)
        {
            MaxDepth = maxDepth;
            Keys = keys;
        }

        public readonly HttpJsonKeys Keys;
        public readonly int MaxDepth;

        public bool Referencing => (Keys & HttpJsonKeys.Refing) == HttpJsonKeys.Refing;
        public bool Referenced => (Keys & HttpJsonKeys.Refed) == HttpJsonKeys.Refed;
        public bool Reference => (Keys & HttpJsonKeys.Ref) == HttpJsonKeys.Ref;
    }
}