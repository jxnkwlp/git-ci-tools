using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;

namespace Git_CI_Tools.Commands
{
	public class ReleaseCommand
	{
		public Command GetCommand()
		{
			var command = new Command("release");

			command.AddCommand(NotesCommand());

			return command;
		}

		private Command NotesCommand()
		{
			var command = new Command("notes", "Generate notes from commit logs.");

			command.AddOption(new Option<string>("--project", "", "The project root path.") { Required = false });
			command.AddOption(new Option<bool>("--include-prerelease", false));

			command.AddOption(new Option<string>("--branch"));

			command.AddOption(new Option<string>("--provider"));
			command.AddOption(new Option<string>("--server-url"));

			command.AddOption(new Option<string>(new[] { "--output", "-o" }));

			command.Handler = CommandHandler.Create<ReleaseNoteCommandOption>(options =>
			{
				if (string.Equals(options.Provider, "gitlab", StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty(options.ServerUrl))
				{
					Console.Error.WriteLine($"No server url provider.");
					return;
				}

				var git = GitContextHelper.InitProject(options.Project);

				if (git == null)
					return;

				var tag = GitContextHelper.FindLatestTag(git, !options.IncludePrerelease);

				if (string.IsNullOrEmpty(options.Branch))
					options.Branch = git.GetCurrentBranch()?.Name;

				if (!git.BranchExisting(options.Branch))
				{
					Console.Error.WriteLine($"The branch '{options.Branch}' not found. ");
					return;
				}

				Console.Out.WriteLine($"Current branch: {options.Branch}");

				var commits = git.GetCommits(options.Branch, fromSha: tag?.Sha).ToList();

				Console.Out.WriteLine($"Find {commits.Count()} commits.");

				IGitProvider gitProvider = GitProviderFactory.Create(options.Provider, options.ServerUrl);

				string notes = ReleaseHelper.GenerateNotes(git.Project, commits, gitProvider);

				Console.Out.WriteLine($"Release notes generated. {Environment.NewLine}");

				if (!string.IsNullOrEmpty(options.Output))
				{
					File.WriteAllText(options.Output, notes);
				}
				else
				{
					Console.Out.WriteLine(notes);
				}
			});

			return command;
		}
	}

	public class ReleaseNoteCommandOption
	{

		public string Provider { get; set; }
		public string ServerUrl { get; set; }

		public string Project { get; set; }
		public string Branch { get; set; }
		public bool IncludePrerelease { get; set; }

		public string Output { get; set; }
	}
}
