﻿using Semver;

namespace Git_CI_Tools
{
	/// <summary>
	///  1.0.0-rc+027dfeea
	/// </summary>
	public class VersionGenerater
	{
		public static SemVersion Parse(string version)
		{
			return SemVersion.Parse(version);
		}

		public static SemVersion New(int major = 1, int minor = 0, int patch = 0, string prerelease = "", string build = "")
		{
			return new SemVersion(major, minor, patch, prerelease, build);
		}

		public static SemVersion Next(SemVersion current = null, bool major = false, bool minor = false, bool patch = false, string prerelease = null, string build = null)
		{
			var version = current ?? New();
			if (major) version = version.Change(version.Major + 1);
			if (minor) version = version.Change(minor: version.Minor + 1);
			if (patch) version = version.Change(patch: version.Patch + 1);

			version = version.Change(prerelease: prerelease, build: build);

			return version;
		}
	}
}
