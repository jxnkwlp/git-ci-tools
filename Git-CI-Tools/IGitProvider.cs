namespace Git_CI_Tools
{
	public interface IGitProvider
	{
		string UserLink(string name, string email);
	}

	public class DefaultGitProvider : IGitProvider
	{
		public string UserLink(string name, string email)
		{
			return name;
		}
	}
}
