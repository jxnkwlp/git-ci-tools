using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using Semver;

namespace Git_CI_Tools.Commands
{
	public class VersionCommand
	{
		public Command GetCommand()
		{
			var command = new Command("version");

			command.AddCommand(VersionCurrentCommand());
			command.AddCommand(VersionNextCommand());

			return command;
		}

		private Command VersionCurrentCommand()
		{
			var command = new Command("current", "Show current version.");

			command.AddOption(new Option<string>("--project") { Required = false });
			command.AddOption(new Option<bool>("--include-prerelease", false));
			command.AddOption(new Option<string>(new string[] { "--format" }, "text", "output formats: json/dotenv/text(default)"));
			command.AddOption(new Option<string>(new string[] { "--output", "-o" }, "", "Output result to file."));

			command.Handler = CommandHandler.Create<VersionCurrentOptions>((options) =>
			{
				var git = GitContextHelper.InitProject(options.Project);

				var tag = GitContextHelper.FindLatestTag(git, options.IncludePrerelease);

				if (tag == null)
					return;

				if (!GitContextHelper.TryParseTagAsVersion(tag.Name, out var version))
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

		private Command VersionNextCommand()
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

				var git = GitContextHelper.InitProject(options.Project);

				var tag = GitContextHelper.FindLatestTag(git, options.IncludePrerelease);

				SemVersion version = VersionGenerater.Parse(options.Default);

				if (tag != null)
				{
					if (!GitContextHelper.TryParseTagAsVersion(tag.Name, out version))
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
				var branch = git.GetCurrentBranch();
				var nextVersion = GitContextHelper.ResolverVersionFromCommit(git, currentVersion, branch.Name, tag?.Sha, options.Major, options.Minor, options.Patch, options.Prerelease, options.Build);

				if (nextVersion.ToString() == currentVersion.ToString() && options.Focus)
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
