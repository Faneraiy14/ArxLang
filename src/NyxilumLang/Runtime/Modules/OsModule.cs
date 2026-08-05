using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NyxilumLang.Runtime.Modules;

public static class OsModule
{
    public static void Register(Dictionary<string, Func<object[], object?>> registry)
    {
        registry["osPlatform"] = args => RuntimeInformation.OSDescription;
        registry["osArchitecture"] = args => RuntimeInformation.OSArchitecture.ToString();
        registry["osMemory"] = args => Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024); // MB
        registry["osCpuCount"] = args => Environment.ProcessorCount;
        registry["osEnv"] = args => {
            if (Sandbox.Enabled)
                throw new Exception("Пісочниця: читання змінних середовища заборонено (NX_SANDBOX=1)");
            return Environment.GetEnvironmentVariable(args[0].ToString()!) ?? "";
        };
        registry["osCwd"] = args => Directory.GetCurrentDirectory();
    }
}
