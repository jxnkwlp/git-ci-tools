using System;
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
				Console.Error.WriteLine("No git repo found at or above: \"{0}\"", project);
				return null;
			}

			return git;
		}

		public static GitTags FindLatestTag(GitContext git, bool prerelease = false)
		{
			var tags = git.GetTags().ToList();

			if (tags.Count == 0)
			{
				Console.Error.WriteLine("No tags found.");
				return null;
			}
			else
			{
				Console.Out.WriteLine($"Find {tags.Count} tags.");
				foreach (var item in tags.Take(5))
				{
					Console.Out.WriteLine($" - {item.Name}");
				}
				if (tags.Count > 5)
					Console.Out.WriteLine("and more ... ");
				// Console.Out.WriteLine(Environment.NewLine);
			}

			var tag = tags.First();

			if (!prerelease)
				//foreach (var item in tags)
				//{
				//	if (TryParseTagAsVersion(item.Name, out var v))
				//	{
				//		if (!string.IsNullOrEmpty(v.Prerelease))
				//			continue;
				//		else
				//		{
				//			tag = item;
				//			break;
				//		}
				//	}
				//	else
				//	{
				//		tag = null;
				//	}
				//}
				// tag = tags.Where(x => !x.Name.Contains("pre") && !x.Name.Contains("dev") && !x.Name.Contains("rc")).FirstOrDefault();
				tag = tags.Where(x => TryParseTagAsVersion(x.Name, out var v) && string.IsNullOrEmpty(v.Prerelease)).FirstOrDefault();

			if (tag == null)
			{
				Console.Error.WriteLine("No tags found.");
				return null;
			}

			Console.Out.WriteLine($"The Latest tag: {tag.Name}");

			return tag;
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
				Console.Error.WriteLine($"The tag '{tagName}' can't parse as version.");
				return false;
			}

			return true;
		}

		public static SemVersion ResolverVersionFromCommit(GitContext gitContext, SemVersion version, string branch = null, string fromSha = null, bool major = false, bool minor = true, bool patch = false, string prerelease = null, string build = null)
		{
			var commits = gitContext.GetCommits(branch, fromSha);

			SemVersion result = version;

			if (major || ReleaseHelper.IsMajor(gitContext.Project, commits))
				result = VersionGenerater.Next(result, major: true);

			else if (!major && (ReleaseHelper.IsMinor(gitContext.Project, commits) || minor))
				result = VersionGenerater.Next(result, minor: true);

			else if (!major && !minor && (ReleaseHelper.IsPatch(gitContext.Project, commits) || patch))
				result = VersionGenerater.Next(result, patch: true);

			result = VersionGenerater.Next(result, prerelease: prerelease, build: build);

			return result;
		}
	}
}
