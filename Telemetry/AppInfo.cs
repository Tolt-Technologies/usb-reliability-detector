using System.Linq;
using System.Reflection;

namespace ToltTech.Telemetry
{
    public static class AppInfo
    {
        public static string Name
        {
            get
            {
                return Assembly.GetEntryAssembly().GetName().Name.Split('.').Last();
            }
        }

        public static string Version
        {
            get
            {
                return Assembly.GetEntryAssembly().GetName().Version.ToString();
            }
        }
    }
}
