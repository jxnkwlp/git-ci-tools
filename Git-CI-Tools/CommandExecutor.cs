using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Git_CI_Tools.Commands;
using Pastel;
using Semver;

namespace Git_CI_Tools
{
    public class CommandExecutor
    {
        public GitChangesCommandResult GitChanges(GitChangesCommandOptions options)
        {
            Console.Out.WriteLine("Extracting Git commit history command.");

            // init git context
            var git = GitContextHelper.InitProject(options.Project);

            if (git == null)
                return null;

            var tag = GitContextHelper.FindLatestTag(git, options.IncludePrerelease);

            // check branch 
            if (string.IsNullOrEmpty(options.Branch))
                options.Branch = git.GetCurrentBranch()?.Name;

            if (!git.BranchExisting(options.Branch))
            {
                Console.Error.WriteLine($"The branch '{options.Branch}' not found. ".Pastel(Color.Red));
                return null;
            }

            Console.Out.WriteLine($"Current branch: {options.Branch}.".Pastel(Color.Green));

            // find commits 
            var commits = git.GetCommits(options.Branch, fromSha: tag?.Sha).ToList();

            Console.Out.WriteLine($"Find {commits.Count()} commits. ".Pastel(Color.SlateGray));

            var changed = new Dictionary<string, List<GitChangedFile>>();

            if (options.TargetPaths?.Any() == true)
            {
                var paths = options.TargetPaths;

                paths = paths
                    .SelectMany(x => FileHelper.SearchPaths(git.Project, x))
                    .Select(x => x.FullName.Substring(git.Project.Length).Replace("\\", "/"))
                    .ToArray();

                foreach (var path in paths)
                {
                    changed[path] = new List<GitChangedFile>();

                    foreach (var item in commits.SelectMany(x => x.ChangedFiles))
                    {
                        if (item.Path.Contains(path))
                        {
                            changed[path].Add(item);
                        }
                    }
                }
            }
            else
            {
                changed["Changes"] = commits.SelectMany(x => x.ChangedFiles).ToList();
            }

            var result = new GitChangesCommandResult()
            {
                Changes = changed,
            };

            if (options.Format == "json")
            {
                var outputResult = JsonSerializer.Serialize(changed);

                if (!string.IsNullOrEmpty(options.Output))
                    File.WriteAllText(options.Output, outputResult);
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                foreach (var item in changed)
                {
                    sb.AppendLine($"## {item.Key}");
                    sb.AppendLine(string.Join(Environment.NewLine, changed.SelectMany(x => x.Value).Select(x => $"- {x.Status} {x.Path}")));
                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(options.Output))
                    File.WriteAllText(options.Output, sb.ToString());

                Console.WriteLine(sb.ToString().Pastel(Color.SlateGray));
            }

            Console.Out.WriteLine();

            return result;
        }

        public VersionCurrentCommandResult VersionCurrent(VersionCurrentCommandOptions options)
        {
            Console.Out.WriteLine("Show current version command.");

            var git = GitContextHelper.InitProject(options.Project);

            if (git == null)
                return null;

            var tag = GitContextHelper.FindLatestTag(git, options.IncludePrerelease);

            if (tag == null)
                return null;

            if (!GitContextHelper.TryParseTagAsVersion(tag.Name, out var version))
            {
                Console.Error.WriteLine($"The tag '{tag.Name}' can't be parse as version.".Pastel(Color.Yellow));
                return null;
            }

            var currentVersion = version;

            Console.Out.WriteLine($"Current version: {currentVersion} ".Pastel(Color.Green));

            string outputText = currentVersion.ToString();

            if (options.Format == "json")
            {
                outputText = JsonSerializer.Serialize(new
                {
                    currentVersion.Major,
                    currentVersion.Minor,
                    currentVersion.Patch,
                    currentVersion.Prerelease,
                    currentVersion.Build,
                    FullSemVersion = currentVersion.ToString(),
                    VersionInfo = currentVersion.ToString(),
                    ShortVersion = currentVersion.Change(build: ""),
                });
            }
            else if (options.Format == "dotenv")
            {
                string name = options.DotenvVarName;
                if (string.IsNullOrEmpty(name))
                    name = "CURRENT_VERSION";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{name}={currentVersion}");
                sb.AppendLine($"{name}_MINI={currentVersion.Change(build: "")}");
                sb.AppendLine($"{name}_SHORT={currentVersion.Change(build: "")}");
                sb.AppendLine($"{name}_MAJOR={currentVersion.Major}");
                sb.AppendLine($"{name}_MINOR={currentVersion.Minor}");
                sb.AppendLine($"{name}_PATCH={currentVersion.Patch}");
                sb.AppendLine($"{name}_PRERELEASE={currentVersion.Prerelease}");
                sb.AppendLine($"{name}_BUILD={currentVersion.Build}");

                outputText = sb.ToString().Trim();
            }

            if (!string.IsNullOrEmpty(options.Output))
            {
                File.WriteAllText(options.Output, outputText);

                Console.Out.WriteLine($"The result has been written to file '{options.Output}'. ".Pastel(Color.SlateGray));
            }

            Console.Out.WriteLine();

            return new VersionCurrentCommandResult
            {
                Version = currentVersion,
            };
        }

        public VersionNextCommandResult VersionNext(VersionNextCommandOptions options)
        {
            Console.Out.WriteLine("Generate next version from git history and tags.");

            var git = GitContextHelper.InitProject(options.Project);

            if (git == null)
                return null;

            GitTags tag = null;

            SemVersion currentVersion = VersionGenerater.New();

            if (!string.IsNullOrEmpty(options.CurrentVersion))
            {
                currentVersion = VersionGenerater.Parse(options.CurrentVersion);
            }
            else
            {
                tag = GitContextHelper.FindLatestTag(git, options.IncludePrerelease);

                if (tag != null && GitContextHelper.TryParseTagAsVersion(tag.Name, out currentVersion))
                {
                    // 
                    // Console.Out.WriteLine("Current version: " + currentVersion);
                }
                else
                {
                    if (string.IsNullOrEmpty(options.DefaultVersion))
                        options.DefaultVersion = "1.0.0";

                    currentVersion = VersionGenerater.Parse(options.DefaultVersion);
                }
            }

            Console.Out.WriteLine($"⚡ Current version: {currentVersion.ToString().Pastel(Color.Green)} ");

            if (string.IsNullOrEmpty(options.Branch))
                options.Branch = git.GetCurrentBranch()?.Name;

            if (!git.BranchExisting(options.Branch))
            {
                Console.Error.WriteLine($"The branch '{options.Branch}' not found. ".Pastel(Color.Red));
                return null;
            }

            if (tag != null)
                Console.Out.WriteLine($"Reverse version from tag {tag} of branch  '{options.Branch}' ... ".Pastel(Color.SlateGray));
            else
                Console.Out.WriteLine($"Reverse version from branch ... ");

            var resolverVersionResult = GitContextHelper.ResolverVersionFromCommit(
                git,
                currentVersion,
                options.Branch,
                tag?.Sha,
                options.MajorVer,
                options.MinorVer,
                options.PatchVer);

            SemVersion nextVersion = resolverVersionResult.Version;

            // nextVersion = VersionGenerater.Next(nextVersion, prerelease: options.PrereleaseVer ?? "", build: options.BuildVer ?? "");

            if (options.AutoDetectBuildVer && options.BuildVer == null)
            {
                var latestCommit = resolverVersionResult.Commits.FirstOrDefault();
                if (latestCommit != null)
                {
                    nextVersion = VersionGenerater.Next(nextVersion, build: latestCommit.Sha.Substring(0, 8));
                }
                else
                {
                    // use current commit
                    var commit = git.Commits[0];
                    nextVersion = VersionGenerater.Next(nextVersion, build: commit.Sha[..8]);
                }
            }
            else
            {
                nextVersion = VersionGenerater.Next(nextVersion, build: options.BuildVer);
            }

            if (options.AutoDetect)
            {
                if (string.IsNullOrEmpty(currentVersion.Prerelease) && nextVersion == currentVersion)
                {
                    nextVersion = VersionGenerater.Next(nextVersion, patch: true);
                }

                if (!string.IsNullOrEmpty(options.PrereleaseVer))
                {
                    nextVersion = VersionGenerater.Next(nextVersion, prerelease: options.PrereleaseVer);
                }

                // TODO
            }

            if (nextVersion.ToString() == currentVersion.ToString() && options.ForceUpdate)
                nextVersion = VersionGenerater.Next(nextVersion, patch: true);

            string outputText = nextVersion.ToString();

            Console.Out.WriteLine($"✔ The next version: {outputText.Pastel(Color.Green)} ");

            Console.Out.WriteLine($"🚗 Version change : {currentVersion.ToString().Pastel(Color.Azure)}  →  {outputText.Pastel(Color.Green)} ");

            if (options.Format == "json")
            {
                outputText = JsonSerializer.Serialize(new
                {
                    nextVersion.Major,
                    nextVersion.Minor,
                    nextVersion.Patch,
                    nextVersion.Prerelease,
                    nextVersion.Build,
                    FullSemVersion = nextVersion.ToString(),
                    VersionInfo = nextVersion.ToString(),
                    ShortVersionInfo = nextVersion.Change(build: "").ToString()
                });
            }
            else if (options.Format == "dotenv")
            {
                string name = options.DotenvVarName;
                if (string.IsNullOrEmpty(name))
                    name = "NEXT_VERSION";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{name}={nextVersion}");
                sb.AppendLine($"{name}_MINI={nextVersion.Change(build: "")}");
                sb.AppendLine($"{name}_SHORT={nextVersion.Change(build: "")}");
                sb.AppendLine($"{name}_MAJOR={nextVersion.Major}");
                sb.AppendLine($"{name}_MINOR={nextVersion.Minor}");
                sb.AppendLine($"{name}_PATCH={nextVersion.Patch}");
                sb.AppendLine($"{name}_PRERELEASE={nextVersion.Prerelease}");
                sb.AppendLine($"{name}_BUILD={nextVersion.Build}");

                outputText = sb.ToString().Trim();
            }
            else
            {
                //Console.Out.WriteLine($"The next version result: {Environment.NewLine}{outputText}. ");
            }

            if (!string.IsNullOrEmpty(options.Output))
            {
                File.WriteAllText(options.Output, outputText);

                Console.Out.WriteLine($"The result has been written to file '{options.Output}'. ".Pastel(Color.SlateGray));
            }

            Console.Out.WriteLine();

            return new VersionNextCommandResult
            {
                Version = nextVersion,
            };
        }

        public VersionUpdateCommandResult VersionUpdate(VersionUpdateCommandOptions options)
        {
            return new VersionUpdateCommandResult();
        }

        public void ReleaseChanges(ReleaseChangesCommandOption options)
        {
            Console.Out.WriteLine("Generate git changes.");

            if (string.Equals(options.Provider, "gitlab", StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty(options.ServerUrl))
            {
                Console.Error.WriteLine($"No server url provider.".Pastel(Color.Red));
                return;
            }

            IGitProvider gitProvider = GitProviderFactory.Create(options.Provider, options.ServerUrl);

            // init git context
            var git = GitContextHelper.InitProject(options.Project);

            if (git == null)
                return;

            var tag = GitContextHelper.FindLatestTag(git, options.IncludePrerelease);

            // check branch 
            if (string.IsNullOrEmpty(options.Branch))
                options.Branch = git.GetCurrentBranch()?.Name;

            if (!git.BranchExisting(options.Branch))
            {
                Console.Error.WriteLine($"The branch '{options.Branch}' not found. ".Pastel(Color.Red));
                return;
            }

            Console.Out.WriteLine($"Current branch: {options.Branch}. ".Pastel(Color.Green));

            // find commits 
            var commits = git.GetCommits(options.Branch, fromSha: tag?.Sha).ToList();

            Console.Out.WriteLine($"Find {commits.Count()} commits. ".Pastel(Color.SlateGray));
            // Console.Out.WriteLine(JsonSerializer.Serialize(commits).Pastel(Color.SlateGray));

            string[] groupbyPaths = null;
            if (options.TargetPaths?.Any() == true)
            {
                groupbyPaths = options.TargetPaths
                  .SelectMany(x => FileHelper.SearchPaths(git.Project, x))
                  .Select(x => x.FullName.Substring(git.Project.Length).Replace("\\", "/"))
                  .ToArray();
            }

            // generate changes			
            string notes = ReleaseConfigHelper.GenerateChanges(git.Project, commits, groupbyPaths, gitProvider);

            // output 
            if (!string.IsNullOrEmpty(options.Output))
            {
                File.WriteAllText(options.Output, notes);

                Console.Out.WriteLine($"The result has been written to file '{options.Output}'. ".Pastel(Color.SlateGray));
            }
            else
            {
                Console.Out.WriteLine($"Release notes generated. {Environment.NewLine}. ".Pastel(Color.SlateGray));
                Console.Out.WriteLine(notes);
            }

            Console.Out.WriteLine();
        }

    }
}
