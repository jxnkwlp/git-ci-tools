using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
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
			var command = new Command("current", "Show current version");

			command.AddOption(new Option<string>("--project", "The project root path"));
			command.AddOption(new Option<bool>("--include-prerelease", () => false));

			command.AddOption(new Option<string>(new string[] { "--format" }, () => "text", "Output format: json/dotenv/text"));
			command.AddOption(new Option<string>(new string[] { "--output", "-o" }, "Output results to the specified file"));

			command.AddOption(new Option<string>("dotenv-var-name"));

			command.Handler = CommandHandler.Create<VersionCurrentOptions>((options) =>
			{
				var git = GitContextHelper.InitProject(options.Project);

				if (git == null)
					return;

				var tag = GitContextHelper.FindLatestTag(git, options.IncludePrerelease);

				if (tag == null)
					return;

				if (!GitContextHelper.TryParseTagAsVersion(tag.Name, out var version))
				{
					return;
				}

				var currentVersion = version;

				Console.Out.WriteLine($"Current version: {currentVersion}. ");

				string outputText = currentVersion.ToString();

				if (options.Format == "json")
				{
					outputText = JsonSerializer.Serialize(new { CurrentVersionText = currentVersion.ToString(), CurrentVersion = currentVersion }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
				}
				else if (options.Format == "dotenv")
				{
					string name = options.DotenvVarName;
					if (string.IsNullOrEmpty(name))
						name = "CURRENT_VERSION";

					StringBuilder sb = new StringBuilder();
					sb.AppendLine($"{name}={currentVersion}");
					sb.AppendLine($"{name}_MINI={currentVersion.Change(build: "")}");
					sb.AppendLine($"{name}_MAJOR={currentVersion.Major}");
					sb.AppendLine($"{name}_MINOR={currentVersion.Minor}");
					sb.AppendLine($"{name}_PATCH={currentVersion.Patch}");
					sb.AppendLine($"{name}_PRERELEASE={currentVersion.Prerelease}");
					sb.AppendLine($"{name}_BUILD={currentVersion.Build}");

					outputText = sb.ToString().Trim();
				}

				if (!string.IsNullOrEmpty(options.Output))
				{
					FileHelper.AppendLine(options.Output, outputText);

					Console.Out.WriteLine($"The result has been written to file '{options.Output}'. ");
				}

			});

			return command;
		}

		private Command VersionNextCommand()
		{
			var command = new Command("next", "Generate next version");

			command.AddOption(new Option<bool>("--debug-mode", () => false) { IsHidden = true });

			command.AddOption(new Option<string>("--project", "The project root path"));
			command.AddOption(new Option<string>("--default-version", () => "1.0.0", "Default version"));
			command.AddOption(new Option<string>("--branch"));

			command.AddOption(new Option<bool>("--include-prerelease", () => false));

			command.AddOption(new Option<bool>("--major-ver", "Set the major version number"));
			command.AddOption(new Option<bool>("--minor-ver", "Set the minor version number"));
			command.AddOption(new Option<bool>("--patch-ver", "Set the patch version number"));
			command.AddOption(new Option<string>("--prerelease-ver", "Set the prerelease version number"));
			command.AddOption(new Option<string>("--build-ver", "Set the build version number"));

			command.AddOption(new Option<bool>("--force", "Force generation of the next version number"));

			command.AddOption(new Option<string>(new string[] { "--format" }, () => "text", "Output format: json/dotenv/text"));
			command.AddOption(new Option<string>(new string[] { "--output", "-o" }, "Output results to the specified file"));

			command.AddOption(new Option<string>("dotenv-var-name"));

			command.Handler = CommandHandler.Create<VersionNextOptions>(options =>
			{
				var git = GitContextHelper.InitProject(options.Project);

				if (git == null)
					return;

				var tag = GitContextHelper.FindLatestTag(git, options.IncludePrerelease);

				SemVersion currentVersion = VersionGenerater.New();

				if (tag != null && GitContextHelper.TryParseTagAsVersion(tag.Name, out currentVersion))
				{
					// 
					// Console.Out.WriteLine("Current version: " + currentVersion);
				}
				else
				{
					if (string.IsNullOrEmpty(options.DefaultVersion))
						options.DefaultVersion = "1.0.0";

					currentVersion = VersionGenerater.Parse(options.DefaultVersion);
				}

				Console.Out.WriteLine($"Current version: {currentVersion}. ");

				DebugWrite(options.DebugMode, () => "Project branchs: " + Environment.NewLine + string.Join(Environment.NewLine, git.GetBranchs().Select(x => x.ToString())));

				if (string.IsNullOrEmpty(options.Branch))
					options.Branch = git.GetCurrentBranch()?.Name;

				if (!git.BranchExisting(options.Branch))
				{
					Console.Error.WriteLine($"The branch '{options.Branch}' not found. ");
					return;
				}

				if (tag != null)
					Console.Out.WriteLine($"Reverse version from tag {tag} of branch  '{options.Branch}' ... ");
				else
					Console.Out.WriteLine($"Reverse version from branch ... ");

				var nextVersion = GitContextHelper.ResolverVersionFromCommit(
					git,
					currentVersion,
					options.Branch,
					tag?.Sha,
					options.MajorVer,
					options.MinorVer,
					options.PatchVer,
					options.PrereleaseVer ?? "",
					options.BuildVer ?? "");

				if (nextVersion.ToString() == currentVersion.ToString() && options.Force)
					nextVersion = VersionGenerater.Next(currentVersion, patch: true);

				string outputText = nextVersion.ToString();

				Console.Out.WriteLine($"The next version: {outputText}. ");

				if (options.Format == "json")
				{
					outputText = JsonSerializer.Serialize(new { NextVersionText = nextVersion.ToString(), NextVersion = nextVersion }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
				}
				else if (options.Format == "dotenv")
				{
					string name = options.DotenvVarName;
					if (string.IsNullOrEmpty(name))
						name = "NEXT_VERSION";

					StringBuilder sb = new StringBuilder();
					sb.AppendLine($"{name}={nextVersion}");
					sb.AppendLine($"{name}_MINI={nextVersion.Change(build: "")}");
					sb.AppendLine($"{name}_MAJOR={nextVersion.Major}");
					sb.AppendLine($"{name}_MINOR={nextVersion.Minor}");
					sb.AppendLine($"{name}_PATCH={nextVersion.Patch}");
					sb.AppendLine($"{name}_PRERELEASE={nextVersion.Prerelease}");
					sb.AppendLine($"{name}_BUILD={nextVersion.Build}");

					outputText = sb.ToString().Trim();
				}
				else
				{
					//Console.Out.WriteLine($"The next version result: {Environment.NewLine}{outputText}. ");
				}

				if (!string.IsNullOrEmpty(options.Output))
				{
					FileHelper.AppendLine(options.Output, outputText);

					Console.Out.WriteLine($"The result has been written to file '{options.Output}'. ");
				}

			});

			return command;
		}

		private static void DebugWrite(bool isDebug, Func<string> outputFunc)
		{
			if (isDebug && outputFunc != null)
				Console.WriteLine(Environment.NewLine + outputFunc());
		}
	}


	public class VersionCurrentOptions
	{
		public string Project { get; set; }
		public string Format { get; set; }
		public string Output { get; set; }

		public bool IncludePrerelease { get; set; }

		public string DotenvVarName { get; set; }
	}

	public class VersionNextOptions
	{
		public string Project { get; set; }
		public string Branch { get; set; }

		//public string Provider { get; set; }
		//public Uri Url { get; set; }
		//public string Token { get; set; }
		public string DefaultVersion { get; set; }

		public bool IncludePrerelease { get; set; }

		public bool MajorVer { get; set; }
		public bool MinorVer { get; set; }
		public bool PatchVer { get; set; }
		public string PrereleaseVer { get; set; }
		public string BuildVer { get; set; }

		public bool Force { get; set; }

		public string Format { get; set; }
		public string Output { get; set; }

		public bool DebugMode { get; set; }

		public string DotenvVarName { get; set; }
	}
}
