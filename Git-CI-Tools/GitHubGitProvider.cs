namespace Git_CI_Tools
{
	public class GitHubGitProvider : IGitProvider
	{
		public string UserLink(string name, string email)
		{
			return $"[{name}](https://github.com/{name.Replace(" ", "%20")})";
		}
	}
}
