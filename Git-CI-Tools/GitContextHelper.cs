using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Semver;

namespace Git_CI_Tools
{
	public static class GitContextHelper
	{
		public static GitContext InitProject(string project)
		{
			var path = project;
			if (string.IsNullOrEmpty(project))
				path = Directory.GetCurrentDirectory();

			var git = new GitContext(path);

			if (!git.IsValid())
			{
				Console.Error.WriteLine("No git repo found at or above: \"{0}\". ", project);
				return null;
			}

			Console.WriteLine($"Project: {git.Project} ");

			return git;
		}

		public static Dictionary<SemVersion, GitTags> GetVersionsFromTags(IEnumerable<GitTags> tags, bool includePrerelease = false)
		{
			if (tags.Count() == 0)
			{
				Console.Error.WriteLine("No tags found. ");
				return null;
			}

			var result = new List<KeyValuePair<SemVersion, GitTags>>();

			foreach (var tag in tags)
			{
				if (TryParseTagAsVersion(tag.Name, out var v))
				{
					if (!result.Any(x => x.Key == v))
						result.Add(new KeyValuePair<SemVersion, GitTags>(v, tag));
				}
			}

			if (!includePrerelease)
			{
				result = result.Where(x => string.IsNullOrEmpty(x.Key.Prerelease)).ToList();
			}

			return result.OrderByDescending(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
		}

		public static GitTags FindLatestTag(GitContext git, bool includePrerelease = false)
		{
			var tags = git.GetTags().ToList();

			if (tags.Count == 0)
			{
				Console.Error.WriteLine("No tags found. ");
				return null;
			}
			else
			{
				Console.Out.WriteLine($"Find {tags.Count} tags: ");
				foreach (var item in tags.Take(6))
				{
					Console.Out.WriteLine($" - {item.Name}");
				}
				if (tags.Count > 6)
					Console.Out.WriteLine("  and more ... ");
			}

			var versions = GetVersionsFromTags(tags, includePrerelease);

			if (versions.Count == 0)
			{
				Console.Error.WriteLine($"No tag found when uninclude pre-release version. ");
				return null;
			}

			var find = versions.First();

			if (includePrerelease)
				Console.Out.WriteLine($"The last preversion version is {find.Key}, tags: {find.Value.Name} ");
			else
				Console.Out.WriteLine($"The last version is {find.Key}, tags: {find.Value.Name} ");

			return find.Value;
		}

		public static bool TryParseTagAsVersion(string tagName, out SemVersion version)
		{
			version = null;
			int index = -1;

			for (int i = 0; i < tagName.Length; i++)
			{
				if (char.IsDigit(tagName[i]))
				{
					index = i;
					break;
				}
			}

			if (index == -1) return false;

			var v = tagName.Substring(index);

			if (!SemVersion.TryParse(v, out version))
			{
				// Console.Error.WriteLine($"The tag '{tagName}' can't parse as version.");
				return false;
			}

			return true;
		}

		public static SemVersion ResolverVersionFromCommit(
			GitContext gitContext,
			SemVersion version,
			string branch = null,
			string fromSha = null,
			bool major = false,
			bool minor = true,
			bool patch = false,
			string prerelease = "",
			string build = "",
			bool ignoreConfig = false)
		{
			var commits = gitContext.GetCommits(branch, fromSha);

			SemVersion result = version;

			if (!ignoreConfig && major || ReleaseConfigHelper.IsMajor(gitContext.Project, commits))
				result = VersionGenerater.Next(result, major: true);

			else if (!ignoreConfig && !major && (ReleaseConfigHelper.IsMinor(gitContext.Project, commits) || minor))
				result = VersionGenerater.Next(result, minor: true);

			else if (!ignoreConfig && !major && !minor && (ReleaseConfigHelper.IsPatch(gitContext.Project, commits) || patch))
				result = VersionGenerater.Next(result, patch: true);

			result = VersionGenerater.Next(result, prerelease: prerelease, build: build);

			return result;
		}
	}
}
