using System;

namespace Git_CI_Tools
{
	public class GitProviderFactory
	{
		public static IGitProvider Create(string provider, string server)
		{
			if (string.Equals(provider, "gitlab", StringComparison.InvariantCultureIgnoreCase))
				return new GitlabGitProvider(server);
			else if (string.Equals(provider, "github", StringComparison.InvariantCultureIgnoreCase))
				return new GitHubGitProvider();

			return new DefaultGitProvider();
		}
	}
}
