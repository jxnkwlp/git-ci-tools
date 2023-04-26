using System.CommandLine;
using System.CommandLine.Invocation;

namespace Git_CI_Tools.Commands;

public class ReleaseCommand : CommandBase
{
    public override Command GetCommand()
    {
        var command = new Command("release");

        command.AddCommand(ChangesCommand());

        return command;
    }

    private Command ChangesCommand()
    {
        var command = new Command("changes", "Generate changes from commit logs");

        command.AddOption(new Option<string>("--provider"));
        command.AddOption(new Option<string>("--server-url"));

        command.AddOption(new Option<string>(new string[] { "--output", "-o" }, "Output results to the specified file"));

        command.AddOption(new Option<string[]>("--target-paths"));

        command.Handler = CommandHandler.Create<ReleaseChangesCommandOption>(options =>
        {
            new CommandExecutor().ReleaseChanges(options);
        });

        return command;
    }
}

public class ReleaseChangesCommandOption
{
    public string Provider { get; set; }
    public string ServerUrl { get; set; }

    public string Project { get; set; }
    public string Branch { get; set; }
    public bool IncludePrerelease { get; set; }

    public string Output { get; set; }

    public string[] TargetPaths { get; set; }

}
