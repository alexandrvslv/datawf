using System;

namespace Xwt
{
    internal class Timeout
    {
        internal static IDisposable Add(int v, Func<bool> p)
        {
            return Application.TimeoutInvoke(v, p);
        }
    }
}