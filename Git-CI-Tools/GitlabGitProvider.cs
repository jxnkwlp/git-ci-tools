namespace Git_CI_Tools;

public class GitlabGitProvider : IGitProvider
{
    private readonly string _server;

    public GitlabGitProvider(string server)
    {
        _server = server;
    }

    public string UserLink(string name, string email)
    {
        string url = _server.TrimEnd('/') + "/" + name;
        return $"[{name}]({url.Replace(" ", "%20")})";
    }
}
