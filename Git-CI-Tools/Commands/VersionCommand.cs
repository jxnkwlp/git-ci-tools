using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Semver;

namespace Git_CI_Tools.Commands
{
    public class VersionCommand : CommandBase
    {
        public override Command GetCommand()
        {
            var command = new Command("version");

            command.AddCommand(VersionCurrentCommand());
            command.AddCommand(VersionNextCommand());

            return command;
        }

        private Command VersionCurrentCommand()
        {
            var command = new Command("current", "Show current version");

            command.AddOption(new Option<string>(new string[] { "--format" }, () => "text", "Output format: json/dotenv/text"));
            command.AddOption(new Option<string>(new string[] { "--output", "-o" }, "Output results to the specified file"));
            command.AddOption(new Option<string>("dotenv-var-name"));

            command.Handler = CommandHandler.Create<VersionCurrentCommandOptions>((options) =>
            {
                var result = new CommandExecutor().VersionCurrent(options);

                if (result != null && IsRunningInGithubAction())
                {
                    WriteVariablesToGithubActionEnvironment(new Dictionary<string, string>() {
                        { "GITCI_CURRENT_VERSION_MINOR", result.Version.Minor.ToString() },
                        { "GITCI_CURRENT_VERSION_MAJOR", result.Version.Major.ToString() },
                        { "GITCI_CURRENT_VERSION_PATCH", result.Version.Patch.ToString() },
                        { "GITCI_CURRENT_VERSION_BUILD", result.Version.Build },
                        { "GITCI_CURRENT_VERSION_PRERELEASE", result.Version.Prerelease },
                        { "GITCI_CURRENT_VERSION", result.Version.ToString() },
                    });
                }
            });

            return command;
        }

        private Command VersionNextCommand()
        {
            var command = new Command("next", "Generate next version");

            command.AddOption(new Option<bool>("--debug-mode", () => false) { IsHidden = true });

            command.AddOption(new Option<string>("--default-version", () => "1.0.0", "Default version"));
            command.AddOption(new Option<string>("--current-version", "Set the current version and not detect from tags"));

            command.AddOption(new Option<bool?>("--major-ver", "Whether incremental major version"));
            command.AddOption(new Option<bool?>("--minor-ver", "Whether incremental minor version"));
            command.AddOption(new Option<bool?>("--patch-ver", "Whether incremental patch version"));
            command.AddOption(new Option<string>("--prerelease-ver", "Set the prerelease version number"));
            command.AddOption(new Option<string>("--build-ver", "Set the build version number"));
            command.AddOption(new Option<bool>("--auto-detect", () => true, "Auto detect "));
            command.AddOption(new Option<bool>("--auto-detect-build-ver", () => true, "Auto detect prerelease build version. Default is git commit short sha number"));

            command.AddOption(new Option<bool>("--force-update", () => true, "Force generation of the next version number"));

            command.AddOption(new Option<string>(new string[] { "--format" }, () => "text", "Output format: json/dotenv/text"));
            command.AddOption(new Option<string>(new string[] { "--output", "-o" }, "Output results to the specified file"));

            command.AddOption(new Option<string>("dotenv-var-name"));

            command.Handler = CommandHandler.Create<VersionNextCommandOptions>(options =>
            {
                var result = new CommandExecutor().VersionNext(options);

                if (result != null && IsRunningInGithubAction())
                {
                    WriteVariablesToGithubActionEnvironment(new Dictionary<string, string>() {
                        { "GITCI_NEXT_VERSION_MINOR", result.Version.Minor.ToString() },
                        { "GITCI_NEXT_VERSION_MAJOR", result.Version.Major.ToString() },
                        { "GITCI_NEXT_VERSION_PATCH", result.Version.Patch.ToString() },
                        { "GITCI_NEXT_VERSION_BUILD", result.Version.Build },
                        { "GITCI_NEXT_VERSION_PRERELEASE", result.Version.Prerelease },
                        { "GITCI_NEXT_VERSION", result.Version.ToString() },
                        { "GITCI_NEXT_NUGET_VERSION", result.Version.ToString() },
                    });
                }
            });

            return command;
        }

        //private Command VersionUpdateCommand()
        //{
        //    var command = new Command("update", "Update version number to project file")
        //    {
        //        Handler = CommandHandler.Create<VersionUpdateCommandOptions>(options =>
        //        {
        //            new CommandExecutor().VersionUpdate(options);
        //        })
        //    };

        //    command.AddOption(new Option<bool>("--update-assembly-info").AddSuggestions("ass", "update"));
        //    command.AddOption(new Option<bool>("--update-version-file").AddSuggestions("ver", "update"));
        //    // command.AddOption(new Option<string[]>("--target-paths"));
        //    command.AddOption(new Option<FileInfo>("--target-json").ExistingOnly().AddSuggestions("target", "json"));

        //    return command;
        //}

        private static void DebugWrite(bool isDebug, Func<string> outputFunc)
        {
            if (isDebug && outputFunc != null)
                Console.WriteLine(Environment.NewLine + outputFunc());
        }
    }


    public class VersionCurrentCommandOptions : CommandOptionBase
    {
        public string DotenvVarName { get; set; }
    }

    public class VersionCurrentCommandResult
    {
        public SemVersion Version { get; set; }

        public Dictionary<string, SemVersion> TargetVersion { get; set; }

    }

    public class VersionNextCommandOptions : CommandOptionBase
    {
        //public string Provider { get; set; }
        //public Uri Url { get; set; }
        //public string Token { get; set; }

        public string DefaultVersion { get; set; }
        public string CurrentVersion { get; set; }

        public bool? MajorVer { get; set; }
        public bool? MinorVer { get; set; }
        public bool? PatchVer { get; set; }
        public string PrereleaseVer { get; set; }
        public string BuildVer { get; set; }

        public bool AutoDetect { get; set; }

        public bool AutoDetectBuildVer { get; set; }

        //public bool IgnoreConfig { get; set; }

        public bool ForceUpdate { get; set; }

        public string DotenvVarName { get; set; }

        public string[] TargetPaths { get; set; }
    }

    public class VersionNextCommandResult
    {
        public SemVersion Version { get; set; }

        public Dictionary<string, SemVersion> ProjectVersion { get; set; }
    }

    public class VersionUpdateCommandOptions
    {
        public bool UpdateAssemblyInfo { get; set; }
        public bool UpdateVersionFile { get; set; }
        //public string[] TargetPaths { get; set; }
        public FileInfo TargetJson { get; set; }
    }

    public class VersionUpdateCommandResult { }

}
