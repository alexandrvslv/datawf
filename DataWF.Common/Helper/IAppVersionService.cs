using System.Reflection;

namespace DataWF.Common
{
    public interface IAppVersionInfo
    {
        string Version { get; }
    }

    public class AppVersionService : IAppVersionInfo
    {
        public string Version => Assembly.GetEntryAssembly().GetName().Version.ToString();
    }
}