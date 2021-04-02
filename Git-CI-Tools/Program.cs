using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Semver;

namespace Git_CI_Tools
{
	internal class Program
	{
		private static async Task<int> Main(string[] args)
		{
			var rootCommand = new RootCommand()
			{
				Description = "",
			};

			rootCommand.AddCommand(VersionCommand());

			return await rootCommand.InvokeAsync(args);
		}

		private static Command VersionCommand()
		{
			var command = new Command("version");

			command.AddCommand(VersionCurrentCommand());
			command.AddCommand(VersionNextCommand());

			return command;
		}

		private static Command VersionCurrentCommand()
		{
			var command = new Command("current", "Show current version.");

			command.AddOption(new Option<string>("--project") { Required = false });
			command.AddOption(new Option<bool>("--include-prerelease", false));
			command.AddOption(new Option<string>(new string[] { "--format" }, "text", "output formats: json/dotenv/text(default)"));
			command.AddOption(new Option<string>(new string[] { "--output", "-o" }, "", "Output result to file."));

			command.Handler = CommandHandler.Create<VersionCurrentOptions>((options) =>
			{
				var git = InitProject(options.Project);

				var tag = FindLatestTag(git, options.IncludePrerelease);

				if (tag == null)
					return;

				if (!TryParseTagAsVersion(tag.Name, out var version))
				{
					return;
				}

				var currentVersion = version;

				Console.Out.WriteLine("Current version: " + currentVersion);

				string outputText = currentVersion.ToString();
				if (options.Format == "json")
				{
					outputText = JsonSerializer.Serialize(new { versionText = currentVersion.ToString(), version = currentVersion });
				}
				else if (options.Format == "dotenv")
				{
					outputText = $"version={currentVersion}";
				}

				if (!string.IsNullOrEmpty(options.Output))
				{
					File.WriteAllText(options.Output, outputText);
				}
			});

			return command;
		}

		private static Command VersionNextCommand()
		{
			var command = new Command("next", "Generate next version.");

			command.AddOption(new Option<string>("--project", "", "The project root path.") { Required = false });
			command.AddOption(new Option<string>("--default", "1.0.0", "Default version.") { Required = false });

			command.AddOption(new Option<bool>("--include-prerelease", false));

			command.AddOption(new Option<bool>("--major"));
			command.AddOption(new Option<bool>("--minor"));
			command.AddOption(new Option<bool>("--patch"));
			command.AddOption(new Option<string>("--prerelease"));
			command.AddOption(new Option<string>("--build"));

			command.AddOption(new Option<bool>(new string[] { "--focus", "-f" }, false, "Focus generate next version."));

			command.AddOption(new Option<string>(new string[] { "--format" }, "text", "output formats: json/dotenv/text(default)"));
			command.AddOption(new Option<string>(new string[] { "--output", "-o" }, "", "Output result to file."));

			command.Handler = CommandHandler.Create<VersionNextOptions>(options =>
			{
				if (string.IsNullOrWhiteSpace(options.Default))
					options.Default = "1.0.0";

				var git = InitProject(options.Project);

				var tag = FindLatestTag(git, options.IncludePrerelease);

				SemVersion version = VersionGenerater.Parse(options.Default);

				if (tag != null)
				{
					if (!TryParseTagAsVersion(tag.Name, out version))
					{
						version = VersionGenerater.Parse(options.Default);
					}
				}
				else
				{
					version = VersionGenerater.Parse(options.Default);
				}

				var currentVersion = version;

				//if (currentVersion == null)
				//	currentVersion = VersionGenerater.New();

				// var nextVersion = VersionGenerater.Next(currentVersion);
				var nextVersion = ResolverVersionFromCommit(git, currentVersion, tag.Sha, options.Major, options.Minor, options.Patch, options.Prerelease, options.Build);

				if (nextVersion.ToString() == currentVersion.ToString())
					nextVersion = VersionGenerater.Next(currentVersion, patch: true);

				Console.Out.WriteLine("Next version: " + nextVersion);

				string outputText = nextVersion.ToString();
				if (options.Format == "json")
				{
					outputText = JsonSerializer.Serialize(new { nextVersionText = nextVersion.ToString(), nextVersion = nextVersion });
				}
				else if (options.Format == "dotenv")
				{
					outputText = $"nextVersion={nextVersion}";
				}

				if (!string.IsNullOrEmpty(options.Output))
				{
					File.WriteAllText(options.Output, outputText);
				}

			});

			return command;
		}




		private static GitContext InitProject(string project)
		{
			var git = new GitContext(project ?? Directory.GetCurrentDirectory());
			if (!git.IsValid())
			{
				Console.Error.WriteLine("No git repo found at or above: \"{0}\"", project);
				return null;
			}

			return git;
		}

		private static GitTags FindLatestTag(GitContext git, bool prerelease = false)
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
				Console.Out.WriteLine($"The latest 5 tags ... ");
				foreach (var item in tags.Take(5))
				{
					Console.Out.WriteLine(item.Name);
				}
				Console.Out.WriteLine(Environment.NewLine);
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

		private static bool TryParseTagAsVersion(string tagName, out SemVersion version)
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

		private static SemVersion ResolverVersionFromCommit(GitContext gitContext, SemVersion version, string from = null, bool major = false, bool minor = true, bool patch = false, string prerelease = null, string build = null)
		{
			var commits = gitContext.GetCommits(from);

			SemVersion result = version;

			if (major || UserConfig.IsMajor(gitContext.Project, commits))
				result = VersionGenerater.Next(result, major: true);

			if (!major && (UserConfig.IsMinor(gitContext.Project, commits) || minor))
				result = VersionGenerater.Next(result, minor: true);

			if (!major && !minor && (UserConfig.IsPatch(gitContext.Project, commits) || patch))
				result = VersionGenerater.Next(result, patch: true);

			result = VersionGenerater.Next(result, prerelease: prerelease, build: build);

			return result;
		}


	}

	public class VersionHandlerArgs
	{
		public string Project { get; set; }
		public string Provider { get; set; }
		public Uri Url { get; set; }
		public string Token { get; set; }
		public string Default { get; set; }
	}

	public class VersionCurrentOptions
	{
		public string Project { get; set; }
		public string Format { get; set; }
		public string Output { get; set; }

		public bool IncludePrerelease { get; set; }
	}

	public class VersionNextOptions
	{
		public string Project { get; set; }
		//public string Provider { get; set; }
		//public Uri Url { get; set; }
		//public string Token { get; set; }
		public string Default { get; set; }

		public bool IncludePrerelease { get; set; }

		public bool Major { get; set; }
		public bool Minor { get; set; }
		public bool Patch { get; set; }
		public string Prerelease { get; set; }
		public string Build { get; set; }

		public bool Focus { get; set; }

		public string Format { get; set; }
		public string Output { get; set; }
	}
}
