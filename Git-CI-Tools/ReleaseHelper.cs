﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Git_CI_Tools
{
	public static class ReleaseHelper
	{
		private static ReleaseConfigOptions _userConfigOptions = null;

		public static bool IsMajor(string root, IEnumerable<GitCommit> commits)
		{
			var options = ResolveConfig(root);

			if (options.VersionResolver?.Major == null)
				return false;

			if (options.VersionResolver.Major.ContainsKey("commits"))
				foreach (var key in options.VersionResolver.Major["commits"])
				{
					if (commits.Any(x => x.Message.Contains(key)))
						return true;
				}
			// TODO labels

			if (options.VersionResolver.Default == "major")
				return true;

			return false;
		}

		public static bool IsMinor(string root, IEnumerable<GitCommit> commits)
		{
			var options = ResolveConfig(root);

			if (options.VersionResolver?.Minor == null)
				return false;

			if (options.VersionResolver.Minor.ContainsKey("commits"))
				foreach (var key in options.VersionResolver.Minor["commits"])
				{
					if (commits.Any(x => x.Message.Contains(key)))
						return true;
				}
			// TODO labels

			if (options.VersionResolver.Default == "minor")
				return true;

			return false;
		}

		public static bool IsPatch(string root, IEnumerable<GitCommit> commits)
		{
			var options = ResolveConfig(root);

			if (options.VersionResolver?.Patch == null)
				return false;

			if (options.VersionResolver.Patch.ContainsKey("commits"))
				foreach (var key in options.VersionResolver.Patch["commits"])
				{
					if (commits.Any(x => x.Message.Contains(key)))
						return true;
				}
			// TODO labels

			if (options.VersionResolver.Default == "patch")
				return true;

			return false;
		}


		public static string GenerateNotes(string root, IEnumerable<GitCommit> commits, IGitProvider gitProvider)
		{
			var options = ResolveConfig(root);

			string notes = string.IsNullOrEmpty(options.Template) ?
				@"## Changes
Contributors: $CONTRIBUTORS

$CHANGES" : options.Template;

			StringBuilder sb = new StringBuilder();

			if (options.Categories == null)
			{
				commits.ToList().ForEach(commit =>
				{
					sb.AppendLine($"* {commit.Sha} {commit.MessageShort.Trim()} (by {gitProvider.UserLink(commit.Author.Name, commit.Author.Email)})");
				});
				sb.AppendLine(Environment.NewLine);
			}
			else
			{
				var otherCommits = new List<GitCommit>();
				var cateCommits = new Dictionary<string, List<GitCommit>>();

				foreach (var cate in options.Categories)
				{
					if (cate.Commits?.Any() == true)
					{
						var list = commits.Where(x => cate.Commits.All(s => x.Message.Contains(s))).ToList();

						cateCommits[cate.Title] = list;
					}
					// TOOD labels
				}

				otherCommits = commits.Where(x => cateCommits.SelectMany(v => v.Value).All(a => a.Sha != x.Sha)).ToList();

				sb.AppendLine("");

				foreach (var item in cateCommits)
				{
					if (item.Value?.Any() == false)
					{
						continue;
					}

					sb.AppendLine($"### {item.Key}");
					item.Value.ForEach(commit =>
					{
						sb.AppendLine($"* {commit.Sha} {commit.MessageShort.Trim()} (by {gitProvider.UserLink(commit.Author.Name, commit.Author.Email)})");
					});

					sb.AppendLine("");
				}

				if (otherCommits.Any())
				{
					sb.AppendLine($"### Other changes");
					otherCommits.ForEach(commit =>
					{
						sb.AppendLine($"* {commit.Sha} {commit.MessageShort.Trim()} (by {gitProvider.UserLink(commit.Author.Name, commit.Author.Email)})");
					});

					//sb.AppendLine(Environment.NewLine);
				}
			}

			string contributors = string.Join(", ", commits.Where(x => x.Author != null).Select(x => x.Author).Distinct().Select(x => gitProvider.UserLink(x.Name, x.Email)));

			notes = notes.Replace("$CONTRIBUTORS", contributors);
			notes = notes.Replace("$CHANGES", sb.ToString());

			return notes;
		}


		public static ReleaseConfigOptions ResolveConfig(string dir)
		{
			if (_userConfigOptions != null)
				return _userConfigOptions;

			var file = Path.Combine(dir, ".gitci", "release-config.yml");

			ReleaseConfigOptions options = new ReleaseConfigOptions();

			if (File.Exists(file))
			{
				var deserializer = new DeserializerBuilder()
					.WithNamingConvention(CamelCaseNamingConvention.Instance)
					.Build();
				options = deserializer.Deserialize<ReleaseConfigOptions>(File.ReadAllText(file));
			}

			_userConfigOptions = options ??= new ReleaseConfigOptions();

			return options;
		}
	}

	public class ReleaseConfigOptions
	{
		[YamlMember(Alias = "version-resolver", ApplyNamingConventions = false)]
		public VersionResolverConfig VersionResolver { get; set; }

		public IEnumerable<ReleaseNoteCategory> Categories { get; set; }

		public string Template { get; set; }


		public ReleaseConfigOptions()
		{
			VersionResolver = new VersionResolverConfig();
		}

		public class VersionResolverConfig
		{
			public Dictionary<string, string[]> Major { get; set; }
			public Dictionary<string, string[]> Minor { get; set; }
			public Dictionary<string, string[]> Patch { get; set; }
			public string Default { get; set; } = "patch";
		}

		public class ReleaseNoteCategory
		{
			public string Title { get; set; }
			public string[] Labels { get; set; }
			public string[] Commits { get; set; }
		}

	}
}
