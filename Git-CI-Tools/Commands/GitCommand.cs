using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Git_CI_Tools.Commands;

public class GitCommand : CommandBase
{
    public override Command GetCommand()
    {
        var command = new Command("git");

        command.AddCommand(ChangesCommand());

        return command;
    }

    private Command ChangesCommand()
    {
        var command = new Command("changes", "Extracting Git commit history");
        command.AddOption(new Option<string[]>("--target-paths"));
        command.AddOption(new Option<string>("--format", () => "markdown", "The changes output to (markdown/json). default is markdown file."));
        command.AddOption(new Option<string>("--output"));

        command.Handler = CommandHandler.Create<GitChangesCommandOptions>(options =>
        {
            new CommandExecutor().GitChanges(options);
        });

        return command;
    }
}

public class GitChangesCommandOptions
{
    public string Project { get; set; }
    public string Branch { get; set; }
    public bool IncludePrerelease { get; set; }

    public string[] TargetPaths { get; set; }

    public string Format { get; set; }

    public string Output { get; set; }
}

public class GitChangesCommandResult
{
    public Dictionary<string, List<GitChangedFile>> Changes { get; set; }
}
