
using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace Git_CI_Tools
{
	public class GitContext
	{
		private readonly string _path;
		private readonly Repository _repository;

		public string Project => _repository.Info.WorkingDirectory; //new DirectoryInfo(_path).Parent.FullName;
		public string Git => _path;

		public GitContext(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentException($"'{nameof(path)}' cannot be null", nameof(path));
			}

			if (Repository.IsValid(path))
				_path = path;
			else
			{
				string dir = Repository.Discover(path);

				if (!string.IsNullOrEmpty(dir))
					_path = dir;
			}

			if (!string.IsNullOrEmpty(_path))
				_repository = new Repository(_path);

		}

		public bool IsValid()
		{
			return _repository != null;
		}

		// public string HeadSha => _repository.Commits.First().Sha;

		public IReadOnlyList<GitTags> GetTags()
		{
			return _repository.Tags.Select(x => new GitTags()
			{
				Name = x.FriendlyName,
				Message = ((Commit)x.Target).Message,
				Sha = x.Target.Sha,
			}).ToList();
		}

		public GitBranch GetCurrentBranch()
		{
			var branch = _repository.Head;

			return new GitBranch()
			{
				Name = branch.FriendlyName,
				Sha = branch.Tip.Sha,
			};
		}

		public IReadOnlyList<GitBranch> GetBranchs()
		{
			return _repository.Branches.Select(x => new GitBranch()
			{
				Name = x.FriendlyName,
				Sha = x.Tip.Sha,
			}).ToList();
		}

		public bool BranchExisting(string name)
		{
			return _repository.Branches.Any(x => x.FriendlyName == name);
		}

		public IReadOnlyList<GitCommit> GetCommits(string branch = null, string fromSha = null)
		{
			ICommitLog commits = _repository.Commits;

			if (!string.IsNullOrWhiteSpace(branch))
				commits = _repository.Branches[branch].Commits;

			Commit fromCommit = null;

			if (fromSha != null)
			{
				var id = _repository.Lookup(fromSha);
				if (id != null && id is Commit c)
					fromCommit = c; // _repository.Commits.First(x => x.Sha == fromSha);
			}

			var fromDate = fromCommit?.Committer.When;

			var query = commits;

			List<Commit> resultCommits = null;

			if (fromDate.HasValue)
				resultCommits = query.Where(x => x.Committer.When >= fromDate.Value).ToList();
			else
				resultCommits = query.ToList();

			return resultCommits.Select(x => new GitCommit()
			{
				Date = x.Committer.When,
				Message = x.Message,
				MessageShort = x.MessageShort,
				Sha = x.Sha,
				Author = new GitAuthor()
				{
					Email = x.Committer.Email,
					Name = x.Committer.Name,
				},
			}).ToList();
		}
	}

	public class GitTags
	{
		public string Name { get; set; }
		public string Message { get; set; }
		public string Sha { get; set; }
		public string Time { get; set; }

		public override string ToString()
		{
			return $"{Name} => {Sha}";
		}
	}

	public class GitCommit
	{
		public string Message { get; set; }
		public string MessageShort { get; set; }
		public string Sha { get; set; }
		public DateTimeOffset Date { get; set; }
		public GitAuthor Author { get; set; }

		public override string ToString()
		{
			return $"{Sha} {MessageShort} {Date} {Author}";
		}
	}

	public class GitAuthor : IEquatable<GitAuthor>
	{
		public string Name { get; set; }
		public string Email { get; set; }

		public override bool Equals(object obj)
		{
			return Equals(obj as GitAuthor);
		}

		public bool Equals(GitAuthor other)
		{
			return other != null &&
				   Name == other.Name &&
				   Email == other.Email;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, Email);
		}

		public override string ToString()
		{
			return $"{Name}[{Email}]";
		}

		public static bool operator ==(GitAuthor left, GitAuthor right)
		{
			return EqualityComparer<GitAuthor>.Default.Equals(left, right);
		}

		public static bool operator !=(GitAuthor left, GitAuthor right)
		{
			return !(left == right);
		}
	}

	public class GitBranch
	{
		public string Name { get; set; }
		public string Sha { get; set; }

		public override string ToString()
		{
			return $"{Name} => {Sha}";
		}
	}
}
