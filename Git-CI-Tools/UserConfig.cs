using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Git_CI_Tools
{
	public class UserConfig
	{
		public static bool IsMajor(string root, IEnumerable<GitCommit> commits)
		{
			var options = ResolveConfig(root);

			if (options.VersionResolver?.Major == null)
				return false;

			if (options.VersionResolver.Major.ContainsKey("commit"))
				foreach (var key in options.VersionResolver.Major["commit"])
				{
					if (commits.Any(x => x.Message.Contains(key) || x.MessageShort.Contains(key)))
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

			if (options.VersionResolver.Minor.ContainsKey("commit"))
				foreach (var key in options.VersionResolver.Minor["commit"])
				{
					if (commits.Any(x => x.Message.Contains(key) || x.MessageShort.Contains(key)))
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

			if (options.VersionResolver.Patch.ContainsKey("commit"))
				foreach (var key in options.VersionResolver.Patch["commit"])
				{
					if (commits.Any(x => x.Message.Contains(key) || x.MessageShort.Contains(key)))
						return true;
				}
			// TODO labels

			if (options.VersionResolver.Default == "patch")
				return true;

			return false;
		}

		public static UserConfigOptions ResolveConfig(string dir)
		{
			var file = Path.Combine(dir, ".gitci", "release-config.yml");

			UserConfigOptions options = new UserConfigOptions();

			if (File.Exists(file))
			{
				var deserializer = new DeserializerBuilder()
					.WithNamingConvention(CamelCaseNamingConvention.Instance)
					.Build();
				options = deserializer.Deserialize<UserConfigOptions>(File.ReadAllText(file));
			}

			options ??= new UserConfigOptions();
			return options;
		}
	}

	public class UserConfigOptions
	{
		[YamlMember(Alias = "version-resolver", ApplyNamingConventions = false)]
		public VersionResolverConfig VersionResolver { get; set; }

		public UserConfigOptions()
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

	}
}
