using System.Reflection;

namespace FROST.Services;

public static class VersionInfo
{
    public static string Current { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(VersionInfo).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var metadataIndex = informationalVersion.IndexOf('+');
            return metadataIndex >= 0
                ? informationalVersion[..metadataIndex]
                : informationalVersion;
        }

        return assembly.GetName().Version?.ToString(3) ?? "1.0.1";
    }
}