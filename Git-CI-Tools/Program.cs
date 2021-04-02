using System;
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

			rootCommand.AddCommand(new VersionCommand().GetCommand());

			return await rootCommand.InvokeAsync(args);
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

}
