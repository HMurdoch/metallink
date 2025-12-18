using System.Reflection;

namespace MetalLink.Api.Versioning;

public static class AppVersion
{
    public static string GetInformationalVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(info) ? asm.GetName().Version?.ToString() ?? "0.0.0.0" : info;
    }
}