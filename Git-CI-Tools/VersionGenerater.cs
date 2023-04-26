using Semver;

namespace Git_CI_Tools;

/// <summary>
///  version sample => 1.0.5-rc+027dfeea
/// </summary>
public class VersionGenerater
{
    public static SemVersion Parse(string version)
    {
        return SemVersion.Parse(version, SemVersionStyles.Any);
    }

    public static SemVersion New(int major = 1, int minor = 0, int patch = 0, string prerelease = "", string build = "")
    {
        return SemVersion.ParsedFrom(major, minor, patch, prerelease, build);
    }

    public static SemVersion Next(SemVersion current = null, bool major = false, bool minor = false, bool patch = false, string prerelease = null, string build = null)
    {
        var version = current ?? New();
        if (major) version = version.With(major: version.Major + 1, minor: 0, patch: 0);
        if (minor) version = version.With(minor: version.Minor + 1, patch: 0);
        if (patch) version = version.With(patch: version.Patch + 1);

        if (!string.IsNullOrEmpty(prerelease))
            version = version.WithPrerelease(prerelease);
        else
            version = version.WithoutPrerelease();

        if (!string.IsNullOrEmpty(build))
            version = version.WithMetadata(build);
        else
            version = version.WithoutMetadata();

        return version;
    }
}
