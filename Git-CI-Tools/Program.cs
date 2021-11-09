using System.CommandLine;
using System.Threading.Tasks;
using Git_CI_Tools.Commands;

namespace Git_CI_Tools
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand()
            {
                Description = "Git tools for ci",
            };

            rootCommand.AddGlobalOption(new Option<string>("--project", "The project root path"));
            rootCommand.AddGlobalOption(new Option<string>("--branch", "The project git branch. Default is current branch "));
            rootCommand.AddGlobalOption(new Option<bool>("--include-prerelease", () => false, "Whether include prerelease tags"));

            rootCommand.AddCommand(new VersionCommand().GetCommand());
            rootCommand.AddCommand(new ReleaseCommand().GetCommand());
            rootCommand.AddCommand(new GitCommand().GetCommand());

            return await rootCommand.InvokeAsync(args);
        }

    }

    //public class VersionHandlerArgs
    //{
    //    public string Project { get; set; }
    //    public string Provider { get; set; }
    //    public Uri Url { get; set; }
    //    public string Token { get; set; }
    //    public string Default { get; set; }
    //}

}
