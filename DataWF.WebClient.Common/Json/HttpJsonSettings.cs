using System;

namespace DataWF.Common
{
    public struct HttpJsonSettings
    {
        public static readonly HttpJsonSettings Default = new HttpJsonSettings(HttpJsonKeys.Full);
        public static readonly HttpJsonSettings None = new HttpJsonSettings(HttpJsonKeys.None);
        public static readonly HttpJsonSettings OnlyReferencing2Level = new HttpJsonSettings(HttpJsonKeys.Refing | HttpJsonKeys.Ref, 2);
        public static readonly HttpJsonSettings OnlyReferencing3Level = new HttpJsonSettings(HttpJsonKeys.Refing | HttpJsonKeys.Ref, 3);
        public static readonly HttpJsonSettings OnlyReferencing4Level = new HttpJsonSettings(HttpJsonKeys.Refing | HttpJsonKeys.Ref, 4);
        public static readonly HttpJsonSettings OnlyReferenced2Level = new HttpJsonSettings(HttpJsonKeys.Refed | HttpJsonKeys.Ref, 2);
        public static readonly HttpJsonSettings OnlyReferenced3Level = new HttpJsonSettings(HttpJsonKeys.Refed | HttpJsonKeys.Ref, 3);
        public static readonly HttpJsonSettings OnlyReferenced4Level = new HttpJsonSettings(HttpJsonKeys.Refed | HttpJsonKeys.Ref, 4);
        public static readonly HttpJsonSettings OnlyReferenced5Level = new HttpJsonSettings(HttpJsonKeys.Refed | HttpJsonKeys.Ref, 5);


        public static readonly string XJsonKeys = "X-Json-Keys";
        public static readonly string XJsonKeyRefered = "X-Json-KeyRefed";
        public static readonly string XJsonKyeRefering = "X-Json-KeyRefing";
        public static readonly string XJsonKeyRef = "X-Json-KeyRef";
        public static readonly string XJsonMaxDepth = "X-Json-MaxDepth";
        /// <summary>
        /// Получение полных обектов
        /// </summary>
        public static readonly string XGetFullJson = "X-Get-Full-Json";

        public HttpJsonSettings(HttpJsonKeys keys = HttpJsonKeys.Full, int maxDepth = 3, bool fullJson = false)
        {
            MaxDepth = maxDepth;
            Keys = keys;
            FullJson = fullJson;
        }

        public readonly HttpJsonKeys Keys;
        public readonly int MaxDepth;
        public readonly bool FullJson;

        public bool Referencing => (Keys & HttpJsonKeys.Refing) == HttpJsonKeys.Refing;
        public bool Referenced => (Keys & HttpJsonKeys.Refed) == HttpJsonKeys.Refed;
        public bool Reference => (Keys & HttpJsonKeys.Ref) == HttpJsonKeys.Ref;
    }
}