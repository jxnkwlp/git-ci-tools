using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

namespace Git_CI_Tools;

public abstract class CommandBase
{
    public abstract Command GetCommand();

    public static bool IsRunningInGithubAction()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
    }

    public static bool IsRunningInGitlab()
    {
        return Environment.GetEnvironmentVariable("CI_SERVER")?.Equals("yes", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    protected void WriteVariablesToGithubActionEnvironment(Dictionary<string, string> variables)
    {
        var gitHubSetEnvFilePath = Environment.GetEnvironmentVariable("GITHUB_ENV");

        if (gitHubSetEnvFilePath != null)
        {
            using var streamWriter = File.AppendText(gitHubSetEnvFilePath);
            foreach (var variable in variables)
            {
                if (!string.IsNullOrEmpty(variable.Value))
                {
                    streamWriter.WriteLine($"{variable.Key}={variable.Value}");
                }
            }
        }
        else
        {
            // TODO
        }
    }
}
