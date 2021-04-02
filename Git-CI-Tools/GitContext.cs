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
			_path = path;

			string dir = Repository.Discover(path);

			if (!string.IsNullOrEmpty(dir))
				_path = dir;

			_repository = new Repository(_path);
		}

		public bool IsValid()
		{
			return Repository.IsValid(_path);
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

		public IReadOnlyList<GitCommit> GetCommits(string fromSha = null)
		{
			Commit fromCommit = null;
			if (fromSha != null)
			{
				fromCommit = _repository.Commits.FirstOrDefault(x => x.Sha == fromSha);
			}

			var fromDate = fromCommit?.Committer.When;

			var query = _repository.Commits;

			if (fromDate.HasValue)
				return query.SkipWhile(x => x.Committer.When <= fromDate.Value).Select(x => new GitCommit()
				{
					Date = x.Committer.When,
					Message = x.Message,
					MessageShort = x.MessageShort,
					Sha = x.Sha,
				}).ToList();
			else
				return query.Select(x => new GitCommit()
				{
					Date = x.Committer.When,
					Message = x.Message,
					MessageShort = x.MessageShort,
					Sha = x.Sha,
				}).ToList();
		}
	}

	public class GitTags
	{
		public string Name { get; set; }
		public string Message { get; set; }
		public string Sha { get; set; }
		public string Time { get; set; }
	}

	public class GitCommit
	{
		public string Message { get; set; }
		public string MessageShort { get; set; }
		public string Sha { get; set; }
		public DateTimeOffset Date { get; set; }
	}
}
