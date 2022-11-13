using System;

namespace Git_CI_Tools
{
    public static class GitProviderFactory
    {
        public static IGitProvider Create(string provider, string server)
        {
            if (string.Equals(provider, "gitlab", StringComparison.InvariantCultureIgnoreCase))
                return new GitlabGitProvider(server);
            else if (string.Equals(provider, "github", StringComparison.InvariantCultureIgnoreCase))
                return new GitHubGitProvider();

            if (CommandBase.IsRunningInGithubAction())
                return new GitHubGitProvider();
            else if (CommandBase.IsRunningInGitlab())
                return new GitlabGitProvider(server);

            return new DefaultGitProvider();
        }
    }
}
